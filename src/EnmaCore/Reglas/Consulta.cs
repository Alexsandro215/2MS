using Enma.Core.Estado;

namespace Enma.Core.Reglas;

/// <summary>Desde donde se empieza a seleccionar.</summary>
public enum Ambito
{
    /// <summary>El portador de la regla.</summary>
    Yo,
    /// <summary>El objetivo de la accion en curso.</summary>
    Objetivo,
    /// <summary>Compañeros del portador, sin incluirlo.</summary>
    Aliados,
    /// <summary>Compañeros del portador, incluyendolo.</summary>
    AliadosConmigo,
    /// <summary>El bando contrario al portador.</summary>
    Enemigos,
    Todos
}

/// <summary>Criterio de ordenacion, para "el que mas X" y "el que menos X".</summary>
public enum Orden { Ninguno, MayorStat, MenorStat, MayorHpPct, MenorHpPct, Azar }

/// <summary>Como se agrega un conjunto a un numero.</summary>
public enum Agregado { Conteo, Suma, Maximo, Minimo, Media, Primero }

/// <summary>
/// Filtro sobre combatientes. Todos los campos son opcionales y se combinan
/// con Y logico. Los casos que no cubra se resuelven con <see cref="Condicion"/>.
/// </summary>
public sealed record Filtro
{
    public Casilla? EnCasilla { get; init; }
    public bool? TieneElemento { get; init; }
    public bool? TieneElementoL2 { get; init; }
    public string? TieneFlag { get; init; }
    public string? SinFlag { get; init; }
    public bool? EnKo { get; init; }
    public string? EsPersonaje { get; init; }
    public bool? PisaTerrenoPropio { get; init; }

    public bool Acepta(Combatiente c, EstadoCombate e, Combatiente portador)
    {
        if (EnCasilla.HasValue && c.Posicion != EnCasilla.Value) return false;
        if (TieneElemento.HasValue && c.TieneElemento != TieneElemento.Value) return false;
        if (TieneElementoL2.HasValue && c.TieneElementoL2 != TieneElementoL2.Value) return false;
        if (TieneFlag is not null && !c.TieneFlag(TieneFlag)) return false;
        if (SinFlag is not null && c.TieneFlag(SinFlag)) return false;
        if (EnKo.HasValue && c.EnKo != EnKo.Value) return false;
        if (EsPersonaje is not null && c.Id != EsPersonaje) return false;
        if (PisaTerrenoPropio == true)
        {
            var t = e.TerrenoDe(c);
            if (t is null || t.BandoDeLaLinea == c.Bando) return false;
        }
        return true;
    }
}

/// <summary>
/// Selecciona un conjunto de combatientes. Es la pieza que permite expresar
/// "n enemigos con E.E.", "compañeros en NEUTRAL" o "el enemigo con mas HP%"
/// sin escribir codigo por cada caso.
/// </summary>
public sealed record Selector(
    Ambito Ambito = Ambito.Yo,
    Filtro? Filtro = null,
    Orden Orden = Orden.Ninguno,
    Stat OrdenPor = Stat.Hp,
    int? Limite = null)
{
    public static readonly Selector Yo = new(Ambito.Yo);
    public static readonly Selector Objetivo = new(Ambito.Objetivo);

    public List<Combatiente> Resolver(EstadoCombate e, Contexto ctx)
    {
        var portador = ctx.Portador;

        // El banquillo no esta en el combate, y a un escondido no lo alcanza
        // el bando contrario: ni objetivo, ni area. Sus compañeros si, que es
        // lo que permite curarlo ahi dentro. Ver tablero.md  5.
        bool Alcanzable(Combatiente c) => c.Bando == portador?.Bando || c.Zona == Zona.Front;

        IEnumerable<Combatiente> conjunto = Ambito switch
        {
            Ambito.Yo => portador is null ? [] : [portador],
            Ambito.Objetivo => ctx.Objetivo is null ? [] : [ctx.Objetivo],
            Ambito.Aliados => e.EnCampo.Where(c => c.Bando == portador?.Bando && c.Id != portador?.Id),
            Ambito.AliadosConmigo => e.EnCampo.Where(c => c.Bando == portador?.Bando),
            Ambito.Enemigos => e.EnCampo.Where(c => c.Bando != portador?.Bando && Alcanzable(c)),
            Ambito.Todos => e.EnCampo.Where(Alcanzable),
            _ => []
        };

        if (Filtro is not null && portador is not null)
            conjunto = conjunto.Where(c => Filtro.Acepta(c, e, portador));

        var lista = conjunto.ToList();

        switch (Orden)
        {
            case Orden.MayorStat: lista.Sort((a, b) => b.BaseRef(OrdenPor).CompareTo(a.BaseRef(OrdenPor))); break;
            case Orden.MenorStat: lista.Sort((a, b) => a.BaseRef(OrdenPor).CompareTo(b.BaseRef(OrdenPor))); break;
            case Orden.MayorHpPct: lista.Sort((a, b) => b.HpPct.CompareTo(a.HpPct)); break;
            case Orden.MenorHpPct: lista.Sort((a, b) => a.HpPct.CompareTo(b.HpPct)); break;
            case Orden.Azar: Barajar(lista, e.Rng); break;
        }

        if (Limite.HasValue && lista.Count > Limite.Value)
            lista = lista.GetRange(0, Limite.Value);

        return lista;
    }

    private static void Barajar(List<Combatiente> lista, Combate.IAleatorio rng)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = (int)(rng.Siguiente() * (i + 1));
            if (j > i) j = i;
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}

/// <summary>
/// Contexto de evaluacion: quien lleva la regla, sobre quien se actua y que
/// evento la disparo. El evaluador lo construye; las reglas solo lo leen.
/// </summary>
public sealed record Contexto(
    Combatiente? Portador = null,
    Combatiente? Objetivo = null,
    Evento? Evento = null)
{
    /// <summary>
    /// La barra de CM del equipo de quien lleva la regla. Una regla sin
    /// portador —un emblema, el entorno— mira la del jugador.
    /// </summary>
    public BarraCm BarraDelPortador(EstadoCombate e) => e.Barra(Portador?.Bando ?? Bando.Aliado);

    public string NombreDelEquipo() => Portador?.Bando == Bando.Enemigo ? "enemigos" : "equipo";
}

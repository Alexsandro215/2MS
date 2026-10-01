using Enma.Core.Estado;

namespace Enma.Core.Reglas;

/// <summary>
/// Reglas que alteran la ESTRUCTURA del turno, no un valor.
///
/// Van aparte a proposito. Ver arquitectura.md  2:
///
///   "Si se meten a la fuerza en el esquema de modificadores, ese esquema se
///    pudre. Mejor dos categorias limpias que una que lo abarca todo mal."
///
/// Son pocas y explicitas, y cada una es un caso real en la maquina de estados.
/// Esa es la parte cara, y por eso el catalogo esta cerrado.
/// </summary>
public enum TipoFlujo
{
    /// <summary>Artens W: el doble de habilidades o basicos este turno.</summary>
    DoblarAcciones,

    /// <summary>Rudentlein R: las habilidades se aplican dos veces.</summary>
    RepetirHabilidad,

    /// <summary>Winchesster E: los spells aplican dos veces el efecto elemental.</summary>
    RepetirEfectoElemental,

    /// <summary>Lein-Xein R: un basico que se repite hasta que falles.</summary>
    RepetirBasicoHastaFallar,

    /// <summary>Jaens Jeans R: invencible N turnos y no puedes hacer nada.</summary>
    InvencibleSinActuar,

    /// <summary>Korkakov, Madrerick: el objetivo no puede cambiar de casilla.</summary>
    Anclar,

    /// <summary>Ordeus Q: te vuelves inmovil por decision propia.</summary>
    Inmovilizar,

    /// <summary>Ultimatum, Kawamato, Winchesster, Setto: aplaza la resolucion N turnos.</summary>
    Preparacion,

    /// <summary>Drimur, Setto: mueve a otro de casilla en contra de su voluntad.</summary>
    MovimientoForzado,

    /// <summary>Artens R: fija un objetivo y no se puede cambiar durante la accion.</summary>
    FijarObjetivo,

    /// <summary>
    /// Quien la lleva puede, DESDE EL BANQUILLO, entrar a relevar a un
    /// compañero en K.O. sin que nadie del front gaste su turno en ello.
    /// Es una pasiva: sin ella, sacar a un caido cuesta el turno de alguien.
    /// </summary>
    RelevoDesdeBanquillo
}

/// <summary>
/// Una alteracion del flujo del turno. A diferencia de <see cref="Regla"/>,
/// esto no produce modificadores: cambia como corre la maquina de estados.
/// </summary>
public sealed record ReglaFlujo
{
    public required string Id { get; init; }
    public required TipoFlujo Tipo { get; init; }
    public required Fuente Fuente { get; init; }

    public Condicion? Condicion { get; init; }
    public Selector Alcance { get; init; } = Selector.Yo;

    /// <summary>
    /// Cuantos turnos dura. 0 = solo la accion en curso. Negativo = todo el
    /// combate, que es lo que necesita una pasiva.
    /// </summary>
    public int Turnos { get; init; }

    /// <summary>Para Preparacion: en cuantos turnos se resuelve.</summary>
    public int TurnosDePreparacion { get; init; }

    /// <summary>Para Preparacion: que pasa cuando vence.</summary>
    public Efecto? EfectoAlVencer { get; init; }

    /// <summary>Para Doblar y Repetir: cuantas veces.</summary>
    public int Veces { get; init; } = 2;

    public string Etiqueta { get; init; } = "";
}

/// <summary>Una preparacion en curso, esperando a vencer.</summary>
public sealed class PreparacionEnCurso
{
    public required string ReglaId { get; init; }
    public required string PortadorId { get; init; }
    public string? ObjetivoId { get; init; }
    public required int TurnosRestantes { get; set; }
    public required Efecto EfectoAlVencer { get; init; }
}

/// <summary>
/// Lleva la cuenta de las alteraciones de flujo vigentes.
///
/// El bucle de combate consulta esto ANTES de resolver una accion, para saber
/// cuantas acciones quedan, si el personaje puede actuar y si algo se repite.
/// </summary>
public sealed class EstadoFlujo
{
    public List<ReglaFlujo> Activas { get; } = new();
    public List<PreparacionEnCurso> Preparaciones { get; } = new();

    /// <summary>Acciones disponibles este turno, ya contando las que doblan.</summary>
    public int AccionesDisponibles(EstadoCombate e, Combatiente quien, int baseAcciones = 1)
    {
        int n = baseAcciones;
        foreach (var f in Activas.Where(f => f.Tipo == TipoFlujo.DoblarAcciones))
        {
            var ctx = new Contexto(quien, quien, null);
            if (f.Condicion is not null && !f.Condicion.Cumple(e, ctx)) continue;
            if (f.Alcance.Resolver(e, ctx).Exists(c => c.Id == quien.Id))
                n *= f.Veces;
        }
        return n;
    }

    /// <summary>
    /// Jaens Jeans R vuelve invencible pero impide actuar. Sin esto, el bucle
    /// de combate le pediria una accion a alguien que no puede darla.
    /// </summary>
    public bool PuedeActuar(EstadoCombate e, Combatiente quien)
    {
        if (quien.EnKo) return false;
        foreach (var f in Activas.Where(f => f.Tipo == TipoFlujo.InvencibleSinActuar))
        {
            var ctx = new Contexto(quien, quien, null);
            if (f.Alcance.Resolver(e, ctx).Exists(c => c.Id == quien.Id)) return false;
        }
        return true;
    }

    /// <summary>true si una pasiva le deja relevar a un caido desde el banquillo.</summary>
    public bool PuedeRelevarDesdeBanquillo(EstadoCombate e, Combatiente quien)
    {
        foreach (var f in Activas.Where(f => f.Tipo == TipoFlujo.RelevoDesdeBanquillo))
        {
            var ctx = new Contexto(quien, quien, null);
            if (f.Condicion is not null && !f.Condicion.Cumple(e, ctx)) continue;
            if (f.Alcance.Resolver(e, ctx).Exists(c => c.Id == quien.Id)) return true;
        }
        return false;
    }

    /// <summary>Cuantas veces se aplica una habilidad. Rudentlein R la dobla.</summary>
    public int RepeticionesDeHabilidad(EstadoCombate e, Combatiente quien)
    {
        int n = 1;
        foreach (var f in Activas.Where(f => f.Tipo == TipoFlujo.RepetirHabilidad))
        {
            var ctx = new Contexto(quien, quien, null);
            if (f.Condicion is not null && !f.Condicion.Cumple(e, ctx)) continue;
            if (f.Alcance.Resolver(e, ctx).Exists(c => c.Id == quien.Id))
                n *= f.Veces;
        }
        return n;
    }

    /// <summary>Registra una preparacion que se resolvera en N turnos.</summary>
    public void Preparar(ReglaFlujo regla, Combatiente portador, Combatiente? objetivo)
    {
        if (regla.EfectoAlVencer is null) return;
        Preparaciones.Add(new PreparacionEnCurso
        {
            ReglaId = regla.Id,
            PortadorId = portador.Id,
            ObjetivoId = objetivo?.Id,
            TurnosRestantes = regla.TurnosDePreparacion,
            EfectoAlVencer = regla.EfectoAlVencer
        });
    }

    /// <summary>
    /// Envejece las preparaciones y resuelve las que vencen.
    /// La stat de Preparacion puede restar un turno al llegar a su umbral.
    /// </summary>
    public List<Cambio> AvanzarPreparaciones(EstadoCombate e, int turnosARestar = 1)
    {
        var cambios = new List<Cambio>();
        for (int i = Preparaciones.Count - 1; i >= 0; i--)
        {
            var p = Preparaciones[i];
            p.TurnosRestantes -= turnosARestar;
            if (p.TurnosRestantes > 0) continue;

            var portador = e.Buscar(p.PortadorId);
            var objetivo = p.ObjetivoId is null ? null : e.Buscar(p.ObjetivoId);
            p.EfectoAlVencer.Aplicar(e, new Contexto(portador, objetivo, null), cambios);
            Preparaciones.RemoveAt(i);
        }
        return cambios;
    }

    /// <summary>Envejece las alteraciones de flujo con duracion.</summary>
    public void AvanzarTurno()
    {
        for (int i = Activas.Count - 1; i >= 0; i--)
            if (Activas[i].Turnos > 0)
                Activas[i] = Activas[i] with { Turnos = Activas[i].Turnos - 1 };
        Activas.RemoveAll(f => f.Turnos == 0 && f.Tipo != TipoFlujo.Anclar);
    }
}

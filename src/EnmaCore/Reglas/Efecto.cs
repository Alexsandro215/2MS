using Enma.Core.Estado;
using Enma.Core.Stats;

namespace Enma.Core.Reglas;

/// <summary>Algo que el efecto hizo. La presentacion observa estos eventos.</summary>
public readonly record struct Cambio(string Tipo, string ObjetivoId, string Detalle, double Valor);

/// <summary>
/// Catalogo CERRADO de verbos de efecto.
///
/// Cerrado a proposito: cuando un kit no cabe se añade un verbo nuevo, que son
/// unas pocas lineas, y esa friccion obliga a mirar si el efecto raro merece
/// existir. Lo que NO es cerrado son los valores: esos son expresiones.
/// </summary>
public abstract record Efecto
{
    public abstract void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log);
}

/// <summary>Varios efectos en orden.</summary>
public sealed record Varios(params Efecto[] Efectos) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var f in Efectos) f.Aplicar(e, ctx, log);
    }
}

/// <summary>
/// Aporta un modificador de stat. Es el caso mas comun y el unico que existia
/// en la version anterior del esquema.
/// </summary>
public sealed record ModificarStat(Stat Stat, Capa Capa, Expr Valor, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double v = Valor.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
            log.Add(new Cambio("stat", c.Id, $"{Stat}:{Capa}", v));
    }
}

/// <summary>Suma a un contador con nombre. Las cargas de Kronwell y Hallen.</summary>
public sealed record DarContador(string Nombre, Expr Cantidad, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double v = Cantidad.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            c.Contadores[Nombre] = c.Contador(Nombre) + v;
            log.Add(new Cambio("contador", c.Id, Nombre, c.Contadores[Nombre]));
        }
    }
}

/// <summary>Vacia un contador. Kronwell E y R, Hallen E.</summary>
public sealed record ConsumirContador(string Nombre, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            double previo = c.Contador(Nombre);
            c.Contadores[Nombre] = 0;
            log.Add(new Cambio("contador-consumido", c.Id, Nombre, previo));
        }
    }
}

/// <summary>
/// Fija un valor absoluto. Ultimatum R ("te dejas a 1 de vida"),
/// Kronwell R ("obtienes 70% de desgaste"), gadgets que fijan el CM.
/// </summary>
public sealed record FijarValor(Lectura Que, Expr Valor, Selector? A = null, string Nombre = "") : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double v = Valor.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            switch (Que)
            {
                case Lectura.HpActual: c.Hp = v; break;
                case Lectura.Escudo: c.Escudo = v; break;
                case Lectura.Desgaste: c.Desgaste.Fijar(v); break;
                case Lectura.Contador: c.Contadores[Nombre] = v; break;
            }
            log.Add(new Cambio("fijar", c.Id, Que.ToString(), v));
        }
    }
}

/// <summary>Suma desgaste de golpe. Kronwell R: "obtienes 70% de desgaste".</summary>
public sealed record SumarDesgaste(Expr Cantidad, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double v = Cantidad.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            c.Desgaste.Sumar(v);
            log.Add(new Cambio("desgaste", c.Id, "+", v));
        }
    }
}

/// <summary>Fija el CM del equipo del portador a una fraccion del maximo.</summary>
public sealed record FijarCm(Expr Fraccion) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        var barra = ctx.BarraDelPortador(e);
        barra.Actual = Math.Clamp(Fraccion.Evaluar(e, ctx), 0, 1) * barra.Max;
        log.Add(new Cambio("cm", ctx.NombreDelEquipo(), "fijado", barra.Actual));
    }
}

/// <summary>
/// Suma o resta CM al pool del equipo del portador, recortando al maximo.
/// Ganar con CLASICAL encendido vale x1.5, como toda obtencion de CM;
/// perder o gastar, no.
/// </summary>
public sealed record AjustarCm(Expr Cantidad) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        var barra = ctx.BarraDelPortador(e);
        double v = Cantidad.Evaluar(e, ctx);
        if (v > 0 && barra.ClasicalActivo) v *= e.AjustesCm.Clasical;
        barra.Sumar(v);
        log.Add(new Cambio("cm", ctx.NombreDelEquipo(), "ajuste", v));
    }
}

/// <summary>
/// Traspasa parte de una stat a otra, dentro del mismo combatiente.
/// Willham W: reduce a 0 tu Anti-ARM y X% de ese valor pasa a tu Anti-RM.
/// </summary>
public sealed record TransferirStat(Stat Origen, Stat Destino, Expr Fraccion, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double f = Fraccion.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            double valor = c.MaxAspirable.GetValueOrDefault(Origen);
            c.MaxAspirable[Origen] = 0;
            c.MaxAspirable[Destino] = c.MaxAspirable.GetValueOrDefault(Destino) + valor * f;
            log.Add(new Cambio("transferir", c.Id, $"{Origen}->{Destino}", valor * f));
        }
    }
}

/// <summary>
/// Quita parte de una stat a unos y se la da a otros.
/// Lein-Xein W EVOL roba critico a un aliado; Willham Q EVOL roba RM.
/// </summary>
public sealed record RobarStat(Stat Stat, Expr Fraccion, Selector De, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double f = Fraccion.Evaluar(e, ctx);
        var ladron = (A ?? Selector.Yo).Resolver(e, ctx);
        foreach (var victima in De.Resolver(e, ctx))
        {
            double robado = victima.MaxAspirable.GetValueOrDefault(Stat) * f;
            victima.MaxAspirable[Stat] = victima.MaxAspirable.GetValueOrDefault(Stat) - robado;
            foreach (var l in ladron)
                l.MaxAspirable[Stat] = l.MaxAspirable.GetValueOrDefault(Stat) + robado;
            log.Add(new Cambio("robar", victima.Id, Stat.ToString(), robado));
        }
    }
}

/// <summary>
/// Crea terreno sobre la casilla de alguien. Floristas y Madrerick.
/// Es una REGLA QUE CREA REGLAS.
/// </summary>
public sealed record CrearTerreno(
    string Tipo, Casilla Casilla, bool EsBuff, int Turnos,
    Regla[] Reglas, Selector? EnLineaDe = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (EnLineaDe ?? Selector.Yo).Resolver(e, ctx))
        {
            var t = new Terreno
            {
                Id = $"{Tipo}:{c.Bando}:{c.Linea}:{Casilla}",
                Tipo = Tipo,
                BandoDeLaLinea = c.Bando,
                Linea = c.Linea,
                Casilla = Casilla,
                TurnosRestantes = Turnos,
                EsBuff = EsBuff
            };
            t.Reglas.AddRange(Reglas);
            e.Terrenos.RemoveAll(x => x.Id == t.Id);
            e.Terrenos.Add(t);
            log.Add(new Cambio("terreno-creado", t.Id, Tipo, Turnos));
        }
    }
}

/// <summary>
/// Destruye terreno. Madrerick Q EVOL elimina una casilla de buffs del enemigo:
/// una regla que borra otras reglas.
/// </summary>
public sealed record EliminarTerreno(Bando? DelBando = null, bool? SoloBuffs = null, int Cuantos = 1, bool AlAzar = true) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        var candidatos = e.Terrenos.Where(t =>
            (!DelBando.HasValue || t.BandoDeLaLinea == DelBando.Value) &&
            (!SoloBuffs.HasValue || t.EsBuff == SoloBuffs.Value)).ToList();

        for (int i = 0; i < Cuantos && candidatos.Count > 0; i++)
        {
            int idx = AlAzar ? (int)(e.Rng.Siguiente() * candidatos.Count) : 0;
            if (idx >= candidatos.Count) idx = candidatos.Count - 1;
            var t = candidatos[idx];
            candidatos.RemoveAt(idx);
            e.Terrenos.Remove(t);
            log.Add(new Cambio("terreno-eliminado", t.Id, t.Tipo, 0));
        }
    }
}

/// <summary>Movimiento forzado o propio. Drimur, Setto, Lein-Xein, Winchesster.</summary>
public sealed record Mover(Casilla Destino, Selector? Quien = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (Quien ?? Selector.Yo).Resolver(e, ctx))
        {
            if (c.TieneFlag("anclado") || c.TieneFlag("inmovil")) continue;
            int recorrido = Math.Abs((int)c.Posicion - (int)Destino);
            c.Posicion = Destino;
            log.Add(new Cambio("mover", c.Id, Destino.ToString(), recorrido));
        }
    }
}

/// <summary>Pone o quita un flag con duracion. Anclaje, inmovilidad, inmunidad.</summary>
public sealed record PonerFlag(string Flag, int Turnos, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            c.Flags[Flag] = Math.Max(c.Flags.GetValueOrDefault(Flag), Turnos);
            log.Add(new Cambio("flag", c.Id, Flag, Turnos));
        }
    }
}

public sealed record QuitarFlag(string Flag, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            c.Flags.Remove(Flag);
            log.Add(new Cambio("flag-quitado", c.Id, Flag, 0));
        }
    }
}

/// <summary>Aplica una marca elemental. La regla de L2 la resuelve el sistema de elementos.</summary>
public sealed record AplicarElemento(string Elemento, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            if (c.TieneElementoL2 || c.TieneFlag("inmune_ee")) continue;
            c.Elementos.Add(new MarcaElemental(Elemento, 1, 1));
            log.Add(new Cambio("elemento", c.Id, Elemento, 1));
        }
    }
}

/// <summary>Limpia debuffs. Halltus W, Ordeus E, Tenacidad.</summary>
public sealed record LimpiarDebuffs(bool IncluirEmociones = false, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            c.Elementos.Clear();
            int quitados = c.Flags.Count;
            c.Flags.Clear();
            log.Add(new Cambio("limpiar", c.Id, IncluirEmociones ? "todo" : "sin-emociones", quitados));
        }
    }
}

/// <summary>Da escudo.</summary>
public sealed record DarEscudo(Expr Cantidad, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double v = Cantidad.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            c.Escudo += v;
            log.Add(new Cambio("escudo", c.Id, "+", v));
        }
    }
}

/// <summary>Cura, pasando por la regla de desgaste.</summary>
public sealed record Curar(Expr Cantidad, bool GeneraDesgaste = true, Selector? A = null) : Efecto
{
    public override void Aplicar(EstadoCombate e, Contexto ctx, List<Cambio> log)
    {
        double v = Cantidad.Evaluar(e, ctx);
        foreach (var c in (A ?? Selector.Yo).Resolver(e, ctx))
        {
            double antes = c.Hp;
            c.Hp = Math.Min(c.Hp + v, c.HpMax);
            c.Desgaste.AplicarCuracion(c.Hp - antes, c.HpMax, GeneraDesgaste);
            log.Add(new Cambio("curar", c.Id, "", c.Hp - antes));
        }
    }
}

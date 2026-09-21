using Enma.Core.Estado;

namespace Enma.Core.Reglas;

public enum OpAritmetica { Suma, Resta, Producto, Division, Minimo, Maximo }

/// <summary>Que lectura se hace de un combatiente.</summary>
public enum Lectura
{
    StatBase, HpActual, HpMaximo, HpPct, HpFaltantePct,
    Escudo, Desgaste, Storage, Posicion, Contador
}

/// <summary>Valores globales del combate que una expresion puede leer.</summary>
public enum Global
{
    CmActual, CmMax, CmPct, Turno, Ciclo,
    CmGastadoEsteTurno, HabilidadesUsadasEsteTurno, BasicosEsteTurno,
    ImpactosAcertados, ImpactosFallados, Marcador
}

/// <summary>
/// Arbol de expresion sobre el estado. Deliberadamente limitado: consultas,
/// agregados y aritmetica basica. Sin bucles ni funciones definidas por el
/// usuario — si un kit no cabe, se añade una lectura nueva al catalogo, y esa
/// friccion obliga a mirar si el efecto raro merece existir.
/// </summary>
public abstract record Expr
{
    public abstract double Evaluar(EstadoCombate e, Contexto ctx);

    public static implicit operator Expr(double v) => new Constante(v);
}

public sealed record Constante(double Valor) : Expr
{
    public override double Evaluar(EstadoCombate e, Contexto ctx) => Valor;
}

/// <summary>
/// Lee un valor de uno o varios combatientes y lo agrega a un numero.
///
/// Es lo que permite "n enemigos con E.E." (Conteo), "el objetivo con mas RM"
/// (Selector con Orden + Primero) o "por cada enemigo en ofensiva" (Conteo).
/// </summary>
public sealed record Leer(
    Lectura Que,
    Selector De,
    Agregado Como = Agregado.Primero,
    Stat Stat = Stat.Hp,
    string Nombre = "") : Expr
{
    public override double Evaluar(EstadoCombate e, Contexto ctx)
    {
        var objetivos = De.Resolver(e, ctx);
        if (objetivos.Count == 0) return 0.0;

        if (Como == Agregado.Conteo) return objetivos.Count;

        var valores = objetivos.Select(ValorDe).ToList();
        return Como switch
        {
            Agregado.Primero => valores[0],
            Agregado.Suma => valores.Sum(),
            Agregado.Maximo => valores.Max(),
            Agregado.Minimo => valores.Min(),
            Agregado.Media => valores.Average(),
            _ => valores[0]
        };
    }

    private double ValorDe(Combatiente c) => Que switch
    {
        Lectura.StatBase => c.BaseRef(Stat),
        Lectura.HpActual => c.Hp,
        Lectura.HpMaximo => c.HpMax,
        Lectura.HpPct => c.HpPct,
        Lectura.HpFaltantePct => c.HpFaltantePct,
        Lectura.Escudo => c.Escudo,
        Lectura.Desgaste => c.Desgaste.Acumulado,
        Lectura.Storage => c.Storage,
        Lectura.Posicion => (double)(int)c.Posicion,
        Lectura.Contador => c.Contador(Nombre),
        _ => 0.0
    };
}

/// <summary>Lee un valor global del combate o del historial del turno.</summary>
public sealed record LeerGlobal(Global Que, string Nombre = "") : Expr
{
    public override double Evaluar(EstadoCombate e, Contexto ctx) => Que switch
    {
        Global.CmActual => e.CmActual,
        Global.CmMax => e.CmMax,
        Global.CmPct => e.CmPct,
        Global.Turno => e.Turno,
        Global.Ciclo => e.Ciclo,
        Global.CmGastadoEsteTurno => e.Historial.CmGastado,
        Global.HabilidadesUsadasEsteTurno => e.Historial.HabilidadesUsadas,
        Global.BasicosEsteTurno => e.Historial.BasicosLanzados,
        Global.ImpactosAcertados => e.Historial.ImpactosAcertados,
        Global.ImpactosFallados => e.Historial.ImpactosFallados,
        Global.Marcador => e.Historial.Marcador(Nombre),
        _ => 0.0
    };
}

/// <summary>Cuenta terrenos que cumplen un criterio.</summary>
public sealed record ContarTerrenos(string? Tipo = null, bool? EsBuff = null, Bando? DelBando = null) : Expr
{
    public override double Evaluar(EstadoCombate e, Contexto ctx) =>
        e.Terrenos.Count(t =>
            (Tipo is null || t.Tipo == Tipo) &&
            (!EsBuff.HasValue || t.EsBuff == EsBuff.Value) &&
            (!DelBando.HasValue || t.BandoDeLaLinea == DelBando.Value));
}

/// <summary>Aritmetica binaria.</summary>
public sealed record Calculo(OpAritmetica Op, Expr A, Expr B) : Expr
{
    public override double Evaluar(EstadoCombate e, Contexto ctx)
    {
        double a = A.Evaluar(e, ctx);
        double b = B.Evaluar(e, ctx);
        return Op switch
        {
            OpAritmetica.Suma => a + b,
            OpAritmetica.Resta => a - b,
            OpAritmetica.Producto => a * b,
            OpAritmetica.Division => b == 0 ? 0 : a / b,
            OpAritmetica.Minimo => Math.Min(a, b),
            OpAritmetica.Maximo => Math.Max(a, b),
            _ => 0.0
        };
    }
}

/// <summary>Azucar para construir expresiones sin tanto ruido.</summary>
public static class Ex
{
    public static Expr K(double v) => new Constante(v);

    public static Expr Stat(Stat s, Selector? de = null) =>
        new Leer(Lectura.StatBase, de ?? Selector.Yo, Agregado.Primero, s);

    public static Expr HpFaltante(Selector? de = null) =>
        new Leer(Lectura.HpFaltantePct, de ?? Selector.Yo);

    public static Expr HpPct(Selector? de = null) =>
        new Leer(Lectura.HpPct, de ?? Selector.Yo);

    public static Expr Desgaste(Selector? de = null) =>
        new Leer(Lectura.Desgaste, de ?? Selector.Yo);

    public static Expr Cargas(string nombre, Selector? de = null) =>
        new Leer(Lectura.Contador, de ?? Selector.Yo, Agregado.Primero, Estado.Stat.Hp, nombre);

    public static Expr Contar(Selector de) =>
        new Leer(Lectura.HpActual, de, Agregado.Conteo);

    public static Expr Global(Global g, string nombre = "") => new LeerGlobal(g, nombre);

    public static Expr Mas(Expr a, Expr b) => new Calculo(OpAritmetica.Suma, a, b);
    public static Expr Menos(Expr a, Expr b) => new Calculo(OpAritmetica.Resta, a, b);
    public static Expr Por(Expr a, Expr b) => new Calculo(OpAritmetica.Producto, a, b);
    public static Expr Entre(Expr a, Expr b) => new Calculo(OpAritmetica.Division, a, b);
    public static Expr Min(Expr a, Expr b) => new Calculo(OpAritmetica.Minimo, a, b);
    public static Expr Max(Expr a, Expr b) => new Calculo(OpAritmetica.Maximo, a, b);
}

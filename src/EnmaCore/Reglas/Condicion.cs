using Enma.Core.Estado;

namespace Enma.Core.Reglas;

public enum Comparador { Igual, Distinto, Mayor, MayorIgual, Menor, MenorIgual }

/// <summary>
/// Arbol booleano sobre el estado. Se combina con Y / O / NO y se apoya en
/// <see cref="Expr"/> para todo lo numerico.
/// </summary>
public abstract record Condicion
{
    public abstract bool Cumple(EstadoCombate e, Contexto ctx);

    public static readonly Condicion Siempre = new Constante(true);
    public static readonly Condicion Nunca = new Constante(false);

    public sealed record Constante(bool Valor) : Condicion
    {
        public override bool Cumple(EstadoCombate e, Contexto ctx) => Valor;
    }
}

public sealed record Y(params Condicion[] Partes) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx)
    {
        foreach (var p in Partes) if (!p.Cumple(e, ctx)) return false;
        return true;
    }
}

public sealed record O(params Condicion[] Partes) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx)
    {
        foreach (var p in Partes) if (p.Cumple(e, ctx)) return true;
        return false;
    }
}

public sealed record No(Condicion Parte) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) => !Parte.Cumple(e, ctx);
}

/// <summary>Compara dos expresiones. Es el caballo de batalla.</summary>
public sealed record Comparar(Expr A, Comparador Op, Expr B) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx)
    {
        double a = A.Evaluar(e, ctx);
        double b = B.Evaluar(e, ctx);
        const double eps = 1e-9;
        return Op switch
        {
            Comparador.Igual => Math.Abs(a - b) < eps,
            Comparador.Distinto => Math.Abs(a - b) >= eps,
            Comparador.Mayor => a > b,
            Comparador.MayorIgual => a >= b - eps,
            Comparador.Menor => a < b,
            Comparador.MenorIgual => a <= b + eps,
            _ => false
        };
    }
}

/// <summary>Hay al menos un combatiente que cumple el selector.</summary>
public sealed record Existe(Selector Quien) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) =>
        Quien.Resolver(e, ctx).Count > 0;
}

/// <summary>El portador (o quien diga el selector) esta en una casilla concreta.</summary>
public sealed record EnCasilla(Casilla Casilla, Selector? Quien = null) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) =>
        (Quien ?? Selector.Yo).Resolver(e, ctx).Exists(c => c.Posicion == Casilla);
}

public sealed record TieneFlag(string Flag, Selector? Quien = null) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) =>
        (Quien ?? Selector.Yo).Resolver(e, ctx).Exists(c => c.TieneFlag(Flag));
}

public sealed record EsBando(Bando Bando, Selector? Quien = null) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) =>
        (Quien ?? Selector.Yo).Resolver(e, ctx).Exists(c => c.Bando == Bando);
}

/// <summary>Modo CLASICAL encendido.</summary>
public sealed record ClasicalActivo : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) => ctx.BarraDelPortador(e).ClasicalActivo;
}

/// <summary>
/// Tirada de probabilidad. Consume del RNG del estado, asi que las
/// simulaciones siguen siendo reproducibles con la misma semilla.
/// </summary>
public sealed record Probabilidad(double P) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) => e.Rng.Siguiente() < P;
}

/// <summary>
/// Breakpoint de emblema: "al alcanzar X emblemas de un tipo". Se expresa
/// contando marcadores del historial o combatientes con una etiqueta.
/// </summary>
public sealed record Breakpoint(Expr Cuenta, int Umbral) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx) =>
        Cuenta.Evaluar(e, ctx) >= Umbral;
}

/// <summary>Azucar para escribir condiciones sin tanto ruido.</summary>
/// <summary>
/// Que quien lanzo la accion sea el portador de la regla.
///
/// Sin esto, DOS KITS EN EL MISMO COMBATE SE DISPARAN ENTRE SI: el evento
/// AlUsarHabilidad no dice de quien es la regla, solo que alguien pulso una
/// tecla, y la R de un personaje acababa lanzando la R del otro.
/// </summary>
public sealed record LoHaceElPortador : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx)
        => ctx.Portador is not null && ctx.Evento?.ActorId == ctx.Portador.Id;
}

/// <summary>
/// Que la habilidad que disparo el evento sea esta tecla.
///
/// Sin esto las cuatro teclas de un kit disparan las cuatro reglas: el motor
/// manda el evento con la tecla en Detalle, y alguien tiene que leerla.
/// </summary>
public sealed record TeclaEs(char Tecla) : Condicion
{
    public override bool Cumple(EstadoCombate e, Contexto ctx)
    {
        string d = ctx.Evento?.Detalle ?? "";
        return d.Length == 1 && char.ToUpperInvariant(d[0]) == char.ToUpperInvariant(Tecla);
    }
}

public static class Cond
{
    /// <summary>La tecla, y que la pulse su dueño. Lo que necesita todo kit.</summary>
    public static Condicion Tecla(char t) => new Y(new LoHaceElPortador(), new TeclaEs(t));

    /// <summary>Solo la tecla, sin mirar quien. Para reglas que reaccionan a otros.</summary>
    public static Condicion SoloTecla(char t) => new TeclaEs(t);

    public static Condicion Mayor(Expr a, Expr b) => new Comparar(a, Comparador.Mayor, b);
    public static Condicion Menor(Expr a, Expr b) => new Comparar(a, Comparador.Menor, b);
    public static Condicion MayorIgual(Expr a, Expr b) => new Comparar(a, Comparador.MayorIgual, b);
    public static Condicion MenorIgual(Expr a, Expr b) => new Comparar(a, Comparador.MenorIgual, b);
    public static Condicion Igual(Expr a, Expr b) => new Comparar(a, Comparador.Igual, b);
}

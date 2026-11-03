using Enma.Core.Estado;

namespace Enma.Core.Reglas;

/// <summary>
/// De donde viene la regla.
///
/// IMPORTANTE: el evaluador NO ramifica sobre esto. Es una etiqueta para
/// depurar y para que la interfaz sepa que icono pintar. Si aparece un
/// `if (Fuente == Fuente.Gadget)` en el resolvedor, la arquitectura se rompio.
/// Ver arquitectura.md  1.
/// </summary>
public enum Fuente
{
    Pasiva, Gadget, Emblema, Emocion, Elemento, Terreno, Comida, Casilla,
    Arma, Armadura, Botas, Accesorio, Recuerdo, Habilidad, Estado, Entorno
}

/// <summary>Cuando se comprueba la regla.</summary>
public enum Disparador
{
    /// <summary>Se evalua continuamente mientras la regla este vigente.</summary>
    Siempre,
    AlIniciarCombate,
    AlIniciarCiclo,
    AlIniciarTurno,
    AlTerminarTurno,
    AlGolpear,
    AlRecibirGolpe,
    AlFallar,
    AlUsarHabilidad,
    AlUsarBasico,
    AlCurar,
    AlSerCurado,
    AlAplicarElemento,
    AlEntrarEnCasilla,
    AlMoverse,
    AlCaerEnKo,
    AlMorir,
    AlRevivir,
    AlUsarObjeto,
    EventoDelDia
}

/// <summary>Cuanto dura lo que la regla concede.</summary>
public readonly record struct Duracion(int Turnos = 0, bool Permanente = false, bool SoloEsteTurno = false)
{
    public static readonly Duracion Instantanea = new();
    public static readonly Duracion Permanente_ = new(Permanente: true);
    public static Duracion PorTurnos(int n) => new(n);
    public static readonly Duracion EsteTurno = new(SoloEsteTurno: true);
}

/// <summary>
/// El esquema unico. Ocho sistemas distintos —pasiva, gadget, emblema, emocion,
/// elemento, terreno, comida, casilla— son filas de esta misma tabla.
///
///   Regla = { fuente · disparador · condicion · efecto · duracion · alcance }
///
/// El alcance vive dentro del efecto, en su Selector.
/// </summary>
public sealed record Regla
{
    public required string Id { get; init; }
    public required Fuente Fuente { get; init; }
    public required Disparador Disparador { get; init; }
    public required Efecto Efecto { get; init; }

    /// <summary>null = siempre se cumple.</summary>
    public Condicion? Condicion { get; init; }

    public Duracion Duracion { get; init; } = Duracion.Instantanea;

    /// <summary>Quien lleva la regla. null = global (emblemas de equipo, entorno).</summary>
    public string? PortadorId { get; init; }

    /// <summary>Etiqueta legible, para depurar.</summary>
    public string Etiqueta { get; init; } = "";

    /// <summary>
    /// Si es true, la regla se retira despues de dispararse una vez.
    /// Util para gadgets de un solo uso y para DEFEND.
    /// </summary>
    public bool UnSoloUso { get; init; }

    /// <summary>
    /// Turnos que le quedan de vida. -1 = no caduca, que es el caso de casi
    /// todo: pasivas, emblemas, equipo. Lo usan los buffs concedidos en
    /// combate, que son los unicos que nacen con fecha de caducidad.
    /// </summary>
    public int TurnosRestantes { get; set; } = -1;
}

/// <summary>Un evento que puede disparar reglas.</summary>
public sealed record Evento(
    Disparador Tipo,
    string? ActorId = null,
    string? ObjetivoId = null,
    double Valor = 0,
    string Detalle = "");

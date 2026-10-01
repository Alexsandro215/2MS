using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;

namespace Enma.Core.Simulacion;

/// <summary>
/// Quien decide las acciones de un bando.
///
/// El motor no sabe si al otro lado hay una persona o una IA: las dos son
/// clientes del mismo motor, con las mismas reglas. Ver arquitectura.md  3.
/// </summary>
public interface IPolitica
{
    /// <summary>Quien abre el turno del bando, entre los que pueden.</summary>
    Combatiente ElegirAbridor(MotorCombate m, IReadOnlyList<Combatiente> candidatos);

    /// <summary>
    /// Las acciones del turno de un personaje, de una en una. Se piden sobre
    /// la marcha, asi que cada una ve el estado que dejo la anterior.
    /// </summary>
    IEnumerable<Accion> Turno(MotorCombate m, Combatiente quien);
}

/// <summary>
/// Primera IA. Juega solo con lo que el motor ya sabe hacer: basicos,
/// rescate, remate, escondite y posicion.
///
/// ⚠️ No usa QWER ni Carga Musical: los kits aun no tienen contenido, y
/// gastar CM en habilidades vacias solo meteria ruido en las cifras. Tampoco
/// se defiende: sin curacion, defenderse solo alarga lo inevitable.
/// </summary>
public sealed class IABasica : IPolitica
{
    /// <summary>Saca del front a los compañeros caidos, gastando su turno.</summary>
    public bool Rescata { get; init; } = true;

    /// <summary>Remata a los rivales caidos en cuanto se puede.</summary>
    public bool Remata { get; init; } = true;

    /// <summary>Las ligeras abren: desgastan la evasion para los que vienen detras.</summary>
    public Combatiente ElegirAbridor(MotorCombate m, IReadOnlyList<Combatiente> candidatos)
        => candidatos.FirstOrDefault(c => c.Arma == ClaseArma.Ligera) ?? candidatos[0];

    public IEnumerable<Accion> Turno(MotorCombate m, Combatiente quien)
    {
        var e = m.Estado;

        // 1. Un compañero caido en el front: sacarlo antes de que lo rematen.
        if (Rescata && Rescate(e, quien) is { } rescate)
        {
            yield return rescate;
            yield break;
        }

        // 2. Volver al combate. Se sale a la defensiva...
        if (quien.Zona == Zona.Escondite)
            yield return new SalirDelEscondite { ActorId = quien.Id };

        // 3. ...y desde la defensiva no se pega.
        if (quien.Posicion == Casilla.Defensiva)
            yield return new Moverse { ActorId = quien.Id, Destino = Casilla.Neutral };

        if (Objetivo(e, quien) is { } objetivo)
            yield return new AtaqueBasico { ActorId = quien.Id, ObjetivoId = objetivo.Id };

        yield return new Pasar { ActorId = quien.Id };
    }

    private static Accion? Rescate(EstadoCombate e, Combatiente quien)
    {
        if (quien.Zona != Zona.Front) return null;

        var caido = e.Combatientes.FirstOrDefault(c =>
            c.Bando == quien.Bando && c.EnKo && !c.Muerto && c.Zona == Zona.Front);
        if (caido is null) return null;

        var recambio = e.Del(quien.Bando)
            .Where(c => c.Zona == Zona.Banquillo)
            .OrderByDescending(c => c.HpPct)
            .FirstOrDefault();
        if (recambio is not null)
            return new Swap { ActorId = quien.Id, SalienteId = caido.Id, EntranteId = recambio.Id };

        if (e.EsconditesLibres > 0)
            return new Esconderse { ActorId = quien.Id, OcultoId = caido.Id };

        return null;
    }

    /// <summary>Rematar si se puede; si no, al rival al que menos vida le queda.</summary>
    private Combatiente? Objetivo(EstadoCombate e, Combatiente quien)
    {
        var rivales = e.Combatientes
            .Where(c => c.Bando != quien.Bando && c.Zona == Zona.Front)
            .ToList();

        return (Remata ? rivales.FirstOrDefault(c => c.Rematable) : null)
            ?? rivales.Where(c => !c.EnKo).OrderBy(c => c.HpPct).FirstOrDefault();
    }
}

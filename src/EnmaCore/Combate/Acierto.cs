namespace Enma.Core.Combate;

/// <summary>
/// Calculo de acierto. Fuente: docs/formulas.md  4.
///
/// La forma de la formula garantiza que el acierto quede SIEMPRE entre 5% y 95%.
/// Cuando PRESS = EVA_eff, el acierto es exactamente 50%.
/// </summary>
public static class Acierto
{
    /// <summary>
    /// Evasion erosionada por stacks de desgaste de esquiva.
    ///
    /// Dos fuentes alimentan la misma EVA_eff pero tienen vidas distintas:
    ///   - Desgaste por esquivar: expira al terminar el turno del BANDO atacante,
    ///     asi que persiste toda la ronda y lo aprovechan todos los aliados.
    ///   - Debuffs de skill o terreno: expiran por turnos propios, o se limpian.
    ///
    /// El desgaste por esquivar es ACUMULATIVO DE EQUIPO: los stacks que genera
    /// el primer aliado siguen ahi cuando entra el cuarto. Por eso las armas
    /// ligeras abren desgastando y la pesada remata con mucha mejor precision.
    /// </summary>
    public static double EvasionEfectiva(double eva, int stacks)
    {
        if (stacks < 0) stacks = 0;
        double factor = Math.Max(
            Constantes.EvaSuelo,
            1.0 - Constantes.DesgasteEvaPorStack * stacks);
        return eva * factor;
    }

    /// <summary>
    /// Probabilidad de impacto, en porcentaje [5, 95].
    ///
    ///   Hit% = 5 + 90 * ( PRESS / (PRESS + EVA_eff) )
    ///
    /// Tabla de referencia con PRESS 50 (manual-tecnico.md  5.2):
    ///   EVA_eff 100 -> 35.0% | 75 -> 40.7% | 50 -> 50.0% | 25 -> 65.0% | 10 -> 80.0%
    /// </summary>
    public static double Probabilidad(double press, double evaEfectiva)
    {
        double suma = press + evaEfectiva;
        if (suma <= 0) return Constantes.HitMin;

        double hit = Constantes.HitMin + Constantes.HitAmplitud * (press / suma);
        return Math.Clamp(hit, Constantes.HitMin, Constantes.HitMax);
    }

    /// <summary>Atajo: de EVA cruda y stacks a probabilidad final.</summary>
    public static double Probabilidad(double press, double eva, int stacks)
        => Probabilidad(press, EvasionEfectiva(eva, stacks));

    /// <summary>
    /// Tirada de impacto.
    ///
    /// Reglas (formulas.md  4):
    ///   - Un fallo NO corta la secuencia de golpes.
    ///   - Si falla: no aplica dano, ni elemento, ni efectos on-hit de ese impacto.
    ///   - Si la accion consume recurso, SE CONSUME aunque falle.
    /// </summary>
    public static bool Impacta(double press, double eva, int stacks, IAleatorio rng)
        => rng.Siguiente() * 100.0 < Probabilidad(press, eva, stacks);
}

/// <summary>
/// Fuente de aleatoriedad inyectable. El nucleo es determinista: dada la misma
/// semilla y las mismas acciones, produce el mismo resultado. Imprescindible
/// para simular miles de combates de forma reproducible.
/// </summary>
public interface IAleatorio
{
    /// <summary>Valor en [0, 1).</summary>
    double Siguiente();
}

/// <summary>Implementacion por defecto, con semilla explicita.</summary>
public sealed class AleatorioSemilla(int semilla) : IAleatorio
{
    private readonly Random _r = new(semilla);
    public double Siguiente() => _r.NextDouble();
}

namespace Enma.Core.Stats;

/// <summary>Las cuatro fases de lectura de poder de una run. Ver formulas.md  1.</summary>
public enum Fase
{
    Early,
    Mid,
    Late,
    /// <summary>
    /// No es un tramo de niveles: es lo que pasa cuando ya no quedan niveles.
    /// g(L) = 1.0, todas las stats congeladas en su MAX, y el desgaste solo sube.
    /// </summary>
    UltraLate
}

/// <summary>
/// Escalado lineal por nivel. Fuente: docs/formulas.md  1 (STATS MASTER).
///
/// El sistema tiene DOS ejes de progresion simultaneos:
///   1. Subir de nivel te acerca a tu techo actual.
///   2. Mejorar el MAX aspirable LEVANTA el techo.
/// Un personaje puede estar al 100% de su aspiracion y aun asi ser debil.
/// </summary>
public static class Escalado
{
    /// <summary>
    /// Factor de escalado lineal: g(L) = L / 18, recortado a [0, 1].
    /// Por encima del nivel maximo se satura en 1.0 (Ultra Late).
    /// </summary>
    public static double G(int nivel)
    {
        if (nivel <= 0) return 0.0;
        if (nivel >= Constantes.NivelMax) return 1.0;
        return (double)nivel / Constantes.NivelMax;
    }

    /// <summary>
    /// Valor de la stat a un nivel dado, antes de desgaste y modificadores.
    /// </summary>
    /// <param name="maxAspirable">
    /// El techo al que ESTE personaje puede llegar ahora mismo en esa stat.
    /// No es un valor global del juego: es variable por personaje y mejorable jugando.
    /// </param>
    public static double Valor(double maxAspirable, int nivel) => maxAspirable * G(nivel);

    /// <summary>
    /// Porcentaje de MAX alcanzado. Mide el progreso de la partida sin mirar el nivel:
    /// lo cerca que esta cada personaje de su propio techo dice cuanto queda de historia.
    /// </summary>
    public static double ProgresoHaciaMax(double valorActual, double maxAspirable)
        => maxAspirable <= 0 ? 0.0 : valorActual / maxAspirable;

    /// <summary>Fase de la run segun el nivel. Ver formulas.md  1.</summary>
    public static Fase FaseDe(int nivel) => nivel switch
    {
        <= 0 => Fase.Early,
        <= 5 => Fase.Early,
        <= 12 => Fase.Mid,
        < Constantes.NivelMax => Fase.Late,
        Constantes.NivelMax => Fase.Late,
        _ => Fase.UltraLate
    };

    /// <summary>
    /// Ultra Late: se ha alcanzado el nivel maximo y aun quedan batallas.
    /// La progresion por nivel se agoto; solo queda mejorar el MAX, los emblemas,
    /// los gadgets y el equipo. Y el desgaste, que solo sube.
    /// </summary>
    public static bool EsUltraLate(int nivel, bool quedanBatallas)
        => nivel >= Constantes.NivelMax && quedanBatallas;
}

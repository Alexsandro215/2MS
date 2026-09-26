namespace Enma.Core;

/// <summary>
/// Constantes canonicas del sistema. Fuente: docs/formulas.md  0.
///
/// Jerarquia declarada: ante contradiccion sobre calculo de stats, agregacion,
/// caps, escalado o desgaste, prevalece STATS MASTER.
/// </summary>
public static class Constantes
{
    // ---- Acierto ----
    /// <summary>Suelo de probabilidad de impacto (%).</summary>
    public const double HitMin = 5.0;

    /// <summary>Techo de probabilidad de impacto (%).</summary>
    public const double HitMax = 95.0;

    /// <summary>Amplitud de la curva de acierto: Hit% = 5 + 90 * ratio.</summary>
    public const double HitAmplitud = 90.0;

    /// <summary>
    /// Reduccion de evasion por stack de desgaste de esquiva.
    /// 0.03 y no 0.01: calibrado para +50% de acierto maximo en late. Ver formulas.md  4.
    /// </summary>
    public const double DesgasteEvaPorStack = 0.03;

    /// <summary>Suelo de la evasion erosionada: nunca baja del 10% de su valor.</summary>
    public const double EvaSuelo = 0.10;

    // ---- Critico ----
    public const double CritCap = 100.0;

    /// <summary>
    /// Multiplicador de critico. Se aplica DESPUES de la reduccion por defensa,
    /// lo que evita que critico y penetracion se retroalimenten.
    /// </summary>
    public const double CritMultiplicador = 1.8;

    // ---- Defensas ----
    public const double ArmCap = 90.0;
    public const double RmCap = 90.0;

    /// <summary>Techo de reduccion por armadura/RM: 90%.</summary>
    public const double ReduccionCap = 0.90;

    /// <summary>Constante de la formula de ratio: Red = DEF / (DEF + 100).</summary>
    public const double ReduccionK = 100.0;

    /// <summary>La penetracion no puede superar el 80% de la defensa del objetivo.</summary>
    public const double PenCap = 0.80;

    /// <summary>Dano verdadero: fraccion maxima (100%).</summary>
    public const double DvCap = 1.0;

    // ---- Escalado ----
    /// <summary>Nivel maximo. g(L) = L / 18.</summary>
    public const int NivelMax = 18;

    // ---- Desgaste (fatiga) ----
    /// <summary>
    /// Cada 20% de la VIDA BASE curada (acumulado) genera +1% de desgaste.
    /// Curarse una barra entera cuesta 5%.
    ///
    /// Calibrado para el objetivo de diseno: que un jugador normal llegue a
    /// LATE con ~35% de desgaste acumulado. Supone una run de ~40 combates,
    /// con late empezando sobre el combate 27, y una curacion media del 25%
    /// de la barra por combate.
    ///
    /// Historial del dial: 1/2 -> se probo 1/3 -> se volvio a 1/2 -> 1/5.
    /// </summary>
    public const double FraccionVidaPorPuntoDesgaste = 0.20;

    /// <summary>Desgaste generado al cruzar el umbral.</summary>
    public const double DesgastePorUmbral = 0.01;

    /// <summary>Cap de Resistencia al Desgaste.</summary>
    public const double ResistenciaDesgasteCap = 0.50;

    // ---- Defensa (accion DEFEND) ----
    /// <summary>DEFEND otorga +50% de ARM y RM, y se consume en el primer impacto.</summary>
    public const double DefendBono = 0.50;

    /// <summary>DEFEND duplica la ganancia de CM al recibir ese golpe.</summary>
    public const double DefendMultiplicadorCm = 2.0;

    // ---- Carga Musical ----
    /// <summary>
    /// Bonus de tempo GENERAL: cualquier arma que conecte en su capa gana un
    /// +20% de CM en ese golpe.
    ///
    /// Es el premio pequeno. El grande se lo lleva la MEDIA, que es la
    /// generadora del equipo — ver BonusRitmico.CmPorPulsoDeLaMedia.
    ///
    /// ⚠️ Antes era 2.0 para todas y la media tenia encima un +15% propio.
    /// Se invirtieron: el x2 pasa a ser SOLO de la media, y este queda como el
    /// suelo comun. Asi la media destaca de verdad como generadora en vez de
    /// llevarse una propina sobre un bonus que ya tenia todo el mundo.
    /// </summary>
    public const double BonusTempo = 0.20;

    /// <summary>Un KO aporta solo la mitad de su storage al pool de equipo.</summary>
    public const double StorageKoFactor = 0.5;

    /// <summary>Al recibir dano se gana la mitad de lo que genera el arma del atacante.</summary>
    public const double CmDefensorFactor = 0.5;

    /// <summary>CLASICAL multiplica x1.5 toda obtencion de CM.</summary>
    public const double ClasicalMultiplicador = 1.5;
}

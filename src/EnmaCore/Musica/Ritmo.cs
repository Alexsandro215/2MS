namespace Enma.Core.Musica;

/// <summary>
/// Las tres capas de una cancion. Cada clase de arma se engancha a una.
/// Ver docs/musica.md  1, "Cada arma sigue una capa distinta de la musica".
/// </summary>
public enum CapaMusical
{
    /// <summary>Ligera. Fluye y puede pasarse de la linea de compas.</summary>
    Melodia,
    /// <summary>Media. El latido constante.</summary>
    Pulso,
    /// <summary>Pesada. El acento estructural, el primer tiempo.</summary>
    Compas
}

/// <summary>
/// Calidad de una pulsacion respecto al ritmo, medida por la PRESENTACION.
///
/// Esta es la frontera entre los dos relojes de arquitectura.md  5. La capa
/// ritmica es continua y vive atada al audio; el nucleo es discreto y
/// determinista. El nucleo NO mide tiempo: recibe este dato ya cocinado y
/// resuelve igual siempre.
///
/// Consecuencia practica: se puede simular. Das calidades ritmicas con una
/// semilla y el combate es reproducible.
/// </summary>
public readonly record struct Ritmo(
    /// <summary>Desfase respecto al objetivo, en segundos. Negativo = te adelantaste.</summary>
    double Desfase = 0.0,
    /// <summary>true si la pulsacion cayo dentro de la ventana buena.</summary>
    bool EnTempo = false,
    /// <summary>true si este golpe cae en el primer tiempo del compas.</summary>
    bool EnCompas = false,
    /// <summary>true si el golpe ocurre pasada la linea de compas (melodia de la ligera).</summary>
    bool PasadoElCompas = false,
    /// <summary>Indice del tiempo dentro del compas, empezando en 0.</summary>
    int TiempoDelCompas = 0,
    /// <summary>Tiempos que tiene el compas. 4 en un 4/4.</summary>
    int TiemposPorCompas = 4)
{
    /// <summary>Pulsacion perfecta al inicio de compas. Util en tests.</summary>
    public static readonly Ritmo Clavado = new(0.0, true, true, false, 0);

    /// <summary>Sin informacion ritmica: el combate resuelve sin bonus ni penalizacion.</summary>
    public static readonly Ritmo Ninguno = new();

    /// <summary>true si este golpe cae en el ultimo tiempo del compas.</summary>
    public bool EnUltimoTiempo => TiempoDelCompas == TiemposPorCompas - 1;
}

/// <summary>
/// Efectos ritmicos por clase de arma. Fuente: docs/musica.md  1.
///
/// Los numeros marcados provisionales estan pendientes de balanceo.
/// </summary>
public static class BonusRitmico
{
    /// <summary>Capa musical a la que se engancha cada arma.</summary>
    public static CapaMusical CapaDe(ClaseArma arma) => arma switch
    {
        ClaseArma.Ligera => CapaMusical.Melodia,
        ClaseArma.Media => CapaMusical.Pulso,
        ClaseArma.Pesada => CapaMusical.Compas,
        _ => CapaMusical.Pulso
    };

    /// <summary>
    /// LIGERA. Los golpes que se alargan pasada la linea de compas aplican
    /// solo el 25% del AD base. Se renuncia al dano a cambio de seguir
    /// generando CM y erosionando evasion, que se cobran por impacto.
    /// </summary>
    public const double AdDeLaMelodia = 0.25;

    /// <summary>⚠️ Provisional. Compases extra que la ligera puede encadenar.</summary>
    public const int CompasesExtraMaximos = 1;

    /// <summary>⚠️ Provisional. MEDIA: extra de CM por golpe en el pulso.</summary>
    public const double ExtraCmPorPulso = 0.15;

    /// <summary>⚠️ Provisional. PESADA: bonus de dano al clavar el primer tiempo.</summary>
    public const double BonusCompasMaximo = 0.50;

    /// <summary>⚠️ Provisional. Medio ancho de la ventana del compas, en segundos.</summary>
    public const double VentanaCompas = 0.12;

    /// <summary>
    /// Multiplicador de dano de un basico segun la capa del arma y la
    /// calidad de la pulsacion.
    /// </summary>
    public static double MultiplicadorDano(ClaseArma arma, Ritmo r) => arma switch
    {
        // La ligera pierde dano al pasarse del compas, y no gana nada por acertar.
        ClaseArma.Ligera => r.PasadoElCompas ? AdDeLaMelodia : 1.0,

        // La media no cobra en dano: cobra en recurso.
        ClaseArma.Media => 1.0,

        // La pesada escala con lo cerca que estuvo del uno. No es todo o nada.
        ClaseArma.Pesada => 1.0 + BonusCompasMaximo * ProximidadAlCompas(r),

        _ => 1.0
    };

    /// <summary>
    /// Cuanto se acerco la pulsacion al primer tiempo del compas, en [0, 1].
    /// 1 es clavado; baja linealmente hacia los bordes de la ventana, tanto
    /// si te adelantas como si te retrasas.
    /// </summary>
    public static double ProximidadAlCompas(Ritmo r)
    {
        if (!r.EnCompas) return 0.0;
        double d = Math.Abs(r.Desfase);
        if (d >= VentanaCompas) return 0.0;
        return 1.0 - d / VentanaCompas;
    }

    /// <summary>
    /// CM extra de la media por golpear en el pulso. Es deliberadamente
    /// pequeno: el premio de verdad es llegar antes a CLASICAL.
    /// </summary>
    public static double ExtraCm(ClaseArma arma, Ritmo r, double cmBase)
        => arma == ClaseArma.Media && r.EnTempo ? cmBase * ExtraCmPorPulso : 0.0;

    /// <summary>
    /// Golpes extra que concede el ritmo. Solo la MEDIA, y solo al alcanzar
    /// el ultimo tiempo del compas.
    ///
    /// El Attack Speed NO regala este golpe: es lo que te lleva lo bastante
    /// lejos dentro de la barra como para poder darlo. Por eso no se suman.
    /// </summary>
    public static int GolpesExtra(ClaseArma arma, Ritmo r)
        => arma == ClaseArma.Media && r.EnUltimoTiempo && r.EnTempo ? 1 : 0;
}

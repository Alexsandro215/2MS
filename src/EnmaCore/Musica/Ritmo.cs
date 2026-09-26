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
    /// LIGERA. Los golpes que caen en el compas extra aplican solo el 25% del
    /// AD base.
    ///
    /// ✅ IMPORTANTE: el recorte es SOLO de AD. La Carga Musical se cobra
    /// INTEGRA, y el desgaste de evasion tambien —ese ni siquiera depende del
    /// dano, sale de que el defensor esquive—.
    ///
    /// Por eso alargarse compensa: dejas de ser fuente de dano y te conviertes
    /// en motor de recurso y en abrelatas de evasion, que es el papel de la
    /// ligera llevado al extremo.
    /// </summary>
    public const double AdDeLaMelodia = 0.25;

    /// <summary>
    /// Multiplicador de CM en el compas extra. Es 1.0 a proposito: la melodia
    /// cuesta dano, no recurso.
    /// </summary>
    public const double CmDeLaMelodia = 1.0;

    /// <summary>
    /// ✅ La melodia da TIEMPO, no desborda golpes.
    ///
    /// Seguir la melodia significa que no te cortan a mitad: terminas el
    /// compas en el que estas y ademas el siguiente ENTERO. Uno solo, no dos.
    ///
    /// Como tienes mas tiempo, caben mas golpes de los que te tocaban. Es un
    /// aumento de VOLUMEN a cambio de potencia, que es exactamente el papel
    /// de la ligera: mas impactos = mas CM y mas desgaste de EVA.
    /// </summary>
    public const int CompasesExtraMaximos = 1;

    /// <summary>
    /// MEDIA: un basico que conecta EN EL PULSO vale el DOBLE de CM.
    ///
    /// Es el premio grande del sistema, y es suyo en exclusiva: la media es la
    /// generadora del equipo y este numero es lo que la hace serlo. Las otras
    /// dos se quedan con el suelo comun de Constantes.BonusTempo (+20%).
    ///
    /// No se suma al suelo comun — lo SUSTITUYE.
    /// </summary>
    public const double CmPorPulsoDeLaMedia = 2.0;

    /// <summary>
    /// PESADA. Un basico que cae en el primer tiempo del compas aplica el
    /// 150% del AD.
    ///
    /// ✅ SOBRE QUE AD: el OPERATIVO DE ESE INSTANTE, ya con el desgaste
    /// descontado. NO sobre el AD potencial del personaje a nivel maximo.
    ///
    /// Un personaje a nivel 9 con la mitad del AD que tendra a nivel 18 cobra
    /// el 150% de LO QUE TIENE AHORA, no de lo que llegara a tener. Quien
    /// llame a MultiplicadorDano tiene que multiplicar por la stat operativa
    /// ya agregada (ver Stats/Agregacion), no por una base guardada.
    ///
    /// Es el mismo eje que <see cref="AdDeLaMelodia"/>: los dos son
    /// multiplicadores del AD de ese momento, uno por debajo de 1 y otro por
    /// encima.
    /// </summary>
    public const double AdDelCompas = 1.50;

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

        // La pesada cobra PLANO: dentro de la ventana es dentro. No se afina
        // el premio por clavar el centro — la ventana ya es el margen de
        // ayuda, y pedir precision fina encima convertiria el arma en un
        // juego de ritmo dentro del juego de ritmo.
        ClaseArma.Pesada => r.EnCompas && Math.Abs(r.Desfase) < VentanaCompas
            ? AdDelCompas : 1.0,

        _ => 1.0
    };

    /// <summary>
    /// Cuanto se acerco la pulsacion al primer tiempo del compas, en [0, 1].
    /// 1 es clavado; baja linealmente hacia los bordes de la ventana, tanto
    /// si te adelantas como si te retrasas.
    ///
    /// ⚠️ YA NO DECIDE EL DANO — la pesada cobra plano. Se conserva para
    /// avisarle al jugador de lo cerca que estuvo, que es informacion util
    /// aunque no cambie el resultado.
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
        => arma == ClaseArma.Media && r.EnTempo
            ? cmBase * (CmPorPulsoDeLaMedia - 1.0)
            : 0.0;

    /// <summary>
    /// Multiplicador de CM de un basico. Siempre 1.0 salvo que alguna regla
    /// futura diga lo contrario.
    ///
    /// Existe para dejar constancia de que el recorte de la melodia NO llega
    /// aqui: en el compas extra la ligera pega al 25% pero cobra la CM entera.
    /// </summary>
    public static double MultiplicadorCm(ClaseArma arma, Ritmo r)
        => r.PasadoElCompas ? CmDeLaMelodia : 1.0;

    /// <summary>
    /// Golpes extra que concede el ritmo.
    ///
    /// MEDIA: uno, al alcanzar el ultimo tiempo del compas. El Attack Speed
    /// NO regala ese golpe — es lo que te lleva lo bastante lejos dentro de la
    /// barra como para poder darlo. Por eso no se suman.
    ///
    /// LIGERA: ver <see cref="GolpesConMelodia"/>. Los suyos no salen de un
    /// numero fijo sino del tiempo que gana.
    /// </summary>
    public static int GolpesExtra(ClaseArma arma, Ritmo r)
        => arma == ClaseArma.Media && r.EnUltimoTiempo && r.EnTempo ? 1 : 0;

    /// <summary>
    /// Golpes totales de la LIGERA al seguir la melodia.
    ///
    /// No te cortan a mitad: terminas el compas en curso y ademas el siguiente
    /// entero. Como el arma dispara a ritmo constante, ese tiempo de mas se
    /// traduce en golpes de mas.
    /// </summary>
    /// <param name="golpesBase">Los que tocarian sin melodia.</param>
    /// <param name="tiemposRestantesDelCompas">Lo que queda del compas actual.</param>
    /// <param name="tiemposPorCompas">4 en un 4/4.</param>
    /// <param name="golpesPorTiempo">Ritmo del arma. La ligera toca corcheas: 2 por tiempo.</param>
    /// <returns>
    /// Total de golpes y cuantos de ellos caen en el compas extra, que son los
    /// que valen el 25%.
    /// </returns>
    public static (int Total, int AlVeinticinco) GolpesConMelodia(
        int golpesBase, int tiemposRestantesDelCompas,
        int tiemposPorCompas = 4, double golpesPorTiempo = 2.0)
    {
        if (golpesBase <= 0) return (0, 0);

        int cabenEnLoQueQueda = (int)Math.Floor(Math.Max(0, tiemposRestantesDelCompas) * golpesPorTiempo);
        int cabenEnElExtra = (int)Math.Floor(tiemposPorCompas * golpesPorTiempo * CompasesExtraMaximos);

        // La melodia da tiempo; el arma lo llena a su ritmo.
        int total = cabenEnLoQueQueda + cabenEnElExtra;

        // Pero nunca menos de los que te tocaban de base.
        total = Math.Max(total, golpesBase);

        int alVeinticinco = Math.Max(0, total - cabenEnLoQueQueda);
        return (total, alVeinticinco);
    }
}

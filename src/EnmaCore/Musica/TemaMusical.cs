namespace Enma.Core.Musica;

/// <summary>
/// Un tramo de la cancion, con su multiplicador de Carga Musical.
/// Ver docs/musica.md  3 y  4.
/// </summary>
public readonly record struct SeccionMusical(
    string Nombre, double Inicio, double Multiplicador)
{
    public static SeccionMusical Calma(double inicio) => new("calma", inicio, 0.5);
    public static SeccionMusical Base(double inicio) => new("base", inicio, 1.0);
    public static SeccionMusical Estribillo(double inicio) => new("estribillo", inicio, 1.8);
}

/// <summary>
/// Datos de una cancion. Es el puente entre el audio y el nucleo.
///
/// LO QUE SE CALCULA (gratis, solo con el BPM y el compas):
///   · el PULSO    — donde cae cada tiempo            → capa de la MEDIA
///   · el COMPAS   — donde cae cada primer tiempo     → capa de la PESADA
///
/// LO QUE HAY QUE GRABAR A MANO:
///   · la MELODIA  — una lista de instantes           → capa de la LIGERA
///
/// Y nada mas. Ni alturas, ni duraciones, ni nombres de nota: solo cuando
/// empieza cada nota de la linea melodica. Es exactamente lo que produce un
/// detector de inicios, asi que marcar una cancion es revisar, no transcribir.
/// </summary>
public sealed class TemaMusical
{
    public required string Id { get; init; }

    /// <summary>Pulsaciones por minuto. De aqui salen el pulso y el compas.</summary>
    public required double Bpm { get; init; }

    /// <summary>4 en un 4/4, 3 en un 3/4, 6 en un 6/8. Varia por cancion.</summary>
    public int TiemposPorCompas { get; init; } = 4;

    /// <summary>Desplazamiento del primer tiempo respecto al inicio del archivo.</summary>
    public double Offset { get; init; }

    /// <summary>Secciones, en orden. Dan el multiplicador de CM segun el momento.</summary>
    public List<SeccionMusical> Secciones { get; } = new();

    /// <summary>
    /// Instantes de inicio de nota de la linea melodica, en segundos y en orden.
    /// ES EL UNICO DATO QUE HAY QUE MARCAR A MANO.
    /// </summary>
    public List<double> PulsosDeMelodia { get; } = new();

    // ------------------------------------------------------------ derivados

    public double DuracionTiempo => 60.0 / Bpm;
    public double DuracionCompas => DuracionTiempo * TiemposPorCompas;

    /// <summary>Indice del compas en el que cae un instante, empezando en 0.</summary>
    public int CompasEn(double t)
        => (int)Math.Floor(Math.Max(0, t - Offset) / DuracionCompas);

    /// <summary>Indice del tiempo dentro de su compas, empezando en 0.</summary>
    public int TiempoEn(double t)
    {
        double dentro = Math.Max(0, t - Offset) % DuracionCompas;
        return (int)Math.Floor(dentro / DuracionTiempo);
    }

    /// <summary>Instante en que empieza el compas que contiene a t.</summary>
    public double InicioDelCompas(double t) => Offset + CompasEn(t) * DuracionCompas;

    /// <summary>
    /// Instante en que acaba el compas SIGUIENTE al que contiene a t.
    /// Es el limite de la extension de la melodia de la ligera: terminas el
    /// compas en curso y ademas el siguiente, y ahi se corta.
    /// </summary>
    public double FinDeLaExtension(double t)
        => InicioDelCompas(t) + DuracionCompas * (1 + BonusRitmico.CompasesExtraMaximos);

    /// <summary>Desfase con signo respecto al tiempo (pulso) mas cercano.</summary>
    public double DesfaseAlPulso(double t)
    {
        double rel = t - Offset;
        double masCercano = Math.Round(rel / DuracionTiempo) * DuracionTiempo;
        return rel - masCercano;
    }

    /// <summary>Desfase con signo respecto al inicio de compas mas cercano.</summary>
    public double DesfaseAlCompas(double t)
    {
        double rel = t - Offset;
        double masCercano = Math.Round(rel / DuracionCompas) * DuracionCompas;
        return rel - masCercano;
    }

    /// <summary>
    /// Desfase con signo respecto al pulso de melodia mas cercano.
    /// Devuelve null si la cancion no tiene melodia grabada — en ese caso la
    /// ligera se alarga a su propio ritmo y no hay nada que juzgar.
    /// </summary>
    public double? DesfaseALaMelodia(double t)
    {
        if (PulsosDeMelodia.Count == 0) return null;

        double mejor = double.MaxValue;
        foreach (double p in PulsosDeMelodia)
        {
            double d = t - p;
            if (Math.Abs(d) < Math.Abs(mejor)) mejor = d;
        }
        return mejor;
    }

    /// <summary>Multiplicador de CM de la seccion en la que cae un instante.</summary>
    public double MultiplicadorEn(double t)
    {
        double m = 1.0;
        foreach (var s in Secciones)
        {
            if (s.Inicio > t) break;
            m = s.Multiplicador;
        }
        return m;
    }

    /// <summary>true si el tema trae melodia marcada.</summary>
    public bool TieneMelodia => PulsosDeMelodia.Count > 0;

    // -------------------------------------------------------------- serializacion

    /// <summary>
    /// Lee el JSON que escribe el grabador (proto/grabador-temas).
    ///
    /// Se hace a mano y no con System.Text.Json para que el nucleo no arrastre
    /// dependencias: tiene que poder compilarse contra cualquier runtime.
    /// </summary>
    public static TemaMusical DesdeJson(string json)
    {
        string Cadena(string clave)
        {
            int i = json.IndexOf($"\"{clave}\"", StringComparison.Ordinal);
            if (i < 0) return "";
            int c = json.IndexOf(':', i);
            if (c < 0) return "";
            int fin = json.IndexOfAny([',', '}', ']'], c + 1);
            if (fin < 0) fin = json.Length;
            return json[(c + 1)..fin].Trim().Trim('"');
        }

        double Numero(string clave, double porDefecto)
            => double.TryParse(Cadena(clave), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : porDefecto;

        var tema = new TemaMusical
        {
            Id = Cadena("id"),
            Bpm = Numero("bpm", 120),
            TiemposPorCompas = (int)Numero("tiemposPorCompas", 4),
            Offset = Numero("offset", 0)
        };

        // La melodia es una lista plana de numeros.
        int m = json.IndexOf("\"melodia\"", StringComparison.Ordinal);
        if (m >= 0)
        {
            int abre = json.IndexOf('[', m);
            int cierra = json.IndexOf(']', abre + 1);
            if (abre >= 0 && cierra > abre)
            {
                foreach (var trozo in json[(abre + 1)..cierra].Split(','))
                {
                    if (double.TryParse(trozo.Trim(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var v))
                        tema.PulsosDeMelodia.Add(v);
                }
            }
        }

        tema.PulsosDeMelodia.Sort();
        return tema;
    }

    /// <summary>Escribe el mismo formato que lee el grabador.</summary>
    public string AJson()
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var melodia = string.Join(", ", PulsosDeMelodia.Select(x => x.ToString("F4", ci)));
        return $$"""
        {
          "id": "{{Id}}",
          "bpm": {{Bpm.ToString("F2", ci)}},
          "tiemposPorCompas": {{TiemposPorCompas}},
          "offset": {{Offset.ToString("F4", ci)}},
          "secciones": [],
          "melodia": [{{melodia}}]
        }
        """;
    }

    // -------------------------------------------------- puente con el nucleo

    /// <summary>
    /// Convierte un instante de audio en el <see cref="Ritmo"/> que consume el
    /// nucleo. Aqui acaba el reloj continuo y empieza el discreto.
    ///
    /// Cada arma se juzga contra SU capa: la media contra el pulso, la pesada
    /// contra el compas, y la ligera contra la melodia si la hay.
    /// </summary>
    public Ritmo Evaluar(double t, ClaseArma arma, double ventana = 0.12,
        double inicioDeLaAccion = double.NaN)
    {
        double desfase = BonusRitmico.CapaDe(arma) switch
        {
            CapaMusical.Pulso => DesfaseAlPulso(t),
            CapaMusical.Compas => DesfaseAlCompas(t),
            CapaMusical.Melodia => DesfaseALaMelodia(t) ?? DesfaseAlPulso(t),
            _ => DesfaseAlPulso(t)
        };

        // "Pasado el compas" se mide contra donde EMPEZO la accion, no contra
        // el instante actual: es lo que distingue el compas propio del extra.
        bool pasado = !double.IsNaN(inicioDeLaAccion)
                      && CompasEn(t) > CompasEn(inicioDeLaAccion);

        return new Ritmo(
            Desfase: desfase,
            EnTempo: Math.Abs(DesfaseAlPulso(t)) <= ventana,
            EnCompas: Math.Abs(DesfaseAlCompas(t)) <= ventana,
            PasadoElCompas: pasado,
            TiempoDelCompas: TiempoEn(t),
            TiemposPorCompas: TiemposPorCompas);
    }
}

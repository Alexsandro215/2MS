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
/// <summary>
/// Un tramo del mapa de tempo: desde donde manda, a que BPM y en que compas.
/// Una cancion de verdad cambia de compas varias veces.
/// </summary>
public readonly record struct Tramo(double Desde, double Bpm, int TiemposPorCompas)
{
    public double DuracionTiempo => 60.0 / Bpm;
    public double DuracionCompas => DuracionTiempo * TiemposPorCompas;
}

public sealed class TemaMusical
{
    public required string Id { get; init; }

    /// <summary>Pulsaciones por minuto. De aqui salen el pulso y el compas.</summary>
    public required double Bpm { get; init; }

    /// <summary>4 en un 4/4, 3 en un 3/4, 6 en un 6/8. Varia por cancion.</summary>
    public int TiemposPorCompas { get; init; } = 4;

    /// <summary>Desplazamiento del primer tiempo respecto al inicio del archivo.</summary>
    public double Offset { get; init; }

    /// <summary>
    /// MAPA DE TEMPO. Los tramos en los que cambia el compas o el BPM.
    ///
    /// Si esta vacio se usa <see cref="Bpm"/> y <see cref="TiemposPorCompas"/>
    /// para toda la cancion, que es como funcionaba antes de que existiera.
    /// </summary>
    public List<Tramo> Rejilla { get; } = new();

    /// <summary>Secciones, en orden. Dan el multiplicador de CM segun el momento.</summary>
    public List<SeccionMusical> Secciones { get; } = new();

    /// <summary>
    /// Instantes de inicio de nota de la linea melodica, en segundos y en orden.
    /// ES EL UNICO DATO QUE HAY QUE MARCAR A MANO.
    /// </summary>
    public List<double> PulsosDeMelodia { get; } = new();

    // ---------------------------------------------------- el mapa de tempo

    /// <summary>La rejilla real. Si no hay mapa, uno solo con el BPM suelto.</summary>
    private List<Tramo> Tramos => Rejilla.Count > 0
        ? Rejilla
        : [new Tramo(Offset, Bpm, TiemposPorCompas)];

    /// <summary>
    /// Indice del tramo cuya rejilla manda en un instante.
    ///
    /// ⚠️ Se decide por el ORIGEN REAL de cada tramo, no por el borde que se
    /// coloco. Entre el borde y el origen sigue sonando el compas del tramo
    /// anterior: decidirlo por el borde hacia que el instante cayera en un
    /// compas que aun no habia empezado, y el anterior quedaba inalcanzable.
    /// </summary>
    public int TramoEn(double t) => TramoEn(t, Origenes());

    private static int TramoEn(double t, double[] o)
    {
        int idx = 0;
        for (int i = 1; i < o.Length; i++)
        {
            if (o[i] <= t) idx = i;
            else break;
        }
        return idx;
    }

    /// <summary>
    /// ORIGEN REAL de la rejilla de cada tramo.
    ///
    /// El borde que se coloca en el grabador dice QUE compas cambia, no
    /// exactamente cuando: el tramo arranca en la primera linea de compas del
    /// anterior que caiga en su borde o despues. Asi el metronomo nunca da un
    /// salto, y colocar el borde a ojo basta.
    ///
    /// Tiene que calcularse IGUAL que en proto/grabador-temas, o el nucleo y
    /// el editor dirian cosas distintas del mismo archivo.
    /// </summary>
    private double[] Origenes()
    {
        var tr = Tramos;
        var o = new double[tr.Count];
        if (tr.Count == 0) return o;

        o[0] = tr[0].Desde;
        for (int i = 1; i < tr.Count; i++)
        {
            double compasAnt = tr[i - 1].DuracionCompas;
            double n = Math.Ceiling((tr[i].Desde - o[i - 1]) / compasAnt - 0.001);
            o[i] = o[i - 1] + Math.Max(0, n) * compasAnt;
        }
        return o;
    }

    /// <summary>Cuantos compases enteros hay antes de cada tramo.</summary>
    private int[] CompasesAntes(double[] o)
    {
        var tr = Tramos;
        var c = new int[tr.Count];
        for (int i = 1; i < tr.Count; i++)
        {
            double largo = o[i] - o[i - 1];
            c[i] = c[i - 1] + (int)Math.Round(largo / tr[i - 1].DuracionCompas);
        }
        return c;
    }

    // ------------------------------------------------------------ derivados

    public double DuracionTiempo => 60.0 / Bpm;
    public double DuracionCompas => DuracionTiempo * TiemposPorCompas;

    /// <summary>Duracion de un compas EN ESE PUNTO de la cancion.</summary>
    public double DuracionCompasEn(double t) => Tramos[TramoEn(t)].DuracionCompas;

    /// <summary>Duracion de un tiempo EN ESE PUNTO de la cancion.</summary>
    public double DuracionTiempoEn(double t) => Tramos[TramoEn(t)].DuracionTiempo;

    /// <summary>Indice del compas en el que cae un instante, empezando en 0.</summary>
    public int CompasEn(double t)
    {
        var o = Origenes();
        var antes = CompasesAntes(o);
        int i = TramoEn(t, o);
        double dentro = Math.Max(0, t - o[i]);
        return antes[i] + (int)Math.Floor(dentro / Tramos[i].DuracionCompas);
    }

    /// <summary>Indice del tiempo dentro de su compas, empezando en 0.</summary>
    public int TiempoEn(double t)
    {
        var o = Origenes();
        int i = TramoEn(t, o);
        var tr = Tramos[i];
        double dentro = Math.Max(0, t - o[i]) % tr.DuracionCompas;
        return (int)Math.Floor(dentro / tr.DuracionTiempo);
    }

    /// <summary>Instante en que empieza el compas que contiene a t.</summary>
    public double InicioDelCompas(double t)
    {
        var o = Origenes();
        int i = TramoEn(t, o);
        double bar = Tramos[i].DuracionCompas;
        return o[i] + Math.Floor(Math.Max(0, t - o[i]) / bar) * bar;
    }

    /// <summary>Principio y final de un compas, por su indice.</summary>
    public (double Desde, double Hasta) LimitesDelCompas(int compas)
    {
        var tr = Tramos;
        var o = Origenes();
        var antes = CompasesAntes(o);

        int i = 0;
        for (int k = 0; k < tr.Count; k++)
            if (antes[k] <= compas) i = k;

        double bar = tr[i].DuracionCompas;
        double a = o[i] + (compas - antes[i]) * bar;
        return (a, a + bar);
    }

    /// <summary>
    /// Instante en que acaba el compas SIGUIENTE al que contiene a t.
    /// Es el limite de la extension de la melodia de la ligera: terminas el
    /// compas en curso y ademas el siguiente, y ahi se corta.
    ///
    /// Por los LIMITES REALES de ese compas siguiente: si es de 6/4 dura la
    /// mitad mas que uno de 4/4, y si cae ya en otro tramo, se mide con el suyo.
    /// </summary>
    public double FinDeLaExtension(double t)
        => LimitesDelCompas(CompasEn(t) + BonusRitmico.CompasesExtraMaximos).Hasta;

    /// <summary>Desfase con signo respecto al tiempo (pulso) mas cercano.</summary>
    public double DesfaseAlPulso(double t) => DesfaseALaRejilla(t, alCompas: false);

    /// <summary>Desfase con signo respecto al inicio de compas mas cercano.</summary>
    public double DesfaseAlCompas(double t) => DesfaseALaRejilla(t, alCompas: true);

    /// <summary>
    /// Desfase con signo respecto a la linea mas cercana de la rejilla que
    /// SUENA en ese instante: cada tiempo, o cada primer tiempo.
    ///
    /// ⚠️ Antes se medía con el BPM y el compas sueltos, como si la cancion
    /// entera fuera un solo tramo. En «Missin Power» eso daba por fallados
    /// 189 de sus 330 primeros tiempos.
    ///
    /// Basta con redondear dentro del tramo que suena: el origen del tramo
    /// siguiente cae en una linea de compas de este por construccion, asi que
    /// la linea del cambio tambien se encuentra.
    /// </summary>
    private double DesfaseALaRejilla(double t, bool alCompas)
    {
        var o = Origenes();
        int i = TramoEn(t, o);
        double paso = alCompas ? Tramos[i].DuracionCompas : Tramos[i].DuracionTiempo;
        double rel = t - o[i];
        return rel - Math.Round(rel / paso) * paso;
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

    // ---------------------------------------------------- bonus experto

    /// <summary>
    /// SEGUNDA VOZ. Una linea melodica de fondo, densa, que solo aparece en
    /// algunos tramos.
    ///
    /// ✅ NO HACE FALTA MARCAR NADA: un compas es de BONUS EXPERTO si tiene
    /// notas de esta voz. La marca se deduce del contenido.
    /// </summary>
    public List<double> PulsosDeFondo { get; } = new();

    /// <summary>Notas de la segunda voz que caen dentro de un compas, en orden.</summary>
    public List<double> FondoDelCompas(int compas)
    {
        // ⚠️ Por los LIMITES REALES del compas, no por un compas fijo: una
        // cancion con 41 tramos de 3/4, 4/4 y 6/4 tiene compases de duraciones
        // distintas, y el bonus experto se define compas a compas.
        var (a, b) = LimitesDelCompas(compas);
        var r = new List<double>();
        foreach (double p in PulsosDeFondo)
            if (p >= a && p < b) r.Add(p);
        r.Sort();
        return r;
    }

    /// <summary>true si ese compas trae segunda voz, o sea si es un bonus experto.</summary>
    public bool EsCompasExperto(int compas) => FondoDelCompas(compas).Count > 0;

    /// <summary>
    /// Como salio un intento sobre un compas de bonus experto.
    /// </summary>
    /// <param name="Elegible">el compas tiene segunda voz</param>
    /// <param name="Enganchado">el primer golpe cayo en la PRIMERA nota del compas</param>
    /// <param name="Gratis">ataques que NO gastan BA</param>
    /// <param name="Completo">se clavo el compas entero, de principio a fin</param>
    public readonly record struct IntentoExperto(
        bool Elegible, bool Enganchado, int Gratis, bool Completo);

    /// <summary>
    /// Resuelve un compas de bonus experto.
    ///
    /// LAS REGLAS, tal cual se decidieron:
    ///
    ///   · Hay que ENGANCHAR en la PRIMERA nota del compas. Entrar a mitad no
    ///     da nada: el compas gratis se coge desde su principio o no se coge.
    ///   · A partir de ahi, cada golpe tiene que caer en la nota SIGUIENTE.
    ///     Mientras aciertes, esos ataques son GRATIS: no gastan BA.
    ///   · Al primer fallo se acaba el regalo y empiezan a gastarse los BA
    ///     reales desde ese golpe. Lo que ya te llevaste, te lo llevaste: el
    ///     premio escala con lo lejos que llegues, no es todo o nada.
    ///   · Solo vale EL COMPAS en el que estas, de principio a fin. No se
    ///     encadena con el siguiente.
    ///
    /// ⚠️ Y una exclusion: terminar bien un compas experto NO da ademas la
    /// extension de la primera melodia (ver BonusRitmico.CompasesExtraMaximos).
    /// Son dos premios al mismo esfuerzo y no se acumulan.
    /// </summary>
    public IntentoExperto ResolverExperto(
        int compas, IReadOnlyList<double> golpes, double? techo = null)
    {
        var notas = FondoDelCompas(compas);
        if (notas.Count == 0) return new IntentoExperto(false, false, 0, false);
        if (golpes.Count == 0) return new IntentoExperto(true, false, 0, false);

        double tope = techo ?? VentanaMelodia;

        // Enganchar = el primer golpe cae en la PRIMERA nota del compas.
        if (Math.Abs(golpes[0] - notas[0]) > VentanaDeFondo(0, notas, tope))
            return new IntentoExperto(true, false, 0, false);

        int gratis = 1;
        for (int g = 1; g < golpes.Count && gratis < notas.Count; g++)
        {
            int siguiente = gratis;
            if (Math.Abs(golpes[g] - notas[siguiente]) > VentanaDeFondo(siguiente, notas, tope))
                break;                                  // fallo: se acaba aqui
            gratis++;
        }

        return new IntentoExperto(true, true, gratis, gratis == notas.Count);
    }

    /// <summary>Misma regla de ventana que la melodia, pero entre las notas del compas.</summary>
    private static double VentanaDeFondo(int i, List<double> notas, double techo)
    {
        double hueco = double.MaxValue;
        if (i > 0) hueco = Math.Min(hueco, notas[i] - notas[i - 1]);
        if (i < notas.Count - 1) hueco = Math.Min(hueco, notas[i + 1] - notas[i]);
        return hueco == double.MaxValue ? techo : Math.Min(techo, hueco / 2.0);
    }

    // ------------------------------------------------- la cadena de la ligera

    /// <summary>
    /// Techo de la ventana de acierto, en segundos. Es UN solo numero para toda
    /// la cancion; cada nota lo recorta segun lo pegada que este a su vecina.
    ///
    /// Sale del "toleranciaMs" del grabador, y es el que usan la cadena y el
    /// bonus experto cuando no se les pasa otro: el ensayo del grabador juzga
    /// con este mismo numero, y el nucleo tiene que decir lo mismo.
    /// </summary>
    public double VentanaMelodia { get; init; } = 0.08;

    /// <summary>
    /// Ventana EFECTIVA de una nota: el techo, recortado a la mitad del hueco
    /// con su vecina mas cercana.
    ///
    /// ✅ Con este recorte dos ventanas pueden TOCARSE pero nunca solaparse, y
    /// eso garantiza que siempre esta definido a que nota golpeo el jugador.
    /// Sin el, en un pasaje de fusas —43 ms de hueco— una ventana de 80 ms
    /// cubriria tres notas a la vez y no habria forma de saber cual era.
    ///
    /// Un solo numero que configurar, y no se puede romper.
    /// </summary>
    public double VentanaDe(int i, double techo)
    {
        if (i < 0 || i >= PulsosDeMelodia.Count) return 0.0;

        double hueco = double.MaxValue;
        if (i > 0)
            hueco = Math.Min(hueco, PulsosDeMelodia[i] - PulsosDeMelodia[i - 1]);
        if (i < PulsosDeMelodia.Count - 1)
            hueco = Math.Min(hueco, PulsosDeMelodia[i + 1] - PulsosDeMelodia[i]);

        // Una nota sola no tiene con quien chocar: se queda con el techo.
        return hueco == double.MaxValue ? techo : Math.Min(techo, hueco / 2.0);
    }

    /// <summary>
    /// Indice de la nota de melodia que cae dentro de la ventana de un
    /// instante, o -1 si ese golpe no pertenece a ninguna.
    /// </summary>
    public int NotaEn(double t, double techo)
    {
        int mejor = -1;
        double mejorD = double.MaxValue;
        for (int i = 0; i < PulsosDeMelodia.Count; i++)
        {
            double d = Math.Abs(t - PulsosDeMelodia[i]);
            if (d < mejorD) { mejorD = d; mejor = i; }
        }
        return mejor >= 0 && mejorD <= VentanaDe(mejor, techo) ? mejor : -1;
    }

    /// <summary>
    /// Cuantos basicos seguidos aguanta la CADENA de la ligera.
    ///
    /// La cadena vive mientras cada basico caiga en la nota de melodia
    /// INMEDIATAMENTE SIGUIENTE a la del basico anterior. Se corta por dos
    /// motivos, y solo por esos dos:
    ///
    ///   · desafinas      — el golpe no cae en ninguna nota
    ///   · te paras       — dejas pasar una nota sin pulsarla
    ///
    /// ⚠️ NO la corta fallarle al enemigo. La cadena se juzga sobre lo que
    /// PULSAS. Es lo que permite que la ligera siga siendo la respuesta a un
    /// rival con evasion alta, que es justo donde mas se falla.
    ///
    /// Los silencios propios de la cancion tampoco la cortan: si no hay nota,
    /// no hay nada que dejar pasar. Por eso se cuenta por NOTAS y no por
    /// tiempo.
    ///
    /// El primer golpe puede engancharse a cualquier nota: la cadena empieza
    /// donde tu decidas empezarla.
    /// </summary>
    /// <returns>
    /// Cuantos golpes de <paramref name="golpes"/> entraron en la cadena antes
    /// de romperse. Si devuelve el total, la cadena aguanto entera.
    /// </returns>
    public int GolpesEnCadena(IReadOnlyList<double> golpes, double? techo = null)
    {
        if (golpes.Count == 0 || !TieneMelodia) return 0;

        double tope = techo ?? VentanaMelodia;
        int anterior = -1;
        for (int g = 0; g < golpes.Count; g++)
        {
            int nota = NotaEn(golpes[g], tope);

            if (nota < 0) return g;                        // desafino
            if (anterior >= 0 && nota != anterior + 1)
                return g;                                  // se salto notas

            anterior = nota;
        }
        return golpes.Count;
    }

    /// <summary>
    /// true si la cadena aguanto los golpes enteros. Es la condicion para
    /// cobrar el compas extra.
    /// </summary>
    public bool CadenaCompleta(IReadOnlyList<double> golpes, double? techo = null)
        => golpes.Count > 0 && GolpesEnCadena(golpes, techo) == golpes.Count;

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

        // El mapa de tempo, si lo trae: [{"desde":..,"bpm":..,"compas":..}, ...]
        var rejilla = new List<Tramo>();
        int ri = json.IndexOf("\"rejilla\"", StringComparison.Ordinal);
        if (ri >= 0)
        {
            int abreR = json.IndexOf('[', ri);
            int cierraR = json.IndexOf(']', abreR + 1);
            if (abreR >= 0 && cierraR > abreR)
            {
                foreach (var trozo in json[(abreR + 1)..cierraR].Split('}'))
                {
                    int llave = trozo.IndexOf('{');
                    if (llave < 0) continue;
                    string obj = trozo[(llave + 1)..];
                    double Campo(string clave, double def)
                    {
                        int k = obj.IndexOf($"\"{clave}\"", StringComparison.Ordinal);
                        if (k < 0) return def;
                        int dp = obj.IndexOf(':', k);
                        if (dp < 0) return def;
                        int fin = obj.IndexOfAny([',', '}'], dp + 1);
                        if (fin < 0) fin = obj.Length;
                        return double.TryParse(obj[(dp + 1)..fin].Trim(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
                    }
                    rejilla.Add(new Tramo(
                        Campo("desde", 0), Campo("bpm", 120), (int)Campo("compas", 4)));
                }
            }
        }

        var tema = new TemaMusical
        {
            Id = Cadena("id"),
            // Si hay rejilla, el BPM suelto es solo un respaldo.
            Bpm = rejilla.Count > 0 ? rejilla[0].Bpm : Numero("bpm", 120),
            TiemposPorCompas = rejilla.Count > 0
                ? rejilla[0].TiemposPorCompas : (int)Numero("tiemposPorCompas", 4),
            Offset = Numero("offset", 0),
            VentanaMelodia = Numero("toleranciaMs", 80) / 1000.0
        };

        // Las dos melodias son listas planas de numeros.
        void LeerLista(string clave, List<double> destino)
        {
            int m = json.IndexOf($"\"{clave}\"", StringComparison.Ordinal);
            if (m < 0) return;
            int abre = json.IndexOf('[', m);
            int cierra = json.IndexOf(']', abre + 1);
            if (abre < 0 || cierra <= abre) return;
            foreach (var trozo in json[(abre + 1)..cierra].Split(','))
            {
                if (double.TryParse(trozo.Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var v))
                    destino.Add(v);
            }
        }

        tema.Rejilla.AddRange(rejilla);
        LeerLista("melodiaFondo", tema.PulsosDeFondo);
        LeerLista("melodia", tema.PulsosDeMelodia);

        tema.PulsosDeMelodia.Sort();
        tema.PulsosDeFondo.Sort();
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

        // Los del compas QUE SUENA, no los sueltos del tema: en un 3/4 el
        // ultimo tiempo es el tercero, y en un 6/4 el cuarto no lo es.
        return new Ritmo(
            Desfase: desfase,
            EnTempo: Math.Abs(DesfaseAlPulso(t)) <= ventana,
            EnCompas: Math.Abs(DesfaseAlCompas(t)) <= ventana,
            PasadoElCompas: pasado,
            TiempoDelCompas: TiempoEn(t),
            TiemposPorCompas: Tramos[TramoEn(t)].TiemposPorCompas);
    }
}

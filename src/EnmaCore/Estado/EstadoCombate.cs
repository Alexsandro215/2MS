namespace Enma.Core.Estado;

/// <summary>
/// Terreno encantado sobre una casilla concreta de una linea concreta.
///
/// Es una REGLA ANCLADA A UN SITIO: la crean kits como Floristas del Hollins
/// o Madrerick, dura N turnos, y otras reglas pueden destruirla
/// (Madrerick Q EVOL elimina una casilla de buffs del enemigo).
/// </summary>
public sealed class Terreno
{
    public required string Id { get; init; }
    public required string Tipo { get; init; }
    public required Bando BandoDeLaLinea { get; init; }
    public required int Linea { get; init; }
    public required Casilla Casilla { get; init; }
    public int TurnosRestantes { get; set; }

    /// <summary>true si sus efectos son beneficiosos para quien lo pisa.</summary>
    public bool EsBuff { get; init; }

    /// <summary>Reglas que aporta mientras esta activo.</summary>
    public List<Reglas.Regla> Reglas { get; } = new();
}

/// <summary>
/// Lo que ha pasado dentro del turno actual.
///
/// Hace falta porque varios kits leen el pasado inmediato:
///   - Korkakov W EVOL: "si FALLAS tu spell anterior..."
///   - Jaens Jeans R EVOL: "recuperas todo el CM GASTADO DE ESTE TURNO"
///   - Hallen Q EVOL: "1 carga por cada HABILIDAD USADA este turno"
/// Sin esto, esas habilidades no se pueden expresar como datos.
/// </summary>
public sealed class HistorialTurno
{
    public double CmGastado { get; set; }
    public int HabilidadesUsadas { get; set; }
    public int BasicosLanzados { get; set; }
    public int ImpactosAcertados { get; set; }
    public int ImpactosFallados { get; set; }

    /// <summary>true si la ultima habilidad resuelta fallo por completo.</summary>
    public bool UltimaHabilidadFallo { get; set; }

    /// <summary>Contadores libres para casos que no merecen campo propio.</summary>
    public Dictionary<string, double> Marcadores { get; } = new();

    public double Marcador(string clave) => Marcadores.GetValueOrDefault(clave);

    public void Reiniciar()
    {
        CmGastado = 0;
        HabilidadesUsadas = 0;
        BasicosLanzados = 0;
        ImpactosAcertados = 0;
        ImpactosFallados = 0;
        UltimaHabilidadFallo = false;
        Marcadores.Clear();
    }
}

/// <summary>
/// Barra de Carga Musical de UN bando, compartida por todos sus miembros.
///
/// Cada bando tiene la suya y con las mismas reglas: la llena su propio
/// bando, la gasta su propio bando y su CLASICAL es solo suyo. Antes habia
/// una sola, y una habilidad enemiga se pagaba con el CM del jugador.
/// </summary>
public sealed class BarraCm
{
    public double Actual { get; set; }
    public double Max { get; set; } = 12.0;
    public bool ClasicalActivo { get; set; }

    public double Pct => Max <= 0 ? 0.0 : Actual / Max;

    public void Sumar(double cantidad) => Actual = Math.Clamp(Actual + cantidad, 0, Max);
}

/// <summary>
/// Estado completo de un combate. Es lo que el motor de reglas consulta.
///
/// El nucleo recibe un estado y una accion, y devuelve un estado nuevo mas una
/// lista de eventos. La presentacion observa esos eventos; nunca los produce.
/// </summary>
public sealed class EstadoCombate
{
    public List<Combatiente> Combatientes { get; } = new();
    public List<Terreno> Terrenos { get; } = new();

    public BarraCm BarraAliada { get; } = new();
    public BarraCm BarraEnemiga { get; } = new();

    public BarraCm Barra(Bando b) => b == Bando.Aliado ? BarraAliada : BarraEnemiga;

    // La barra aliada es la del jugador, y la que lee casi todo: estos
    // atajos la dejan a mano sin tener que nombrar el bando.

    /// <summary>CM de la barra aliada.</summary>
    public double CmActual { get => BarraAliada.Actual; set => BarraAliada.Actual = value; }
    public double CmMax { get => BarraAliada.Max; set => BarraAliada.Max = value; }
    public double CmPct => BarraAliada.Pct;

    /// <summary>CLASICAL del bando aliado.</summary>
    public bool ClasicalActivo { get => BarraAliada.ClasicalActivo; set => BarraAliada.ClasicalActivo = value; }

    /// <summary>Ciclo = turno completo del bando aliado + turno completo del enemigo.</summary>
    public int Ciclo { get; set; }
    public int Turno { get; set; }
    public Bando BandoActivo { get; set; } = Bando.Aliado;

    public HistorialTurno Historial { get; } = new();

    /// <summary>Reglas vigentes que no vienen de un terreno (pasivas, emblemas, buffs...).</summary>
    public List<Reglas.Regla> ReglasActivas { get; } = new();

    /// <summary>Fuente de azar. Inyectada para que las simulaciones sean reproducibles.</summary>
    public Combate.IAleatorio Rng { get; set; } = new Combate.AleatorioSemilla(0);

    // ---- el entorno ----

    /// <summary>
    /// Escondites del entorno, de 0 a 3. Los pelean los DOS bandos: uno por
    /// personaje, y quien llega primero lo ocupa. Un entorno con 0 es un
    /// entorno agresivo. Ver tablero.md  5.
    /// </summary>
    public int Escondites { get; set; }

    /// <summary>Flag de escape por combate: contra jefes y villanos no se huye.</summary>
    public bool SePuedeHuir { get; set; } = true;

    /// <summary>⚠️ Provisional. Probabilidad de que salga bien un intento de huida.</summary>
    public double ProbabilidadDeHuida { get; set; } = 0.5;

    public int EsconditesLibres => Escondites - Combatientes.Count(c => c.Zona == Zona.Escondite);

    public Combatiente? Buscar(string id) => Combatientes.Find(c => c.Id == id);

    public IEnumerable<Combatiente> Vivos => Combatientes.Where(c => !c.EnKo);

    /// <summary>Todos los del bando que siguen vivos, banquillo incluido.</summary>
    public IEnumerable<Combatiente> Del(Bando b) => Vivos.Where(c => c.Bando == b);

    /// <summary>Vivos que estan en el combate: el front y los escondidos, no el banquillo.</summary>
    public IEnumerable<Combatiente> EnCampo => Vivos.Where(c => c.Zona != Zona.Banquillo);

    public IEnumerable<Combatiente> DelCampo(Bando b) => EnCampo.Where(c => c.Bando == b);

    /// <summary>
    /// Todas las reglas vigentes: las activas mas las que aportan los terrenos.
    /// El evaluador no distingue de donde vienen.
    /// </summary>
    public IEnumerable<Reglas.Regla> TodasLasReglas()
    {
        foreach (var r in ReglasActivas) yield return r;
        foreach (var t in Terrenos)
            foreach (var r in t.Reglas)
                yield return r;
    }

    /// <summary>Terreno que ocupa un combatiente ahora mismo, si lo hay.</summary>
    public Terreno? TerrenoDe(Combatiente c) => Terrenos.Find(t =>
        t.BandoDeLaLinea == c.Bando && t.Linea == c.Linea && t.Casilla == c.Posicion);

    /// <summary>
    /// Recalcula la capacidad de CM de los dos bandos y recorta el excedente.
    /// Activo x1, K.O. x0.5, muerto x0. Ver manual-tecnico.md  6.1.
    /// </summary>
    public void RecalcularCm()
    {
        foreach (var b in new[] { Bando.Aliado, Bando.Enemigo })
        {
            var barra = Barra(b);
            barra.Max = Musica.CargaMusical.Capacidad(
                Combatientes.Where(c => c.Bando == b && !c.EnKo).Select(c => c.Storage),
                Combatientes.Where(c => c.Bando == b && c.EnKo && !c.Muerto).Select(c => c.Storage));
            barra.Actual = Math.Min(barra.Actual, barra.Max);
        }
    }
}

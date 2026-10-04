using System.Globalization;
using Enma.Core;
using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Simulacion;
using Godot;

namespace Enma.Ventana;

/// <summary>
/// La ventana de combate: el nucleo (src/EnmaCore) con una cara.
///
/// No sabe ninguna regla. Pinta el estado, convierte cada boton en una
/// Accion y se la pasa al motor; si el motor la rechaza, enseña por que. Es
/// la misma partida que el modo "jugar" de EnmaSim, contra el mismo muñeco,
/// pero con raton.
///
/// Desde la linea de ordenes, detras de "--":
///   --semilla=7                       los mismos dados que la consola
///   --guion=abrir:ligera,basicos,...  acciones seguidas, como si se pulsaran
///   --captura=ruta.png                guarda una imagen y se cierra
/// </summary>
public partial class Combate : Control
{
    private const double Tolerancia = 1e-9;

    private static readonly Color Fondo = new(0.075f, 0.082f, 0.105f);
    private static readonly Color Tarjeta = new(0.13f, 0.14f, 0.18f);
    private static readonly Color TarjetaActiva = new(0.20f, 0.18f, 0.10f);
    private static readonly Color Texto = new(0.90f, 0.91f, 0.94f);
    private static readonly Color Gris = new(0.60f, 0.63f, 0.70f);
    private static readonly Color Dorado = new(0.98f, 0.80f, 0.32f);
    private static readonly Color Verde = new(0.55f, 0.85f, 0.55f);
    private static readonly Color Rojo = new(0.95f, 0.45f, 0.45f);
    private static readonly Color Cian = new(0.45f, 0.85f, 0.95f);

    private EstadoCombate _e = null!;
    private MotorCombate _m = null!;
    private int _semilla;

    private readonly Dictionary<string, Rendimiento> _registro = new();
    private readonly Dictionary<string, double> _cmALaBarra = new();
    private readonly HashSet<string> _recienEntrados = new();
    private readonly HashSet<string> _enOfensiva = new();
    private double _cmPerdida;
    private bool _cicloEmpezado;

    /// <summary>A quien has elegido para abrir, mientras aun no ha hecho nada.</summary>
    private string? _elegido;

    // ---- la interfaz ----
    private Label _cabecera = null!;
    private Label _cm = null!;
    private Label _objetos = null!;
    private Label _orden = null!;
    private ProgressBar _barraCm = null!;
    private VBoxContainer _equipo = null!;
    private VBoxContainer _rival = null!;
    private VBoxContainer _acciones = null!;
    private RichTextLabel _bitacora = null!;
    private VBoxContainer _stats = null!;
    private Label _statsTitulo = null!;
    private Label _statsSub = null!;
    private Label _statsNota = null!;
    private CheckButton _vistaAspirable = null!;
    private SpinBox _nivel = null!;
    private SpinBox _desgaste = null!;
    private Label _desgasteEfecto = null!;
    private VBoxContainer _statsCuerpo = null!;
    private ScrollContainer _statsScroll = null!;

    /// <summary>Para las capturas: bajar el panel de stats hasta el fondo antes de sacarla.</summary>
    private bool _bajarAlCapturar;

    /// <summary>true: el panel enseña el MAX aspirable, editable. false: lo que vale ahora.</summary>
    private bool _verAspirable;

    /// <summary>De quien se ven las stats. null = de quien tiene el turno.</summary>
    private string? _inspeccionado;

    public override void _Ready()
    {
        var args = Argumentos();
        _semilla = args.TryGetValue("semilla", out var s) && int.TryParse(s, out int n)
            ? n
            : (int)(Time.GetTicksUsec() & 0xFFFF);

        GetWindow().Title = "E.N.M.A. · combate de prueba contra el muñeco";
        Theme = new Theme { DefaultFontSize = 16 };
        Construir();
        Empezar(_semilla);

        if (args.TryGetValue("guion", out var guion)) EjecutarGuion(guion);
        if (args.TryGetValue("captura", out var ruta)) _ = Capturar(ruta);
    }

    private void Empezar(int semilla)
    {
        _semilla = semilla;
        _e = EncuentrosDePrueba.ContraMuneco(semilla);
        _m = new MotorCombate(_e);

        _registro.Clear();
        _cmALaBarra.Clear();
        foreach (var c in _e.Combatientes)
        {
            _registro[c.Id] = new Rendimiento();
            _cmALaBarra[c.Id] = 0;
        }
        _recienEntrados.Clear();
        _enOfensiva.Clear();
        _cmPerdida = 0;
        _cicloEmpezado = false;
        _elegido = null;
        _inspeccionado = null;
        _secciones = [.. SeccionesDelDiseno];

        _bitacora.Clear();
        Escribir($"[b]Combate de prueba contra el muñeco[/b] · semilla {semilla}");
        Escribir("El muñeco tiene un millón de vida y no ataca nunca. Las habilidades QWER aún no tienen efecto, y el ritmo del básico no está conectado: todos los golpes cuentan como fuera de tempo.", Gris);
        Escribir($"[b]— Ciclo 1 —[/b]", Dorado);
        Refrescar();
    }

    // ------------------------------------------------------------ jugar

    /// <summary>Quien tiene el turno: el motor lo sabe, o lo has elegido tu para abrir.</summary>
    private Combatiente? Actual()
        => _m.ActorDeTurno ?? (_elegido is null ? null : _e.Buscar(_elegido));

    private void Abrir(string id)
    {
        if (_m.ActorDeTurno is null && _m.PendientesDeActuar().Any(c => c.Id == id))
            _elegido = id;
        Refrescar();
    }

    private void Hacer(Accion accion)
    {
        if (_m.Desenlace != Desenlace.EnCurso) return;

        var quien = _e.Buscar(accion.ActorId)!;
        double cmAntes = _e.CmActual;
        var r = _m.Ejecutar(accion);
        Narrar(quien, accion, r, cmAntes);

        if (r.Permitida && (r.CierraTurno || !_m.PendientesDeActuar().Any(c => c.Id == quien.Id)))
            _elegido = null;

        Avanzar();
        Refrescar();
    }

    /// <summary>
    /// Si tu bando ya ha terminado, juega el rival —el muñeco no hace nada— y
    /// empieza el ciclo siguiente.
    /// </summary>
    private void Avanzar()
    {
        if (_m.Desenlace != Desenlace.EnCurso) { Final(); return; }
        if (_e.BandoActivo != Bando.Aliado || _m.ActorDeTurno is not null) return;
        if (_m.PendientesDeActuar().Any()) return;

        _m.CerrarTurnoDeBando();
        Escribir("Turno del rival: el muñeco aguanta. No ataca.", Gris);
        _m.CerrarTurnoDeBando();

        _recienEntrados.Clear();
        _enOfensiva.Clear();
        _cicloEmpezado = false;
        Escribir($"[b]— Ciclo {_e.Ciclo + 1} —[/b]", Dorado);
    }

    // ------------------------------------------------------------ acciones

    private AtaqueBasico? Basicos(Combatiente quien)
    {
        var rival = _e.Combatientes.FirstOrDefault(c =>
            c.Bando != quien.Bando && c.Zona == Zona.Front && (!c.EnKo || c.Rematable));
        return rival is null ? null : new AtaqueBasico { ActorId = quien.Id, ObjetivoId = rival.Id };
    }

    private static readonly string[] NombresSeccion = ["Calma", "Base", "Estribillo"];

    /// <summary>
    /// Lo que multiplica la Carga Musical en cada seccion de la cancion. Sin la
    /// cancion conectada, los elige el boton; aqui se pueden cambiar.
    /// </summary>
    private static readonly double[] SeccionesDelDiseno = [0.5, 1.0, 1.8];

    private double[] _secciones = [.. SeccionesDelDiseno];

    // ------------------------------------------------------------ pantalla

    private void Construir()
    {
        var fondo = new ColorRect { Color = Fondo };
        fondo.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(fondo);

        var margen = new MarginContainer();
        margen.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (var lado in new[] { "left", "right", "top", "bottom" })
            margen.AddThemeConstantOverride($"margin_{lado}", 16);
        AddChild(margen);

        var todo = new VBoxContainer();
        todo.AddThemeConstantOverride("separation", 8);
        margen.AddChild(todo);

        var arriba = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        arriba.AddThemeConstantOverride("separation", 16);
        todo.AddChild(arriba);

        // ---- izquierda: estado, tablero y acciones ----
        var izquierda = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.45f };
        izquierda.AddThemeConstantOverride("separation", 10);
        arriba.AddChild(izquierda);

        var cabecera = new VBoxContainer();
        cabecera.AddThemeConstantOverride("separation", 4);
        _cabecera = Etiqueta("", 22, Dorado);
        cabecera.AddChild(_cabecera);

        var filaCm = new HBoxContainer();
        filaCm.AddThemeConstantOverride("separation", 10);
        filaCm.AddChild(Etiqueta("Carga Musical", 16, Gris));
        _barraCm = new ProgressBar
        {
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(340, 20),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _barraCm.AddThemeStyleboxOverride("fill", Caja(Cian, 4, 0));
        _barraCm.AddThemeStyleboxOverride("background", Caja(new Color(0.2f, 0.22f, 0.27f), 4, 0));
        filaCm.AddChild(_barraCm);
        _cm = Etiqueta("", 16);
        filaCm.AddChild(_cm);
        cabecera.AddChild(filaCm);

        _objetos = Etiqueta("", 14, Gris);
        cabecera.AddChild(_objetos);
        _orden = Etiqueta("", 14, Gris);
        _orden.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cabecera.AddChild(_orden);
        izquierda.AddChild(Panel(cabecera));

        var tablero = new HBoxContainer();
        tablero.AddThemeConstantOverride("separation", 10);
        _equipo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.6f };
        _equipo.AddThemeConstantOverride("separation", 4);
        _rival = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _rival.AddThemeConstantOverride("separation", 6);
        tablero.AddChild(_equipo);
        tablero.AddChild(_rival);
        izquierda.AddChild(tablero);

        _acciones = new VBoxContainer();
        _acciones.AddThemeConstantOverride("separation", 6);
        var panelAcciones = Panel(_acciones);
        panelAcciones.SizeFlagsVertical = SizeFlags.ExpandFill;
        izquierda.AddChild(panelAcciones);

        // ---- derecha: las stats de quien inspecciones ----
        // La cabecera es fija, asi sus controles no se rehacen mientras los
        // usas. El cuerpo si se rehace, y se desplaza si no cabe.
        _stats = new VBoxContainer();
        _stats.AddThemeConstantOverride("separation", 4);

        var filaTitulo = new HBoxContainer();
        filaTitulo.AddThemeConstantOverride("separation", 8);
        _statsTitulo = Etiqueta("", 20, Dorado);
        filaTitulo.AddChild(_statsTitulo);
        _statsSub = Etiqueta("", 14, Gris);
        _statsSub.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        filaTitulo.AddChild(_statsSub);
        filaTitulo.AddChild(Etiqueta("Actual", 14, Gris));
        _vistaAspirable = new CheckButton
        {
            FocusMode = FocusModeEnum.None,
            TooltipText = "Apagado: lo que vale cada stat a su nivel, con desgaste y reglas.\n"
                + "Encendido: el MAX aspirable, lo que valdría a nivel 18. Ahí se puede editar.",
        };
        _vistaAspirable.Toggled += on =>
        {
            _verAspirable = on;
            Callable.From(Refrescar).CallDeferred();
        };
        filaTitulo.AddChild(_vistaAspirable);
        filaTitulo.AddChild(Etiqueta("Aspirable", 14, Gris));
        _stats.AddChild(filaTitulo);

        var filaNivel = new HBoxContainer();
        filaNivel.AddThemeConstantOverride("separation", 6);
        filaNivel.AddChild(Etiqueta("⭐", 15));
        filaNivel.AddChild(Etiqueta("Nivel", 14));
        _nivel = Spin(1, Constantes.NivelMax, 1, "", v => CambiarNivel((int)v));
        _nivel.MinValue = 1;
        _nivel.AllowGreater = false;
        _nivel.CustomMinimumSize = new Vector2(80, 0);
        _nivel.TooltipText = "Cada stat vale su MAX × nivel / 18. El nivel del equipo fija también la fase de la run, y con ella el coste de las teclas.";
        filaNivel.AddChild(_nivel);
        filaNivel.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });
        filaNivel.AddChild(Etiqueta("🥀", 15));
        filaNivel.AddChild(Etiqueta("Desgaste", 14));
        _desgaste = Spin(0, 100, 1, "%", CambiarDesgaste);
        _desgaste.AllowGreater = false;
        _desgaste.TooltipText = "Deterioro acumulado: multiplica todas las stats por (1 − desgaste). Lo genera curarse, y dentro de la run no baja.";
        filaNivel.AddChild(_desgaste);
        _desgasteEfecto = Etiqueta("", 13, Gris);
        filaNivel.AddChild(_desgasteEfecto);
        _stats.AddChild(filaNivel);

        _statsNota = Etiqueta("", 12, Gris);
        _statsNota.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _stats.AddChild(_statsNota);

        _statsCuerpo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _statsCuerpo.AddThemeConstantOverride("separation", 4);
        var desplazable = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        desplazable.AddChild(_statsCuerpo);
        _stats.AddChild(desplazable);
        _statsScroll = desplazable;

        var panelStats = Panel(_stats);
        panelStats.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        arriba.AddChild(panelStats);

        // ---- abajo, a lo ancho: lo que va pasando ----
        var tituloRegistro = new HBoxContainer();
        tituloRegistro.AddChild(new Label { Text = "Registro", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        tituloRegistro.AddChild(Boton("Resumen", Resumen));
        tituloRegistro.AddChild(Boton("Otra partida", () => Empezar((int)(Time.GetTicksUsec() & 0xFFFF)),
            ayuda: "Empieza de nuevo con otra semilla. También deshace los cambios de stats."));
        todo.AddChild(tituloRegistro);

        _bitacora = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SelectionEnabled = true,
            CustomMinimumSize = new Vector2(0, 130),
        };
        _bitacora.AddThemeFontSizeOverride("normal_font_size", 15);
        _bitacora.AddThemeFontSizeOverride("bold_font_size", 15);
        todo.AddChild(Panel(_bitacora));
    }

    private void Refrescar()
    {
        RefrescarMenosStats();
        PintarStats(Actual());
    }

    /// <summary>
    /// Todo menos el cuerpo de las stats. Es lo que se rehace mientras editas
    /// una, para no quitarte el campo de debajo del cursor.
    /// </summary>
    private void RefrescarMenosStats()
    {
        var enTurno = Actual();

        _cabecera.Text = _m.Desenlace switch
        {
            Desenlace.VictoriaAliada => "¡El muñeco ha caído!",
            Desenlace.Huida => "Habéis huido",
            Desenlace.DerrotaAliada => "Derrota",
            _ => $"Ciclo {_e.Ciclo + 1} · tu turno",
        };
        _barraCm.MaxValue = Math.Max(0.001, _e.CmMax);
        _barraCm.Value = _e.CmActual;
        _cm.Text = $"{Cm(_e.CmActual)} / {Cm(_e.CmMax)}" + (_e.ClasicalActivo ? "    ✦ CLASICAL" : "");
        _cm.AddThemeColorOverride("font_color", _e.ClasicalActivo ? Cian : Texto);
        _objetos.Text = $"Comida gratis: {EstadoGratis(true)}    ·    Consumible gratis: {EstadoGratis(false)}";

        var pendientes = _m.PendientesDeActuar().ToList();
        var abierto = _m.ActorDeTurno;
        _orden.Text = abierto is null
            ? "Orden: eliges quién abre; después sigue solo, de derecha a izquierda (de la línea más alta a la más baja), y da la vuelta."
            : "Orden: " + string.Join("  →  ", pendientes.Select(c => c.Id == abierto.Id ? $"▶ {c.Nombre}" : c.Nombre));

        PintarEquipo(enTurno, pendientes.Select(c => c.Id).ToHashSet());
        PintarRival();
        PintarAcciones(enTurno, pendientes);
    }

    private void PintarEquipo(Combatiente? enTurno, HashSet<string> pendientes)
    {
        Vaciar(_equipo);
        _equipo.AddChild(Etiqueta("TU EQUIPO", 14, Gris));

        var tuyos = _e.Combatientes.Where(c => c.Bando == Bando.Aliado).ToList();
        foreach (var c in tuyos.Where(c => c.Zona != Zona.Banquillo).OrderByDescending(c => c.Linea))
        {
            string nota = c.Muerto ? "muerto"
                : c.EnKo ? "K.O."
                : _recienEntrados.Contains(c.Id) ? "actúa el turno que viene"
                : enTurno?.Id == c.Id ? "su turno"
                : pendientes.Contains(c.Id) ? ""
                : "ya actuó";
            _equipo.AddChild(Ficha(c, enTurno?.Id == c.Id, nota));
        }

        var banquillo = tuyos.Where(c => c.Zona == Zona.Banquillo).ToList();
        if (banquillo.Count > 0)
            _equipo.AddChild(Etiqueta("Banquillo: " + string.Join(", ",
                banquillo.Select(c => $"{c.Nombre} ({NombreArma(c.Arma)}, {Vida(c)})")), 15, Gris));
    }

    private void PintarRival()
    {
        Vaciar(_rival);
        _rival.AddChild(Etiqueta("RIVAL", 14, Gris));
        foreach (var r in _e.Combatientes.Where(c => c.Bando == Bando.Enemigo))
        {
            var v = new VBoxContainer();
            v.AddThemeConstantOverride("separation", 6);
            v.AddChild(Etiqueta(r.Nombre, 22));
            v.AddChild(BarraDeVida(r, Rojo));
            v.AddChild(Etiqueta($"{Dano(r.HpMax - r.Hp)} de daño recibido", 17, Dorado));
            v.AddChild(Etiqueta($"le quedan {Dano(r.Hp)}", 15, Gris));
            v.AddChild(Etiqueta("No ataca nunca.", 14, Gris));
            _rival.AddChild(Inspeccionable(Panel(v), r.Id));
        }
    }

    private void PintarAcciones(Combatiente? quien, List<Combatiente> pendientes)
    {
        Vaciar(_acciones);

        if (_m.Desenlace != Desenlace.EnCurso)
        {
            _acciones.AddChild(Etiqueta("Se acabó la prueba. El resumen está en el registro.", 18));
            _acciones.AddChild(Fila(Boton("Otra partida", () => Empezar((int)(Time.GetTicksUsec() & 0xFFFF)))));
            return;
        }

        if (quien is null)
        {
            _acciones.AddChild(Etiqueta("¿Quién abre el turno?", 20, Dorado));
            _acciones.AddChild(Etiqueta("Después sigue solo, de derecha a izquierda.", 15, Gris));
            _acciones.AddChild(Fila(pendientes.Select(c =>
                (Control)Boton($"{c.Nombre} ({NombreArma(c.Arma)})", () => Abrir(c.Id))).ToArray()));
            return;
        }

        var titulo = new HBoxContainer();
        titulo.AddChild(Etiqueta($"Turno de {quien.Nombre.ToUpperInvariant()}", 20, Dorado));
        titulo.AddChild(Etiqueta($"   {NombreArma(quien.Arma)} · {Donde(quien)}", 17, Gris));
        if (_m.ActorDeTurno is null)
        {
            titulo.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            titulo.AddChild(Boton("Que abra otro", () => { _elegido = null; Refrescar(); }));
        }
        _acciones.AddChild(titulo);

        string id = quien.Id;
        var basicos = Basicos(quien);
        var teclas = "QWER".Select(t =>
            (Control)Boton($"{t} · {Cm(_m.CosteDeTecla(t))}", () => Hacer(new UsarHabilidad { ActorId = id, Tecla = t }),
                ayuda: $"Habilidad {t}: cuesta {Cm(_m.CosteDeTecla(t))} CM. Aún no tiene efecto.")).ToArray();

        // Empezada la ofensiva solo quedan basicos, QWER o terminar.
        if (_enOfensiva.Contains(id))
        {
            _acciones.AddChild(Etiqueta("Ya empezó la ofensiva: solo quedan básicos, QWER o terminar.", 15, Gris));
            _acciones.AddChild(Fila(
                Boton("Básicos", () => { if (basicos is not null) Hacer(basicos); }, desactivado: basicos is null),
                Separador(), Etiqueta("QWER", 15, Gris), Fila(teclas), Separador(),
                Boton("Terminar el turno", () => Hacer(new Pasar { ActorId = id }))));
            return;
        }

        _acciones.AddChild(Fila(
            Boton("Básicos", () => { if (basicos is not null) Hacer(basicos); }, desactivado: basicos is null,
                ayuda: "Lanza todos los golpes del arma contra el muñeco. Prohibido desde la defensiva."),
            Separador(), Etiqueta("QWER", 15, Gris), Fila(teclas), Separador(),
            Boton("Terminar el turno", () => Hacer(new Pasar { ActorId = id }))));

        var casillas = new[] { Casilla.Defensiva, Casilla.Neutral, Casilla.Ofensiva }.Select(c =>
            (Control)Boton(c.ToString(), () => Hacer(new Moverse { ActorId = id, Destino = c }),
                desactivado: quien.Zona == Zona.Front && quien.Posicion == c,
                ayuda: c switch
                {
                    Casilla.Ofensiva => "+30% de precisión, −30% de evasión",
                    Casilla.Defensiva => "−30% de precisión, +30% de evasión. Sin básicos.",
                    _ => "Ni gana ni pierde nada",
                })).ToArray();
        _acciones.AddChild(Fila(Etiqueta("Moverse", 15, Gris), Fila(casillas)));

        var cargas = Enumerable.Range(0, NombresSeccion.Length).Select(k =>
            (Control)Boton($"{NombresSeccion[k]} ×{Num(_secciones[k])}", () => Hacer(new CargarMusica { ActorId = id, MultiplicadorSeccion = _secciones[k] }),
                ayuda: "Aporta 3 CM × la sección de la canción. Si llena la barra, enciende CLASICAL. Cierra el turno.")).ToArray();
        _acciones.AddChild(Fila(
            Etiqueta("Carga Musical", 15, Gris), Fila(cargas), Separador(),
            Boton("Defender", () => Hacer(new Defender { ActorId = id }), ayuda: "+50% de ARM y RM contra el primer golpe. Cierra el turno."),
            Boton("Huir", () => Hacer(new Huir { ActorId = id }), ayuda: "Todo el equipo o nadie, al 50%. Si sale mal, pierdes el turno.")));

        var objetos = new Control[]
        {
            Boton($"Comida ({EstadoGratis(true)})", () => Hacer(new UsarObjeto { ActorId = id, ObjetoId = "pan", EsComida = true })),
            Boton($"Consumible ({EstadoGratis(false)})", () => Hacer(new UsarObjeto { ActorId = id, ObjetoId = "vendaje" })),
            Boton("Gadget", () => Hacer(new UsarObjeto { ActorId = id, ObjetoId = "artilugio", EsGadget = true }),
                ayuda: "Siempre gasta el turno."),
        };
        _acciones.AddChild(Fila(Etiqueta("Objetos", 15, Gris), Fila(objetos)));

        var otras = new List<Control>
        {
            quien.Zona == Zona.Escondite
                ? Boton("Salir del escondite", () => Hacer(new SalirDelEscondite { ActorId = id }),
                    ayuda: "Gratis, pero sales a la casilla defensiva.")
                : Boton($"Esconderse ({_e.EsconditesLibres} libre{(_e.EsconditesLibres == 1 ? "" : "s")})",
                    () => Hacer(new Esconderse { ActorId = id }),
                    ayuda: "El rival no puede tocarte; sin básicos ni habilidades. Cierra el turno."),
        };
        var banquillo = _e.Del(quien.Bando).Where(c => c.Zona == Zona.Banquillo).ToList();
        if (banquillo.Count > 0)
        {
            otras.Add(Separador());
            otras.Add(Etiqueta("Swap", 15, Gris));
            otras.AddRange(banquillo.Select(b => (Control)Boton($"Entra {b.Nombre}",
                () => Hacer(new Swap { ActorId = id, EntranteId = b.Id }),
                ayuda: $"{b.Nombre} ocupa tu casilla y actúa el turno que viene. Cierra tu turno.")));
        }
        _acciones.AddChild(Fila(otras.ToArray()));
    }

    // ------------------------------------------------------------ registro

    private void Narrar(Combatiente quien, Accion accion, ResultadoAccion r, double cmAntes)
    {
        if (!r.Permitida)
        {
            Escribir($"✗ {quien.Nombre}: no se puede — {r.Motivo}", Rojo);
            return;
        }

        _cicloEmpezado = true;
        Simulador.Anotar(_registro, r);
        double alaBarra = _e.CmActual - cmAntes;
        if (alaBarra > Tolerancia) _cmALaBarra[quien.Id] += alaBarra;
        if (accion is AtaqueBasico or UsarHabilidad) _enOfensiva.Add(quien.Id);

        switch (accion)
        {
            case AtaqueBasico b:
                var objetivo = _e.Buscar(b.ObjetivoId)!;
                Escribir($"[b]{quien.Nombre}[/b] lanza {r.Impactos.Count} golpe{(r.Impactos.Count == 1 ? "" : "s")} a {objetivo.Nombre}:");
                // Con un decimal: asi los golpes suman el total, y el total
                // cuadra con lo que ensena la tarjeta del rival.
                foreach (var g in r.Impactos)
                {
                    if (!g.Acerto) { Escribir("    falla", Gris); continue; }
                    if (g.Remato) { Escribir("    acierta · lo REMATA", Rojo); continue; }
                    Escribir($"    acierta  {Dano(g.DanoAplicado + g.AbsorbidoPorEscudo)}{(g.Critico ? "  ¡crítico!" : "")}{(g.ProvocoKo ? "  ¡K.O.!" : "")}   +{Cm(g.CmAtacante)} CM",
                        g.Critico ? Dorado : Verde);
                }
                double total = r.Impactos.Where(g => g.Acerto).Sum(g => g.DanoAplicado + g.AbsorbidoPorEscudo);
                double generada = r.Impactos.Sum(g => g.CmAtacante);
                Escribir($"    {r.Impactos.Count(g => g.Acerto)} de {r.Impactos.Count} · {Dano(total)} de daño · +{Cm(generada)} CM");
                double perdida = generada - Math.Max(0, alaBarra);
                if (perdida > Tolerancia)
                {
                    _cmPerdida += perdida;
                    Escribir($"    La barra está llena: {Cm(perdida)} CM no caben y se pierden.", Gris);
                }
                break;

            case UsarHabilidad h:
                Escribir($"{quien.Nombre} usa {h.Tecla}: gasta {Cm(cmAntes - _e.CmActual)} CM (aún sin efecto).");
                break;

            case CargarMusica:
                bool encendido = r.Cambios.Any(c => c.Tipo == "clasical");
                Escribir($"{quien.Nombre}: Carga Musical, la barra pasa de {Cm(cmAntes)} a {Cm(_e.CmActual)}.");
                if (encendido)
                    Escribir("    ✦ ¡CLASICAL encendido! Toda la CM que ganéis vale ×1.5 hasta vuestro próximo turno.", Cian);
                break;

            case UsarObjeto o:
                string que = o.EsGadget ? "un gadget" : o.EsComida ? "la comida" : "el consumible";
                Escribir(o.EsGadget ? $"{quien.Nombre} usa {que}: gasta el turno, como siempre."
                    : r.CierraTurno ? $"{quien.Nombre} usa {que}, pero no era gratis: le cuesta el turno."
                    : $"{quien.Nombre} usa {que} gratis.");
                if (r.Cambios.Any(c => c.Tipo == "compromiso"))
                    Escribir("    Desde la defensiva no arranca la espera… pero queda anclado ahí este turno.", Gris);
                break;

            case Moverse mv:
                Escribir($"{quien.Nombre} se mueve a la casilla {mv.Destino.ToString().ToLowerInvariant()}.");
                break;

            case Defender:
                Escribir($"{quien.Nombre} se pone en guardia.");
                break;

            case Esconderse:
                Escribir($"{quien.Nombre} se esconde: el rival ya no puede tocarle.");
                break;

            case SalirDelEscondite:
                Escribir($"{quien.Nombre} sale del escondite, a la casilla defensiva.");
                break;

            case Swap s:
                _recienEntrados.Add(s.EntranteId);
                Escribir($"{quien.Nombre} se va al banquillo y entra {_e.Buscar(s.EntranteId)!.Nombre}, que actuará el turno que viene.");
                break;

            case Huir:
                bool logrado = r.Cambios.Any(c => c.Tipo == "huida" && c.Detalle == "lograda");
                Escribir(logrado ? "¡Huís del combate!" : "Intentáis huir… y no hay forma. El turno se pierde.", logrado ? Verde : Rojo);
                break;

            case Pasar:
                Escribir($"{quien.Nombre} termina su turno.", Gris);
                break;
        }
    }

    private void Final()
    {
        string como = _m.Desenlace switch
        {
            Desenlace.VictoriaAliada => "¡El muñeco ha caído!",
            Desenlace.Huida => "Habéis huido.",
            Desenlace.DerrotaAliada => "Derrota.",
            _ => "Fin de la prueba.",
        };
        Escribir($"[b]{como}[/b]", Dorado);
        Resumen();
    }

    private void Resumen()
    {
        int ciclos = _e.Ciclo + (_cicloEmpezado ? 1 : 0);
        Escribir($"[b]Resumen · {ciclos} ciclo{(ciclos == 1 ? "" : "s")} jugado{(ciclos == 1 ? "" : "s")}[/b]", Dorado);

        var activos = _e.Combatientes
            .Where(c => c.Bando == Bando.Aliado && (_registro[c.Id].Golpes > 0 || _cmALaBarra[c.Id] > Tolerancia))
            .ToList();
        if (activos.Count == 0)
        {
            Escribir("    Nadie ha golpeado ni cargado todavía.", Gris);
            return;
        }

        foreach (var c in activos)
        {
            var r = _registro[c.Id];
            string acierto = r.Golpes == 0 ? "—" : $"{100.0 * r.Aciertos / r.Golpes:F0}%";
            string porCiclo = ciclos == 0 ? "—" : Dano(r.Dano / ciclos);
            Escribir($"    {c.Nombre}: {r.Golpes} golpes · acierto {acierto} · {Dano(r.Dano)} de daño ({porCiclo} por ciclo) · {Cm(_cmALaBarra[c.Id])} CM a la barra");
        }
        if (_cmPerdida > Tolerancia)
            Escribir($"    Con la barra llena se perdieron {Cm(_cmPerdida)} CM de golpes.", Gris);
    }

    private void Escribir(string bbcode, Color? color = null)
    {
        _bitacora.AppendText(color is { } c ? $"[color=#{c.ToHtml(false)}]{bbcode}[/color]" : bbcode);
        _bitacora.AppendText("\n");
    }

    // ------------------------------------------------------------ guion y captura

    /// <summary>
    /// Acciones seguidas, como si se pulsaran los botones: abrir:ligera,
    /// basicos, mover:1-3, tecla:q, carga:1-3, objeto:1-3, defender, huir,
    /// esconder, salir, swap:suplente, terminar.
    /// </summary>
    private void EjecutarGuion(string guion)
    {
        foreach (var paso in guion.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var partes = paso.Split(':');
            string orden = partes[0].ToLowerInvariant();
            string arg = partes.Length > 1 ? partes[1] : "";

            if (orden == "abrir") { Abrir(arg); continue; }
            if (orden == "inspeccionar") { _inspeccionado = arg; Refrescar(); continue; }
            if (orden == "bajar") { _bajarAlCapturar = true; continue; }
            if (orden == "cm" && partes.Length > 2
                && typeof(AjustesCm).GetProperty(arg, System.Reflection.BindingFlags.IgnoreCase
                    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) is { } propiedad
                && double.TryParse(partes[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double valorCm))
            {
                propiedad.SetValue(_e.AjustesCm, valorCm);
                Escribir($"✎ CM · {propiedad.Name} → {Num3(valorCm)}", Cian);
                Refrescar();
                continue;
            }
            if (orden == "vista") { _verAspirable = arg == "aspirable"; Refrescar(); continue; }
            if (orden is "nivel" or "desgaste" && partes.Length > 2 && double.TryParse(partes[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double dato))
            {
                _inspeccionado = arg;
                if (orden == "nivel") CambiarNivel((int)dato);
                else CambiarDesgaste(dato);
                continue;
            }
            if (orden == "max" && partes.Length > 3 && double.TryParse(partes[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double maximo)
                && _e.Buscar(arg) is { } quienMax
                && Primarias.Concat(Ofensivas).Concat(Defensivas).Concat(DeKit)
                    .FirstOrDefault(x => x.Stat.ToString().Equals(partes[2], StringComparison.OrdinalIgnoreCase)) is { } infoMax)
            {
                CambiarMax(quienMax, infoMax, maximo);
                Refrescar();          // desde el guion no hay un campo que ya enseñe el valor nuevo
                continue;
            }

            var quien = Actual();
            int n = int.TryParse(arg, out int v) ? v : 0;
            Accion? accion = quien is null ? null : orden switch
            {
                "basicos" => Basicos(quien),
                "mover" when n is >= 1 and <= 3 => new Moverse { ActorId = quien.Id, Destino = (Casilla)(n - 1) },
                "tecla" when arg.Length == 1 => new UsarHabilidad { ActorId = quien.Id, Tecla = char.ToUpperInvariant(arg[0]) },
                "carga" when n is >= 1 and <= 3 => new CargarMusica { ActorId = quien.Id, MultiplicadorSeccion = _secciones[n - 1] },
                "objeto" when n is >= 1 and <= 3 => new UsarObjeto
                {
                    ActorId = quien.Id, ObjetoId = n == 1 ? "pan" : n == 2 ? "vendaje" : "artilugio",
                    EsComida = n == 1, EsGadget = n == 3,
                },
                "defender" => new Defender { ActorId = quien.Id },
                "huir" => new Huir { ActorId = quien.Id },
                "esconder" => new Esconderse { ActorId = quien.Id },
                "salir" => new SalirDelEscondite { ActorId = quien.Id },
                "swap" => new Swap { ActorId = quien.Id, EntranteId = arg },
                "terminar" => new Pasar { ActorId = quien.Id },
                _ => null,
            };

            if (accion is null) Escribir($"guion: no puedo hacer «{paso}»", Rojo);
            else Hacer(accion);
        }
    }

    private async Task Capturar(string ruta)
    {
        for (int i = 0; i < 6; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (_bajarAlCapturar)
        {
            _statsScroll.ScrollVertical = 100_000;
            for (int i = 0; i < 4; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        var error = GetViewport().GetTexture().GetImage().SavePng(ruta);
        GD.Print(error == Error.Ok ? $"captura: {ruta}" : $"captura fallida: {error}");
        GetTree().Quit();
    }

    private static Dictionary<string, string> Argumentos()
    {
        var r = new Dictionary<string, string>();
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var t = a.TrimStart('-').Split('=', 2);
            r[t[0].ToLowerInvariant()] = t.Length > 1 ? t[1] : "";
        }
        return r;
    }

    // ------------------------------------------------------------ piezas

    private Control Ficha(Combatiente c, bool enTurno, string nota)
    {
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 6);

        var arriba = new HBoxContainer();
        arriba.AddThemeConstantOverride("separation", 10);
        arriba.AddChild(Etiqueta(enTurno ? "▶" : "", 19, Dorado));
        arriba.AddChild(Etiqueta(c.Nombre, 19));
        arriba.AddChild(Etiqueta($"{NombreArma(c.Arma)} · línea {c.Linea}", 15, Gris));
        arriba.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        arriba.AddChild(Etiqueta(nota, 15, enTurno ? Dorado : Gris));
        v.AddChild(arriba);

        var abajo = new HBoxContainer();
        abajo.AddThemeConstantOverride("separation", 6);
        if (c.EnKo || c.Zona == Zona.Escondite)
        {
            abajo.AddChild(Etiqueta(c.Muerto ? "muerto" : c.EnKo ? "K.O." : "escondido", 15, c.EnKo ? Rojo : Cian));
        }
        else
        {
            foreach (var x in new[] { Casilla.Defensiva, Casilla.Neutral, Casilla.Ofensiva })
            {
                bool aqui = c.Posicion == x;
                var chip = Etiqueta(x switch { Casilla.Defensiva => "DEF", Casilla.Neutral => "NEU", _ => "OFE" }, 14, aqui ? Fondo : Gris);
                chip.HorizontalAlignment = HorizontalAlignment.Center;
                chip.CustomMinimumSize = new Vector2(46, 0);
                var p = new PanelContainer();
                p.AddThemeStyleboxOverride("panel", Caja(aqui ? Dorado : new Color(0.2f, 0.22f, 0.27f), 4, 3));
                p.AddChild(chip);
                abajo.AddChild(p);
            }
        }
        abajo.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });
        var vida = BarraDeVida(c, Verde);
        vida.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        abajo.AddChild(vida);
        abajo.AddChild(Etiqueta(Vida(c), 15, Gris));
        v.AddChild(abajo);

        return Inspeccionable(Panel(v, enTurno ? TarjetaActiva : Tarjeta), c.Id);
    }

    // ------------------------------------------------------------ stats

    /// <summary>Si el motor ya la usa en un combate. Las que no, salen tenues.</summary>
    private enum Uso { EnCombate, SinGolpesMagicos, SinRitmo, AunNo }

    private sealed record InfoStat(Stat Stat, string Icono, string Nombre, bool Porcentaje, Uso Uso, string Ayuda);

    // ⚠️ Lo que el motor usa hoy, segun el inventario del combate. Si se
    // conecta una stat nueva, hay que pasarla aqui de AunNo a EnCombate.
    private static readonly InfoStat[] Primarias =
    [
        new(Stat.Hp, "❤️", "Vida", false, Uso.EnCombate, "Salud. A 0, cae en K.O."),
        new(Stat.Ad, "⚔️", "Daño físico (AD)", false, Uso.EnCombate, "La referencia del daño físico: la de los básicos."),
        new(Stat.Ap, "🔮", "Daño mágico (AP)", false, Uso.SinGolpesMagicos, "La referencia del daño mágico."),
        new(Stat.Arm, "🛡️", "Armadura", false, Uso.EnCombate, "Reduce el daño físico con la fórmula de ratio, ARM / (ARM + 100), hasta un 90%."),
        new(Stat.Rm, "🧿", "Resistencia mágica", false, Uso.SinGolpesMagicos, "Reduce el daño mágico con la misma fórmula de ratio."),
        new(Stat.Press, "🎯", "Precisión", false, Uso.EnCombate, "Entra en el acierto, contra la evasión del rival. La casilla ofensiva la sube un 30%."),
        new(Stat.Eva, "💨", "Evasión", false, Uso.EnCombate, "Entra en el acierto del rival. Cada golpe que esquiva la erosiona el resto de esa ronda."),
        new(Stat.Crit, "💥", "Crítico", true, Uso.EnCombate, "Probabilidad de crítico: ×1.8 después de la armadura. Tope 100%."),
    ];

    private static readonly InfoStat[] Ofensivas =
    [
        new(Stat.Preparacion, "⏳", "Preparación", true, Uso.AunNo, "Resta un turno a las preparaciones al llegar al umbral. Tope 75%."),
        new(Stat.VelocidadAtaque, "⚡", "Velocidad de ataque", true, Uso.EnCombate, "Golpes por tanda = base × (1 + VA). Tope 60%."),
        new(Stat.Tempo, "🎶", "Tempo", true, Uso.AunNo, "CM por golpe × (1 + Tempo). Tope 50%."),
        new(Stat.PenArmadura, "🗡️", "Penetración de armadura", false, Uso.EnCombate, "Resta armadura al objetivo, hasta un 80% de la suya."),
        new(Stat.PenMagica, "🪄", "Penetración mágica", false, Uso.SinGolpesMagicos, "Resta resistencia mágica al objetivo, hasta un 80% de la suya."),
        new(Stat.RoboVida, "🩸", "Robo de vida", true, Uso.AunNo, "Cura un % del daño que haces. Tope 25%."),
        new(Stat.DominioArmas, "🤺", "Dominio de armas", true, Uso.AunNo, "Potencia del arma × (1 + WM). Tope 50%."),
        new(Stat.DominioObjetos, "🧰", "Dominio de objetos", true, Uso.AunNo, "Daño de los objetos × (1 + IM). Tope 60%."),
        new(Stat.DanoArea, "🌀", "Daño en área", true, Uso.AunNo, "Daño de área × (1 + AOE). Tope 80%."),
        new(Stat.DanoExplosivo, "💣", "Daño explosivo", true, Uso.AunNo, "Daño explosivo × (1 + EXPL). Tope 100%."),
        new(Stat.UltimaBala, "💀", "Última bala", true, Uso.EnCombate, "Si tras tu golpe al rival le queda menos vida que este %, cae en K.O. Tope 10–15%."),
        new(Stat.Rebote, "🔁", "Rebote", true, Uso.AunNo, "Probabilidad de que el daño rebote a otro enemigo. Tope 30%."),
    ];

    private static readonly InfoStat[] Defensivas =
    [
        new(Stat.RegeneracionVida, "🌿", "Regeneración", true, Uso.AunNo, "Recupera un % de la vida base cada turno, sin desgaste. Tope 10%."),
        new(Stat.DominioMedico, "🩺", "Dominio médico", true, Uso.AunNo, "Curación × (1 + MM). Tope 50%."),
        new(Stat.ResistenciaDesgaste, "🔋", "Resistencia al desgaste", true, Uso.AunNo, "Reduce el desgaste que te afecta. Tope 50%."),
        new(Stat.Inquebrantable, "♻️", "Inquebrantable", true, Uso.AunNo, "Probabilidad de levantarse solo tras caer en K.O. Tope 25%."),
        new(Stat.EficienciaEscudo, "💠", "Eficiencia de escudo", true, Uso.AunNo, "Escudos × (1 + SHD). Tope 50%."),
        new(Stat.ExtensionEscudo, "🔷", "Extensión de escudo", true, Uso.AunNo, "Probabilidad de que un escudo dure un turno más. Tope 30%."),
        new(Stat.ExtensionInvulnerable, "🌟", "Ext. de invulnerabilidad", true, Uso.AunNo, "Probabilidad de que la invulnerabilidad dure un turno más. Tope 25%."),
        new(Stat.Tenacidad, "🦾", "Tenacidad", true, Uso.AunNo, "Limpia los debuffs al empezar el turno, no las emociones. Tope 30%."),
        new(Stat.AmplificadorBuff, "📈", "Amplificador de buffs", true, Uso.AunNo, "Potencia de los buffs que recibes. Tope 50%."),
        new(Stat.ExtensionBuff, "⏫", "Extensión de buffs", true, Uso.AunNo, "Duración de los buffs que recibes. Tope 25%."),
        new(Stat.AmplificadorDebuff, "📉", "Amplificador de debuffs", true, Uso.AunNo, "Potencia de los debuffs que aplicas. Tope 50%."),
        new(Stat.ExtensionDebuff, "⏬", "Extensión de debuffs", true, Uso.AunNo, "Duración de los debuffs que aplicas. Tope 25%."),
        new(Stat.DominioComida, "🍲", "Dominio de comida", true, Uso.AunNo, "Potencia de la comida. Tope 50%."),
        new(Stat.ExtensionComida, "🍱", "Extensión de comida", true, Uso.AunNo, "Duración de la comida. Tope 30%."),
        new(Stat.Reflejos, "🤸", "Reflejos", true, Uso.AunNo, "Intercepta el daño letal dirigido a un aliado. Tope 20%."),
    ];

    private static readonly InfoStat[] DeKit =
    [
        new(Stat.AntiArm, "🔨", "Anti-armadura (Willham)", false, Uso.AunNo, "Propia del kit de Willham."),
        new(Stat.AntiRm, "🧪", "Anti-RM (Willham)", false, Uso.AunNo, "Propia del kit de Willham."),
    ];

    private Combatiente Inspeccionado()
        => (_inspeccionado is null ? null : _e.Buscar(_inspeccionado))
           ?? Actual()
           ?? _e.Combatientes.First(x => x.Bando == Bando.Aliado);

    private const string AyudaAporte = "Lo que aporta a la barra del equipo: entero en pie, la mitad en K.O., nada muerto.";

    private void PintarStats(Combatiente? enTurno)
    {
        var c = (_inspeccionado is null ? null : _e.Buscar(_inspeccionado))
                ?? enTurno
                ?? _e.Combatientes.First(x => x.Bando == Bando.Aliado);

        _statsTitulo.Text = $"Stats de {c.Nombre.ToUpperInvariant()}";
        _statsSub.Text = $"{NombreArma(c.Arma)} · {Donde(c)}";
        _vistaAspirable.SetPressedNoSignal(_verAspirable);
        _nivel.SetValueNoSignal(c.Nivel);
        _desgaste.SetValueNoSignal(Math.Round(c.Desgaste.Acumulado * 100, 1));
        _desgasteEfecto.Text = $"todas las stats ×{Enma.Core.Stats.Desgaste.Multiplicador(c.Desgaste.Acumulado).ToString("0.##", CultureInfo.InvariantCulture)}";
        _statsNota.Text = _verAspirable
            ? "El MAX aspirable: lo que valdría a nivel 18. Edítalo aquí; la vista actual enseña lo que resuelve el motor a su nivel."
            : "Lo que resuelve el motor a su nivel · tenue: aún sin efecto · ratón encima: qué es · pulsa una ficha: ver otra";

        Vaciar(_statsCuerpo);
        var ev = new Enma.Core.Reglas.Evaluador();
        Control Celda(InfoStat info) => _verAspirable ? Editable(info, c) : Entrada(info, c, ev);

        _statsCuerpo.AddChild(Grupo("PRIMARIAS"));
        var primarias = Rejilla(2);
        foreach (var info in Primarias) primarias.AddChild(Celda(info));
        primarias.AddChild(_verAspirable
            ? Fila("🎵", "Aporte a la barra de CM", AyudaAporte, Uso.EnCombate, Spin(c.Storage, 50, 0.5, "", v => CambiarAporte(c, v)))
            : EntradaLibre("🎵", "Aporte a la barra de CM", Cm(c.Storage), "", Texto, AyudaAporte, Uso.EnCombate));
        primarias.AddChild(EntradaLibre("🩻", "Daño verdadero", "—", "", Texto,
            "Porción del daño que ignora la armadura y la RM. El manual la cuenta como primaria, pero aún no existe como stat en el motor.", Uso.AunNo));
        _statsCuerpo.AddChild(primarias);

        var secundarias = new HBoxContainer();
        secundarias.AddThemeConstantOverride("separation", 14);
        foreach (var (nombre, grupo) in new[] { ("OFENSIVAS", Ofensivas), ("DEFENSIVAS", Defensivas) })
        {
            var columna = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            columna.AddThemeConstantOverride("separation", 0);
            columna.AddChild(Grupo(nombre));
            foreach (var info in grupo) columna.AddChild(Celda(info));
            if (grupo == Ofensivas)
            {
                // Caben debajo: las ofensivas son tres menos que las defensivas.
                columna.AddChild(Grupo("PROPIAS DE KIT"));
                foreach (var info in DeKit) columna.AddChild(Celda(info));
            }
            secundarias.AddChild(columna);
        }
        _statsCuerpo.AddChild(secundarias);

        if (_verAspirable)
        {
            var cabeceraCm = new HBoxContainer();
            var grupoCm = Grupo("CARGA MUSICAL · ESTE COMBATE, PARA TODOS");
            grupoCm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cabeceraCm.AddChild(grupoCm);
            cabeceraCm.AddChild(Boton("Volver al diseño", VolverAlDiseno,
                ayuda: "Deja todos los números de la Carga Musical como dice el diseño."));
            _statsCuerpo.AddChild(cabeceraCm);
            _statsCuerpo.AddChild(AjustesDeCm());
        }
        else
        {
            _statsCuerpo.AddChild(Grupo($"CARGA MUSICAL · {NombreArma(c.Arma).ToUpperInvariant()}"));
            _statsCuerpo.AddChild(BonusDeCm(c, ev));
        }
    }

    /// <summary>
    /// Los numeros de la economia de CM de este combate, editables. Valen para
    /// todos: el motor los lee de AjustesCm en cada golpe, carga y tecla.
    /// </summary>
    private GridContainer AjustesDeCm()
    {
        var g = Rejilla(2);
        var a = new Func<AjustesCm>(() => _e.AjustesCm);

        g.AddChild(Ajuste("🎼", "Por golpe · ligera", "CM por golpe de la ligera que conecta. Diseño: 1/8, la corchea.",
            () => a().PorGolpeLigera, v => a().PorGolpeLigera = v, 0.005));
        g.AddChild(Ajuste("🎯", "En tempo · ligera ×", "Lo que se multiplica en tempo. Diseño: ×1.2, el suelo común.",
            () => a().TempoLigera, v => a().TempoLigera = v, 0.05));
        g.AddChild(Ajuste("🎼", "Por golpe · media", "CM por golpe de la media que conecta. Diseño: 1/4, la negra.",
            () => a().PorGolpeMedia, v => a().PorGolpeMedia = v, 0.005));
        g.AddChild(Ajuste("🎯", "En tempo · media ×", "Diseño: ×2, el premio de la generadora del equipo.",
            () => a().TempoMedia, v => a().TempoMedia = v, 0.05));
        g.AddChild(Ajuste("🎼", "Por golpe · pesada", "CM por golpe de la pesada que conecta. Diseño: 1/2, la blanca.",
            () => a().PorGolpePesada, v => a().PorGolpePesada = v, 0.005));
        g.AddChild(Ajuste("🎯", "En tempo · pesada ×", "Diseño: ×1.2, el suelo común. Los documentos aún dicen ×2 para todas.",
            () => a().TempoPesada, v => a().TempoPesada = v, 0.05));
        g.AddChild(Ajuste("🛡️", "Al recibir · fracción", "Al recibir un golpe se gana esta fracción de la figura de quien pega. Diseño: la mitad.",
            () => a().AlRecibir, v => a().AlRecibir = v, 0.05));
        g.AddChild(Ajuste("🔰", "En guardia ×", "Defendiendo, lo que se gana al recibir se multiplica por esto. Diseño: ×2.",
            () => a().EnGuardia, v => a().EnGuardia = v, 0.1));
        g.AddChild(Ajuste("✦", "CLASICAL ×", "Lo que multiplica CLASICAL toda la CM de su bando. Diseño: ×1.5.",
            () => a().Clasical, v => a().Clasical = v, 0.05));
        for (int k = 0; k < NombresSeccion.Length; k++)
        {
            int i = k;
            g.AddChild(Ajuste("🎵", $"Sección {NombresSeccion[i].ToLowerInvariant()} ×",
                "Lo que multiplica la Carga Musical en esa sección de la canción. Diseño: calma ×0.5, base ×1, estribillo ×1.8.",
                () => _secciones[i], v => _secciones[i] = v, 0.1));
        }
        g.AddChild(Ajuste("🔑", "Precio de Q", "Precio base de la tecla, antes del descuento de la fase. Diseño: 1.",
            () => a().PrecioQ, v => a().PrecioQ = v, 0.5));
        g.AddChild(Ajuste("📅", "Fase early ×", "Descuento del precio en la fase early (niveles 1 a 5). Diseño: ×1.",
            () => a().FaseEarly, v => a().FaseEarly = v, 0.05));
        g.AddChild(Ajuste("🔑", "Precio de W", "Diseño: 2.",
            () => a().PrecioW, v => a().PrecioW = v, 0.5));
        g.AddChild(Ajuste("📅", "Fase mid ×", "Descuento en la fase mid (niveles 6 a 12). Diseño: ×0.6.",
            () => a().FaseMid, v => a().FaseMid = v, 0.05));
        g.AddChild(Ajuste("🔑", "Precio de E", "Diseño: 3.",
            () => a().PrecioE, v => a().PrecioE = v, 0.5));
        g.AddChild(Ajuste("📅", "Fase late ×", "Descuento en la fase late (niveles 13 a 18). Diseño: ×0.4. El suelo duro es ×0.33.",
            () => a().FaseLate, v => a().FaseLate = v, 0.05));
        g.AddChild(Ajuste("🔑", "Precio de R", "Diseño: 4.",
            () => a().PrecioR, v => a().PrecioR = v, 0.5));
        return g;
    }

    private Control Ajuste(string icono, string nombre, string ayuda, Func<double> leer, Action<double> escribir, double paso)
    {
        var campo = Spin(leer(), 100, paso, "", v =>
        {
            double antes = leer();
            if (Math.Abs(antes - v) < 1e-9) return;
            escribir(v);
            Escribir($"✎ CM · {nombre.TrimEnd('×', ' ')}: {Num3(antes)} → {Num3(v)}", Cian);
            RefrescarMenosStats();
        });
        return Fila(icono, nombre, ayuda, Uso.EnCombate, campo);
    }

    private void VolverAlDiseno()
    {
        _e.AjustesCm = new AjustesCm();
        _secciones = [.. SeccionesDelDiseno];
        Escribir("✎ CM · vuelven todos los números del diseño", Cian);
        Refrescar();
    }

    /// <summary>Una stat en la vista aspirable: su MAX, en un campo que se puede editar.</summary>
    private Control Editable(InfoStat info, Combatiente c)
    {
        double max = info.Stat == Stat.Hp ? 10_000_000 : 100_000;
        var campo = Spin(c.MaxAspirable.GetValueOrDefault(info.Stat), max, 1, info.Porcentaje ? "%" : "",
            v => CambiarMax(c, info, v));
        return Fila(info.Icono, info.Nombre, info.Ayuda, info.Uso, campo);
    }

    // ---- editar: cada cambio va al estado del combate y queda en el registro ----

    private void CambiarMax(Combatiente c, InfoStat info, double valor)
    {
        double antes = c.MaxAspirable.GetValueOrDefault(info.Stat);
        if (Math.Abs(antes - valor) < 1e-9) return;

        double vida = c.HpPct;
        c.MaxAspirable[info.Stat] = valor;
        if (info.Stat == Stat.Hp) ConservarVida(c, vida);

        Escribir($"✎ {c.Nombre} · {info.Nombre}: aspirable {Num(antes)} → {Num(valor)}{(info.Porcentaje ? "%" : "")}", Cian);
        RefrescarMenosStats();
    }

    private void CambiarNivel(int nivel)
    {
        var c = Inspeccionado();
        if (c.Nivel == nivel) return;

        double vida = c.HpPct;
        int antes = c.Nivel;
        c.Nivel = nivel;
        ConservarVida(c, vida);

        Escribir($"✎ {c.Nombre} · nivel {antes} → {nivel}", Cian);
        Refrescar();
    }

    private void CambiarDesgaste(double porcentaje)
    {
        var c = Inspeccionado();
        double antes = c.Desgaste.Acumulado;
        if (Math.Abs(antes * 100 - porcentaje) < 1e-9) return;

        double vida = c.HpPct;
        c.Desgaste.Fijar(porcentaje / 100.0);
        ConservarVida(c, vida);

        Escribir($"✎ {c.Nombre} · desgaste {Num(antes * 100)}% → {Num(porcentaje)}%", Cian);
        Refrescar();
    }

    private void CambiarAporte(Combatiente c, double valor)
    {
        double antes = c.Storage;
        if (Math.Abs(antes - valor) < 1e-9) return;

        c.Storage = valor;
        _e.RecalcularCm();

        Escribir($"✎ {c.Nombre} · aporte a la barra {Cm(antes)} → {Cm(valor)}", Cian);
        RefrescarMenosStats();
    }

    /// <summary>Si cambia la vida maxima, la actual conserva su porcentaje: quien estaba lleno sigue lleno.</summary>
    private static void ConservarVida(Combatiente c, double porcentaje)
    {
        if (!c.EnKo) c.Hp = porcentaje * c.HpMax;
    }

    private static SpinBox Spin(double valor, double maximo, double paso, string sufijo, Action<double> alCambiar)
    {
        var campo = new SpinBox
        {
            MinValue = 0,
            MaxValue = maximo,
            Step = paso,
            AllowGreater = true,
            Suffix = sufijo,
            SelectAllOnFocus = true,
            Alignment = HorizontalAlignment.Right,
            CustomMinimumSize = new Vector2(96, 0),
        };
        campo.SetValueNoSignal(valor);
        campo.GetLineEdit().AddThemeFontSizeOverride("font_size", 13);
        campo.ValueChanged += v => alCambiar(v);
        return campo;
    }

    /// <summary>
    /// La economia de CM de un combatiente, con las mismas funciones que usa
    /// el motor. Con CLASICAL encendido en su bando, todo sale ya x1.5.
    /// </summary>
    private GridContainer BonusDeCm(Combatiente c, Enma.Core.Reglas.Evaluador ev)
    {
        var a = _e.AjustesCm;
        bool clasical = _e.Barra(c.Bando).ClasicalActivo;
        double x = clasical ? a.Clasical : 1.0;
        string marca = clasical ? $"✦ ×{Num(a.Clasical)}" : "";

        double golpe = CargaMusical.BasePorImpacto(c.Arma, a);
        double enTempo = CargaMusical.GananciaPorImpacto(c.Arma, enTempo: true, ajustes: a);
        int golpes = CargaMusical.Golpes(CargaMusical.GolpesBase(c.Arma),
            ev.ResolverStat(_e, c, Stat.VelocidadAtaque) / 100.0);
        string figura = c.Arma switch
        {
            ClaseArma.Ligera => "corchea",
            ClaseArma.Media => "negra",
            ClaseArma.Pesada => "blanca",
            _ => "figura",
        };
        string bonoTempo = $"×{Num(a.Tempo(c.Arma))}";
        double recibirMin = CargaMusical.GananciaAlRecibir(ClaseArma.Ligera, defendActivo: false, ajustes: a);
        double recibirMax = CargaMusical.GananciaAlRecibir(ClaseArma.Pesada, defendActivo: false, ajustes: a);
        double aporte = c.Storage;

        var g = Rejilla(2);
        g.AddChild(EntradaLibre("🎼", $"Por golpe ({figura})", Cm(golpe * x), marca, Cian,
            "La figura del arma: ligera 1/8, media 1/4, pesada 1/2. Solo si el golpe conecta: fallar da 0.",
            Uso.EnCombate));
        g.AddChild(EntradaLibre("🎯", "En tempo, por golpe", Cm(enTempo * x), (bonoTempo + " " + marca).Trim(), Verde,
            "En el pulso, la media cobra ×2: es la generadora del equipo. Ligera y pesada, +20%. "
            + "Los documentos aún dicen ×2 para todas: está pendiente de decidir.",
            Uso.SinRitmo));
        g.AddChild(EntradaLibre("⚔️", $"Por tanda ({golpes} golpe{(golpes == 1 ? "" : "s")})", $"{Cm(golpes * golpe * x)} → {Cm(golpes * enTempo * x)}", marca, Cian,
            "Si conectan todos los golpes de la tanda: fuera de tempo → en tempo.",
            Uso.EnCombate));
        g.AddChild(EntradaLibre("🔢", "Golpes para 1 CM", $"{Num(1 / (golpe * x))} → {Num(1 / (enTempo * x))}", marca, Cian,
            "Golpes conectados para ganar 1 CM: fuera de tempo → en tempo. El diseño da 8, 4 y 2 fuera de tempo.",
            Uso.EnCombate));
        g.AddChild(EntradaLibre("🛡️", "Al recibir un golpe", $"{Cm(recibirMin * x)}–{Cm(recibirMax * x)}", marca, Cian,
            "La mitad de la figura de quien te pega: de una ligera 0.0625, de una media 0.125, de una pesada 0.25. "
            + "En guardia, el doble. El muñeco no pega, así que aquí no se ve.",
            Uso.EnCombate));
        g.AddChild(EntradaLibre("🎵", "Carga Musical", string.Join(" · ", _secciones.Select(m => Cm(aporte * m * x))), marca, Cian,
            "Tu aporte × la sección de la canción: calma, base y estribillo. Si llena la barra, enciende CLASICAL. Cierra el turno.",
            Uso.EnCombate));
        g.AddChild(EntradaLibre("✦", "CLASICAL", clasical ? "encendido" : "apagado", clasical ? $"×{Num(a.Clasical)}" : "", Cian,
            "×1.5 a toda la CM que gana el bando, hasta su siguiente turno. Se enciende cuando una Carga Musical llena la barra.",
            Uso.EnCombate));
        g.AddChild(EntradaLibre("🔑", "Coste QWER", string.Join(" · ", "QWER".Select(t => Cm(_m.CosteDeTecla(t)))), "", Texto,
            "Lo que cuesta cada tecla en esta fase de la run: precio base × descuento de la fase.",
            Uso.EnCombate));
        return g;
    }

    /// <summary>Una stat del motor: su valor ahora y, si alguna regla la mueve, cuanto y por que.</summary>
    private Control Entrada(InfoStat info, Combatiente c, Enma.Core.Reglas.Evaluador ev)
    {
        double operativa = Enma.Core.Stats.Agregacion.StatOperativa(c.BaseRef(info.Stat), c.Desgaste.Acumulado);
        double final = ev.ResolverStat(_e, c, info.Stat);

        string valor = info.Stat == Stat.Hp
            ? Vida(c)
            : info.Porcentaje ? $"{final.ToString("0.#", CultureInfo.InvariantCulture)}%"
            : final.ToString("0", CultureInfo.InvariantCulture);

        string cambio = "";
        Color color = Texto;
        double delta = final - operativa;
        if (Math.Abs(delta) > 1e-6 && info.Stat != Stat.Hp)
        {
            string magnitud = operativa > 1e-9
                ? $"{(Math.Abs(delta / operativa) * 100).ToString("0.#", CultureInfo.InvariantCulture)}%"
                : Math.Abs(delta).ToString("0.#", CultureInfo.InvariantCulture);
            cambio = (delta > 0 ? "▲ +" : "▼ −") + magnitud;
            color = delta > 0 ? Verde : Rojo;
        }

        var ayuda = new List<string>
        {
            info.Ayuda,
            $"Base a nivel {c.Nivel}: {operativa.ToString("0.#", CultureInfo.InvariantCulture)}{(info.Porcentaje ? "%" : "")}",
        };
        foreach (var m in ev.ModificadoresDe(_e, c, info.Stat))
        {
            string cuanto = m.Capa == Enma.Core.Stats.Capa.Flat
                ? m.Valor.ToString("+0.#;−0.#", CultureInfo.InvariantCulture)
                : (m.Valor * 100).ToString("+0.#;−0.#", CultureInfo.InvariantCulture) + "%";
            ayuda.Add($"{cuanto} por {m.Origen}");
        }

        return EntradaLibre(info.Icono, info.Nombre, valor, cambio, color, string.Join("\n", ayuda), info.Uso);
    }

    private static Control EntradaLibre(string icono, string nombre, string valor, string cambio, Color colorCambio,
        string ayuda, Uso uso)
    {
        bool apagada = uso is Uso.AunNo or Uso.SinRitmo;
        var v = Etiqueta(valor, 14, apagada ? new Color(Gris, 0.6f) : Texto);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        v.CustomMinimumSize = new Vector2(52, 0);
        return Fila(icono, nombre, ayuda, uso, v, cambio, colorCambio);
    }

    /// <summary>
    /// Una fila del panel: icono, nombre y lo que vaya a la derecha —un valor
    /// o un campo para editarlo—. La ayuda sale al pasar el raton.
    /// </summary>
    private static Control Fila(string icono, string nombre, string ayuda, Uso uso, Control valor,
        string cambio = "", Color? colorCambio = null)
    {
        bool apagada = uso is Uso.AunNo or Uso.SinRitmo;
        var fila = new HBoxContainer { MouseFilter = MouseFilterEnum.Stop, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        fila.AddThemeConstantOverride("separation", 5);
        fila.TooltipText = $"{nombre}\n{ayuda}" + uso switch
        {
            Uso.AunNo => "\nEl motor aún no la usa.",
            Uso.SinGolpesMagicos => "\nConectada, pero aún no hay golpes mágicos.",
            Uso.SinRitmo => "\nEl motor lo aplica, pero hoy ningún golpe va en tempo: el ritmo aún no está conectado.",
            _ => "",
        };

        var ico = Etiqueta(icono, 15);
        ico.CustomMinimumSize = new Vector2(24, 0);
        ico.HorizontalAlignment = HorizontalAlignment.Center;
        if (apagada) ico.Modulate = new Color(1, 1, 1, 0.4f);
        fila.AddChild(ico);

        var n = Etiqueta(nombre, 13, apagada ? new Color(Gris, 0.6f) : Texto);
        n.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        n.ClipText = true;
        fila.AddChild(n);

        if (cambio.Length > 0) fila.AddChild(Etiqueta(cambio, 12, colorCambio ?? Texto));
        fila.AddChild(valor);
        return fila;
    }

    private static Label Grupo(string nombre)
    {
        var l = Etiqueta(nombre, 12, Dorado);
        l.CustomMinimumSize = new Vector2(0, 20);
        l.VerticalAlignment = VerticalAlignment.Bottom;
        return l;
    }

    private static GridContainer Rejilla(int columnas)
    {
        var g = new GridContainer { Columns = columnas };
        g.AddThemeConstantOverride("h_separation", 14);
        g.AddThemeConstantOverride("v_separation", 0);
        return g;
    }

    /// <summary>Pulsar la ficha enseña sus stats a la derecha.</summary>
    private PanelContainer Inspeccionable(PanelContainer panel, string id)
    {
        panel.MouseFilter = MouseFilterEnum.Stop;
        panel.MouseDefaultCursorShape = CursorShape.PointingHand;
        IgnorarRaton(panel);
        panel.GuiInput += evento =>
        {
            if (evento is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                _inspeccionado = id;
                Callable.From(Refrescar).CallDeferred();
            }
        };

        bool mirado = id == (_inspeccionado ?? Actual()?.Id);
        if (mirado && panel.GetThemeStylebox("panel") is StyleBoxFlat caja)
        {
            var marcada = (StyleBoxFlat)caja.Duplicate();
            marcada.SetBorderWidthAll(2);
            marcada.BorderColor = Cian;
            panel.AddThemeStyleboxOverride("panel", marcada);
        }
        return panel;
    }

    private static void IgnorarRaton(Node nodo)
    {
        foreach (var hijo in nodo.GetChildren())
        {
            if (hijo is Control c) c.MouseFilter = MouseFilterEnum.Ignore;
            IgnorarRaton(hijo);
        }
    }

    private static ProgressBar BarraDeVida(Combatiente c, Color color)
    {
        var barra = new ProgressBar
        {
            MaxValue = Math.Max(1, c.HpMax),
            Value = c.Hp,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(160, 14),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        barra.AddThemeStyleboxOverride("fill", Caja(color, 3, 0));
        barra.AddThemeStyleboxOverride("background", Caja(new Color(0.2f, 0.22f, 0.27f), 3, 0));
        return barra;
    }

    private static PanelContainer Panel(Control contenido, Color? fondo = null)
    {
        var p = new PanelContainer();
        p.AddThemeStyleboxOverride("panel", Caja(fondo ?? Tarjeta, 8, 10));
        p.AddChild(contenido);
        return p;
    }

    private static StyleBoxFlat Caja(Color color, int radio, int margen)
    {
        var caja = new StyleBoxFlat { BgColor = color };
        caja.SetCornerRadiusAll(radio);
        caja.SetContentMarginAll(margen);
        return caja;
    }

    private static HBoxContainer Fila(params Control[] cosas)
    {
        var fila = new HBoxContainer();
        fila.AddThemeConstantOverride("separation", 8);
        foreach (var c in cosas) fila.AddChild(c);
        return fila;
    }

    private static Control Separador() => new Control { CustomMinimumSize = new Vector2(14, 0) };

    private static Button Boton(string texto, Action alPulsar, bool desactivado = false, string ayuda = "")
    {
        var b = new Button { Text = texto, Disabled = desactivado, TooltipText = ayuda, FocusMode = FocusModeEnum.None };
        b.Pressed += () => Callable.From(alPulsar).CallDeferred();
        return b;
    }

    private static Label Etiqueta(string texto, int tamano = 17, Color? color = null)
    {
        var l = new Label { Text = texto, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        l.AddThemeFontSizeOverride("font_size", tamano);
        l.AddThemeColorOverride("font_color", color ?? Texto);
        return l;
    }

    private static void Vaciar(Node nodo)
    {
        foreach (var hijo in nodo.GetChildren())
        {
            nodo.RemoveChild(hijo);
            hijo.QueueFree();
        }
    }

    private string EstadoGratis(bool comida)
    {
        var (usado, espera) = _m.ObjetoGratis(Bando.Aliado, comida);
        return usado ? "ya usado este turno"
            : espera > 0 ? $"vuelve en {espera} turno{(espera == 1 ? "" : "s")}"
            : "listo";
    }

    private static string Num(double x) => x.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Num3(double x) => x.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Dano(double x) => x.ToString("0.0", CultureInfo.InvariantCulture);

    private static string Cm(double x)
        => (Math.Abs(x) < Tolerancia ? 0.0 : x).ToString("0.###", CultureInfo.InvariantCulture);

    private static string Donde(Combatiente c)
        => c.Zona == Zona.Escondite ? "escondido" : c.Posicion.ToString().ToLowerInvariant();

    private static string Vida(Combatiente c)
        => $"{c.Hp.ToString("F0", CultureInfo.InvariantCulture)} / {c.HpMax.ToString("F0", CultureInfo.InvariantCulture)}";

    private static string NombreArma(ClaseArma a) => a switch
    {
        ClaseArma.Ligera => "ligera",
        ClaseArma.Media => "media",
        ClaseArma.Pesada => "pesada",
        _ => a.ToString().ToLowerInvariant(),
    };
}

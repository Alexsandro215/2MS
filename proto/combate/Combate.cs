using System.Globalization;
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

    public override void _Ready()
    {
        var args = Argumentos();
        _semilla = args.TryGetValue("semilla", out var s) && int.TryParse(s, out int n)
            ? n
            : (int)(Time.GetTicksUsec() & 0xFFFF);

        GetWindow().Title = "E.N.M.A. · combate de prueba contra el muñeco";
        Theme = new Theme { DefaultFontSize = 17 };
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

    private static readonly (string Nombre, double Mult)[] Secciones =
        [("Calma ×0.5", 0.5), ("Base ×1", 1.0), ("Estribillo ×1.8", 1.8)];

    // ------------------------------------------------------------ pantalla

    private void Construir()
    {
        var fondo = new ColorRect { Color = Fondo };
        fondo.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(fondo);

        var margen = new MarginContainer();
        margen.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (var lado in new[] { "left", "right", "top", "bottom" })
            margen.AddThemeConstantOverride($"margin_{lado}", 18);
        AddChild(margen);

        var columnas = new HBoxContainer();
        columnas.AddThemeConstantOverride("separation", 18);
        margen.AddChild(columnas);

        // ---- izquierda: estado, tablero y acciones ----
        var izquierda = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.75f };
        izquierda.AddThemeConstantOverride("separation", 12);
        columnas.AddChild(izquierda);

        var cabecera = new VBoxContainer();
        cabecera.AddThemeConstantOverride("separation", 6);
        _cabecera = Etiqueta("", 24, Dorado);
        cabecera.AddChild(_cabecera);

        var filaCm = new HBoxContainer();
        filaCm.AddThemeConstantOverride("separation", 10);
        filaCm.AddChild(Etiqueta("Carga Musical", 17, Gris));
        _barraCm = new ProgressBar
        {
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(360, 22),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _barraCm.AddThemeStyleboxOverride("fill", Caja(Cian, 4, 0));
        _barraCm.AddThemeStyleboxOverride("background", Caja(new Color(0.2f, 0.22f, 0.27f), 4, 0));
        filaCm.AddChild(_barraCm);
        _cm = Etiqueta("", 17);
        filaCm.AddChild(_cm);
        cabecera.AddChild(filaCm);

        _objetos = Etiqueta("", 15, Gris);
        cabecera.AddChild(_objetos);
        _orden = Etiqueta("", 15, Gris);
        _orden.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cabecera.AddChild(_orden);
        izquierda.AddChild(Panel(cabecera));

        var tablero = new HBoxContainer();
        tablero.AddThemeConstantOverride("separation", 12);
        _equipo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.6f };
        _equipo.AddThemeConstantOverride("separation", 8);
        _rival = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _rival.AddThemeConstantOverride("separation", 8);
        tablero.AddChild(_equipo);
        tablero.AddChild(_rival);
        izquierda.AddChild(tablero);

        _acciones = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _acciones.AddThemeConstantOverride("separation", 10);
        izquierda.AddChild(Panel(_acciones));

        // ---- derecha: lo que va pasando ----
        var derecha = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        derecha.AddThemeConstantOverride("separation", 8);
        columnas.AddChild(derecha);

        var arriba = new HBoxContainer();
        arriba.AddChild(new Label { Text = "Registro", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        arriba.AddChild(Boton("Resumen", Resumen));
        arriba.AddChild(Boton("Otra partida", () => Empezar((int)(Time.GetTicksUsec() & 0xFFFF))));
        derecha.AddChild(arriba);

        _bitacora = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SelectionEnabled = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _bitacora.AddThemeFontSizeOverride("normal_font_size", 15);
        _bitacora.AddThemeFontSizeOverride("bold_font_size", 15);
        var panelRegistro = Panel(_bitacora);
        panelRegistro.SizeFlagsVertical = SizeFlags.ExpandFill;
        derecha.AddChild(panelRegistro);
    }

    private void Refrescar()
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
            v.AddChild(Etiqueta("No ataca nunca. EVA 40.", 14, Gris));
            _rival.AddChild(Panel(v));
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

        var cargas = Secciones.Select(s =>
            (Control)Boton(s.Nombre, () => Hacer(new CargarMusica { ActorId = id, MultiplicadorSeccion = s.Mult }),
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

            var quien = Actual();
            int n = int.TryParse(arg, out int v) ? v : 0;
            Accion? accion = quien is null ? null : orden switch
            {
                "basicos" => Basicos(quien),
                "mover" when n is >= 1 and <= 3 => new Moverse { ActorId = quien.Id, Destino = (Casilla)(n - 1) },
                "tecla" when arg.Length == 1 => new UsarHabilidad { ActorId = quien.Id, Tecla = char.ToUpperInvariant(arg[0]) },
                "carga" when n is >= 1 and <= 3 => new CargarMusica { ActorId = quien.Id, MultiplicadorSeccion = Secciones[n - 1].Mult },
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

        return Panel(v, enTurno ? TarjetaActiva : Tarjeta);
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
        p.AddThemeStyleboxOverride("panel", Caja(fondo ?? Tarjeta, 8, 12));
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
        b.Pressed += alPulsar;
        return b;
    }

    private static Label Etiqueta(string texto, int tamano = 17, Color? color = null)
    {
        var l = new Label { Text = texto, VerticalAlignment = VerticalAlignment.Center };
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

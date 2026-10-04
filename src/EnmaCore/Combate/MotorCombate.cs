using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Reglas;

namespace Enma.Core.Combate;

/// <summary>Resultado de intentar ejecutar una accion.</summary>
public sealed record ResultadoAccion(
    bool Permitida,
    string Motivo,
    List<EventoImpacto> Impactos,
    List<Cambio> Cambios,
    bool CierraTurno);

/// <summary>Como termino un combate.</summary>
public enum Desenlace { EnCurso, VictoriaAliada, DerrotaAliada, Huida }

/// <summary>
/// Maquina de turnos. Fuente: manual-tecnico.md  1.4 y  2.
///
///   CICLO = turno completo del bando aliado + turno completo del bando enemigo
///
/// El nucleo no decide nada de presentacion: recibe acciones y devuelve
/// eventos. La IA enemiga es otro cliente de este mismo motor, no una copia
/// paralela que se desincroniza.
/// </summary>
public sealed class MotorCombate
{
    private readonly Evaluador _ev;
    private readonly Resolvedor _res;

    public EstadoCombate Estado { get; }
    public EstadoFlujo Flujo { get; } = new();

    /// <summary>Stacks de erosion de EVA por defensor. Persisten toda la ronda del bando atacante.</summary>
    private readonly Dictionary<string, int> _stacksEva = new();

    /// <summary>Quien ya actuo en este turno de bando.</summary>
    private readonly HashSet<string> _yaActuaron = new();

    /// <summary>Teclas ya consumidas este turno, por personaje.</summary>
    private readonly Dictionary<string, HashSet<char>> _teclasUsadas = new();

    /// <summary>Quien entro en modo QWER: ya no puede volver a basicos.</summary>
    private readonly HashSet<string> _enModoQwer = new();

    /// <summary>
    /// Tandas de basicos lanzadas este turno. Una tanda son todos los golpes
    /// del arma; se lanza una por turno, salvo que algo la doble (Artens W).
    /// </summary>
    private readonly Dictionary<string, int> _tandasDeBasicos = new();

    /// <summary>
    /// Quien ya empezo la ofensiva. Desde ahi solo quedan basicos, QWER o
    /// pasar: se cerraron su movimiento, su ventana de objetos y las demas
    /// vias. El turno se elige UNA vez. Ver manual-tecnico.md  2.1.
    /// </summary>
    private readonly HashSet<string> _enOfensiva = new();

    /// <summary>
    /// REGLA DE COMPROMISO: quien uso un objeto gratis desde la DEFENSIVA
    /// queda anclado ahi el resto de su turno. Ver tablero.md  3.
    /// </summary>
    private readonly HashSet<string> _comprometidos = new();

    /// <summary>Quien abrio el turno del bando y desde que linea.</summary>
    private string? _abridor;
    private int _lineaAbridor;

    /// <summary>Quien esta a mitad de su turno: ha actuado y aun no lo ha cerrado.</summary>
    private string? _enCurso;

    private bool _huida;

    /// <summary>
    /// La comida y el consumible gratis de cada bando: si ya se usaron en
    /// esta ventana, y cuantos turnos del bando faltan para que vuelvan.
    /// </summary>
    private sealed class ObjetosGratis
    {
        public bool ComidaUsada, ConsumibleUsado;
        public int EsperaComida, EsperaConsumible;
    }

    private readonly Dictionary<Bando, ObjetosGratis> _objetos = new()
    {
        [Bando.Aliado] = new(),
        [Bando.Enemigo] = new(),
    };

    /// <summary>
    /// Como esta la comida o el consumible gratis de un bando: si ya se uso
    /// en esta ventana, y cuantos turnos del bando faltan para que vuelva.
    /// Es para mostrarlo: las reglas las aplica el motor.
    /// </summary>
    public (bool UsadoEnEstaVentana, int Espera) ObjetoGratis(Bando bando, bool esComida)
    {
        var v = _objetos[bando];
        return esComida ? (v.ComidaUsada, v.EsperaComida) : (v.ConsumibleUsado, v.EsperaConsumible);
    }

    /// <summary>Lo que cuesta ahora una tecla QWER, segun la fase de la run.</summary>
    public double CosteDeTecla(char tecla)
        => CargaMusical.Coste(char.ToUpperInvariant(tecla), FaseDeLaRun(), ajustes: Estado.AjustesCm);

    public MotorCombate(EstadoCombate estado, Evaluador? evaluador = null)
    {
        Estado = estado;
        _ev = evaluador ?? new Evaluador();
        _res = new Resolvedor(_ev);

        // Lo que hace cada casilla vale en todo combate. Antes solo existia
        // dentro de un test, y la posicion no cambiaba el acierto.
        Tablero.Instalar(estado);
    }

    public Desenlace Desenlace
    {
        get
        {
            if (_huida) return Desenlace.Huida;

            // Cuenta quien esta en el combate. El banquillo no pelea: si
            // el front entero cae, el banquillo solo salva la partida si
            // alguien de el puede entrar por su cuenta.
            bool aliadosVivos = Estado.DelCampo(Bando.Aliado).Any() || HayRelevoPosible(Bando.Aliado);
            bool enemigosVivos = Estado.DelCampo(Bando.Enemigo).Any();
            if (!enemigosVivos) return Desenlace.VictoriaAliada;
            if (!aliadosVivos) return Desenlace.DerrotaAliada;
            return Desenlace.EnCurso;
        }
    }

    private bool HayRelevoPosible(Bando b)
        => Estado.Combatientes.Any(c => c.Bando == b && c.EnKo && c.Zona == Zona.Front)
           && Estado.Del(b).Any(c => c.Zona == Zona.Banquillo && Flujo.PuedeRelevarDesdeBanquillo(Estado, c));

    // ---------------------------------------------------------- orden de turnos

    /// <summary>
    /// El bando activo en orden de turno: de DERECHA A IZQUIERDA, empezando
    /// por quien abrio y dando la vuelta al llegar al extremo. Antes de que
    /// nadie abra, desde el extremo derecho.
    ///
    /// ⚠️ "Derecha" es la linea mas alta. Si en pantalla las lineas se pintan
    /// al reves, se cambia aqui y solo aqui.
    /// </summary>
    private List<Combatiente> Orden()
    {
        var bando = Estado.Combatientes
            .Select((c, i) => (c, i))
            .Where(x => x.c.Bando == Estado.BandoActivo && x.c.Zona != Zona.Banquillo)
            .ToList();
        if (bando.Count == 0) return [];

        int lineas = Math.Max(6, bando.Max(x => x.c.Linea) + 1);
        int inicio = _abridor is null ? lineas - 1 : _lineaAbridor;

        return bando
            .OrderBy(x => ((inicio - x.c.Linea) % lineas + lineas) % lineas)
            .ThenBy(x => x.i)
            .Select(x => x.c)
            .ToList();
    }

    /// <summary>Los que aun no han actuado y pueden hacerlo, EN ORDEN de turno.</summary>
    public IEnumerable<Combatiente> PendientesDeActuar()
        => Orden().Where(c => !_yaActuaron.Contains(c.Id) && Flujo.PuedeActuar(Estado, c));

    /// <summary>
    /// De quien es el turno ahora.
    ///
    /// null mientras nadie haya abierto: el jugador elige quien abre, y puede
    /// ser cualquiera de <see cref="PendientesDeActuar"/>. Desde ahi el orden
    /// sigue solo y nadie se salta su vez.
    /// </summary>
    public Combatiente? ActorDeTurno
    {
        get
        {
            if (_enCurso is not null)
            {
                var c = Estado.Buscar(_enCurso);
                if (c is not null && c.Zona != Zona.Banquillo && Flujo.PuedeActuar(Estado, c))
                    return c;
            }
            return _abridor is null ? null : PendientesDeActuar().FirstOrDefault();
        }
    }

    // ----------------------------------------------------------------- acciones

    private static ResultadoAccion No(string motivo) => new(false, motivo, [], [], false);

    public ResultadoAccion Ejecutar(Accion accion)
    {
        var impactos = new List<EventoImpacto>();
        var cambios = new List<Cambio>();

        var actor = Estado.Buscar(accion.ActorId);
        if (actor is null) return new(false, "actor inexistente", impactos, cambios, false);
        if (actor.EnKo) return new(false, "el actor esta en KO", impactos, cambios, false);
        if (actor.Bando != Estado.BandoActivo) return new(false, "no es su bando", impactos, cambios, false);
        if (!Flujo.PuedeActuar(Estado, actor)) return new(false, "no puede actuar", impactos, cambios, false);

        // El relevo desde el banquillo va por fuera del orden: no es el turno
        // de nadie, lo concede una pasiva.
        bool relevo = accion is Swap && actor.Zona == Zona.Banquillo;
        if (!relevo)
        {
            if (actor.Zona == Zona.Banquillo) return No("esta en el banquillo");
            if (_yaActuaron.Contains(actor.Id)) return No("ya actuo este turno");

            var turno = ActorDeTurno;
            if (turno is not null && turno.Id != actor.Id)
                return No($"es el turno de {turno.Id}");

            if (_enOfensiva.Contains(actor.Id) && accion is not (AtaqueBasico or UsarHabilidad or Pasar))
                return No("ya empezo la ofensiva: solo quedan basicos, QWER o terminar el turno");
        }

        var r = accion switch
        {
            AtaqueBasico b => Basico(b, actor, impactos, cambios),
            UsarHabilidad h => Habilidad(h, actor, impactos, cambios),
            Defender => Defensa(actor, cambios),
            CargarMusica cm => Cargar(cm, actor, cambios),
            Moverse m => Mover(m, actor, cambios),
            UsarObjeto o => Objeto(o, actor, cambios),
            Esconderse h => Esconder(h, actor, cambios),
            SalirDelEscondite => Salir(actor, cambios),
            Swap s => Cambiar(s, actor, cambios),
            Huir => Huida(actor, cambios),
            Pasar => Terminar(actor, "pasa", impactos, cambios),
            _ => No("accion desconocida")
        };

        if (r.Permitida && !relevo)
        {
            if (_abridor is null) { _abridor = actor.Id; _lineaAbridor = actor.Linea; }
            _enCurso = r.CierraTurno ? null : actor.Id;
        }
        return r;
    }

    private ResultadoAccion Basico(AtaqueBasico b, Combatiente actor,
        List<EventoImpacto> impactos, List<Cambio> cambios)
    {
        if (actor.Zona == Zona.Escondite)
            return No("escondido no se puede atacar");

        // Regla de modo: una vez en QWER, no se vuelve a los basicos.
        if (_enModoQwer.Contains(actor.Id))
            return new(false, "ya entro en modo QWER", impactos, cambios, false);

        // La casilla DEFENSIVA prohibe todo AD, incluido el basico.
        if (actor.Posicion == Casilla.Defensiva)
            return new(false, "los basicos estan prohibidos en la casilla defensiva", impactos, cambios, false);

        // Un caido deja de ser objetivo normal. Solo se le vuelve a atacar
        // para REMATARLO: el bando contrario, y pasada su ventana de rescate.
        var objetivo = Estado.Buscar(b.ObjetivoId);
        if (objetivo is null || objetivo.Muerto)
            return new(false, "objetivo invalido", impactos, cambios, false);
        bool remate = objetivo.EnKo;
        if (remate && (!objetivo.Rematable || objetivo.Bando == actor.Bando))
            return No("objetivo invalido: esta en K.O. y aun no se le puede rematar");
        if (objetivo.Zona != Zona.Front)
            return No("objetivo fuera de alcance: escondido o en el banquillo");

        int tandas = _tandasDeBasicos.GetValueOrDefault(actor.Id);
        if (tandas >= Flujo.AccionesDisponibles(Estado, actor))
            return No("ya lanzo sus basicos este turno");

        _tandasDeBasicos[actor.Id] = tandas + 1;
        _enOfensiva.Add(actor.Id);

        int golpes = CargaMusical.Golpes(
            CargaMusical.GolpesBase(actor.Arma),
            _ev.ResolverStat(Estado, actor, Stat.VelocidadAtaque) / 100.0);

        double mult = actor.Arma switch
        {
            ClaseArma.Ligera => 0.6,
            ClaseArma.Media => 1.0,
            ClaseArma.Pesada => 1.5,
            _ => 1.0
        };

        int stacks = _stacksEva.GetValueOrDefault(objetivo.Id);

        // No se puede cambiar de objetivo entre golpe y golpe: todos van al mismo.
        for (int i = 0; i < golpes; i++)
        {
            if (objetivo.Muerto || (objetivo.EnKo && !remate)) break;
            var golpe = new Resolvedor.Golpe(
                new Impacto(TipoDano.Fisico, 0, MultSkill: mult),
                EsBasico: true,
                DobleDesgasteEva: actor.Arma == ClaseArma.Ligera);
            impactos.Add(_res.ResolverImpacto(Estado, actor, objetivo, golpe, ref stacks));
        }

        _stacksEva[objetivo.Id] = stacks;
        Estado.Historial.BasicosLanzados++;
        _ev.Resolver(Estado, new Evento(Disparador.AlUsarBasico, actor.Id, objetivo.Id));

        return new(true, "", impactos, cambios, false);
    }

    private ResultadoAccion Habilidad(UsarHabilidad h, Combatiente actor,
        List<EventoImpacto> impactos, List<Cambio> cambios)
    {
        char tecla = char.ToUpperInvariant(h.Tecla);

        if (actor.Zona == Zona.Escondite)
            return No("escondido no se pueden usar habilidades");

        if (h.ObjetivoId is not null)
        {
            var objetivo = Estado.Buscar(h.ObjetivoId);
            if (objetivo is null)
                return No("objetivo invalido");
            if (objetivo.Zona == Zona.Banquillo
                || (objetivo.Bando != actor.Bando && objetivo.Zona != Zona.Front))
                return No("objetivo fuera de alcance: escondido o en el banquillo");
        }

        if (!_teclasUsadas.TryGetValue(actor.Id, out var usadas))
            _teclasUsadas[actor.Id] = usadas = new HashSet<char>();
        if (usadas.Contains(tecla))
            return new(false, $"la tecla {tecla} ya se uso este turno", impactos, cambios, false);

        double coste = CargaMusical.Coste(tecla, FaseDeLaRun(), ajustes: Estado.AjustesCm);
        var barra = Estado.Barra(actor.Bando);
        // Con tolerancia: con la barra en 1.2 la E de 1.2 se paga, aunque la
        // cuenta en coma flotante diga 1.2000000000000002.
        if (barra.Actual + Constantes.ToleranciaCm < coste)
            return new(false, "CM insuficiente", impactos, cambios, false);

        barra.Actual = Math.Max(0.0, barra.Actual - coste);
        Estado.Historial.CmGastado += coste;
        Estado.Historial.HabilidadesUsadas++;
        usadas.Add(tecla);
        _enModoQwer.Add(actor.Id);
        _enOfensiva.Add(actor.Id);

        // Las reglas de la habilidad son datos: el motor solo dispara el evento.
        int veces = Flujo.RepeticionesDeHabilidad(Estado, actor);
        for (int i = 0; i < veces; i++)
        {
            var r = _ev.Resolver(Estado,
                new Evento(Disparador.AlUsarHabilidad, actor.Id, h.ObjetivoId, coste, tecla.ToString()));
            cambios.AddRange(r.Cambios);
        }

        return new(true, "", impactos, cambios, false);
    }

    private ResultadoAccion Defensa(Combatiente actor, List<Cambio> cambios)
    {
        actor.Flags["defendiendo"] = 1;
        cambios.Add(new Cambio("defensa", actor.Id, "activada", 0));
        return Terminar(actor, "defiende", new List<EventoImpacto>(), cambios);
    }

    private ResultadoAccion Cargar(CargarMusica cm, Combatiente actor, List<Cambio> cambios)
    {
        var barra = Estado.Barra(actor.Bando);
        string equipo = actor.Bando == Bando.Aliado ? "equipo" : "enemigos";

        // CLASICAL multiplica TODA obtencion de CM, tambien la de esta accion,
        // y se multiplica con la seccion. Ver musica.md  3.
        bool yaEncendido = barra.ClasicalActivo;
        double multiplicador = cm.MultiplicadorSeccion
            * (yaEncendido ? Estado.AjustesCm.Clasical : 1.0);

        var (llena, resultante) = CargaMusical.IntentarClasical(
            barra.Actual, barra.Max, actor.Storage, multiplicador);

        barra.Actual = resultante;
        if (llena && !yaEncendido)
        {
            barra.ClasicalActivo = true;
            cambios.Add(new Cambio("clasical", equipo, "encendido", resultante));
        }
        cambios.Add(new Cambio("cm", equipo, "carga-musical", resultante));

        return Terminar(actor, "carga musica", new List<EventoImpacto>(), cambios);
    }

    private ResultadoAccion Mover(Moverse m, Combatiente actor, List<Cambio> cambios)
    {
        if (actor.Zona == Zona.Escondite)
            return No("esta escondido: primero tiene que salir");

        if (actor.TieneFlag("anclado") || actor.TieneFlag("inmovil"))
            return new(false, "esta anclado o inmovil", new List<EventoImpacto>(), cambios, false);

        if (_comprometidos.Contains(actor.Id))
            return No("uso un objeto gratis desde la defensiva: queda anclado ahi este turno");

        actor.Posicion = m.Destino;
        cambios.Add(new Cambio("mover", actor.Id, m.Destino.ToString(), 0));
        _ev.Resolver(Estado, new Evento(Disparador.AlEntrarEnCasilla, actor.Id));
        return new(true, "", new List<EventoImpacto>(), cambios, false);
    }

    /// <summary>
    /// Ventana pre-ofensiva. Ver formulas.md  11 y tablero.md  3.
    ///
    ///   · 1 comida y 1 consumible GRATIS por bando y ventana, independientes
    ///   · cada uno tarda 3 turnos del bando en volver
    ///   · usarlo desde la DEFENSIVA no arranca esa espera... pero ancla ahi
    ///   · usarlo sin que toque —ya usado, o esperando— se puede, pero deja de
    ///     ser gratis: CONSUME EL TURNO y no reinicia la espera
    /// </summary>
    private ResultadoAccion Objeto(UsarObjeto o, Combatiente actor, List<Cambio> cambios)
    {
        var impactos = new List<EventoImpacto>();

        var r = _ev.Resolver(Estado, new Evento(Disparador.AlUsarObjeto, actor.Id, o.ObjetivoId, 0, o.ObjetoId));
        cambios.AddRange(r.Cambios);

        // Un gadget SIEMPRE consume turno.
        if (o.EsGadget) return Terminar(actor, "usa gadget", impactos, cambios);

        var v = _objetos[actor.Bando];
        bool usado = o.EsComida ? v.ComidaUsada : v.ConsumibleUsado;
        int espera = o.EsComida ? v.EsperaComida : v.EsperaConsumible;

        if (usado || espera > 0)
            return Terminar(actor, "usa un objeto que no era gratis", impactos, cambios);

        bool desdeDefensiva = actor.Zona == Zona.Front && actor.Posicion == Casilla.Defensiva;
        int nuevaEspera = desdeDefensiva ? 0 : Constantes.CooldownObjetoGratis;
        if (o.EsComida) { v.ComidaUsada = true; v.EsperaComida = nuevaEspera; }
        else { v.ConsumibleUsado = true; v.EsperaConsumible = nuevaEspera; }

        if (desdeDefensiva)
        {
            _comprometidos.Add(actor.Id);
            cambios.Add(new Cambio("compromiso", actor.Id, "anclado en la defensiva", 0));
        }

        return new(true, "", impactos, cambios, false);
    }

    /// <summary>
    /// HIDE, propio o de un compañero en K.O. Los escondites son del entorno y
    /// los pelean los dos bandos: si no queda ninguno libre, no hay donde ir.
    /// </summary>
    private ResultadoAccion Esconder(Esconderse h, Combatiente actor, List<Cambio> cambios)
    {
        var oculto = h.OcultoId is null ? actor : Estado.Buscar(h.OcultoId);
        if (oculto is null) return No("no existe a quien esconder");
        if (actor.Zona != Zona.Front || oculto.Zona != Zona.Front)
            return No("solo se esconde desde el front");
        if (oculto != actor && (oculto.Bando != actor.Bando || !oculto.EnKo))
            return No("a otro solo se le esconde si es un compañero en K.O.");
        if (Estado.EsconditesLibres <= 0)
            return No("no queda ningun escondite libre");

        oculto.Zona = Zona.Escondite;
        cambios.Add(new Cambio("esconder", oculto.Id, "escondite", 0));
        return Terminar(actor, oculto == actor ? "se esconde" : $"esconde a {oculto.Id}",
            new List<EventoImpacto>(), cambios);
    }

    /// <summary>Salir es gratis, pero siempre a la DEFENSIVA.</summary>
    private ResultadoAccion Salir(Combatiente actor, List<Cambio> cambios)
    {
        if (actor.Zona != Zona.Escondite) return No("no esta escondido");

        actor.Zona = Zona.Front;
        actor.Posicion = Casilla.Defensiva;
        cambios.Add(new Cambio("salir", actor.Id, Casilla.Defensiva.ToString(), 0));
        _ev.Resolver(Estado, new Evento(Disparador.AlEntrarEnCasilla, actor.Id));
        return new(true, "", new List<EventoImpacto>(), cambios, false);
    }

    /// <summary>
    /// SWAP. El que entra hereda la casilla y la linea exactas del que sale, y
    /// no actua hasta el turno siguiente: si lo hiciera, cambiar seria gratis.
    /// </summary>
    private ResultadoAccion Cambiar(Swap s, Combatiente actor, List<Cambio> cambios)
    {
        var entrante = Estado.Buscar(s.EntranteId);
        var saliente = s.SalienteId is null ? actor : Estado.Buscar(s.SalienteId);
        if (entrante is null || saliente is null) return No("cambio invalido");
        if (entrante.Bando != actor.Bando || saliente.Bando != actor.Bando)
            return No("solo se cambia dentro del propio bando");
        if (entrante.Zona != Zona.Banquillo || entrante.EnKo)
            return No("el que entra tiene que estar en el banquillo y en pie");
        if (saliente.Zona != Zona.Front)
            return No("el que sale tiene que estar en el front");

        bool relevo = actor.Zona == Zona.Banquillo;
        if (relevo)
        {
            if (entrante != actor) return No("desde el banquillo solo puede entrar uno mismo");
            if (!saliente.EnKo) return No("el relevo desde el banquillo es para un compañero en K.O.");
            if (!Flujo.PuedeRelevarDesdeBanquillo(Estado, actor))
                return No("relevar desde el banquillo necesita una pasiva que lo permita");
        }
        else if (saliente != actor && !saliente.EnKo)
        {
            return No("a otro solo se le cambia si esta en K.O.");
        }

        entrante.Zona = Zona.Front;
        entrante.Linea = saliente.Linea;
        entrante.Posicion = saliente.Posicion;
        saliente.Zona = Zona.Banquillo;
        _yaActuaron.Add(entrante.Id);

        cambios.Add(new Cambio("swap", saliente.Id, $"entra {entrante.Id}", 0));
        _ev.Resolver(Estado, new Evento(Disparador.AlEntrarEnCasilla, entrante.Id));

        return relevo
            ? new(true, "releva desde el banquillo", new List<EventoImpacto>(), cambios, true)
            : Terminar(actor, "cambio", new List<EventoImpacto>(), cambios);
    }

    /// <summary>
    /// ESCAPE. Huye el equipo entero o no huye nadie; si sale mal, el turno
    /// se pierde igual.
    ///
    /// ⚠️ Por ahora solo huye el bando del jugador: no hay desenlace para un
    /// enemigo que escapa.
    /// </summary>
    private ResultadoAccion Huida(Combatiente actor, List<Cambio> cambios)
    {
        if (actor.Bando != Bando.Aliado) return No("los enemigos no huyen");
        if (!Estado.SePuedeHuir) return No("de este combate no se puede huir");

        bool sale = Estado.Rng.Siguiente() < Estado.ProbabilidadDeHuida;
        if (sale) _huida = true;
        cambios.Add(new Cambio("huida", "equipo", sale ? "lograda" : "fallida", 0));

        return Terminar(actor, sale ? "huye" : "intenta huir y no puede",
            new List<EventoImpacto>(), cambios);
    }

    private ResultadoAccion Terminar(Combatiente actor, string motivo,
        List<EventoImpacto> impactos, List<Cambio> cambios)
    {
        _yaActuaron.Add(actor.Id);
        var r = _ev.Resolver(Estado, new Evento(Disparador.AlTerminarTurno, actor.Id));
        cambios.AddRange(r.Cambios);
        return new(true, motivo, impactos, cambios, true);
    }

    /// <summary>Cierra el turno de un personaje sin mas accion.</summary>
    public void CerrarTurnoDe(string actorId)
    {
        _yaActuaron.Add(actorId);
        if (_enCurso == actorId) _enCurso = null;
        if (_abridor is null && Estado.Buscar(actorId) is { } c)
        {
            _abridor = c.Id;
            _lineaAbridor = c.Linea;
        }
    }

    // -------------------------------------------------------- avance de turnos

    /// <summary>
    /// Pasa al siguiente bando. Al cerrar el turno del bando enemigo se completa
    /// un ciclo: expira la ventana elemental, se rematan los KO no atendidos y
    /// envejecen duraciones.
    /// </summary>
    public List<Cambio> CerrarTurnoDeBando()
    {
        var cambios = new List<Cambio>();

        // El desgaste por esquivar expira al terminar el turno del BANDO atacante.
        _stacksEva.Clear();
        _yaActuaron.Clear();
        _teclasUsadas.Clear();
        _enModoQwer.Clear();
        _tandasDeBasicos.Clear();
        _enOfensiva.Clear();
        _comprometidos.Clear();
        _abridor = null;
        _enCurso = null;

        // Se cierra la ventana de objetos del bando, y su espera avanza un turno.
        var v = _objetos[Estado.BandoActivo];
        v.ComidaUsada = v.ConsumibleUsado = false;
        v.EsperaComida = Math.Max(0, v.EsperaComida - 1);
        v.EsperaConsumible = Math.Max(0, v.EsperaConsumible - 1);

        if (Estado.BandoActivo == Bando.Aliado)
        {
            Estado.BandoActivo = Bando.Enemigo;
            EmpiezaElTurnoDe(Bando.Enemigo);
            return cambios;
        }

        // ---- Fin de ciclo ----
        Estado.BandoActivo = Bando.Aliado;
        Estado.Ciclo++;
        EmpiezaElTurnoDe(Bando.Aliado);

        // La ventana elemental se reinicia al terminar el turno del villano.
        _res.Ventana.Reiniciar();

        cambios.AddRange(Flujo.AvanzarPreparaciones(Estado));
        Flujo.AvanzarTurno();
        _ev.AvanzarTurno(Estado);
        _ev.Resolver(Estado, new Evento(Disparador.AlIniciarCiclo));

        Estado.RecalcularCm();
        return cambios;
    }

    /// <summary>
    /// Lo que pasa al empezar el turno de un bando.
    ///
    /// CLASICAL se apaga: dura hasta el siguiente turno del bando que lo
    /// encendio.
    ///
    /// Y sus caidos cuentan un turno mas en K.O. Ese turno es la VENTANA DE
    /// RESCATE: si al llegar el siguiente turno rival siguen expuestos en el
    /// front, ya se les puede rematar. El remate no es automatico: lo hace un
    /// golpe del rival, y puede fallar. Ver manual-tecnico.md  10.3.
    /// </summary>
    private void EmpiezaElTurnoDe(Bando b)
    {
        Estado.Barra(b).ClasicalActivo = false;

        foreach (var c in Estado.Combatientes.Where(c => c.Bando == b))
            c.TurnosEnKo = c.EnKo && !c.Muerto ? c.TurnosEnKo + 1 : 0;
    }

    private FaseCm FaseDeLaRun()
    {
        int nivel = Estado.Del(Bando.Aliado).Select(c => c.Nivel).DefaultIfEmpty(1).Max();
        return Stats.Escalado.FaseDe(nivel) switch
        {
            Stats.Fase.Early => FaseCm.Early,
            Stats.Fase.Mid => FaseCm.Mid,
            _ => FaseCm.Late
        };
    }
}

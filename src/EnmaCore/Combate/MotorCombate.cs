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

    /// <summary>Objetos gratis consumidos en la ventana actual, por bando.</summary>
    private readonly Dictionary<Bando, (bool Comida, bool Consumible)> _objetosGratis = new();

    public MotorCombate(EstadoCombate estado, Evaluador? evaluador = null)
    {
        Estado = estado;
        _ev = evaluador ?? new Evaluador();
        _res = new Resolvedor(_ev);
        _objetosGratis[Bando.Aliado] = (false, false);
        _objetosGratis[Bando.Enemigo] = (false, false);
    }

    public Desenlace Desenlace
    {
        get
        {
            bool aliadosVivos = Estado.Del(Bando.Aliado).Any();
            bool enemigosVivos = Estado.Del(Bando.Enemigo).Any();
            if (!enemigosVivos) return Desenlace.VictoriaAliada;
            if (!aliadosVivos) return Desenlace.DerrotaAliada;
            return Desenlace.EnCurso;
        }
    }

    /// <summary>Combatientes del bando activo que aun no han actuado y pueden hacerlo.</summary>
    public IEnumerable<Combatiente> PendientesDeActuar()
        => Estado.Del(Estado.BandoActivo)
                 .Where(c => !_yaActuaron.Contains(c.Id) && Flujo.PuedeActuar(Estado, c));

    // ----------------------------------------------------------------- acciones

    public ResultadoAccion Ejecutar(Accion accion)
    {
        var impactos = new List<EventoImpacto>();
        var cambios = new List<Cambio>();

        var actor = Estado.Buscar(accion.ActorId);
        if (actor is null) return new(false, "actor inexistente", impactos, cambios, false);
        if (actor.EnKo) return new(false, "el actor esta en KO", impactos, cambios, false);
        if (actor.Bando != Estado.BandoActivo) return new(false, "no es su bando", impactos, cambios, false);
        if (!Flujo.PuedeActuar(Estado, actor)) return new(false, "no puede actuar", impactos, cambios, false);

        switch (accion)
        {
            case AtaqueBasico b: return Basico(b, actor, impactos, cambios);
            case UsarHabilidad h: return Habilidad(h, actor, impactos, cambios);
            case Defender: return Defensa(actor, cambios);
            case CargarMusica cm: return Cargar(cm, actor, cambios);
            case Moverse m: return Mover(m, actor, cambios);
            case UsarObjeto o: return Objeto(o, actor, cambios);
            case Esconderse: return Esconder(actor, cambios);
            case Pasar: return Terminar(actor, "pasa", impactos, cambios);
            default: return new(false, "accion desconocida", impactos, cambios, false);
        }
    }

    private ResultadoAccion Basico(AtaqueBasico b, Combatiente actor,
        List<EventoImpacto> impactos, List<Cambio> cambios)
    {
        // Regla de modo: una vez en QWER, no se vuelve a los basicos.
        if (_enModoQwer.Contains(actor.Id))
            return new(false, "ya entro en modo QWER", impactos, cambios, false);

        // La casilla DEFENSIVA prohibe todo AD, incluido el basico.
        if (actor.Posicion == Casilla.Defensiva)
            return new(false, "los basicos estan prohibidos en la casilla defensiva", impactos, cambios, false);

        var objetivo = Estado.Buscar(b.ObjetivoId);
        if (objetivo is null || objetivo.EnKo)
            return new(false, "objetivo invalido", impactos, cambios, false);

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
            if (objetivo.EnKo) break;
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

        if (!_teclasUsadas.TryGetValue(actor.Id, out var usadas))
            _teclasUsadas[actor.Id] = usadas = new HashSet<char>();
        if (usadas.Contains(tecla))
            return new(false, $"la tecla {tecla} ya se uso este turno", impactos, cambios, false);

        double coste = CargaMusical.Coste(tecla, FaseDeLaRun());
        if (Estado.CmActual < coste)
            return new(false, "CM insuficiente", impactos, cambios, false);

        Estado.CmActual -= coste;
        Estado.Historial.CmGastado += coste;
        Estado.Historial.HabilidadesUsadas++;
        usadas.Add(tecla);
        _enModoQwer.Add(actor.Id);

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
        var (encendido, resultante) = CargaMusical.IntentarClasical(
            Estado.CmActual, Estado.CmMax, actor.Storage, cm.MultiplicadorSeccion);

        Estado.CmActual = resultante;
        if (encendido)
        {
            Estado.ClasicalActivo = true;
            cambios.Add(new Cambio("clasical", "equipo", "encendido", resultante));
        }
        cambios.Add(new Cambio("cm", "equipo", "carga-musical", resultante));

        return Terminar(actor, "carga musica", new List<EventoImpacto>(), cambios);
    }

    private ResultadoAccion Mover(Moverse m, Combatiente actor, List<Cambio> cambios)
    {
        if (actor.TieneFlag("anclado") || actor.TieneFlag("inmovil"))
            return new(false, "esta anclado o inmovil", new List<EventoImpacto>(), cambios, false);

        actor.Posicion = m.Destino;
        cambios.Add(new Cambio("mover", actor.Id, m.Destino.ToString(), 0));
        _ev.Resolver(Estado, new Evento(Disparador.AlEntrarEnCasilla, actor.Id));
        return new(true, "", new List<EventoImpacto>(), cambios, false);
    }

    private ResultadoAccion Objeto(UsarObjeto o, Combatiente actor, List<Cambio> cambios)
    {
        var impactos = new List<EventoImpacto>();
        var (comida, consumible) = _objetosGratis[actor.Bando];
        bool yaUsado = o.EsComida ? comida : consumible;

        // La casilla DEFENSIVA elimina la espera: usar un objeto gratis desde
        // ahi no arranca su cooldown de 3 turnos.
        bool desdeDefensiva = actor.Posicion == Casilla.Defensiva;

        if (yaUsado && !o.EsGadget && !desdeDefensiva)
            return new(false, "el objeto gratis ya se uso en esta ventana", impactos, cambios, false);

        if (!desdeDefensiva)
            _objetosGratis[actor.Bando] = o.EsComida ? (true, consumible) : (comida, true);

        var r = _ev.Resolver(Estado, new Evento(Disparador.AlUsarObjeto, actor.Id, o.ObjetivoId, 0, o.ObjetoId));
        cambios.AddRange(r.Cambios);

        // Un gadget SIEMPRE consume turno.
        return o.EsGadget
            ? Terminar(actor, "usa gadget", impactos, cambios)
            : new(true, "", impactos, cambios, false);
    }

    private ResultadoAccion Esconder(Combatiente actor, List<Cambio> cambios)
    {
        actor.Flags["oculto"] = 1;
        cambios.Add(new Cambio("esconder", actor.Id, "oculto", 0));
        return Terminar(actor, "se esconde", new List<EventoImpacto>(), cambios);
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
    public void CerrarTurnoDe(string actorId) => _yaActuaron.Add(actorId);

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
        _objetosGratis[Estado.BandoActivo] = (false, false);

        if (Estado.BandoActivo == Bando.Aliado)
        {
            Estado.BandoActivo = Bando.Enemigo;
            return cambios;
        }

        // ---- Fin de ciclo ----
        Estado.BandoActivo = Bando.Aliado;
        Estado.Ciclo++;

        // Un K.O. debe atenderse antes del siguiente turno del villano.
        // Si llega y sigue en K.O. -> remate -> muerte definitiva.
        foreach (var c in Estado.Combatientes.Where(c => c.EnKo && c.Bando == Bando.Aliado))
        {
            if (c.Flags.ContainsKey("atendido")) { c.Flags.Remove("atendido"); continue; }
            cambios.Add(new Cambio("muerte", c.Id, "rematado tras K.O. no atendido", 0));
            _ev.Resolver(Estado, new Evento(Disparador.AlMorir, c.Id));
        }

        // CLASICAL termina al iniciar el siguiente turno aliado.
        Estado.ClasicalActivo = false;

        // La ventana elemental se reinicia al terminar el turno del villano.
        _res.Ventana.Reiniciar();

        cambios.AddRange(Flujo.AvanzarPreparaciones(Estado));
        Flujo.AvanzarTurno();
        _ev.AvanzarTurno(Estado);
        _ev.Resolver(Estado, new Evento(Disparador.AlIniciarCiclo));

        Estado.RecalcularCm();
        return cambios;
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

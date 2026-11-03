using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Reglas;

namespace Enma.Core.Combate;

/// <summary>Lo que ocurrio en un impacto. La presentacion observa esto.</summary>
public sealed record EventoImpacto(
    string AtacanteId,
    string DefensorId,
    bool Acerto,
    double DanoAplicado,
    double AbsorbidoPorEscudo,
    bool Critico,
    double CmAtacante,
    double CmDefensor,
    bool ProvocoKo,
    bool PorUltimaBala,
    string? ElementoAplicado,
    bool Remato = false);

/// <summary>
/// Ventana elemental del ciclo.
///
/// L2 se produce si hay 2 exitos en el MISMO ciclo, por 2 personajes DISTINTOS,
/// sobre el mismo objetivo. Se reinicia al terminar el turno del villano.
/// Ver formulas.md  8.
/// </summary>
public sealed class VentanaElemental
{
    private readonly Dictionary<string, HashSet<string>> _actoresPorObjetivo = new();

    /// <summary>Registra un exito y dice si con el se alcanza L2.</summary>
    public bool RegistrarExito(string objetivoId, string actorId)
    {
        if (!_actoresPorObjetivo.TryGetValue(objetivoId, out var actores))
            _actoresPorObjetivo[objetivoId] = actores = new HashSet<string>();
        actores.Add(actorId);
        return actores.Count >= 2;
    }

    public int ActoresSobre(string objetivoId)
        => _actoresPorObjetivo.TryGetValue(objetivoId, out var a) ? a.Count : 0;

    public void Reiniciar() => _actoresPorObjetivo.Clear();
}

/// <summary>
/// Resolvedor de impactos. Implementa paso a paso el orden canonico de
/// formulas.md  13. El orden no es negociable: es lo que elimina la
/// ambiguedad mas cara de un juego de combate.
/// </summary>
public sealed class Resolvedor(Evaluador evaluador)
{
    private readonly Evaluador _ev = evaluador;

    public VentanaElemental Ventana { get; } = new();

    /// <summary>Descripcion de un golpe a resolver.</summary>
    public readonly record struct Golpe(
        Impacto Perfil,
        string? Elemento = null,
        double ProbabilidadElemento = 0.0,
        bool EsBasico = true,
        bool DobleDesgasteEva = false);

    /// <summary>
    /// Resuelve un impacto completo, con sus ocho pasos.
    /// Muta el estado y devuelve el evento resultante.
    /// </summary>
    public EventoImpacto ResolverImpacto(
        EstadoCombate e, Combatiente atacante, Combatiente defensor, Golpe golpe,
        ref int stacksEvaDefensor)
    {
        // ---- 1. HIT-CHECK. Un fallo NO corta la secuencia. ----
        double press = _ev.ResolverStat(e, atacante, Stat.Press);
        double eva = _ev.ResolverStat(e, defensor, Stat.Eva);
        bool acierto = Acierto.Impacta(press, eva, stacksEvaDefensor, e.Rng);

        if (!acierto)
        {
            // El defensor esquivo: su EVA se erosiona para el resto de la ronda.
            stacksEvaDefensor += golpe.DobleDesgasteEva ? 2 : 1;
            e.Historial.ImpactosFallados++;
            return new EventoImpacto(atacante.Id, defensor.Id, false,
                0, 0, false, 0, 0, false, false, null);
        }

        e.Historial.ImpactosAcertados++;

        // ---- REMATE. A un caido le basta con que el golpe conecte. ----
        // Sirve cualquier dano, y por eso no se calcula: conectar es morir.
        // ⚠️ No genera CM: el contador es de dano conectado, y aqui no hay dano.
        if (defensor.EnKo)
        {
            defensor.Muerto = true;
            _ev.Resolver(e, new Evento(Disparador.AlMorir, defensor.Id, atacante.Id));
            e.RecalcularCm();
            return new EventoImpacto(atacante.Id, defensor.Id, true,
                0, 0, false, 0, 0, false, false, null, Remato: true);
        }

        // ---- INMUNIDAD. El golpe conecta, pero no hace nada. ----
        // El flag existia y solo anulaba la CM: un "invulnerable" que recibia
        // dano entero. Conectar y no doler es lo que da Halltus R.
        if (defensor.TieneFlag("invulnerable"))
            return new EventoImpacto(atacante.Id, defensor.Id, true,
                0, 0, false, 0, 0, false, false, null);

        // ---- 2. DANO ----
        bool critDecidido = e.Rng.Siguiente() * 100.0 < _ev.ResolverStat(e, atacante, Stat.Crit);

        var perfil = golpe.Perfil with
        {
            AtkOfensivo = golpe.Perfil.AtkOfensivo > 0
                ? golpe.Perfil.AtkOfensivo
                : _ev.ResolverStat(e, atacante,
                    golpe.Perfil.Tipo == TipoDano.Magico ? Stat.Ap : Stat.Ad),
            Penetracion = golpe.Perfil.Penetracion > 0
                ? golpe.Perfil.Penetracion
                : _ev.ResolverStat(e, atacante,
                    golpe.Perfil.Tipo == TipoDano.Magico ? Stat.PenMagica : Stat.PenArmadura)
        };

        bool defendActivo = defensor.TieneFlag("defendiendo");
        var def = new Defensor(
            Armadura: _ev.ResolverStat(e, defensor, Stat.Arm) * (defendActivo ? 1.5 : 1.0),
            ResistenciaMagica: _ev.ResolverStat(e, defensor, Stat.Rm) * (defendActivo ? 1.5 : 1.0),
            Escudo: defensor.Escudo,
            DefendActivo: defendActivo);

        var r = Dano.Calcular(perfil, def, critDecidido);

        // La vida solo encaja lo que le quedaba: el exceso del golpe que
        // tumba no es dano hecho, y no debe inflar ninguna cuenta.
        double aVida = Math.Min(r.AplicadoAVida, Math.Max(0.0, defensor.Hp));

        defensor.Escudo -= r.AbsorbidoPorEscudo;
        defensor.Hp -= r.AplicadoAVida;

        // DEFEND se consume en el primer impacto recibido.
        if (defendActivo) defensor.Flags.Remove("defendiendo");

        // ---- 3. POST-HIT INMEDIATO: Carga Musical ----
        // Cada uno alimenta la barra de SU bando, y CLASICAL multiplica solo
        // lo que gana el bando que lo tiene encendido.
        var barraAtacante = e.Barra(atacante.Bando);
        var barraDefensor = e.Barra(defensor.Bando);

        // CLASICAL no regala el tempo: solo multiplica x1.5 lo que se gana.
        // Antes contaba todo golpe como en tempo y la media cobraba x3.
        bool enTempo = atacante.TieneFlag("en_tempo");
        var ajustes = e.AjustesCm;
        double cmAtacante = golpe.EsBasico
            ? CargaMusical.GananciaPorImpacto(atacante.Arma, enTempo, ajustes: ajustes)
            : 0.0;
        double cmDefensor = CargaMusical.GananciaAlRecibir(
            atacante.Arma, defendActivo, defensor.TieneFlag("invulnerable"), ajustes);

        if (barraAtacante.ClasicalActivo) cmAtacante *= ajustes.Clasical;
        if (barraDefensor.ClasicalActivo) cmDefensor *= ajustes.Clasical;

        barraAtacante.Sumar(cmAtacante);
        barraDefensor.Sumar(cmDefensor);

        // ---- 4. PASIVAS ON-HIT / ON-BEING-HIT ----
        _ev.Resolver(e, new Evento(Disparador.AlGolpear, atacante.Id, defensor.Id, r.Final));
        _ev.Resolver(e, new Evento(Disparador.AlRecibirGolpe, defensor.Id, atacante.Id, r.Final));

        // ---- 5. ELEMENTOS ----
        string? elementoAplicado = null;
        if (golpe.Elemento is not null && !defensor.TieneElementoL2
            && !defensor.TieneFlag("inmune_ee")
            && e.Rng.Siguiente() < golpe.ProbabilidadElemento)
        {
            bool alcanzaL2 = Ventana.RegistrarExito(defensor.Id, atacante.Id);
            var existente = defensor.Elementos.Find(x => x.Elemento == golpe.Elemento);

            if (existente is null)
                defensor.Elementos.Add(new MarcaElemental(golpe.Elemento, alcanzaL2 ? 2 : 1, alcanzaL2 ? 3 : 1));
            else if (alcanzaL2)
            {
                existente.Nivel = 2;
                existente.TurnosRestantes = 3;
            }

            elementoAplicado = golpe.Elemento;
            _ev.Resolver(e, new Evento(Disparador.AlAplicarElemento, atacante.Id, defensor.Id));
        }

        // ---- 6 y 7. KO, MUERTE y ULTIMA BALA ----
        bool ko = false, porUltimaBala = false;

        if (defensor.Hp <= 0)
        {
            defensor.Hp = 0;
            ko = true;
        }
        else
        {
            // Ultima Bala: condicionador POST-DANO. No altera el dano.
            // No cuenta escudos, solo HP real.
            double umbral = _ev.ResolverStat(e, atacante, Stat.UltimaBala) / 100.0;
            if (umbral > 0 && defensor.HpPct <= umbral)
            {
                ko = true;
                porUltimaBala = true;
                defensor.Hp = 0;
            }
        }

        if (ko)
        {
            defensor.EnKo = true;
            defensor.TurnosEnKo = 0;
            _ev.Resolver(e, new Evento(Disparador.AlCaerEnKo, defensor.Id, atacante.Id));
            // ---- 8. Recalcular capacidad de CM y recortar de inmediato ----
            e.RecalcularCm();
        }

        return new EventoImpacto(
            atacante.Id, defensor.Id, true,
            aVida, r.AbsorbidoPorEscudo, r.CriticoAplicado,
            cmAtacante, cmDefensor, ko, porUltimaBala, elementoAplicado);
    }
}

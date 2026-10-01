using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Simulacion;
using Xunit;

namespace Enma.Core.Tests;

public class SimulacionTests
{
    /// <summary>Dos contra dos, con un suplente y un escondite.</summary>
    private static EstadoCombate Encuentro(int semilla)
    {
        static Combatiente Pj(string id, Bando b, ClaseArma arma, int linea, Zona zona = Zona.Front)
        {
            var c = Duelo.Peleador(Escenario.Pj(id, b, Casilla.Neutral, linea: linea), hp: 800);
            c.Arma = arma;
            c.Zona = zona;
            c.MaxAspirable[Stat.Crit] = 0;
            return c;
        }

        var e = Escenario.Combate(
            Pj("ligera", Bando.Aliado, ClaseArma.Ligera, 1),
            Pj("pesada", Bando.Aliado, ClaseArma.Pesada, 0),
            Pj("suplente", Bando.Aliado, ClaseArma.Media, 0, Zona.Banquillo),
            Pj("rival1", Bando.Enemigo, ClaseArma.Media, 1),
            Pj("rival2", Bando.Enemigo, ClaseArma.Media, 0));
        e.Rng = new AleatorioSemilla(semilla);
        e.Escondites = 1;
        return e;
    }

    [Fact]
    public void Un_combate_simulado_llega_a_su_final_sin_acciones_ilegales()
    {
        var r = Simulador.Jugar(Encuentro(1), new IABasica { Remata = false }, new IABasica { Rescata = false });

        Assert.NotEqual(Desenlace.EnCurso, r.Desenlace);
        Assert.Equal(0, r.AccionesRechazadas);
    }

    [Fact]
    public void Con_la_misma_semilla_sale_el_mismo_combate()
    {
        // Es lo que permite comparar dos builds sin que el azar contamine.
        Assert.Equal(Huella(5), Huella(5));
        Assert.NotEqual(Huella(5), Huella(6));

        static string Huella(int semilla)
        {
            var r = Simulador.Jugar(Encuentro(semilla), new IABasica(), new IABasica());
            return $"{r.Desenlace}:{r.Ciclos}:" + string.Join(",",
                r.PorCombatiente.Select(x => $"{x.Key}={x.Value.Dano:F1}"));
        }
    }

    [Fact]
    public void El_lote_cuenta_todos_los_combates()
    {
        var l = Simulador.Lote(Encuentro, 50, new IABasica(), new IABasica());

        Assert.Equal(50, l.Combates);
        Assert.Equal(50, l.Victorias + l.Derrotas + l.Huidas + l.SinResolver);
        Assert.Equal(0, l.AccionesRechazadas);
    }

    // ------------------------------------------------------------- la IA

    [Fact]
    public void Abren_las_ligeras()
    {
        var m = Mesa.Montar();
        m.Estado.Buscar("a2")!.Arma = ClaseArma.Ligera;

        var abre = new IABasica().ElegirAbridor(m, m.PendientesDeActuar().ToList());
        Assert.Equal("a2", abre.Id);
    }

    [Fact]
    public void La_IA_saca_del_front_a_un_caido()
    {
        var m = Mesa.Montar();
        m.Estado.Buscar("a1")!.EnKo = true;

        var swap = Assert.IsType<Swap>(new IABasica().Turno(m, m.Estado.Buscar("a2")!).First());
        Assert.Equal("a1", swap.SalienteId);
        Assert.Equal("suplente", swap.EntranteId);
    }

    [Fact]
    public void Sin_nadie_en_el_banquillo_la_IA_lo_esconde()
    {
        var m = Mesa.Montar(escondites: 1);
        m.Estado.Buscar("suplente")!.EnKo = true;    // no hay recambio
        m.Estado.Buscar("a1")!.EnKo = true;

        var h = Assert.IsType<Esconderse>(new IABasica().Turno(m, m.Estado.Buscar("a2")!).First());
        Assert.Equal("a1", h.OcultoId);
    }

    [Fact]
    public void La_IA_remata_antes_que_seguir_pegando()
    {
        var m = Mesa.Montar();
        m.Estado.Buscar("a1")!.EnKo = true;
        m.OtroCiclo();
        m.CerrarTurnoDeBando();                      // el turno del remate

        var basico = new IABasica().Turno(m, m.Estado.Buscar("rival")!).OfType<AtaqueBasico>().First();
        Assert.Equal("a1", basico.ObjetivoId);
    }
}

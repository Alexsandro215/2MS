using System.Linq;
using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Reglas;
using Enma.Core.Simulacion;
using Enma.Core.Stats;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// Halltus es el contrario de Kronwell: ni una de sus teclas pega. Prueba la
/// otra mitad del motor —selectores por casilla y por estado, y buffs que
/// duran— que un kit de daño no toca.
/// </summary>
public class HalltusEnCombateTests
{
    private readonly Evaluador _ev = new();

    /// <summary>El equipo de prueba, con la pesada de Halltus.</summary>
    private static MotorCombate Montar(int semilla = 1)
    {
        var e = EncuentrosDePrueba.ContraMuneco(semilla);
        e.ReglasActivas.AddRange(Kits.Halltus("pesada"));
        e.Barra(Bando.Aliado).Actual = e.CmMax;
        return new MotorCombate(e);
    }

    [Fact]
    public void La_Q_sube_el_AD_de_los_companeros_pero_no_el_suyo()
    {
        var m = Montar();
        var ev = new Evaluador();
        var ligera = m.Estado.Buscar("ligera")!;
        var halltus = m.Estado.Buscar("pesada")!;

        double antesAliado = ev.ResolverStat(m.Estado, ligera, Stat.Ad);
        double antesSuyo = ev.ResolverStat(m.Estado, halltus, Stat.Ad);

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'q' });

        Assert.True(r.Permitida, r.Motivo);
        Assert.True(ev.ResolverStat(m.Estado, ligera, Stat.Ad) > antesAliado,
            "el compañero tiene que subir");
        Assert.Equal(antesSuyo, ev.ResolverStat(m.Estado, halltus, Stat.Ad), 3);
    }

    [Fact]
    public void El_buff_de_la_Q_caduca_por_turnos()
    {
        // Regla.Duracion existia y no la miraba nadie: un buff duraba para
        // siempre. Este test es el que lo fija.
        var m = Montar();
        var ev = new Evaluador();
        var ligera = m.Estado.Buscar("ligera")!;
        double base_ = ev.ResolverStat(m.Estado, ligera, Stat.Ad);

        m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'q' });
        Assert.True(ev.ResolverStat(m.Estado, ligera, Stat.Ad) > base_);

        ev.AvanzarTurno(m.Estado);
        Assert.True(ev.ResolverStat(m.Estado, ligera, Stat.Ad) > base_, "aun le queda un turno");

        ev.AvanzarTurno(m.Estado);
        Assert.Equal(base_, ev.ResolverStat(m.Estado, ligera, Stat.Ad), 3);
    }

    [Fact]
    public void Repetir_la_Q_refresca_el_buff_y_no_lo_apila()
    {
        var m = Montar();
        var ev = new Evaluador();
        var ligera = m.Estado.Buscar("ligera")!;

        m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'q' });
        double unaVez = ev.ResolverStat(m.Estado, ligera, Stat.Ad);

        // Otro turno, misma tecla.
        m.CerrarTurnoDeBando();
        m.CerrarTurnoDeBando();
        m.Estado.Barra(Bando.Aliado).Actual = m.Estado.CmMax;
        m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'q' });

        Assert.Equal(unaVez, ev.ResolverStat(m.Estado, ligera, Stat.Ad), 3);
    }

    [Fact]
    public void La_W_limpia_los_debuffs_de_los_companeros()
    {
        var m = Montar();
        var ligera = m.Estado.Buscar("ligera")!;
        ligera.Flags["anclado"] = 3;

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'w' });

        Assert.True(r.Permitida, r.Motivo);
        Assert.False(ligera.TieneFlag("anclado"));
    }

    [Fact]
    public void La_EVA_de_la_W_escala_con_cuantos_companeros_tenian_debuffs()
    {
        var ev = new Evaluador();

        double EvaTrasLimpiar(int cuantosSucios)
        {
            var m = Montar();
            var sucios = new[] { "ligera", "media" }.Take(cuantosSucios);
            foreach (var id in sucios) m.Estado.Buscar(id)!.Flags["anclado"] = 3;

            var halltus = m.Estado.Buscar("pesada")!;
            m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'w' });
            return ev.ResolverStat(m.Estado, halltus, Stat.Eva);
        }

        double ninguno = EvaTrasLimpiar(0);
        double uno = EvaTrasLimpiar(1);
        double dos = EvaTrasLimpiar(2);

        Assert.True(uno > ninguno, $"con 1 sucio ({uno:F1}) deberia pasar de {ninguno:F1}");
        Assert.True(dos > uno, $"con 2 sucios ({dos:F1}) deberia pasar de {uno:F1}");
    }

    [Fact]
    public void La_E_solo_alcanza_a_los_companeros_en_NEUTRAL()
    {
        var m = Montar();
        var ev = new Evaluador();
        var enNeutral = m.Estado.Buscar("ligera")!;
        var fuera = m.Estado.Buscar("media")!;
        fuera.Posicion = Casilla.Ofensiva;

        double antesDentro = ev.ResolverStat(m.Estado, enNeutral, Stat.Arm);
        double antesFuera = ev.ResolverStat(m.Estado, fuera, Stat.Arm);

        m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'e' });

        Assert.True(ev.ResolverStat(m.Estado, enNeutral, Stat.Arm) > antesDentro);
        Assert.Equal(antesFuera, ev.ResolverStat(m.Estado, fuera, Stat.Arm), 3);
    }

    [Fact]
    public void La_R_da_inmunidad_solo_a_los_expuestos_y_con_vida_suficiente()
    {
        var m = Montar();
        var expuesto = m.Estado.Buscar("ligera")!;
        var tocado = m.Estado.Buscar("media")!;
        var atras = m.Estado.Buscar("pesada")!;

        expuesto.Posicion = Casilla.Ofensiva;
        tocado.Posicion = Casilla.Ofensiva;
        tocado.Hp = tocado.HpMax * 0.2;      // por debajo del minimo

        m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'r' });

        Assert.True(expuesto.TieneFlag("invulnerable"), "en ofensiva y entero: protegido");
        Assert.False(tocado.TieneFlag("invulnerable"), "en ofensiva pero tocado: no");
        Assert.False(atras.TieneFlag("invulnerable"), "no esta en ofensiva: no");
    }

    [Fact]
    public void Un_inmune_encaja_el_golpe_sin_recibir_dano()
    {
        // El flag existia y solo anulaba la CM: pegarle a un "invulnerable" le
        // quitaba la vida entera igual.
        var e = EncuentrosDePrueba.ContraMuneco(1);
        var ligera = e.Buscar("ligera")!;
        ligera.Flags["invulnerable"] = 2;
        ligera.MaxAspirable[Stat.Eva] = 0;
        var muneco = e.Buscar("muneco")!;
        muneco.MaxAspirable[Stat.Ad] = 500;
        muneco.MaxAspirable[Stat.Press] = 10_000;
        e.BandoActivo = Bando.Enemigo;
        var m = new MotorCombate(e);

        double antes = ligera.Hp;
        var r = m.Ejecutar(new AtaqueBasico { ActorId = "muneco", ObjetivoId = "ligera" });

        Assert.NotEmpty(r.Impactos);
        Assert.All(r.Impactos, i => Assert.True(i.Acerto));
        Assert.Equal(antes, ligera.Hp, 3);
    }

    [Fact]
    public void Dos_kits_en_el_mismo_combate_no_se_disparan_entre_si()
    {
        // El evento AlUsarHabilidad solo dice que ALGUIEN pulso una tecla. Si
        // la regla no exige ademas ser de quien la pulsa, la R de Halltus
        // lanzaba tambien la R de Kronwell: desgaste y explosion incluidos.
        var m = Montar();                       // media lleva Kronwell, pesada Halltus
        var kronwell = m.Estado.Buscar("media")!;

        m.Ejecutar(new UsarHabilidad { ActorId = "pesada", Tecla = 'r' });

        Assert.Equal(0.0, kronwell.Desgaste.Acumulado, 3);
    }
}

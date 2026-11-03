using System.Linq;
using Enma.Core;
using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Reglas;
using Enma.Core.Simulacion;
using Enma.Core.Stats;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// El primer kit colgado de un combatiente de verdad y disparado por el motor.
///
/// Los tests de KitsTests.cs prueban que el ESQUEMA aguanta cada habilidad.
/// Estos prueban el otro lado: que una tecla pulsada en el motor acabe en daño
/// sobre un cuerpo, pasando por el hit-check y la Carga Musical como un basico.
/// </summary>
public class KronwellEnCombateTests
{
    /// <summary>El muñeco quieto, sin esquivar ni criticar: solo se mide el daño.</summary>
    private static MotorCombate Montar(int semilla = 1)
    {
        var e = EncuentrosDePrueba.ContraMuneco(semilla);
        var muneco = e.Buscar("muneco")!;
        muneco.MaxAspirable[Stat.Eva] = 0;    // que no esquive
        var media = e.Buscar("media")!;
        media.MaxAspirable[Stat.Crit] = 0;    // que no critique
        e.Barra(Bando.Aliado).Actual = e.CmMax;
        return new MotorCombate(e);
    }

    [Fact]
    public void La_Q_de_Kronwell_hace_dano_magico_al_muneco()
    {
        var m = Montar();
        var muneco = m.Estado.Buscar("muneco")!;
        double antes = muneco.Hp;

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'q', ObjetivoId = "muneco" });

        Assert.True(r.Permitida, r.Motivo);
        Assert.NotEmpty(r.Impactos);
        Assert.True(muneco.Hp < antes, "la Q tiene que quitar vida");
    }

    [Fact]
    public void Cada_tecla_dispara_solo_su_regla()
    {
        // Sin la condicion de tecla, pulsar W disparaba tambien Q, E y R.
        var m = Montar();
        var muneco = m.Estado.Buscar("muneco")!;
        double antes = muneco.Hp;

        // La W no pega: solo concentra y da cargas.
        var r = m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'w' });

        Assert.True(r.Permitida, r.Motivo);
        Assert.Empty(r.Impactos);
        Assert.Equal(antes, muneco.Hp, 3);
        Assert.Equal(2.0, m.Estado.Buscar("media")!.Contador("cargas_kronwell"), 3);
    }

    [Fact]
    public void La_E_escala_con_las_cargas_que_dejaron_Q_y_W()
    {
        var m = Montar();
        var media = m.Estado.Buscar("media")!;
        var muneco = m.Estado.Buscar("muneco")!;

        // Q y W acumulan: 2 + 2 = 4 cargas.
        m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'q', ObjetivoId = "muneco" });
        m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'w' });
        Assert.Equal(4.0, media.Contador("cargas_kronwell"), 3);

        double antesDeLaE = muneco.Hp;
        var r = m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'e', ObjetivoId = "muneco" });
        double danoE = antesDeLaE - muneco.Hp;

        Assert.True(r.Permitida, r.Motivo);
        Assert.True(danoE > 0, "la E con 4 cargas tiene que pegar");

        // Y las gasta: la barra de cargas queda a cero.
        Assert.Equal(0.0, media.Contador("cargas_kronwell"), 3);
    }

    [Fact]
    public void Sin_cargas_la_E_no_pide_ningun_golpe()
    {
        // n = 0 no es un golpe de 0: un impacto de 0 consumiria el hit-check,
        // erosionaria la EVA del rival y generaria Carga Musical.
        var m = Montar();
        var muneco = m.Estado.Buscar("muneco")!;
        double antes = muneco.Hp;

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'e', ObjetivoId = "muneco" });

        Assert.True(r.Permitida, r.Motivo);
        Assert.Empty(r.Impactos);
        Assert.Equal(antes, muneco.Hp, 3);
    }

    [Fact]
    public void La_concentracion_de_la_W_sube_el_golpe_siguiente_y_se_gasta()
    {
        var media1 = Montar();
        var media2 = Montar();

        // Camino A: Q sola.
        var m1 = media1.Estado.Buscar("muneco")!;
        double hp1 = m1.Hp;
        media1.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'q', ObjetivoId = "muneco" });
        double sinConcentrar = hp1 - m1.Hp;

        // Camino B: W y luego Q.
        var m2 = media2.Estado.Buscar("muneco")!;
        media2.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'w' });
        double hp2 = m2.Hp;
        media2.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'q', ObjetivoId = "muneco" });
        double concentrada = hp2 - m2.Hp;

        Assert.True(concentrada > sinConcentrar,
            $"concentrada {concentrada:F1} deberia pasar de {sinConcentrar:F1}");

        // Y se gasta en ese golpe: no queda para el siguiente.
        Assert.Equal(0.0, media2.Estado.Buscar("media")!.Contador("concentracion_kronwell"), 3);
    }

    [Fact]
    public void La_R_cobra_el_70_por_ciento_de_desgaste()
    {
        var m = Montar();
        var media = m.Estado.Buscar("media")!;

        m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'q', ObjetivoId = "muneco" });
        Assert.Equal(0.0, media.Desgaste.Acumulado, 3);

        m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'r', ObjetivoId = "muneco" });

        Assert.Equal(0.70, media.Desgaste.Acumulado, 3);
    }

    [Fact]
    public void El_golpe_de_habilidad_pasa_por_el_hit_check_como_un_basico()
    {
        // No hay dos caminos de daño: si la habilidad no pasara por el
        // resolvedor, la EVA no la afectaria y seria daño garantizado.
        var e = EncuentrosDePrueba.ContraMuneco(3);
        e.Buscar("muneco")!.MaxAspirable[Stat.Eva] = 100_000;   // imposible acertar
        e.Barra(Bando.Aliado).Actual = e.CmMax;
        var m = new MotorCombate(e);

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'q', ObjetivoId = "muneco" });

        Assert.True(r.Permitida, r.Motivo);
        Assert.NotEmpty(r.Impactos);
        Assert.All(r.Impactos, i => Assert.False(i.Acerto));
    }

    [Fact]
    public void La_R_alcanza_a_todo_el_frente_rival_y_la_E_solo_a_uno()
    {
        var e = EncuentrosDePrueba.ContraMuneco(1);
        // Un segundo enemigo delante, para notar la diferencia de alcance.
        var otro = e.Buscar("muneco")!;
        e.Combatientes.Add(new Combatiente
        {
            Id = "muneco2", Nombre = "Muñeco 2", Bando = Bando.Enemigo,
            Arma = ClaseArma.Media, Linea = 1, Nivel = Constantes.NivelMax, Zona = Zona.Front
        });
        var m2 = e.Buscar("muneco2")!;
        foreach (Stat s in System.Enum.GetValues<Stat>()) m2.MaxAspirable[s] = 0;
        m2.MaxAspirable[Stat.Hp] = 1_000_000;
        m2.Hp = m2.HpMax;
        otro.MaxAspirable[Stat.Eva] = 0;

        e.RecalcularCm();
        e.Barra(Bando.Aliado).Actual = e.CmMax;
        var m = new MotorCombate(e);
        m.Estado.Buscar("media")!.MaxAspirable[Stat.Crit] = 0;

        // Con cargas puestas por la W.
        m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'w' });

        double hp2Antes = m2.Hp;
        var rR = m.Ejecutar(new UsarHabilidad { ActorId = "media", Tecla = 'r', ObjetivoId = "muneco" });

        Assert.True(rR.Permitida, rR.Motivo);
        Assert.Equal(2, rR.Impactos.Count);             // los dos muñecos
        Assert.True(m2.Hp < hp2Antes, "la R tiene que alcanzar al segundo");
    }
}

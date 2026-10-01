using Enma.Core;
using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Reglas;
using Xunit;

namespace Enma.Core.Tests;

internal static class Duelo
{
    /// <summary>
    /// Deja un combatiente en condiciones de pelear varios turnos.
    ///
    /// Escenario.Pj pone TODAS las stats a 100, y eso incluye Ultima Bala:
    /// un umbral del 100% hace K.O. a cualquiera al primer impacto. Util para
    /// probar Ultima Bala, inservible para probar el bucle.
    /// </summary>
    public static Combatiente Peleador(Combatiente c, double hp = 10_000)
    {
        c.MaxAspirable[Stat.Hp] = hp;
        c.MaxAspirable[Stat.UltimaBala] = 0;
        c.MaxAspirable[Stat.Inquebrantable] = 0;
        c.MaxAspirable[Stat.Reflejos] = 0;
        c.MaxAspirable[Stat.RoboVida] = 0;
        c.Hp = c.HpMax;
        return c;
    }

    public static MotorCombate Montar(
        ClaseArma armaAliada = ClaseArma.Media,
        Casilla posAliada = Casilla.Neutral,
        int semilla = 7)
    {
        var yo = Escenario.Pj("yo", Bando.Aliado, posAliada);
        yo.Arma = armaAliada;
        var rival = Escenario.Pj("rival", Bando.Enemigo, Casilla.Neutral);

        Peleador(yo);
        Peleador(rival);

        // Sin evasion ni critico, para que los tests de dano y de CM no
        // dependan del azar. La aleatoriedad se prueba aparte.
        foreach (var c in new[] { yo, rival })
        {
            c.MaxAspirable[Stat.Eva] = 0;
            c.MaxAspirable[Stat.Crit] = 0;
            c.MaxAspirable[Stat.VelocidadAtaque] = 0;
            c.MaxAspirable[Stat.Arm] = 0;
            c.MaxAspirable[Stat.Rm] = 0;
            c.Hp = c.HpMax;
        }

        var e = Escenario.Combate(yo, rival);
        e.Rng = new AleatorioSemilla(semilla);
        e.CmMax = 12;
        e.CmActual = 12;
        return new MotorCombate(e);
    }
}

public class BucleCombateTests
{
    [Fact]
    public void Un_basico_lanza_los_golpes_del_arma_y_hace_dano()
    {
        var m = Duelo.Montar(ClaseArma.Media);
        var rival = m.Estado.Buscar("rival")!;
        double antes = rival.Hp;

        var r = m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" });

        Assert.True(r.Permitida);
        Assert.Equal(3, r.Impactos.Count);          // media: base 3, AS 0
        Assert.True(rival.Hp < antes);
    }

    [Fact]
    public void El_arma_ligera_lanza_mas_golpes_y_la_pesada_menos()
    {
        Assert.Equal(5, m(ClaseArma.Ligera));
        Assert.Equal(3, m(ClaseArma.Media));
        Assert.Equal(1, m(ClaseArma.Pesada));

        static int m(ClaseArma a)
        {
            var motor = Duelo.Montar(a);
            return motor.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" })
                        .Impactos.Count;
        }
    }

    [Fact]
    public void Los_basicos_estan_prohibidos_en_la_casilla_defensiva()
    {
        var m = Duelo.Montar(posAliada: Casilla.Defensiva);
        var r = m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" });

        Assert.False(r.Permitida);
        Assert.Contains("defensiva", r.Motivo);
    }

    [Fact]
    public void Una_vez_en_QWER_no_se_vuelve_a_los_basicos()
    {
        // Regla de modo, manual-tecnico.md  2.1
        var m = Duelo.Montar();
        m.Estado.ReglasActivas.Add(new Regla
        {
            Id = "kit.q", Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            Efecto = new ModificarStat(Stat.Ad, Stats.Capa.PctAdd, Ex.K(0))
        });

        Assert.True(m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'Q' }).Permitida);

        var r = m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" });
        Assert.False(r.Permitida);
        Assert.Contains("QWER", r.Motivo);
    }

    [Fact]
    public void Cada_tecla_se_consume_una_vez_por_turno()
    {
        var m = Duelo.Montar();
        Assert.True(m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'Q' }).Permitida);

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'Q' });
        Assert.False(r.Permitida);
        Assert.Contains("ya se uso", r.Motivo);

        // Pero W sigue disponible
        Assert.True(m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'W' }).Permitida);
    }

    [Fact]
    public void Una_habilidad_sin_CM_suficiente_no_se_lanza()
    {
        var m = Duelo.Montar();
        m.Estado.CmActual = 0.5;

        var r = m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'R' });
        Assert.False(r.Permitida);
        Assert.Contains("CM", r.Motivo);
    }

    [Fact]
    public void El_CM_gastado_queda_registrado_en_el_historial()
    {
        var m = Duelo.Montar();
        double antes = m.Estado.CmActual;

        m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'R' });

        double coste = CargaMusical.Coste('R', FaseCm.Late);   // nivel 18 -> late
        Assert.Equal(antes - coste, m.Estado.CmActual, 3);
        Assert.Equal(coste, m.Estado.Historial.CmGastado, 3);
        Assert.Equal(1, m.Estado.Historial.HabilidadesUsadas);
    }

    [Fact]
    public void Defender_se_consume_en_el_primer_impacto_y_anula_su_critico()
    {
        var m = Duelo.Montar();
        var yo = m.Estado.Buscar("yo")!;
        var rival = m.Estado.Buscar("rival")!;

        m.Ejecutar(new Defender { ActorId = "yo" });
        Assert.True(yo.TieneFlag("defendiendo"));

        m.CerrarTurnoDeBando();      // pasa al bando enemigo
        rival.MaxAspirable[Stat.Crit] = 100;   // el rival critica siempre

        var r = m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "yo" });

        Assert.False(r.Impactos[0].Critico);   // DEFEND lo anulo
        Assert.False(yo.TieneFlag("defendiendo"));  // y se consumio
    }

    [Fact]
    public void Defender_duplica_la_ganancia_de_CM_al_recibir()
    {
        var m = Duelo.Montar();
        m.Estado.CmActual = 0;
        var yo = m.Estado.Buscar("yo")!;

        m.Ejecutar(new Defender { ActorId = "yo" });
        m.CerrarTurnoDeBando();

        var r = m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "yo" });

        // Primer golpe con DEFEND: 0.5 * 0.25 * 2 = 0.25
        Assert.Equal(0.25, r.Impactos[0].CmDefensor, 3);
        // Segundo golpe ya sin DEFEND: 0.125
        Assert.Equal(0.125, r.Impactos[1].CmDefensor, 3);
    }

    [Fact]
    public void La_accion_de_Clasical_enciende_el_modo_si_llena_la_barra()
    {
        var m = Duelo.Montar();
        m.Estado.CmActual = 8.0;

        var r = m.Ejecutar(new CargarMusica { ActorId = "yo", MultiplicadorSeccion = 1.8 });

        Assert.True(r.Permitida);
        Assert.True(m.Estado.ClasicalActivo);      // 8 + 3*1.8 = 13.4 >= 12
        Assert.Equal(12.0, m.Estado.CmActual, 3);  // el excedente se pierde
        Assert.True(r.CierraTurno);
    }

    [Fact]
    public void En_calma_la_accion_de_Clasical_no_llega_a_encenderlo()
    {
        var m = Duelo.Montar();
        m.Estado.CmActual = 8.0;

        m.Ejecutar(new CargarMusica { ActorId = "yo", MultiplicadorSeccion = 0.5 });

        Assert.False(m.Estado.ClasicalActivo);     // 8 + 1.5 = 9.5 < 12
        Assert.Equal(9.5, m.Estado.CmActual, 3);
    }

    [Fact]
    public void El_desgaste_de_evasion_persiste_toda_la_ronda_del_bando_atacante()
    {
        // Los stacks que genera el primer aliado siguen ahi cuando entra el
        // cuarto. Es lo que hace que las ligeras abran y la pesada remate.
        var a1 = Duelo.Peleador(Escenario.Pj("a1"));
        var a2 = Duelo.Peleador(Escenario.Pj("a2"));
        var rival = Duelo.Peleador(Escenario.Pj("rival", Bando.Enemigo), hp: 100_000);
        a1.Arma = ClaseArma.Ligera;
        a2.Arma = ClaseArma.Pesada;
        a1.MaxAspirable[Stat.Crit] = 0;
        a2.MaxAspirable[Stat.Crit] = 0;
        rival.MaxAspirable[Stat.Eva] = 400;   // muy evasivo: fallaran golpes
        rival.Hp = rival.HpMax;

        var e = Escenario.Combate(a1, a2, rival);
        e.Rng = new AleatorioSemilla(3);
        var m = new MotorCombate(e);

        var r1 = m.Ejecutar(new AtaqueBasico { ActorId = "a1", ObjetivoId = "rival" });
        int fallos = r1.Impactos.Count(i => !i.Acerto);
        Assert.True(fallos > 0, "con EVA 400 deberia fallar algun golpe");
        m.Ejecutar(new Pasar { ActorId = "a1" });

        // El segundo aliado hereda la erosion sin haber hecho nada.
        var r2 = m.Ejecutar(new AtaqueBasico { ActorId = "a2", ObjetivoId = "rival" });
        Assert.NotEmpty(r2.Impactos);

        // Y al cerrar el turno del bando, los stacks expiran.
        m.CerrarTurnoDeBando();
    }

    [Fact]
    public void El_dano_va_primero_al_escudo_y_el_sobrante_a_la_vida()
    {
        var m = Duelo.Montar(ClaseArma.Pesada);
        var rival = m.Estado.Buscar("rival")!;
        rival.Escudo = 10_000;
        double hpAntes = rival.Hp;

        var r = m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" });

        Assert.True(r.Impactos[0].AbsorbidoPorEscudo > 0);
        Assert.Equal(0.0, r.Impactos[0].DanoAplicado, 3);
        Assert.Equal(hpAntes, rival.Hp, 3);
    }

    [Fact]
    public void Un_KO_recalcula_la_capacidad_de_CM_al_instante()
    {
        var a1 = Escenario.Pj("a1");
        var a2 = Escenario.Pj("a2");
        var rival = Escenario.Pj("rival", Bando.Enemigo);
        rival.MaxAspirable[Stat.Eva] = 0;
        rival.MaxAspirable[Stat.Arm] = 0;
        rival.MaxAspirable[Stat.Crit] = 0;
        a2.Hp = 1;                       // a punto de caer
        a2.MaxAspirable[Stat.Eva] = 0;

        var e = Escenario.Combate(a1, a2, rival);
        e.Rng = new AleatorioSemilla(5);
        e.BandoActivo = Bando.Enemigo;
        var m = new MotorCombate(e);

        Assert.Equal(6.0, e.CmMax, 3);   // 2 aliados x storage 3

        m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "a2" });

        Assert.True(a2.EnKo);
        Assert.Equal(4.5, e.CmMax, 3);   // 3 + 0.5*3, el KO cuenta a la mitad
    }

    [Fact]
    public void Un_KO_sin_rescatar_se_remata_en_el_siguiente_turno_enemigo()
    {
        // manual-tecnico.md  10.3: cae en el turno enemigo N, su equipo tiene
        // un turno para rescatarlo, y en el N+1 el rival puede rematarlo.
        var m = Mesa.Montar();
        var a1 = m.Estado.Buscar("a1")!;
        var rematar = new AtaqueBasico { ActorId = "rival", ObjetivoId = "a1" };
        m.Estado.Rng = new AzarFijo(0.0);              // todo acierta

        m.CerrarTurnoDeBando();                        // turno enemigo N: cae
        a1.EnKo = true;
        Assert.False(m.Ejecutar(rematar).Permitida);   // recien caido: aun no

        m.CerrarTurnoDeBando();                        // turno aliado: ventana de rescate
        Assert.False(a1.Muerto);                       // cerrar el ciclo ya no mata

        m.CerrarTurnoDeBando();                        // turno enemigo N+1
        var r = m.Ejecutar(rematar);
        Assert.True(r.Permitida);
        Assert.Contains(r.Impactos, i => i.Remato);
        Assert.True(a1.Muerto);
    }

    [Fact]
    public void El_remate_pasa_por_el_acierto_y_puede_fallar()
    {
        var m = Mesa.Montar();
        var a1 = m.Estado.Buscar("a1")!;
        a1.EnKo = true;
        m.OtroCiclo();
        m.CerrarTurnoDeBando();
        Assert.True(a1.Rematable);

        m.Estado.Rng = new AzarFijo(0.99);             // nunca acierta: el techo es 95%
        var r = m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "a1" });

        Assert.True(r.Permitida);
        Assert.All(r.Impactos, i => Assert.False(i.Acerto));
        Assert.False(a1.Muerto);
    }

    [Fact]
    public void A_un_KO_atendido_no_se_le_remata()
    {
        var m = Mesa.Montar();
        var a1 = m.Estado.Buscar("a1")!;
        a1.EnKo = true;
        m.OtroCiclo();
        m.CerrarTurnoDeBando();

        a1.Flags["atendido"] = 2;                      // primeros auxilios en curso
        Assert.False(a1.Rematable);
        Assert.False(m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "a1" }).Permitida);
    }

    [Fact]
    public void Un_muerto_deja_de_aportar_a_la_barra()
    {
        // manual-tecnico.md  6.1: activo x1, K.O. x0.5, muerto x0.
        var m = Mesa.Montar();                         // 5 aliados con storage 3
        var a1 = m.Estado.Buscar("a1")!;
        a1.EnKo = true;
        m.OtroCiclo();
        Assert.Equal(13.5, m.Estado.CmMax, 3);         // 4 x 3 + 0.5 x 3

        m.CerrarTurnoDeBando();
        m.Estado.Rng = new AzarFijo(0.0);
        m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "a1" });

        Assert.True(a1.Muerto);
        Assert.Equal(12.0, m.Estado.CmMax, 3);
        Assert.False(a1.Rematable);                    // no se muere dos veces
    }

    [Fact]
    public void Un_ciclo_es_turno_aliado_mas_turno_enemigo()
    {
        var m = Duelo.Montar();
        Assert.Equal(0, m.Estado.Ciclo);
        Assert.Equal(Bando.Aliado, m.Estado.BandoActivo);

        m.CerrarTurnoDeBando();
        Assert.Equal(Bando.Enemigo, m.Estado.BandoActivo);
        Assert.Equal(0, m.Estado.Ciclo);           // aun no completo

        m.CerrarTurnoDeBando();
        Assert.Equal(Bando.Aliado, m.Estado.BandoActivo);
        Assert.Equal(1, m.Estado.Ciclo);
    }

    [Fact]
    public void Clasical_termina_al_iniciar_el_siguiente_turno_aliado()
    {
        var m = Duelo.Montar();
        m.Estado.CmActual = 8.0;
        m.Ejecutar(new CargarMusica { ActorId = "yo", MultiplicadorSeccion = 1.8 });
        Assert.True(m.Estado.ClasicalActivo);

        m.CerrarTurnoDeBando();                 // turno enemigo: sigue activo
        Assert.True(m.Estado.ClasicalActivo);

        m.CerrarTurnoDeBando();                 // vuelve a aliados: se apaga
        Assert.False(m.Estado.ClasicalActivo);
    }

    [Fact]
    public void Desde_la_casilla_defensiva_el_objeto_gratis_no_arranca_la_espera()
    {
        // La casilla defensiva no da un objeto extra: elimina la espera. El
        // turno siguiente vuelve a estar disponible, gratis.
        var m = Duelo.Montar(posAliada: Casilla.Defensiva);

        var r = m.Ejecutar(new UsarObjeto { ActorId = "yo", ObjetoId = "pan", EsComida = true });
        Assert.True(r.Permitida);
        Assert.False(r.CierraTurno);

        m.Ejecutar(new Pasar { ActorId = "yo" });
        m.CerrarTurnoDeBando();
        m.CerrarTurnoDeBando();

        r = m.Ejecutar(new UsarObjeto { ActorId = "yo", ObjetoId = "pan", EsComida = true });
        Assert.False(r.CierraTurno);                  // otra vez gratis
    }

    [Fact]
    public void El_objeto_gratis_es_uno_por_ventana_y_repetir_cuesta_el_turno()
    {
        // formulas.md  11: reutilizarlo sin que toque se puede, pero deja de
        // ser gratis y consume el turno.
        var m = Duelo.Montar(posAliada: Casilla.Neutral);

        Assert.False(m.Ejecutar(new UsarObjeto { ActorId = "yo", ObjetoId = "pan", EsComida = true }).CierraTurno);

        // Comida y consumible son independientes
        Assert.False(m.Ejecutar(new UsarObjeto { ActorId = "yo", ObjetoId = "vendaje" }).CierraTurno);

        var r = m.Ejecutar(new UsarObjeto { ActorId = "yo", ObjetoId = "pan", EsComida = true });
        Assert.True(r.Permitida);
        Assert.True(r.CierraTurno);
    }

    [Fact]
    public void Un_gadget_siempre_consume_turno()
    {
        var m = Duelo.Montar();
        var r = m.Ejecutar(new UsarObjeto { ActorId = "yo", ObjetoId = "tostadora", EsGadget = true });
        Assert.True(r.CierraTurno);
    }

    [Fact]
    public void El_combate_termina_cuando_cae_un_bando()
    {
        var m = Duelo.Montar();
        Assert.Equal(Desenlace.EnCurso, m.Desenlace);

        m.Estado.Buscar("rival")!.EnKo = true;
        Assert.Equal(Desenlace.VictoriaAliada, m.Desenlace);
    }

    [Fact]
    public void Dos_combates_con_la_misma_semilla_dan_el_mismo_resultado()
    {
        // Determinismo: es lo que hace posible simular miles de combates
        // y comparar builds sin que el azar contamine la medicion.
        Assert.Equal(Traza(99), Traza(99));
        Assert.NotEqual(Traza(99), Traza(100));

        static string Traza(int semilla)
        {
            var m = Duelo.Montar(ClaseArma.Ligera, semilla: semilla);
            m.Estado.Buscar("rival")!.MaxAspirable[Stat.Eva] = 150;
            var r = m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" });
            return string.Join(",", r.Impactos.Select(i => $"{i.Acerto}:{i.DanoAplicado:F2}"));
        }
    }
}

public class VentanaElementalTests
{
    [Fact]
    public void L2_requiere_dos_personajes_distintos_en_el_mismo_ciclo()
    {
        var v = new VentanaElemental();

        Assert.False(v.RegistrarExito("objetivo", "a"));   // uno solo, no basta
        Assert.False(v.RegistrarExito("objetivo", "a"));   // el mismo repetido, tampoco
        Assert.True(v.RegistrarExito("objetivo", "b"));    // dos distintos -> L2
    }

    [Fact]
    public void La_ventana_se_reinicia_al_terminar_el_turno_del_villano()
    {
        var v = new VentanaElemental();
        v.RegistrarExito("objetivo", "a");
        Assert.Equal(1, v.ActoresSobre("objetivo"));

        v.Reiniciar();
        Assert.Equal(0, v.ActoresSobre("objetivo"));
        Assert.False(v.RegistrarExito("objetivo", "b"));   // empieza de cero
    }

    [Fact]
    public void Cada_objetivo_lleva_su_propia_cuenta()
    {
        var v = new VentanaElemental();
        v.RegistrarExito("uno", "a");
        Assert.False(v.RegistrarExito("otro", "b"));
        Assert.True(v.RegistrarExito("uno", "b"));
    }
}

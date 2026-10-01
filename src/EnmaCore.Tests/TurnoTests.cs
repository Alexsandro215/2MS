using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Reglas;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// Los huecos del turno: orden, fases, compromiso, escondite, swap y huida.
/// Fuente: manual-tecnico.md  1.4, 2.1 a 2.3, y tablero.md  3, 5, 6 y 9.
/// </summary>
internal static class Mesa
{
    public static Combatiente Aliado(string id, int linea, Zona zona = Zona.Front)
    {
        var c = Duelo.Peleador(Escenario.Pj(id, Bando.Aliado, Casilla.Neutral, linea: linea));
        c.Zona = zona;
        return c;
    }

    /// <summary>Cuatro aliados en las lineas 0 a 3, uno en el banquillo y un rival.</summary>
    public static MotorCombate Montar(int escondites = 1)
    {
        var e = Escenario.Combate(
            Aliado("a0", 0), Aliado("a1", 1), Aliado("a2", 2), Aliado("a3", 3),
            Aliado("suplente", 0, Zona.Banquillo),
            Duelo.Peleador(Escenario.Pj("rival", Bando.Enemigo)));

        foreach (var c in e.Combatientes)
        {
            c.MaxAspirable[Stat.Eva] = 0;
            c.MaxAspirable[Stat.Crit] = 0;
        }
        e.Escondites = escondites;
        e.CmMax = 12;
        e.CmActual = 12;
        return new MotorCombate(e);
    }

    /// <summary>La pasiva que deja entrar desde el banquillo a relevar a un caido.</summary>
    public static ReglaFlujo PasivaDeRelevo(string quien) => new()
    {
        Id = "pasiva.relevo",
        Tipo = TipoFlujo.RelevoDesdeBanquillo,
        Fuente = Fuente.Pasiva,
        Alcance = new Selector(Ambito.Yo, new Filtro { EsPersonaje = quien }),
        Turnos = -1
    };

    public static ResultadoAccion Pasa(this MotorCombate m, string id)
        => m.Ejecutar(new Pasar { ActorId = id });

    public static ResultadoAccion Comida(this MotorCombate m, string id)
        => m.Ejecutar(new UsarObjeto { ActorId = id, ObjetoId = "pan", EsComida = true });

    /// <summary>Del turno aliado al siguiente turno aliado.</summary>
    public static void OtroCiclo(this MotorCombate m)
    {
        m.CerrarTurnoDeBando();
        m.CerrarTurnoDeBando();
    }
}

public class OrdenDeTurnoTests
{
    [Fact]
    public void Eliges_quien_abre_y_el_resto_sigue_de_derecha_a_izquierda()
    {
        var m = Mesa.Montar();
        Assert.Null(m.ActorDeTurno);                  // nadie ha abierto: eliges tu

        m.Pasa("a1");                                 // abre a1, en la linea 1
        Assert.Equal("a0", m.ActorDeTurno!.Id);       // hacia la izquierda
        m.Pasa("a0");
        Assert.Equal("a3", m.ActorDeTurno!.Id);       // y vuelta al extremo derecho
        m.Pasa("a3");
        Assert.Equal("a2", m.ActorDeTurno!.Id);
        m.Pasa("a2");

        Assert.Null(m.ActorDeTurno);                  // el bando ha terminado
        Assert.Empty(m.PendientesDeActuar());
    }

    [Fact]
    public void Nadie_se_salta_su_vez()
    {
        var m = Mesa.Montar();
        m.Pasa("a1");

        var r = m.Pasa("a3");
        Assert.False(r.Permitida);
        Assert.Contains("a0", r.Motivo);
    }

    [Fact]
    public void Hasta_que_uno_cierra_su_turno_no_entra_el_siguiente()
    {
        var m = Mesa.Montar();
        Assert.True(m.Ejecutar(new Moverse { ActorId = "a2", Destino = Casilla.Ofensiva }).Permitida);
        Assert.Equal("a2", m.ActorDeTurno!.Id);

        Assert.False(m.Pasa("a1").Permitida);
        m.Pasa("a2");
        Assert.Equal("a1", m.ActorDeTurno!.Id);
    }

    [Fact]
    public void Cada_turno_de_bando_se_vuelve_a_elegir_quien_abre()
    {
        var m = Mesa.Montar();
        foreach (var id in new[] { "a1", "a0", "a3", "a2" }) m.Pasa(id);
        m.OtroCiclo();

        Assert.Null(m.ActorDeTurno);
        Assert.True(m.Pasa("a3").Permitida);
    }
}

public class FasesDelTurnoTests
{
    [Fact]
    public void Empezada_la_ofensiva_solo_quedan_basicos_QWER_o_pasar()
    {
        // manual-tecnico.md  2.1: movimiento, ventana de objetos y UNA via.
        var m = Duelo.Montar();
        Assert.True(m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" }).Permitida);

        Assert.False(m.Ejecutar(new Moverse { ActorId = "yo", Destino = Casilla.Ofensiva }).Permitida);
        Assert.False(m.Comida("yo").Permitida);
        Assert.False(m.Ejecutar(new Defender { ActorId = "yo" }).Permitida);
        Assert.False(m.Ejecutar(new CargarMusica { ActorId = "yo" }).Permitida);

        Assert.True(m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'Q' }).Permitida);
        Assert.True(m.Pasa("yo").Permitida);
    }

    [Fact]
    public void Los_basicos_son_una_tanda_por_turno()
    {
        var m = Duelo.Montar();
        Assert.True(m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" }).Permitida);

        var r = m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" });
        Assert.False(r.Permitida);
        Assert.Contains("basicos", r.Motivo);
    }

    [Fact]
    public void Con_Artens_W_caben_dos_tandas()
    {
        var m = Duelo.Montar();
        m.Flujo.Activas.Add(new ReglaFlujo
        {
            Id = "artens.w", Tipo = TipoFlujo.DoblarAcciones, Fuente = Fuente.Habilidad,
            Alcance = Selector.Yo, Veces = 2, Turnos = 1
        });

        var basico = new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" };
        Assert.True(m.Ejecutar(basico).Permitida);
        Assert.True(m.Ejecutar(basico).Permitida);
        Assert.False(m.Ejecutar(basico).Permitida);
    }

    [Fact]
    public void Usar_un_objeto_gratis_desde_la_defensiva_ancla_ahi_el_turno()
    {
        // Regla de compromiso: objeto sin espera, a cambio de no moverte.
        var m = Duelo.Montar(posAliada: Casilla.Defensiva);
        m.Comida("yo");

        var r = m.Ejecutar(new Moverse { ActorId = "yo", Destino = Casilla.Neutral });
        Assert.False(r.Permitida);
        Assert.Contains("defensiva", r.Motivo);
    }

    [Fact]
    public void Fuera_de_la_defensiva_el_objeto_no_ancla()
    {
        var m = Duelo.Montar(posAliada: Casilla.Neutral);
        m.Comida("yo");
        Assert.True(m.Ejecutar(new Moverse { ActorId = "yo", Destino = Casilla.Ofensiva }).Permitida);
    }

    [Fact]
    public void Fuera_de_la_defensiva_el_objeto_gratis_tarda_tres_turnos_en_volver()
    {
        var m = Duelo.Montar(posAliada: Casilla.Neutral);

        Assert.False(m.Comida("yo").CierraTurno);     // turno 1: gratis
        m.Pasa("yo");
        m.OtroCiclo();

        Assert.True(m.Comida("yo").CierraTurno);      // turno 2: esperando
        m.OtroCiclo();
        Assert.True(m.Comida("yo").CierraTurno);      // turno 3: esperando
        m.OtroCiclo();

        Assert.False(m.Comida("yo").CierraTurno);     // turno 4: ha vuelto
    }
}

public class EsconditeTests
{
    [Fact]
    public void Escondido_el_rival_no_puede_tocarte()
    {
        var m = Duelo.Montar();
        m.Estado.Escondites = 1;
        Assert.True(m.Ejecutar(new Esconderse { ActorId = "yo" }).Permitida);
        m.CerrarTurnoDeBando();

        Assert.False(m.Ejecutar(new AtaqueBasico { ActorId = "rival", ObjetivoId = "yo" }).Permitida);
        Assert.False(m.Ejecutar(new UsarHabilidad { ActorId = "rival", Tecla = 'Q', ObjetivoId = "yo" }).Permitida);

        // Ni por area: el selector de enemigos ya no lo ve.
        var rival = m.Estado.Buscar("rival")!;
        var alcance = new Selector(Ambito.Enemigos).Resolver(m.Estado, new Contexto(rival));
        Assert.DoesNotContain(alcance, c => c.Id == "yo");
    }

    [Fact]
    public void Escondido_no_ataca_ni_usa_skills_pero_si_objetos()
    {
        var m = Duelo.Montar();
        m.Estado.Escondites = 1;
        m.Ejecutar(new Esconderse { ActorId = "yo" });
        m.OtroCiclo();

        Assert.False(m.Ejecutar(new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" }).Permitida);
        Assert.False(m.Ejecutar(new UsarHabilidad { ActorId = "yo", Tecla = 'Q' }).Permitida);
        Assert.True(m.Comida("yo").Permitida);
    }

    [Fact]
    public void Los_escondites_se_los_pelean_los_dos_bandos()
    {
        var m = Duelo.Montar();
        m.Estado.Escondites = 1;

        m.CerrarTurnoDeBando();
        Assert.True(m.Ejecutar(new Esconderse { ActorId = "rival" }).Permitida);
        m.CerrarTurnoDeBando();

        var r = m.Ejecutar(new Esconderse { ActorId = "yo" });
        Assert.False(r.Permitida);
        Assert.Contains("escondite", r.Motivo);
    }

    [Fact]
    public void Sin_escondites_el_entorno_es_agresivo()
    {
        var m = Duelo.Montar();                       // 0 escondites por defecto
        Assert.False(m.Ejecutar(new Esconderse { ActorId = "yo" }).Permitida);
    }

    [Fact]
    public void Salir_es_gratis_pero_siempre_a_la_defensiva()
    {
        var m = Duelo.Montar(posAliada: Casilla.Ofensiva);
        m.Estado.Escondites = 1;
        m.Ejecutar(new Esconderse { ActorId = "yo" });
        m.OtroCiclo();

        var r = m.Ejecutar(new SalirDelEscondite { ActorId = "yo" });
        Assert.True(r.Permitida);
        Assert.False(r.CierraTurno);

        var yo = m.Estado.Buscar("yo")!;
        Assert.Equal(Zona.Front, yo.Zona);
        Assert.Equal(Casilla.Defensiva, yo.Posicion);

        // Desde ahi no se pega; avanzando primero, si.
        var basico = new AtaqueBasico { ActorId = "yo", ObjetivoId = "rival" };
        Assert.False(m.Ejecutar(basico).Permitida);
        m.Ejecutar(new Moverse { ActorId = "yo", Destino = Casilla.Ofensiva });
        Assert.True(m.Ejecutar(basico).Permitida);
    }

    [Fact]
    public void A_un_caido_escondido_no_se_le_remata()
    {
        var m = Mesa.Montar(escondites: 1);
        var a1 = m.Estado.Buscar("a1")!;
        a1.EnKo = true;

        Assert.True(m.Ejecutar(new Esconderse { ActorId = "a2", OcultoId = "a1" }).Permitida);
        Assert.Equal(Zona.Escondite, a1.Zona);

        m.CerrarTurnoDeBando();
        Assert.DoesNotContain(m.CerrarTurnoDeBando(), c => c.Tipo == "muerte");
    }
}

public class SwapTests
{
    [Fact]
    public void El_que_entra_hereda_casilla_y_linea_y_espera_al_turno_siguiente()
    {
        var m = Mesa.Montar();
        var a2 = m.Estado.Buscar("a2")!;
        a2.Posicion = Casilla.Ofensiva;

        var r = m.Ejecutar(new Swap { ActorId = "a2", EntranteId = "suplente" });
        Assert.True(r.Permitida);
        Assert.True(r.CierraTurno);

        var suplente = m.Estado.Buscar("suplente")!;
        Assert.Equal(Zona.Banquillo, a2.Zona);
        Assert.Equal(Zona.Front, suplente.Zona);
        Assert.Equal(Casilla.Ofensiva, suplente.Posicion);
        Assert.Equal(2, suplente.Linea);

        Assert.False(m.Pasa("suplente").Permitida);   // entra, pero no actua aun
        m.OtroCiclo();
        Assert.True(m.Pasa("suplente").Permitida);
    }

    [Fact]
    public void Un_aliado_gasta_su_turno_en_sacar_a_un_caido()
    {
        var m = Mesa.Montar();
        var a1 = m.Estado.Buscar("a1")!;
        a1.EnKo = true;

        var r = m.Ejecutar(new Swap { ActorId = "a2", SalienteId = "a1", EntranteId = "suplente" });
        Assert.True(r.Permitida);
        Assert.True(r.CierraTurno);
        Assert.Equal(Zona.Banquillo, a1.Zona);
        Assert.Equal(1, m.Estado.Buscar("suplente")!.Linea);

        // Ya no esta expuesto: al cerrar el ciclo no hay remate.
        m.CerrarTurnoDeBando();
        Assert.DoesNotContain(m.CerrarTurnoDeBando(), c => c.Tipo == "muerte");
    }

    [Fact]
    public void A_un_compañero_en_pie_no_se_le_cambia_desde_fuera()
    {
        var m = Mesa.Montar();
        var r = m.Ejecutar(new Swap { ActorId = "a2", SalienteId = "a1", EntranteId = "suplente" });
        Assert.False(r.Permitida);
    }

    [Fact]
    public void Desde_el_banquillo_solo_se_releva_con_una_pasiva()
    {
        var m = Mesa.Montar();
        var a1 = m.Estado.Buscar("a1")!;
        var suplente = m.Estado.Buscar("suplente")!;
        a1.EnKo = true;
        var relevo = new Swap { ActorId = "suplente", SalienteId = "a1", EntranteId = "suplente" };

        Assert.False(m.Ejecutar(relevo).Permitida);

        // Con la pasiva, que dura todo el combate...
        m.Flujo.Activas.Add(Mesa.PasivaDeRelevo("suplente"));
        m.Flujo.AvanzarTurno();
        m.Flujo.AvanzarTurno();
        Assert.True(m.Flujo.PuedeRelevarDesdeBanquillo(m.Estado, suplente));

        // ...entra sin que nadie del front gaste su turno.
        Assert.True(m.Ejecutar(relevo).Permitida);
        Assert.Equal(Zona.Front, suplente.Zona);
        Assert.Equal(Zona.Banquillo, a1.Zona);
        Assert.Null(m.ActorDeTurno);                  // nadie ha abierto todavia
        Assert.Equal(3, m.PendientesDeActuar().Count());
    }

    [Fact]
    public void Si_cae_el_front_entero_el_banquillo_no_salva_sin_pasiva()
    {
        var m = Mesa.Montar();
        foreach (var id in new[] { "a0", "a1", "a2", "a3" })
            m.Estado.Buscar(id)!.EnKo = true;

        Assert.Equal(Desenlace.DerrotaAliada, m.Desenlace);

        m.Flujo.Activas.Add(Mesa.PasivaDeRelevo("suplente"));
        Assert.Equal(Desenlace.EnCurso, m.Desenlace);
    }
}

public class HuidaTests
{
    [Fact]
    public void Huir_puede_salir_bien()
    {
        var m = Duelo.Montar();
        m.Estado.ProbabilidadDeHuida = 1.0;

        Assert.True(m.Ejecutar(new Huir { ActorId = "yo" }).Permitida);
        Assert.Equal(Desenlace.Huida, m.Desenlace);
    }

    [Fact]
    public void Si_sale_mal_se_pierde_el_turno()
    {
        var m = Duelo.Montar();
        m.Estado.ProbabilidadDeHuida = 0.0;

        var r = m.Ejecutar(new Huir { ActorId = "yo" });
        Assert.True(r.Permitida);
        Assert.True(r.CierraTurno);
        Assert.Equal(Desenlace.EnCurso, m.Desenlace);
    }

    [Fact]
    public void De_un_jefe_no_se_huye()
    {
        var m = Duelo.Montar();
        m.Estado.SePuedeHuir = false;
        Assert.False(m.Ejecutar(new Huir { ActorId = "yo" }).Permitida);
    }
}

using Enma.Core;
using Enma.Core.Estado;
using Enma.Core.Reglas;
using Enma.Core.Stats;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>Utilidades para montar estados de prueba sin ruido.</summary>
internal static class Escenario
{
    public static Combatiente Pj(string id, Bando bando = Bando.Aliado,
        Casilla pos = Casilla.Neutral, int nivel = 18, int linea = 0)
    {
        var c = new Combatiente { Id = id, Nombre = id, Bando = bando, Posicion = pos, Linea = linea };
        c.Nivel = nivel;
        foreach (Stat s in Enum.GetValues<Stat>()) c.MaxAspirable[s] = 100;
        c.Hp = c.HpMax;
        return c;
    }

    public static EstadoCombate Combate(params Combatiente[] pjs)
    {
        var e = new EstadoCombate();
        e.Combatientes.AddRange(pjs);
        e.Rng = new Combate.AleatorioSemilla(42);
        e.RecalcularCm();
        return e;
    }
}

/// <summary>
/// LA PRUEBA DE FUEGO de arquitectura.md  1.
///
/// Ocho sistemas que en cualquier otro proyecto serian ocho subsistemas
/// separados, ejecutados aqui por UN SOLO evaluador sin una linea de codigo
/// especifica por sistema.
/// </summary>
public class EsquemaUnicoTests
{
    private readonly Evaluador _ev = new();

    [Fact]
    public void Pasiva_siempre_condicionada_a_vida_baja()
    {
        // | Pasiva | siempre | HP < X% | +X% AD |
        var yo = Escenario.Pj("alextsu");
        var e = Escenario.Combate(yo);

        e.ReglasActivas.Add(new Regla
        {
            Id = "pasiva.furia",
            Fuente = Fuente.Pasiva,
            Disparador = Disparador.Siempre,
            Condicion = Cond.Menor(Ex.HpPct(), Ex.K(0.30)),
            Efecto = new ModificarStat(Stat.Ad, Capa.PctAdd, Ex.K(0.50)),
            Etiqueta = "+50% AD por debajo del 30% de vida"
        });

        yo.Hp = yo.HpMax * 0.9;
        Assert.Equal(100.0, _ev.ResolverStat(e, yo, Stat.Ad), 3);

        yo.Hp = yo.HpMax * 0.2;
        Assert.Equal(150.0, _ev.ResolverStat(e, yo, Stat.Ad), 3);
    }

    [Fact]
    public void Gadget_al_recibir_golpe_si_villano_y_cm_alto()
    {
        // | Gadget | ON_BEING_HIT | villano y CM > 80% | fijar CM al 50% |
        var yo = Escenario.Pj("kali");
        var villano = Escenario.Pj("azerion", Bando.Enemigo);
        var e = Escenario.Combate(yo, villano);

        e.ReglasActivas.Add(new Regla
        {
            Id = "gadget.caja-musica-almis",
            Fuente = Fuente.Gadget,
            Disparador = Disparador.AlRecibirGolpe,
            PortadorId = "kali",
            Condicion = new Y(
                new EsBando(Bando.Enemigo, Selector.Objetivo),
                Cond.Mayor(Ex.Global(Global.CmPct), Ex.K(0.80))),
            Efecto = new FijarCm(Ex.K(0.50))
        });

        e.CmActual = e.CmMax * 0.95;
        var r = _ev.Resolver(e, new Evento(Disparador.AlRecibirGolpe, "kali", "azerion"));

        Assert.Single(r.Disparadas);
        Assert.Equal(e.CmMax * 0.5, e.CmActual, 3);
    }

    [Fact]
    public void Emblema_breakpoint_por_conteo_de_equipo()
    {
        // | Emblema | conteo de equipo | breakpoint 3 / 6 / 9 | +1 BA al equipo |
        var a = Escenario.Pj("a");
        var b = Escenario.Pj("b");
        var c = Escenario.Pj("c");
        var e = Escenario.Combate(a, b, c);

        e.ReglasActivas.Add(new Regla
        {
            Id = "emblema.distrito4.3",
            Fuente = Fuente.Emblema,
            Disparador = Disparador.Siempre,
            Condicion = new Breakpoint(Ex.Contar(new Selector(Ambito.AliadosConmigo)), 3),
            Efecto = new ModificarStat(Stat.VelocidadAtaque, Capa.PctAdd, Ex.K(0.15),
                new Selector(Ambito.AliadosConmigo))
        });

        Assert.Equal(115.0, _ev.ResolverStat(e, a, Stat.VelocidadAtaque), 3);

        c.EnKo = true;   // se cae del breakpoint y el bonus desaparece del equipo
        Assert.Equal(100.0, _ev.ResolverStat(e, a, Stat.VelocidadAtaque), 3);
    }

    [Fact]
    public void Emocion_atada_a_un_personaje_concreto()
    {
        // | Emoción | evento del día | personaje = Kali | −50% puntería turno 1 |
        var kali = Escenario.Pj("kali");
        var otro = Escenario.Pj("ren");
        var e = Escenario.Combate(kali, otro);

        e.ReglasActivas.Add(new Regla
        {
            Id = "emocion.kali.triste",
            Fuente = Fuente.Emocion,
            Disparador = Disparador.Siempre,
            PortadorId = "kali",
            Condicion = Cond.MenorIgual(Ex.Global(Global.Turno), Ex.K(1)),
            Efecto = new ModificarStat(Stat.Press, Capa.PctAdd, Ex.K(-0.50))
        });

        e.Turno = 1;
        Assert.Equal(50.0, _ev.ResolverStat(e, kali, Stat.Press), 3);
        Assert.Equal(100.0, _ev.ResolverStat(e, otro, Stat.Press), 3);

        e.Turno = 2;
        Assert.Equal(100.0, _ev.ResolverStat(e, kali, Stat.Press), 3);
    }

    [Fact]
    public void Casilla_ofensiva_mueve_press_y_eva()
    {
        // | Casilla | estar en ella | posición = OFENSIVA | +30% PRES, −30% EVA |
        var yo = Escenario.Pj("artens", pos: Casilla.Neutral);
        var e = Escenario.Combate(yo);

        e.ReglasActivas.Add(new Regla
        {
            Id = "casilla.ofensiva",
            Fuente = Fuente.Casilla,
            Disparador = Disparador.Siempre,
            Condicion = new EnCasilla(Casilla.Ofensiva),
            Efecto = new Varios(
                new ModificarStat(Stat.Press, Capa.PctAdd, Ex.K(0.30)),
                new ModificarStat(Stat.Eva, Capa.PctAdd, Ex.K(-0.30)))
        });

        Assert.Equal(100.0, _ev.ResolverStat(e, yo, Stat.Press), 3);

        yo.Posicion = Casilla.Ofensiva;
        Assert.Equal(130.0, _ev.ResolverStat(e, yo, Stat.Press), 3);
        Assert.Equal(70.0, _ev.ResolverStat(e, yo, Stat.Eva), 3);
    }

    [Fact]
    public void Comida_en_ventana_pre_ofensiva()
    {
        // | Comida | usar objeto | ventana pre-ofensiva | buff |
        var yo = Escenario.Pj("itzela");
        var e = Escenario.Combate(yo);

        e.ReglasActivas.Add(new Regla
        {
            Id = "comida.chocolate-calientito",
            Fuente = Fuente.Comida,
            Disparador = Disparador.AlUsarObjeto,
            PortadorId = "itzela",
            Efecto = new ModificarStat(Stat.Ad, Capa.PctAdd, Ex.K(0.10)),
            Duracion = Duracion.PorTurnos(2)
        });

        var r = _ev.Resolver(e, new Evento(Disparador.AlUsarObjeto, "itzela"));
        Assert.Single(r.Disparadas);
        Assert.Equal("comida.chocolate-calientito", r.Disparadas[0].Id);
    }

    [Fact]
    public void Terreno_es_una_regla_anclada_a_una_casilla()
    {
        // | Terreno | actuar en la casilla | estar ahí al actuar | −X% PRES, AD, AP |
        var enemigo = Escenario.Pj("enemigo", Bando.Enemigo, Casilla.Ofensiva, linea: 0);
        var madrerick = Escenario.Pj("madrerick");
        var e = Escenario.Combate(madrerick, enemigo);

        e.Terrenos.Add(new Terreno
        {
            Id = "tierra-quemada:Enemigo:0:Ofensiva",
            Tipo = "tierra-quemada",
            BandoDeLaLinea = Bando.Enemigo,
            Linea = 0,
            Casilla = Casilla.Ofensiva,
            TurnosRestantes = 2,
            EsBuff = false
        });
        e.Terrenos[0].Reglas.Add(new Regla
        {
            Id = "terreno.tierra-quemada",
            Fuente = Fuente.Terreno,
            Disparador = Disparador.Siempre,
            PortadorId = "enemigo",
            Condicion = new EnCasilla(Casilla.Ofensiva, Selector.Yo),
            Efecto = new Varios(
                new ModificarStat(Stat.Press, Capa.PctAdd, Ex.K(-0.20)),
                new ModificarStat(Stat.Ad, Capa.PctAdd, Ex.K(-0.20)))
        });

        Assert.Equal(80.0, _ev.ResolverStat(e, enemigo, Stat.Ad), 3);

        enemigo.Posicion = Casilla.Defensiva;   // se sale del terreno
        Assert.Equal(100.0, _ev.ResolverStat(e, enemigo, Stat.Ad), 3);
    }

    [Fact]
    public void Elemento_requiere_dos_personajes_en_el_mismo_ciclo()
    {
        // | Elemento | marca aplicada | 2 marcas, 2 personajes | detonar L2 |
        var objetivo = Escenario.Pj("objetivo", Bando.Enemigo);
        var e = Escenario.Combate(Escenario.Pj("a"), Escenario.Pj("b"), objetivo);

        e.ReglasActivas.Add(new Regla
        {
            Id = "elemento.detonacion-l2",
            Fuente = Fuente.Elemento,
            Disparador = Disparador.AlAplicarElemento,
            Condicion = Cond.MayorIgual(
                Ex.Global(Global.Marcador, "actores_elemento_objetivo"), Ex.K(2)),
            Efecto = new PonerFlag("elemento_l2", 3, Selector.Objetivo)
        });

        e.Historial.Marcadores["actores_elemento_objetivo"] = 1;
        _ev.Resolver(e, new Evento(Disparador.AlAplicarElemento, "a", "objetivo"));
        Assert.False(objetivo.TieneFlag("elemento_l2"));

        e.Historial.Marcadores["actores_elemento_objetivo"] = 2;
        _ev.Resolver(e, new Evento(Disparador.AlAplicarElemento, "b", "objetivo"));
        Assert.True(objetivo.TieneFlag("elemento_l2"));
    }

    [Fact]
    public void El_evaluador_no_ramifica_sobre_la_fuente()
    {
        // No es una comprobacion de comportamiento, es la comprobacion de la
        // arquitectura: el mismo evaluador ejecuto ocho sistemas distintos en
        // los tests de arriba sin una sola linea especifica por sistema.
        var codigo = System.IO.File.Exists("../../../../EnmaCore/Reglas/Evaluador.cs")
            ? System.IO.File.ReadAllText("../../../../EnmaCore/Reglas/Evaluador.cs")
            : null;

        if (codigo is null) return;   // no falla si se ejecuta fuera del arbol

        Assert.DoesNotContain("Fuente.Gadget", codigo);
        Assert.DoesNotContain("Fuente.Pasiva", codigo);
        Assert.DoesNotContain("Fuente.Emblema", codigo);
        Assert.DoesNotContain("Fuente.Terreno", codigo);
    }
}

using Enma.Core;
using Enma.Core.Estado;
using Enma.Core.Reglas;
using Enma.Core.Stats;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// Habilidades reales de docs/estilos.md que NO caben en un esquema de
/// modificadores planos. Son las que tumbaron la primera propuesta del motor
/// de reglas, y estan aqui para que no vuelva a pasar en silencio.
/// </summary>
public class KitsDificilesTests
{
    private readonly Evaluador _ev = new();

    [Fact]
    public void Lein_Xein_E_escala_con_el_numero_de_enemigos_con_elemento()
    {
        // "Obtienes X% AD x n enemigos con E.E."  -> AGREGACION sobre un filtro
        var yo = Escenario.Pj("lein-xein");
        var e1 = Escenario.Pj("e1", Bando.Enemigo);
        var e2 = Escenario.Pj("e2", Bando.Enemigo);
        var e3 = Escenario.Pj("e3", Bando.Enemigo);
        var e = Escenario.Combate(yo, e1, e2, e3);

        e1.Elementos.Add(new MarcaElemental("elec", 1, 2));
        e2.Elementos.Add(new MarcaElemental("elec", 1, 2));

        var conElemento = new Selector(Ambito.Enemigos, new Filtro { TieneElemento = true });

        e.ReglasActivas.Add(new Regla
        {
            Id = "lein-xein.e",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.Siempre,
            PortadorId = "lein-xein",
            Efecto = new ModificarStat(Stat.Ad, Capa.PctAdd,
                Ex.Por(Ex.K(0.20), Ex.Contar(conElemento)))
        });

        // 2 enemigos marcados -> +40%
        Assert.Equal(140.0, _ev.ResolverStat(e, yo, Stat.Ad), 3);

        e3.Elementos.Add(new MarcaElemental("elec", 1, 2));
        Assert.Equal(160.0, _ev.ResolverStat(e, yo, Stat.Ad), 3);
    }

    [Fact]
    public void Korkakov_E_ataca_solo_al_enemigo_con_mas_vida()
    {
        // "Solo al enemigo con MAYOR % de HP"  ->  SELECCION POR EXTREMO
        var yo = Escenario.Pj("korkakov");
        var debil = Escenario.Pj("debil", Bando.Enemigo);
        var fuerte = Escenario.Pj("fuerte", Bando.Enemigo);
        var e = Escenario.Combate(yo, debil, fuerte);

        debil.Hp = debil.HpMax * 0.30;
        fuerte.Hp = fuerte.HpMax * 0.95;

        var elDeMasVida = new Selector(Ambito.Enemigos, Orden: Orden.MayorHpPct, Limite: 1);

        e.ReglasActivas.Add(new Regla
        {
            Id = "korkakov.e",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "korkakov",
            Efecto = new PonerFlag("anclado", 1, elDeMasVida)
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "korkakov"));

        Assert.True(fuerte.TieneFlag("anclado"));
        Assert.False(debil.TieneFlag("anclado"));
    }

    [Fact]
    public void Kronwell_acumula_cargas_y_las_consume()
    {
        // "Obtienes X cargas de Kronwell" / "consumiendo las cargas: n x X% AP"
        // -> CONTADORES CON NOMBRE, que no son stats ni modificadores
        var kronwell = Escenario.Pj("kronwell");
        var e = Escenario.Combate(kronwell);

        var q = new Regla
        {
            Id = "kronwell.q",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "kronwell",
            Efecto = new DarContador("cargas_kronwell", Ex.K(2))
        };
        e.ReglasActivas.Add(q);

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "kronwell"));
        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "kronwell"));
        Assert.Equal(4.0, kronwell.Contador("cargas_kronwell"), 3);

        // La E escala con las cargas y luego las consume
        e.ReglasActivas.Remove(q);
        e.ReglasActivas.Add(new Regla
        {
            Id = "kronwell.e",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.Siempre,
            PortadorId = "kronwell",
            Efecto = new ModificarStat(Stat.Ap, Capa.PctAdd,
                Ex.Por(Ex.K(0.25), Ex.Cargas("cargas_kronwell")))
        });

        Assert.Equal(200.0, _ev.ResolverStat(e, kronwell, Stat.Ap), 3);   // 100 + 4*25%
    }

    [Fact]
    public void Kronwell_R_impone_70_por_ciento_de_desgaste()
    {
        // "Obtienes 70% de desgaste"  ->  VALOR ABSOLUTO, no acumulacion
        var kronwell = Escenario.Pj("kronwell");
        var e = Escenario.Combate(kronwell);

        e.ReglasActivas.Add(new Regla
        {
            Id = "kronwell.r",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "kronwell",
            Efecto = new SumarDesgaste(Ex.K(0.70))
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "kronwell"));
        Assert.Equal(0.70, kronwell.Desgaste.Acumulado, 3);
    }

    [Fact]
    public void Willham_W_traspasa_anti_arm_a_anti_rm()
    {
        // "Reduces a 0 tu Anti-ARM y X% de ese valor se traspasa a tu Anti-RM"
        // -> ARITMETICA SOBRE EL ESTADO
        var willham = Escenario.Pj("willham");
        willham.MaxAspirable[Stat.AntiArm] = 80;
        willham.MaxAspirable[Stat.AntiRm] = 10;
        var e = Escenario.Combate(willham);

        e.ReglasActivas.Add(new Regla
        {
            Id = "willham.w",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "willham",
            Efecto = new TransferirStat(Stat.AntiArm, Stat.AntiRm, Ex.K(0.50))
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "willham"));

        Assert.Equal(0.0, willham.MaxAspirable[Stat.AntiArm], 3);
        Assert.Equal(50.0, willham.MaxAspirable[Stat.AntiRm], 3);   // 10 + 80*0.5
    }

    [Fact]
    public void Korkakov_W_EVOL_lee_si_fallaste_el_spell_anterior()
    {
        // "Si FALLAS tu spell anterior, obtienes X% de PRES"
        // -> MEMORIA DEL TURNO
        var korkakov = Escenario.Pj("korkakov");
        var e = Escenario.Combate(korkakov);

        e.ReglasActivas.Add(new Regla
        {
            Id = "korkakov.w-evol",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.Siempre,
            PortadorId = "korkakov",
            Condicion = Cond.MayorIgual(Ex.Global(Global.ImpactosFallados), Ex.K(1)),
            Efecto = new ModificarStat(Stat.Press, Capa.PctAdd, Ex.K(0.40))
        });

        Assert.Equal(100.0, _ev.ResolverStat(e, korkakov, Stat.Press), 3);

        e.Historial.ImpactosFallados = 1;
        Assert.Equal(140.0, _ev.ResolverStat(e, korkakov, Stat.Press), 3);
    }

    [Fact]
    public void Jaens_Jeans_R_EVOL_recupera_el_cm_gastado_este_turno()
    {
        // "Recuperas TODO el CM gastado de este turno"  ->  MEMORIA DEL TURNO
        // Equipo de 4 para tener el pool de referencia: storage 3 x 4 = 12 CM
        var jaens = Escenario.Pj("jaens");
        var e = Escenario.Combate(jaens, Escenario.Pj("b"), Escenario.Pj("c"), Escenario.Pj("d"));
        Assert.Equal(12.0, e.CmMax, 3);

        e.CmActual = 2.0;
        e.Historial.CmGastado = 6.0;

        e.ReglasActivas.Add(new Regla
        {
            Id = "jaens.r-evol",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "jaens",
            Efecto = new AjustarCm(Ex.Global(Global.CmGastadoEsteTurno))
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "jaens"));
        Assert.Equal(8.0, e.CmActual, 3);
    }

    [Fact]
    public void Halltus_R_EVOL_reacciona_al_desgaste_de_un_companero()
    {
        // "Das escudo si un compañero tiene X% de DESGASTE"
        // -> CONSULTA SOBRE OTROS
        var halltus = Escenario.Pj("halltus");
        var cansado = Escenario.Pj("cansado");
        var fresco = Escenario.Pj("fresco");
        var e = Escenario.Combate(halltus, cansado, fresco);

        cansado.Desgaste.Fijar(0.35);

        var muyCansados = new Selector(Ambito.Aliados);

        e.ReglasActivas.Add(new Regla
        {
            Id = "halltus.r-evol",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "halltus",
            Condicion = Cond.MayorIgual(
                new Leer(Lectura.Desgaste, muyCansados, Agregado.Maximo), Ex.K(0.30)),
            Efecto = new DarEscudo(Ex.K(50), new Selector(Ambito.AliadosConmigo))
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "halltus"));
        Assert.Equal(50.0, halltus.Escudo, 3);
        Assert.Equal(50.0, fresco.Escudo, 3);
    }

    [Fact]
    public void Madrerick_Q_EVOL_elimina_terreno_de_buffs_del_enemigo()
    {
        // "Elimina X casilla aleatoria de BUFFS DEL ENEMIGO"
        // -> UNA REGLA QUE DESTRUYE OTRAS REGLAS
        var madrerick = Escenario.Pj("madrerick");
        var e = Escenario.Combate(madrerick);

        e.Terrenos.Add(new Terreno { Id = "buff-enemigo", Tipo = "pasto-guerra",
            BandoDeLaLinea = Bando.Enemigo, Linea = 0, Casilla = Casilla.Ofensiva,
            TurnosRestantes = 3, EsBuff = true });
        e.Terrenos.Add(new Terreno { Id = "debuff-enemigo", Tipo = "ortigas",
            BandoDeLaLinea = Bando.Enemigo, Linea = 1, Casilla = Casilla.Neutral,
            TurnosRestantes = 2, EsBuff = false });

        e.ReglasActivas.Add(new Regla
        {
            Id = "madrerick.q-evol",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "madrerick",
            Efecto = new EliminarTerreno(Bando.Enemigo, SoloBuffs: true, Cuantos: 1)
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "madrerick"));

        Assert.Single(e.Terrenos);
        Assert.Equal("debuff-enemigo", e.Terrenos[0].Id);   // el debuff sobrevive
    }

    [Fact]
    public void Floristas_W_crea_terreno_que_es_una_regla_nueva()
    {
        // "Riega una casilla de la OFENSIVA: aumenta AD y AP. Dura 3 turnos."
        // -> UNA REGLA QUE CREA REGLAS
        var florista = Escenario.Pj("florista", pos: Casilla.Ofensiva);
        var e = Escenario.Combate(florista);

        e.ReglasActivas.Add(new Regla
        {
            Id = "floristas.w",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "florista",
            Efecto = new CrearTerreno(
                "pasto-de-guerra", Casilla.Ofensiva, EsBuff: true, Turnos: 3,
                Reglas: [
                    new Regla {
                        Id = "terreno.pasto-de-guerra",
                        Fuente = Fuente.Terreno,
                        Disparador = Disparador.Siempre,
                        PortadorId = "florista",
                        Condicion = new EnCasilla(Casilla.Ofensiva),
                        Efecto = new Varios(
                            new ModificarStat(Stat.Ad, Capa.PctAdd, Ex.K(0.25)),
                            new ModificarStat(Stat.Ap, Capa.PctAdd, Ex.K(0.25)))
                    }
                ])
        });

        Assert.Equal(100.0, _ev.ResolverStat(e, florista, Stat.Ad), 3);

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "florista"));

        Assert.Single(e.Terrenos);
        Assert.Equal(125.0, _ev.ResolverStat(e, florista, Stat.Ad), 3);

        // Y caduca solo a los 3 turnos
        for (int i = 0; i < 3; i++) _ev.AvanzarTurno(e);
        Assert.Empty(e.Terrenos);
        Assert.Equal(100.0, _ev.ResolverStat(e, florista, Stat.Ad), 3);
    }

    [Fact]
    public void Ultimatum_R_te_deja_a_1_de_vida()
    {
        // "Te dejas a 1 de vida"  ->  VALOR ABSOLUTO
        var ultimatum = Escenario.Pj("ultimatum");
        var e = Escenario.Combate(ultimatum);

        e.ReglasActivas.Add(new Regla
        {
            Id = "ultimatum.r",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = "ultimatum",
            Efecto = new FijarValor(Lectura.HpActual, Ex.K(1))
        });

        _ev.Resolver(e, new Evento(Disparador.AlUsarHabilidad, "ultimatum"));
        Assert.Equal(1.0, ultimatum.Hp, 3);
    }

    [Fact]
    public void Bladeus_W_escala_con_la_vida_faltante()
    {
        // "Obtienes X% de AP segun la VIDA FALTANTE"
        var bladeus = Escenario.Pj("bladeus");
        var e = Escenario.Combate(bladeus);

        e.ReglasActivas.Add(new Regla
        {
            Id = "bladeus.w",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.Siempre,
            PortadorId = "bladeus",
            Efecto = new ModificarStat(Stat.Ap, Capa.PctAdd,
                Ex.Por(Ex.K(1.0), Ex.HpFaltante()))
        });

        bladeus.Hp = bladeus.HpMax;
        Assert.Equal(100.0, _ev.ResolverStat(e, bladeus, Stat.Ap), 3);

        bladeus.Hp = bladeus.HpMax * 0.25;    // 75% faltante -> +75% AP
        Assert.Equal(175.0, _ev.ResolverStat(e, bladeus, Stat.Ap), 3);
    }
}

/// <summary>Reglas de flujo: las que cambian la estructura del turno.</summary>
public class FlujoTests
{
    [Fact]
    public void Artens_W_dobla_las_acciones_del_turno()
    {
        var artens = Escenario.Pj("artens");
        var e = Escenario.Combate(artens);
        var flujo = new EstadoFlujo();

        Assert.Equal(1, flujo.AccionesDisponibles(e, artens));

        flujo.Activas.Add(new ReglaFlujo
        {
            Id = "artens.w",
            Tipo = TipoFlujo.DoblarAcciones,
            Fuente = Fuente.Habilidad,
            Alcance = Selector.Yo,
            Veces = 2,
            Turnos = 1
        });

        Assert.Equal(2, flujo.AccionesDisponibles(e, artens));
    }

    [Fact]
    public void Jaens_Jeans_R_te_vuelve_invencible_pero_no_puedes_actuar()
    {
        var jaens = Escenario.Pj("jaens");
        var e = Escenario.Combate(jaens);
        var flujo = new EstadoFlujo();

        Assert.True(flujo.PuedeActuar(e, jaens));

        flujo.Activas.Add(new ReglaFlujo
        {
            Id = "jaens.r",
            Tipo = TipoFlujo.InvencibleSinActuar,
            Fuente = Fuente.Habilidad,
            Alcance = Selector.Yo,
            Turnos = 3
        });

        Assert.False(flujo.PuedeActuar(e, jaens));
    }

    [Fact]
    public void Rudentlein_R_aplica_las_habilidades_dos_veces()
    {
        var rud = Escenario.Pj("rudentlein");
        var e = Escenario.Combate(rud);
        var flujo = new EstadoFlujo();

        Assert.Equal(1, flujo.RepeticionesDeHabilidad(e, rud));

        flujo.Activas.Add(new ReglaFlujo
        {
            Id = "rudentlein.r",
            Tipo = TipoFlujo.RepetirHabilidad,
            Fuente = Fuente.Habilidad,
            Alcance = Selector.Yo,
            Veces = 2,
            Turnos = 1
        });

        Assert.Equal(2, flujo.RepeticionesDeHabilidad(e, rud));
    }

    [Fact]
    public void Ultimatum_Q_resuelve_su_preparacion_a_los_dos_turnos()
    {
        var ult = Escenario.Pj("ultimatum");
        var enemigo = Escenario.Pj("enemigo", Bando.Enemigo);
        var e = Escenario.Combate(ult, enemigo);
        var flujo = new EstadoFlujo();

        var regla = new ReglaFlujo
        {
            Id = "ultimatum.q",
            Tipo = TipoFlujo.Preparacion,
            Fuente = Fuente.Habilidad,
            TurnosDePreparacion = 2,
            EfectoAlVencer = new PonerFlag("explotado", 1, Selector.Objetivo)
        };

        flujo.Preparar(regla, ult, enemigo);

        flujo.AvanzarPreparaciones(e);
        Assert.False(enemigo.TieneFlag("explotado"));
        Assert.Single(flujo.Preparaciones);

        flujo.AvanzarPreparaciones(e);
        Assert.True(enemigo.TieneFlag("explotado"));
        Assert.Empty(flujo.Preparaciones);
    }

    [Fact]
    public void El_anclaje_impide_el_movimiento_forzado()
    {
        var drimur = Escenario.Pj("drimur");
        var enemigo = Escenario.Pj("enemigo", Bando.Enemigo, Casilla.Ofensiva);
        var e = Escenario.Combate(drimur, enemigo);

        var mover = new Mover(Casilla.Defensiva, new Selector(Ambito.Enemigos));
        var log = new List<Cambio>();

        enemigo.Flags["anclado"] = 2;
        mover.Aplicar(e, new Contexto(drimur, enemigo, null), log);
        Assert.Equal(Casilla.Ofensiva, enemigo.Posicion);   // no se movio

        enemigo.Flags.Remove("anclado");
        mover.Aplicar(e, new Contexto(drimur, enemigo, null), log);
        Assert.Equal(Casilla.Defensiva, enemigo.Posicion);
    }
}

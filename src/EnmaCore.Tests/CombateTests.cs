using Enma.Core;
using Enma.Core.Combate;
using Xunit;

namespace Enma.Core.Tests;

public class AciertoTests
{
    [Theory]
    // manual-tecnico.md  5.2, tabla de referencia con PRESS del atacante = 50
    [InlineData(100, 35.0)]
    [InlineData(75, 41.0)]   // el doc decia 40.7: errata detectada por este test
    [InlineData(50, 50.0)]
    [InlineData(25, 65.0)]
    [InlineData(10, 80.0)]
    public void Tabla_de_referencia_con_press_50(double evaEff, double esperado)
        => Assert.Equal(esperado, Acierto.Probabilidad(50, evaEff), 1);

    [Fact]
    public void El_acierto_queda_siempre_entre_5_y_95()
    {
        Assert.Equal(Constantes.HitMin, Acierto.Probabilidad(0, 1000), 4);
        Assert.True(Acierto.Probabilidad(100000, 1) <= Constantes.HitMax);
        Assert.Equal(Constantes.HitMin, Acierto.Probabilidad(0, 0), 4);
    }

    [Fact]
    public void Cuando_press_iguala_a_eva_el_acierto_es_exactamente_50()
        => Assert.Equal(50.0, Acierto.Probabilidad(77, 77), 6);

    [Fact]
    public void El_desgaste_de_evasion_es_del_3_por_ciento_por_stack()
    {
        // 0.03, no 0.01: calibrado para +50% de acierto maximo en late.
        Assert.Equal(100.0, Acierto.EvasionEfectiva(100, 0), 4);
        Assert.Equal(97.0, Acierto.EvasionEfectiva(100, 1), 4);
        Assert.Equal(70.0, Acierto.EvasionEfectiva(100, 10), 4);
    }

    [Fact]
    public void La_evasion_erosionada_nunca_baja_del_10_por_ciento()
    {
        // El suelo evita que la evasion se borre por completo a media ronda.
        Assert.Equal(10.0, Acierto.EvasionEfectiva(100, 30), 4);
        Assert.Equal(10.0, Acierto.EvasionEfectiva(100, 1000), 4);
    }

    [Fact]
    public void Calibracion_objetivo_mas_50_por_ciento_de_acierto_en_late()
    {
        // formulas.md  4: objetivo evasivo PRESS 50 vs EVA 100, base 35%.
        // En late, con ~18 stacks acumulados por la ronda, el acierto sube a ~52%.
        double bas = Acierto.Probabilidad(50, 100, 0);
        double late = Acierto.Probabilidad(50, 100, 18);

        Assert.Equal(35.0, bas, 1);
        Assert.InRange(late, 50.0, 53.0);
        Assert.InRange((late - bas) / bas, 0.42, 0.52); // ~+48%
    }

    [Fact]
    public void Se_apunta_solo_al_arquetipo_evasivo()
    {
        // Cuanta mas evasion tenga el objetivo, mayor el beneficio relativo.
        // No hace falta declarar que esto es anti-evasion: lo es por construccion.
        double gananciaEvasivo = Ganancia(eva: 100);
        double gananciaIgualado = Ganancia(eva: 50);
        Assert.True(gananciaEvasivo > gananciaIgualado);

        static double Ganancia(double eva)
        {
            double b = Acierto.Probabilidad(50, eva, 0);
            return (Acierto.Probabilidad(50, eva, 18) - b) / b;
        }
    }

    [Fact]
    public void El_nucleo_es_determinista_con_la_misma_semilla()
    {
        var a = new AleatorioSemilla(1234);
        var b = new AleatorioSemilla(1234);
        for (int i = 0; i < 100; i++)
            Assert.Equal(
                Acierto.Impacta(50, 50, 0, a),
                Acierto.Impacta(50, 50, 0, b));
    }
}

public class DanoTests
{
    [Fact]
    public void La_reduccion_usa_formula_de_ratio_con_techo_del_90()
    {
        // Red = DEF / (DEF + 100)
        Assert.Equal(0.0, Dano.Reduccion(0), 4);
        Assert.Equal(0.5, Dano.Reduccion(100), 4);
        Assert.Equal(0.75, Dano.Reduccion(300), 4);
        // Techo: nunca supera 90%, asi 100 de armadura no es invulnerabilidad
        Assert.Equal(Constantes.ReduccionCap, Dano.Reduccion(1_000_000), 4);
    }

    [Fact]
    public void La_penetracion_no_supera_el_80_por_ciento_de_la_defensa()
    {
        Assert.Equal(80.0, Dano.PenetracionEfectiva(penetracion: 500, defensa: 100), 4);
        Assert.Equal(40.0, Dano.PenetracionEfectiva(penetracion: 40, defensa: 100), 4);
    }

    [Fact]
    public void El_critico_se_aplica_DESPUES_de_la_reduccion()
    {
        // Esto es lo que evita que critico y penetracion se retroalimenten.
        var imp = new Impacto(TipoDano.Fisico, AtkOfensivo: 100);
        var def = new Defensor(Armadura: 100, ResistenciaMagica: 0);

        var sinCrit = Dano.Calcular(imp, def, criticoDecidido: false);
        var conCrit = Dano.Calcular(imp, def, criticoDecidido: true);

        // 100 bruto -> 50% de reduccion -> 50 -> x1.8 = 90
        Assert.Equal(50.0, sinCrit.Final, 4);
        Assert.Equal(90.0, conCrit.Final, 4);
        // Si se aplicara antes seria 100*1.8=180 -> 50% -> 90. Coincide aqui,
        // pero difiere en cuanto hay penetracion. Ver el test siguiente.
    }

    [Fact]
    public void Critico_y_penetracion_no_se_retroalimentan()
    {
        var imp = new Impacto(TipoDano.Fisico, AtkOfensivo: 100, Penetracion: 80);
        var def = new Defensor(Armadura: 100, ResistenciaMagica: 0);

        var r = Dano.Calcular(imp, def, criticoDecidido: true);

        // Pen efectiva 80 -> ARM final 20 -> Red = 20/120 = 0.1667
        // 100 * (1 - 0.1667) = 83.33 -> x1.8 = 150.0
        Assert.Equal(0.1667, r.ReduccionUsada, 3);
        Assert.Equal(150.0, r.Final, 1);
    }

    [Fact]
    public void Defend_anula_el_critico_en_el_primer_impacto()
    {
        var imp = new Impacto(TipoDano.Fisico, AtkOfensivo: 100);
        var def = new Defensor(Armadura: 100, ResistenciaMagica: 0, DefendActivo: true);

        var r = Dano.Calcular(imp, def, criticoDecidido: true);

        Assert.False(r.CriticoAplicado);
        Assert.Equal(50.0, r.Final, 4); // el x1.8 no se aplica
    }

    [Fact]
    public void El_dano_verdadero_ignora_defensas_pero_NO_escudos()
    {
        var imp = new Impacto(TipoDano.Verdadero, AtkOfensivo: 100);
        var def = new Defensor(Armadura: 10_000, ResistenciaMagica: 10_000, Escudo: 30);

        var r = Dano.Calcular(imp, def, criticoDecidido: false);

        Assert.Equal(0.0, r.ReduccionUsada, 4);
        Assert.Equal(100.0, r.Final, 4);
        Assert.Equal(30.0, r.AbsorbidoPorEscudo, 4);
        Assert.Equal(70.0, r.AplicadoAVida, 4);
    }

    [Fact]
    public void El_DV_separa_una_fraccion_que_no_se_reduce()
    {
        // 30% del dano se trata como verdadero; el 70% restante pasa por armadura.
        var imp = new Impacto(TipoDano.Fisico, AtkOfensivo: 100, DvPct: 0.30);
        var def = new Defensor(Armadura: 100, ResistenciaMagica: 0);

        var r = Dano.Calcular(imp, def, criticoDecidido: false);

        Assert.Equal(30.0, r.Verdadero, 4);
        Assert.Equal(35.0, r.NormalReducido, 4); // 70 * 0.5
        Assert.Equal(65.0, r.Final, 4);
    }

    [Fact]
    public void El_dano_final_nunca_es_negativo()
    {
        var imp = new Impacto(TipoDano.Fisico, AtkOfensivo: 0);
        var def = new Defensor(Armadura: 500, ResistenciaMagica: 500);
        Assert.Equal(0.0, Dano.Calcular(imp, def, false).Final, 4);
    }
}

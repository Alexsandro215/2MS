using Enma.Core;
using Enma.Core.Stats;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// Cada caso sale de una tabla de docs/formulas.md. Si un numero cambia en el
/// documento, este test debe fallar: es la red que impide la deriva silenciosa.
/// </summary>
public class EscaladoTests
{
    [Theory]
    // docs/manual-tecnico.md  3.1, tabla de factor por nivel
    [InlineData(1, 0.056)]
    [InlineData(7, 0.389)]
    [InlineData(13, 0.722)]
    [InlineData(18, 1.000)]
    public void G_sigue_la_tabla_de_factor_por_nivel(int nivel, double esperado)
        => Assert.Equal(esperado, Escalado.G(nivel), 3);

    [Fact]
    public void G_se_satura_en_1_por_encima_del_nivel_maximo()
    {
        // Ultra Late: el eje del nivel se agota, g(L) queda congelado en 1.0
        Assert.Equal(1.0, Escalado.G(18));
        Assert.Equal(1.0, Escalado.G(25));
    }

    [Fact]
    public void El_MAX_es_por_personaje_y_puede_ser_pequeno()
    {
        // Un personaje con MAX de critico de 3.9% es perfectamente valido:
        // el critico no es abundante en este juego.
        double critALv18 = Escalado.Valor(maxAspirable: 3.9, nivel: 18);
        Assert.Equal(3.9, critALv18, 3);

        // Al 90% de su aspiracion, su critico sigue siendo ~3.5%
        double critAl90 = Escalado.Valor(3.9, 16);
        Assert.Equal(0.889, Escalado.ProgresoHaciaMax(critAl90, 3.9), 3);
    }

    [Theory]
    [InlineData(3, Fase.Early)]
    [InlineData(6, Fase.Mid)]
    [InlineData(11, Fase.Mid)]
    [InlineData(16, Fase.Late)]
    [InlineData(18, Fase.Late)]
    [InlineData(19, Fase.UltraLate)]
    public void Las_fases_de_run_son_cuatro(int nivel, Fase esperada)
        => Assert.Equal(esperada, Escalado.FaseDe(nivel));
}

public class DesgasteTests
{
    [Fact]
    public void Ejemplo_canonico_de_las_tres_fases()
    {
        // formulas.md  3: 20% actual, 10% mitigacion -> 18% real.
        // Con torniquete de 5% -> 13% efectivo. Al expirar vuelve a 18%.
        double real = Desgaste.Real(acumulado: 0.20, mitigacion: 0.10);
        Assert.Equal(0.18, real, 4);

        double efectivo = Desgaste.Efectivo(real, supresionTemporal: 0.05);
        Assert.Equal(0.13, efectivo, 4);

        // Al expirar el torniquete
        Assert.Equal(0.18, Desgaste.Efectivo(real, 0.0), 4);
    }

    [Fact]
    public void La_mitigacion_esta_capada_al_50_por_ciento()
    {
        // Con la Resistencia Desgaste al tope, un 20% acumulado baja a 10% real.
        Assert.Equal(0.10, Desgaste.Real(0.20, 0.50), 4);
        // Pedir mas del cap no da mas
        Assert.Equal(0.10, Desgaste.Real(0.20, 0.99), 4);
    }

    [Fact]
    public void Curarse_media_barra_cuesta_exactamente_un_uno_por_ciento()
    {
        // La regla es PROPORCIONAL: cuesta lo mismo seas quien seas
        // y estes en el nivel que estes.
        var d1 = new Desgaste();
        d1.AplicarCuracion(curacion: 50, vidaBaseEfectiva: 100);
        Assert.Equal(0.01, d1.Acumulado, 4);

        var d2 = new Desgaste();
        d2.AplicarCuracion(curacion: 500, vidaBaseEfectiva: 1000);
        Assert.Equal(0.01, d2.Acumulado, 4);
    }

    [Fact]
    public void Proyeccion_a_final_de_run_moderada()
    {
        // formulas.md  3: curacion moderada (25% de barra) x 40 combates -> 20%
        var d = new Desgaste();
        for (int i = 0; i < 40; i++)
            d.AplicarCuracion(curacion: 25, vidaBaseEfectiva: 100);
        Assert.Equal(0.20, d.Acumulado, 4);
    }

    [Fact]
    public void La_regeneracion_y_la_skill_propia_no_generan_desgaste()
    {
        var d = new Desgaste();
        d.AplicarCuracion(curacion: 1000, vidaBaseEfectiva: 100, generaDesgaste: false);
        Assert.Equal(0.0, d.Acumulado);
    }

    [Fact]
    public void Dentro_de_una_run_el_desgaste_solo_sube()
    {
        // En Ultra Late no hay via de recuperacion: es la unica fase en la que
        // el equipo unicamente puede empeorar.
        var d = new Desgaste();
        double previo = 0;
        for (int i = 0; i < 10; i++)
        {
            d.AplicarCuracion(50, 100);
            Assert.True(d.Acumulado >= previo);
            previo = d.Acumulado;
        }
    }
}

public class AgregacionTests
{
    [Fact]
    public void Ejemplo_canonico_los_porcentajes_leen_la_stat_operativa()
    {
        // formulas.md  2:
        //   BaseRef AD 48, desgaste 10% -> StatOperativa 43.2
        //   Arma +25 flat, arma +2%, buff +3%
        //   El 2% y el 3% leen 43.2, NO 48 ni 68.2
        //   43.2 + 25 + (0.864 + 1.296) = 70.36
        double op = Agregacion.StatOperativa(baseRef: 48, desgasteEfectivo: 0.10);
        Assert.Equal(43.2, op, 4);

        double final = Agregacion.StatFinal(op, new[]
        {
            new Modificador(Capa.Flat,   25.00, "arma"),
            new Modificador(Capa.PctAdd,  0.02, "arma"),
            new Modificador(Capa.PctAdd,  0.03, "buff"),
        });

        Assert.Equal(70.36, final, 4);
    }

    [Fact]
    public void Los_porcentajes_NO_se_encadenan()
    {
        // Esta es la regla que impide las explosiones exponenciales.
        // Encadenado daria 100 * 1.5 * 1.5 = 225. Aditivo da 100 + 100*1.0 = 200.
        double op = 100;
        double final = Agregacion.StatFinal(op, new[]
        {
            new Modificador(Capa.PctAdd, 0.50),
            new Modificador(Capa.PctAdd, 0.50),
        });
        Assert.Equal(200.0, final, 4);
        Assert.NotEqual(225.0, final, 4);
    }

    [Fact]
    public void El_pipeline_completo_encadena_nivel_desgaste_y_modificadores()
    {
        double r = Agregacion.Resolver(
            maxAspirable: 100,
            nivel: 18,
            desgasteAcumulado: 0.20,
            mitigacionDesgaste: 0.10,
            supresionTemporal: 0.05,
            modificadores: Array.Empty<Modificador>());

        // BaseRef 100 -> desgaste efectivo 13% -> 87
        Assert.Equal(87.0, r, 4);
    }
}

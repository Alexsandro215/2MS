using Enma.Core;
using Enma.Core.Musica;
using Xunit;

namespace Enma.Core.Tests;

public class CargaMusicalTests
{
    [Theory]
    // musica.md  1: cada arma es una subdivision ritmica
    [InlineData(ClaseArma.Ligera, 0.125)]  // corchea
    [InlineData(ClaseArma.Media, 0.250)]   // negra
    [InlineData(ClaseArma.Pesada, 0.500)]  // blanca
    public void La_cm_base_sigue_la_figura_ritmica(ClaseArma arma, double esperado)
        => Assert.Equal(esperado, CargaMusical.BasePorImpacto(arma), 4);

    [Fact]
    public void En_tempo_el_basico_vale_el_doble()
    {
        // x2, no el +1/3 de CM MASTER, que queda superado.
        Assert.Equal(0.125, CargaMusical.GananciaPorImpacto(ClaseArma.Ligera, enTempo: false), 4);
        Assert.Equal(0.250, CargaMusical.GananciaPorImpacto(ClaseArma.Ligera, enTempo: true), 4);
        Assert.Equal(0.500, CargaMusical.GananciaPorImpacto(ClaseArma.Media, enTempo: true), 4);
    }

    [Fact]
    public void Si_falla_el_impacto_no_hay_cm()
        => Assert.Equal(0.0, CargaMusical.GananciaPorImpacto(ClaseArma.Pesada, true, impacto: false), 4);

    [Theory]
    // musica.md  1: redondeo hacia arriba solo si faltan 0.25 o menos
    [InlineData(1, 0.75, 2)]    // 1.75 -> 2
    [InlineData(1, 0.65, 1)]    // 1.65 -> 1
    [InlineData(5, 0.00, 5)]
    [InlineData(5, 0.15, 6)]    // 5.75 -> 6
    [InlineData(5, 0.60, 8)]    // 8.00 -> 8
    [InlineData(3, 0.25, 4)]    // 3.75 -> 4
    [InlineData(3, 0.60, 5)]    // 4.80 -> 5
    public void Redondeo_de_golpes(int basePartida, double velocidadAtaque, int esperado)
        => Assert.Equal(esperado, CargaMusical.Golpes(basePartida, velocidadAtaque));

    [Fact]
    public void La_pesada_con_base_1_no_llega_nunca_a_2_golpes_por_AS()
    {
        // Necesitaria 1 * (1 + AS) >= 1.75, o sea AS 75%, por encima del cap del 60%.
        // Con base 1, el Attack Speed no le sirve de nada.
        Assert.Equal(1, CargaMusical.Golpes(1, 0.60));

        // En cuanto la base sube a 2, el AS vuelve a valer: el salto mas grande del juego.
        Assert.Equal(3, CargaMusical.Golpes(2, 0.60));
    }

    [Fact]
    public void La_capacidad_cuenta_los_KO_a_la_mitad()
    {
        // Pool de referencia: storage 3 x 4 personajes = 12 CM
        Assert.Equal(12.0, CargaMusical.Capacidad(new[] { 3.0, 3, 3, 3 }, Array.Empty<double>()), 4);
        // Uno cae en KO: aporta solo 1.5
        Assert.Equal(10.5, CargaMusical.Capacidad(new[] { 3.0, 3, 3 }, new[] { 3.0 }), 4);
    }

    [Fact]
    public void Los_modificadores_de_cm_suman_linealmente()
    {
        // Ejemplo: X=1 con +20% y +10% -> 1.3
        Assert.Equal(1.3, CargaMusical.AplicarModificadores(1.0, new[] { 0.20, 0.10 }), 4);
    }

    [Fact]
    public void Al_recibir_dano_el_tempo_no_aplica_pero_defend_duplica()
    {
        double normal = CargaMusical.GananciaAlRecibir(ClaseArma.Media, defendActivo: false);
        double defendiendo = CargaMusical.GananciaAlRecibir(ClaseArma.Media, defendActivo: true);

        Assert.Equal(0.125, normal, 4);           // 0.5 * 0.25
        Assert.Equal(0.250, defendiendo, 4);      // x2 por DEFEND
        Assert.Equal(0.0, CargaMusical.GananciaAlRecibir(ClaseArma.Media, false, invulnerable: true), 4);
    }

    [Theory]
    // formulas.md  7: precios base que no cambian nunca. Combo completo = 10.
    [InlineData('Q', 1)]
    [InlineData('W', 2)]
    [InlineData('E', 3)]
    [InlineData('R', 4)]
    public void Precios_base_por_tecla(char tecla, int esperado)
        => Assert.Equal(esperado, CargaMusical.PrecioBase(tecla));

    [Fact]
    public void El_combo_completo_baja_de_10_a_6_a_4()
    {
        // Toda la progresion vive en el coste: la moneda no se infla,
        // las cosas bajan de precio.
        Assert.Equal(10.0, ComboCompleto(FaseCm.Early), 4);
        Assert.Equal(6.0, ComboCompleto(FaseCm.Mid), 4);
        Assert.Equal(4.0, ComboCompleto(FaseCm.Late), 4);

        static double ComboCompleto(FaseCm f)
            => "QWER".Sum(t => CargaMusical.Coste(t, f));
    }

    [Fact]
    public void Con_pool_12_caben_3_combos_en_late()
    {
        double pool = 12.0;
        double combo = "QWER".Sum(t => CargaMusical.Coste(t, FaseCm.Late));
        Assert.Equal(3, (int)(pool / combo));
    }

    [Fact]
    public void Un_kit_puede_desviarse_del_baseline()
    {
        // La desviacion respecto al baseline ES la identidad del estilo.
        // Kit de late: impagable al principio, devastador al final.
        double costeR = CargaMusical.Coste('R', FaseCm.Late, factorPropioDelKit: 0.25);
        Assert.Equal(1.0, costeR, 4);
    }

    [Fact]
    public void Clasical_se_enciende_con_la_accion_si_llena_la_barra()
    {
        // El disparador es la ACCION, no "empezar el turno lleno".
        // En estribillo (x1.8) el aporte es 5.4 y encender sale mucho mas barato.
        var (encendidoEstribillo, _) = CargaMusical.IntentarClasical(
            cmActual: 7.0, cmMax: 12.0, storagePropio: 3.0, multiplicadorSeccion: 1.8);
        Assert.True(encendidoEstribillo);

        // En calma (x0.5) el aporte es solo 1.5 y no llega
        var (encendidoCalma, resultante) = CargaMusical.IntentarClasical(
            cmActual: 7.0, cmMax: 12.0, storagePropio: 3.0, multiplicadorSeccion: 0.5);
        Assert.False(encendidoCalma);
        Assert.Equal(8.5, resultante, 4);
    }

    [Fact]
    public void El_excedente_al_desbordar_se_pierde()
    {
        // Esto limita solo los multiplicadores altos, sin tope artificial.
        var (encendido, resultante) = CargaMusical.IntentarClasical(
            cmActual: 11.0, cmMax: 12.0, storagePropio: 3.0, multiplicadorSeccion: 1.8);
        Assert.True(encendido);
        Assert.Equal(12.0, resultante, 4); // 11 + 5.4 = 16.4, recortado a 12
    }
}

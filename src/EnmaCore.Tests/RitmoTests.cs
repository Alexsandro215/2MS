using Enma.Core.Musica;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// Cada arma se engancha a una capa distinta de la musica.
/// Fuente: docs/musica.md  1.
/// </summary>
public class RitmoTests
{
    [Theory]
    [InlineData(ClaseArma.Ligera, CapaMusical.Melodia)]
    [InlineData(ClaseArma.Media, CapaMusical.Pulso)]
    [InlineData(ClaseArma.Pesada, CapaMusical.Compas)]
    public void Cada_arma_sigue_su_capa(ClaseArma arma, CapaMusical capa)
        => Assert.Equal(capa, BonusRitmico.CapaDe(arma));

    // ---------------------------------------------------------------- LIGERA

    [Fact]
    public void La_ligera_pierde_tres_cuartas_partes_del_dano_al_pasarse_del_compas()
    {
        var dentro = new Ritmo(PasadoElCompas: false);
        var fuera = new Ritmo(PasadoElCompas: true);

        Assert.Equal(1.00, BonusRitmico.MultiplicadorDano(ClaseArma.Ligera, dentro), 4);
        Assert.Equal(0.25, BonusRitmico.MultiplicadorDano(ClaseArma.Ligera, fuera), 4);
    }

    [Fact]
    public void La_ligera_no_gana_dano_por_clavar_el_ritmo()
    {
        // Su moneda no es el dano: es el desgaste de EVA y la CM, que se
        // cobran por impacto. Por eso alargarse compensa aunque pegue menos.
        Assert.Equal(1.00, BonusRitmico.MultiplicadorDano(ClaseArma.Ligera, Ritmo.Clavado), 4);
    }

    // ----------------------------------------------------------------- MEDIA

    [Fact]
    public void La_media_cobra_en_recurso_y_no_en_dano()
    {
        Assert.Equal(1.00, BonusRitmico.MultiplicadorDano(ClaseArma.Media, Ritmo.Clavado), 4);

        double extra = BonusRitmico.ExtraCm(ClaseArma.Media, new Ritmo(EnTempo: true), cmBase: 0.25);
        Assert.True(extra > 0);
        Assert.Equal(0.25 * BonusRitmico.ExtraCmPorPulso, extra, 6);
    }

    [Fact]
    public void El_extra_de_cm_de_la_media_es_deliberadamente_pequeno()
    {
        // El premio de verdad no es el goteo: es llegar antes a CLASICAL.
        Assert.InRange(BonusRitmico.ExtraCmPorPulso, 0.05, 0.25);
    }

    [Fact]
    public void Solo_la_media_gana_cm_extra_por_el_pulso()
    {
        var enTempo = new Ritmo(EnTempo: true);
        Assert.Equal(0.0, BonusRitmico.ExtraCm(ClaseArma.Ligera, enTempo, 0.125), 6);
        Assert.Equal(0.0, BonusRitmico.ExtraCm(ClaseArma.Pesada, enTempo, 0.5), 6);
    }

    [Fact]
    public void La_media_gana_un_golpe_al_alcanzar_el_ultimo_tiempo_del_compas()
    {
        // El Attack Speed no regala el golpe: te lleva lo bastante lejos en
        // la barra como para poder darlo.
        var enElUltimo = new Ritmo(EnTempo: true, TiempoDelCompas: 3, TiemposPorCompas: 4);
        var aMedias = new Ritmo(EnTempo: true, TiempoDelCompas: 2, TiemposPorCompas: 4);

        Assert.Equal(1, BonusRitmico.GolpesExtra(ClaseArma.Media, enElUltimo));
        Assert.Equal(0, BonusRitmico.GolpesExtra(ClaseArma.Media, aMedias));
    }

    [Fact]
    public void Llegar_al_ultimo_tiempo_fuera_de_ritmo_no_cuenta()
    {
        var fueraDeTempo = new Ritmo(EnTempo: false, TiempoDelCompas: 3, TiemposPorCompas: 4);
        Assert.Equal(0, BonusRitmico.GolpesExtra(ClaseArma.Media, fueraDeTempo));
    }

    // ---------------------------------------------------------------- PESADA

    [Fact]
    public void La_pesada_cobra_el_bonus_entero_al_clavar_el_uno()
    {
        double m = BonusRitmico.MultiplicadorDano(ClaseArma.Pesada, Ritmo.Clavado);
        Assert.Equal(1.0 + BonusRitmico.BonusCompasMaximo, m, 4);
    }

    [Fact]
    public void El_bonus_de_la_pesada_NO_es_todo_o_nada()
    {
        // Baja por cercania, tanto si te adelantas como si te retrasas.
        double mitadTarde = BonusRitmico.MultiplicadorDano(ClaseArma.Pesada,
            new Ritmo(Desfase: BonusRitmico.VentanaCompas / 2, EnCompas: true));
        double mitadPronto = BonusRitmico.MultiplicadorDano(ClaseArma.Pesada,
            new Ritmo(Desfase: -BonusRitmico.VentanaCompas / 2, EnCompas: true));

        Assert.Equal(1.0 + BonusRitmico.BonusCompasMaximo * 0.5, mitadTarde, 4);
        Assert.Equal(mitadTarde, mitadPronto, 6);   // simetrico
    }

    [Fact]
    public void Fuera_de_la_ventana_la_pesada_no_gana_nada()
    {
        double fuera = BonusRitmico.MultiplicadorDano(ClaseArma.Pesada,
            new Ritmo(Desfase: BonusRitmico.VentanaCompas * 2, EnCompas: true));
        Assert.Equal(1.0, fuera, 4);

        double sinCompas = BonusRitmico.MultiplicadorDano(ClaseArma.Pesada,
            new Ritmo(Desfase: 0, EnCompas: false));
        Assert.Equal(1.0, sinCompas, 4);
    }

    [Fact]
    public void La_pesada_tiene_una_via_de_poder_que_no_depende_de_subir_su_base()
    {
        // Es lo que arregla que con base 1 el Attack Speed no le sirva de nada.
        // Un solo golpe clavado vale lo mismo que 1.5 golpes sin ritmo.
        double clavado = BonusRitmico.MultiplicadorDano(ClaseArma.Pesada, Ritmo.Clavado);
        Assert.True(clavado >= 1.4);
    }

    // ----------------------------------------------------------- determinismo

    [Fact]
    public void Sin_informacion_ritmica_nadie_gana_ni_pierde()
    {
        // El nucleo tiene que poder resolver un combate sin capa ritmica,
        // que es como corren las simulaciones y los tests de balance.
        foreach (var arma in Enum.GetValues<ClaseArma>())
            Assert.Equal(1.0, BonusRitmico.MultiplicadorDano(arma, Ritmo.Ninguno), 4);
    }

    [Fact]
    public void El_nucleo_no_mide_tiempo_lo_recibe_ya_medido()
    {
        // La frontera entre los dos relojes de arquitectura.md  5: la
        // presentacion mide contra el audio, el nucleo solo lee el dato.
        // Mismo Ritmo, mismo resultado, siempre.
        var r = new Ritmo(Desfase: 0.04, EnCompas: true);
        for (int i = 0; i < 50; i++)
            Assert.Equal(
                BonusRitmico.MultiplicadorDano(ClaseArma.Pesada, r),
                BonusRitmico.MultiplicadorDano(ClaseArma.Pesada, r), 9);
    }
}

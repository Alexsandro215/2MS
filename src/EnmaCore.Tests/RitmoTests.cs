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

    [Fact]
    public void La_melodia_da_TIEMPO_y_por_eso_caben_mas_golpes()
    {
        // No es que los golpes se desborden del compas: es que terminas el
        // compas en curso y ademas el siguiente, y ese tiempo se llena.
        //
        // ⚠️ El ritmo exacto de la ligera (golpes por tiempo) esta sin fijar,
        // asi que aqui se comprueban las PROPIEDADES, no una cifra concreta.
        var (total, alVeinticinco) = BonusRitmico.GolpesConMelodia(
            golpesBase: 7, tiemposRestantesDelCompas: 1, tiemposPorCompas: 4);

        Assert.True(total > 7, "seguir la melodia tiene que dar MAS golpes");
        Assert.True(alVeinticinco > 0, "alguno tiene que caer en el compas extra");
        Assert.True(alVeinticinco < total, "no todos pueden estar al 25%");
    }

    [Fact]
    public void Con_el_ejemplo_del_autor_siete_de_base_dan_nueve()
    {
        // "suponiendo tiene 7 BA y en dos compases se usa 9 BA"
        // Eso sale con un ritmo de 1.5 golpes por tiempo en un 4/4, con 2
        // tiempos restantes: 3 + 6 = 9.
        //
        // ⚠️ Ese 1.5 es una LECTURA del ejemplo, no un valor decidido.
        var (total, _) = BonusRitmico.GolpesConMelodia(
            golpesBase: 7, tiemposRestantesDelCompas: 2,
            tiemposPorCompas: 4, golpesPorTiempo: 1.5);

        Assert.Equal(9, total);
    }

    [Fact]
    public void La_melodia_nunca_deja_al_arma_con_menos_golpes_de_los_suyos()
    {
        // Aunque quede poquisimo compas, no puede salir perdiendo.
        var (total, _) = BonusRitmico.GolpesConMelodia(
            golpesBase: 7, tiemposRestantesDelCompas: 0, tiemposPorCompas: 1, golpesPorTiempo: 1);
        Assert.True(total >= 7);
    }

    [Fact]
    public void Solo_se_puede_encadenar_UN_compas_extra()
    {
        // OJO: solo el siguiente. No es una racha que se gestiona.
        Assert.Equal(1, BonusRitmico.CompasesExtraMaximos);

        var (conMucho, _) = BonusRitmico.GolpesConMelodia(5, 4, 4, 2);
        var (conPoco, _) = BonusRitmico.GolpesConMelodia(5, 0, 4, 2);

        // La diferencia entre empezar el compas y acabarlo es, como mucho,
        // un compas entero de golpes. Nunca dos.
        Assert.True(conMucho - conPoco <= 4 * 2);
    }

    [Fact]
    public void Los_golpes_del_compas_extra_son_los_que_valen_el_25_por_ciento()
    {
        var (total, alVeinticinco) = BonusRitmico.GolpesConMelodia(
            golpesBase: 4, tiemposRestantesDelCompas: 2, tiemposPorCompas: 4);

        // Lo que cabia en lo que quedaba del compas va a daño pleno;
        // el resto, al 25%.
        Assert.Equal(total - 4, alVeinticinco);
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

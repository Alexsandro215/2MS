using Enma.Core.Musica;
using Xunit;

namespace Enma.Core.Tests;

/// <summary>
/// El puente entre el audio y el nucleo. Comprueba la asimetria que define
/// cuanto trabajo de autoria cuesta cada arma:
///
///   pulso y compas  ->  se CALCULAN con el BPM
///   melodia         ->  se GRABA, y solo como lista de instantes
/// </summary>
public class TemaMusicalTests
{
    /// <summary>120 BPM en 4/4: tiempo cada 0.5 s, compas cada 2 s.</summary>
    private static TemaMusical Tema120()
    {
        var t = new TemaMusical { Id = "prueba", Bpm = 120, TiemposPorCompas = 4 };
        t.Secciones.Add(SeccionMusical.Calma(0));
        t.Secciones.Add(SeccionMusical.Base(8));
        t.Secciones.Add(SeccionMusical.Estribillo(24));
        return t;
    }

    [Fact]
    public void El_pulso_y_el_compas_salen_solos_del_BPM()
    {
        var t = Tema120();
        Assert.Equal(0.5, t.DuracionTiempo, 6);
        Assert.Equal(2.0, t.DuracionCompas, 6);
    }

    [Fact]
    public void El_compas_varia_por_cancion_y_todo_se_recalcula()
    {
        var tresPorCuatro = new TemaMusical { Id = "vals", Bpm = 120, TiemposPorCompas = 3 };
        Assert.Equal(1.5, tresPorCuatro.DuracionCompas, 6);

        var seisPorOcho = new TemaMusical { Id = "6/8", Bpm = 120, TiemposPorCompas = 6 };
        Assert.Equal(3.0, seisPorOcho.DuracionCompas, 6);
    }

    [Theory]
    [InlineData(0.0, 0, 0)]
    [InlineData(0.5, 0, 1)]
    [InlineData(1.5, 0, 3)]
    [InlineData(2.0, 1, 0)]
    [InlineData(5.0, 2, 2)]
    public void Situar_un_instante_en_su_compas_y_su_tiempo(double t, int compas, int tiempo)
    {
        var tema = Tema120();
        Assert.Equal(compas, tema.CompasEn(t));
        Assert.Equal(tiempo, tema.TiempoEn(t));
    }

    [Fact]
    public void La_extension_de_la_melodia_acaba_al_final_del_compas_siguiente()
    {
        var t = Tema120();

        // A mitad del compas 0 (0-2 s): la extension llega hasta el final del 1.
        Assert.Equal(4.0, t.FinDeLaExtension(1.0), 6);

        // Desde el compas 2 (4-6 s): hasta el final del 3.
        Assert.Equal(8.0, t.FinDeLaExtension(5.0), 6);
    }

    [Fact]
    public void Solo_un_compas_extra_nunca_dos()
    {
        var t = Tema120();
        double desde = 0.5;
        double hasta = t.FinDeLaExtension(desde);
        Assert.Equal(2, (int)Math.Round((hasta - t.InicioDelCompas(desde)) / t.DuracionCompas));
    }

    [Fact]
    public void El_desfase_al_pulso_tiene_signo()
    {
        var t = Tema120();
        Assert.Equal(0.04, t.DesfaseAlPulso(0.54), 6);    // tarde
        Assert.Equal(-0.04, t.DesfaseAlPulso(0.46), 6);   // pronto
        Assert.Equal(0.0, t.DesfaseAlPulso(1.0), 6);      // clavado
    }

    // --------------------------------------------------------------- melodia

    [Fact]
    public void Sin_melodia_grabada_no_hay_nada_que_juzgar()
    {
        var t = Tema120();
        Assert.False(t.TieneMelodia);
        Assert.Null(t.DesfaseALaMelodia(1.0));
    }

    [Fact]
    public void La_melodia_es_solo_una_lista_de_instantes()
    {
        // Ni alturas, ni duraciones, ni nombres de nota. Solo cuando empieza
        // cada una, que es lo que produce un detector de inicios.
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([0.45, 0.68, 0.91, 1.13]);

        Assert.True(t.TieneMelodia);
        Assert.Equal(0.02, t.DesfaseALaMelodia(0.70)!.Value, 6);
        Assert.Equal(-0.03, t.DesfaseALaMelodia(0.65)!.Value, 6);
    }

    [Fact]
    public void Si_no_hay_melodia_la_ligera_cae_al_pulso()
    {
        // Asi una cancion sin melodia marcada sigue siendo jugable con ligera:
        // se alarga a su propio ritmo en vez de al de la melodia.
        var t = Tema120();
        var r = t.Evaluar(0.54, ClaseArma.Ligera);
        Assert.Equal(0.04, r.Desfase, 6);
    }

    // ------------------------------------------------- cada arma, su capa

    [Fact]
    public void Cada_arma_se_juzga_contra_su_propia_capa()
    {
        var t = Tema120();
        t.PulsosDeMelodia.Add(0.45);

        // t = 0.54 s. Pulso mas cercano: 0.5 (+0.04). Compas: 0 (+0.54).
        // Melodia: 0.45 (+0.09).
        Assert.Equal(0.04, t.Evaluar(0.54, ClaseArma.Media).Desfase, 6);
        Assert.Equal(0.54, t.Evaluar(0.54, ClaseArma.Pesada).Desfase, 6);
        Assert.Equal(0.09, t.Evaluar(0.54, ClaseArma.Ligera).Desfase, 6);
    }

    [Fact]
    public void Pasado_el_compas_se_mide_contra_donde_empezo_la_accion()
    {
        var t = Tema120();

        // Accion iniciada en el compas 0, golpe todavia en el compas 0
        Assert.False(t.Evaluar(1.5, ClaseArma.Ligera, inicioDeLaAccion: 0.5).PasadoElCompas);

        // Mismo inicio, pero el golpe ya cae en el compas 1
        Assert.True(t.Evaluar(2.5, ClaseArma.Ligera, inicioDeLaAccion: 0.5).PasadoElCompas);
    }

    // -------------------------------------------------------------- secciones

    [Theory]
    [InlineData(2.0, 0.5)]    // calma
    [InlineData(10.0, 1.0)]   // base
    [InlineData(30.0, 1.8)]   // estribillo
    public void El_multiplicador_depende_de_la_seccion(double t, double esperado)
        => Assert.Equal(esperado, Tema120().MultiplicadorEn(t), 6);

    // ---------------------------------------------------------- serializacion

    [Fact]
    public void Lee_el_JSON_que_escribe_el_grabador()
    {
        // Formato exacto de proto/grabador-temas.
        var json = """
        {
          "id": "boss_artur",
          "archivo": "boss_artur.ogg",
          "bpm": 132.00,
          "tiemposPorCompas": 4,
          "offset": 0.1840,
          "secciones": [],
          "melodia": [0.4500, 0.6800, 0.9100, 1.1300]
        }
        """;

        var t = TemaMusical.DesdeJson(json);

        Assert.Equal("boss_artur", t.Id);
        Assert.Equal(132.0, t.Bpm, 4);
        Assert.Equal(4, t.TiemposPorCompas);
        Assert.Equal(0.184, t.Offset, 4);
        Assert.Equal(4, t.PulsosDeMelodia.Count);
        Assert.Equal(0.45, t.PulsosDeMelodia[0], 4);
        Assert.True(t.TieneMelodia);
    }

    [Fact]
    public void Un_tema_sin_melodia_se_lee_igual()
    {
        var t = TemaMusical.DesdeJson("""
        { "id": "batalla_normal", "bpm": 110, "tiemposPorCompas": 3, "offset": 0, "melodia": [] }
        """);

        Assert.Equal(3, t.TiemposPorCompas);
        Assert.False(t.TieneMelodia);
    }

    [Fact]
    public void Ida_y_vuelta_por_JSON_conserva_el_tema()
    {
        var original = Tema120();
        original.PulsosDeMelodia.AddRange([0.45, 0.68, 0.91]);

        var copia = TemaMusical.DesdeJson(original.AJson());

        Assert.Equal(original.Bpm, copia.Bpm, 4);
        Assert.Equal(original.TiemposPorCompas, copia.TiemposPorCompas);
        Assert.Equal(original.PulsosDeMelodia.Count, copia.PulsosDeMelodia.Count);
        Assert.Equal(original.PulsosDeMelodia[1], copia.PulsosDeMelodia[1], 4);
    }

    [Fact]
    public void Encender_Clasical_sale_mas_barato_en_el_estribillo()
    {
        var t = Tema120();
        double enCalma = CargaMusical.AporteAccionClasical(3.0, t.MultiplicadorEn(2.0));
        double enEstribillo = CargaMusical.AporteAccionClasical(3.0, t.MultiplicadorEn(30.0));

        Assert.Equal(1.5, enCalma, 6);
        Assert.Equal(5.4, enEstribillo, 6);
        Assert.True(enEstribillo > enCalma * 3);
    }
}

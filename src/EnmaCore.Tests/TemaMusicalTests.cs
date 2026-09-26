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

    // ------------------------------------------------- la cadena de la ligera

    /// <summary>Melodia de 6 notas, a 0.25 s una de otra.</summary>
    private static TemaMusical ConMelodia()
    {
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([1.00, 1.25, 1.50, 1.75, 2.00, 2.25]);
        return t;
    }

    // ---------------------------------------------------- mapa de tempo

    /// <summary>
    /// Los cuatro primeros tramos de «Missin Power», tal cual los marco el
    /// grabador. 4/4 y 6/4 alternandose a 172 BPM.
    /// </summary>
    private static TemaMusical ConRejilla()
    {
        var t = new TemaMusical { Id = "missin", Bpm = 172 };
        t.Rejilla.AddRange([
            new Tramo(0.0000, 172, 4),
            new Tramo(2.7897, 172, 6),
            new Tramo(4.8824, 172, 4),
            new Tramo(7.6721, 172, 6),
        ]);
        return t;
    }

    [Fact]
    public void Sin_rejilla_todo_funciona_como_antes()
    {
        // La compatibilidad importa: hay tests y temas que no traen mapa.
        var t = Tema120();
        Assert.Equal(0, t.CompasEn(1.0));
        Assert.Equal(1, t.CompasEn(2.0));
        Assert.Equal(2.0, t.DuracionCompasEn(1.0), 6);
    }

    [Fact]
    public void Con_rejilla_cada_tramo_tiene_su_propio_compas()
    {
        var t = ConRejilla();
        Assert.Equal(1.3953, t.DuracionCompasEn(1.0), 4);    // 4/4
        Assert.Equal(2.0930, t.DuracionCompasEn(3.5), 4);    // 6/4
        Assert.Equal(1.3953, t.DuracionCompasEn(5.5), 4);    // 4/4
    }

    [Fact]
    public void El_tramo_arranca_en_la_linea_de_compas_del_anterior()
    {
        // El borde dice QUE compas cambia, no exactamente cuando. El tramo 1
        // se puso en 2.7897, pero empieza en el segundo compas del tramo 0.
        var t = ConRejilla();
        Assert.Equal(2.7907, t.InicioDelCompas(2.80), 4);
    }

    [Fact]
    public void Los_compases_se_numeran_seguidos_a_traves_de_los_tramos()
    {
        var t = ConRejilla();
        Assert.Equal(0, t.CompasEn(0.5));      // 4/4, compas 0
        Assert.Equal(1, t.CompasEn(2.0));      // 4/4, compas 1
        Assert.Equal(2, t.CompasEn(3.0));      // ya en el 6/4
        Assert.Equal(3, t.CompasEn(5.0));      // vuelta al 4/4
    }

    [Fact]
    public void Los_limites_de_un_compas_respetan_su_tramo()
    {
        var t = ConRejilla();

        var cuatro = t.LimitesDelCompas(0);
        Assert.Equal(1.3953, cuatro.Hasta - cuatro.Desde, 4);

        var seis = t.LimitesDelCompas(2);
        Assert.Equal(2.0930, seis.Hasta - seis.Desde, 4);
    }

    [Fact]
    public void La_rejilla_se_lee_del_JSON_del_grabador()
    {
        var t = TemaMusical.DesdeJson("""
        {
          "id": "missin",
          "rejilla": [
            { "desde": 0.0, "bpm": 172.0, "compas": 4 },
            { "desde": 2.7897, "bpm": 172.0, "compas": 6 }
          ],
          "melodia": [], "melodiaFondo": []
        }
        """);

        Assert.Equal(2, t.Rejilla.Count);
        Assert.Equal(172.0, t.Rejilla[0].Bpm, 4);
        Assert.Equal(6, t.Rejilla[1].TiemposPorCompas);
        Assert.Equal(2.0930, t.DuracionCompasEn(3.0), 4);
    }

    [Fact]
    public void El_bonus_experto_usa_los_compases_DEL_MAPA()
    {
        // Es lo que hacia falta para que el bonus valga en una cancion de
        // verdad: sin esto, un compas de 6/4 se medía como si fuera 4/4 y las
        // notas de fondo caian en el compas equivocado.
        var t = ConRejilla();
        var seis = t.LimitesDelCompas(2);                    // el 6/4

        // Seis negras dentro de ese compas
        for (int k = 0; k < 6; k++)
            t.PulsosDeFondo.Add(seis.Desde + k * 60.0 / 172.0);

        Assert.True(t.EsCompasExperto(2));
        Assert.Equal(6, t.FondoDelCompas(2).Count);
        Assert.False(t.EsCompasExperto(1));

        var golpes = new List<double>();
        for (int k = 0; k < 6; k++) golpes.Add(seis.Desde + k * 60.0 / 172.0);
        var r = t.ResolverExperto(2, golpes);
        Assert.True(r.Completo);
        Assert.Equal(6, r.Gratis);
    }

    // ------------------------------------------------- bonus experto

    /// <summary>
    /// 120 BPM, 4/4: el compas 2 va de 4,0 a 6,0 s. Le metemos una segunda voz
    /// de seis notas, una cada 0,3 s.
    /// </summary>
    private static TemaMusical ConFondo()
    {
        var t = Tema120();
        t.PulsosDeFondo.AddRange([4.0, 4.3, 4.6, 4.9, 5.2, 5.5]);
        return t;
    }

    [Fact]
    public void Un_compas_es_experto_si_tiene_segunda_voz()
    {
        var t = ConFondo();
        Assert.True(t.EsCompasExperto(2));
        Assert.False(t.EsCompasExperto(0));
        Assert.False(t.EsCompasExperto(5));
    }

    [Fact]
    public void No_hace_falta_marcar_nada_la_marca_sale_del_contenido()
    {
        // Es la decision de diseno: el juego deduce que compases son gratis
        // mirando donde hay segunda voz. No hay una lista aparte que mantener.
        var t = ConFondo();
        Assert.Equal(6, t.FondoDelCompas(2).Count);
        Assert.Empty(t.FondoDelCompas(1));
    }

    [Fact]
    public void Clavar_el_compas_entero_lo_hace_todo_gratis()
    {
        var t = ConFondo();
        var r = t.ResolverExperto(2, [4.0, 4.3, 4.6, 4.9, 5.2, 5.5]);

        Assert.True(r.Enganchado);
        Assert.Equal(6, r.Gratis);
        Assert.True(r.Completo);
    }

    [Fact]
    public void Hay_que_ENGANCHAR_en_la_primera_nota_del_compas()
    {
        // Entrar a mitad no da nada: el compas gratis se coge desde su
        // principio o no se coge.
        var t = ConFondo();
        var r = t.ResolverExperto(2, [4.6, 4.9, 5.2]);

        Assert.True(r.Elegible);
        Assert.False(r.Enganchado);
        Assert.Equal(0, r.Gratis);
    }

    [Fact]
    public void Al_primer_fallo_se_acaba_el_regalo_pero_lo_ganado_se_queda()
    {
        // El premio escala con lo lejos que llegues. No es todo o nada.
        var t = ConFondo();
        var r = t.ResolverExperto(2, [4.0, 4.3, 4.6, 4.75, 5.2, 5.5]);

        Assert.True(r.Enganchado);
        Assert.Equal(3, r.Gratis);      // las tres primeras, y ahi se corto
        Assert.False(r.Completo);
    }

    [Fact]
    public void Saltarse_una_nota_tambien_corta()
    {
        var t = ConFondo();
        var r = t.ResolverExperto(2, [4.0, 4.3, 4.9]);   // se salta la de 4.6
        Assert.Equal(2, r.Gratis);
        Assert.False(r.Completo);
    }

    [Fact]
    public void Solo_cuenta_el_compas_en_curso_no_se_encadena_con_el_siguiente()
    {
        var t = ConFondo();
        t.PulsosDeFondo.AddRange([6.0, 6.3]);            // compas 3

        var r = t.ResolverExperto(2, [4.0, 4.3, 4.6, 4.9, 5.2, 5.5, 6.0, 6.3]);
        Assert.Equal(6, r.Gratis);                       // no cuenta los del 3
        Assert.True(r.Completo);
    }

    [Fact]
    public void Un_compas_sin_segunda_voz_no_da_nada()
    {
        var t = ConFondo();
        var r = t.ResolverExperto(1, [2.0, 2.3]);
        Assert.False(r.Elegible);
        Assert.Equal(0, r.Gratis);
    }

    [Fact]
    public void La_segunda_voz_se_lee_de_melodiaFondo()
    {
        var t = TemaMusical.DesdeJson("""
        { "id": "x", "bpm": 120, "tiemposPorCompas": 4,
          "melodia": [1.0, 2.0], "melodiaFondo": [4.0, 4.3, 4.6] }
        """);
        Assert.Equal(2, t.PulsosDeMelodia.Count);
        Assert.Equal(3, t.PulsosDeFondo.Count);
        Assert.True(t.EsCompasExperto(2));
    }

    // --------------------------------- la ventana se recorta sola por nota

    [Fact]
    public void Una_nota_suelta_se_queda_con_el_techo()
    {
        var t = Tema120();
        t.PulsosDeMelodia.Add(1.0);
        Assert.Equal(0.08, t.VentanaDe(0, 0.08), 6);
    }

    [Fact]
    public void Con_notas_juntas_la_ventana_se_recorta_a_la_mitad_del_hueco()
    {
        // Un pasaje de fusas a 172 BPM: 43,6 ms de hueco.
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([1.0000, 1.0436, 1.0872, 1.1308]);

        // Techo 80 ms, pero solo caben 21,8.
        Assert.Equal(0.0218, t.VentanaDe(1, 0.08), 4);
        Assert.Equal(0.0218, t.VentanaDe(2, 0.08), 4);
    }

    [Fact]
    public void Dos_ventanas_nunca_se_solapan()
    {
        // Es la propiedad que hace falta: si se solaparan, no estaria definido
        // a que nota golpeo el jugador — y la cadena de la ligera depende de
        // saberlo.
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([1.0, 1.0436, 1.2, 1.5, 1.5872, 2.4]);

        for (int i = 1; i < t.PulsosDeMelodia.Count; i++)
        {
            double borde = t.PulsosDeMelodia[i - 1] + t.VentanaDe(i - 1, 0.08);
            double siguiente = t.PulsosDeMelodia[i] - t.VentanaDe(i, 0.08);
            Assert.True(borde <= siguiente + 1e-9,
                $"las ventanas de {i - 1} y {i} se pisan");
        }
    }

    [Fact]
    public void En_un_pasaje_denso_cada_golpe_cae_en_UNA_nota()
    {
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([1.0000, 1.0436, 1.0872]);

        Assert.Equal(0, t.NotaEn(1.0100, 0.08));
        Assert.Equal(1, t.NotaEn(1.0400, 0.08));
        Assert.Equal(2, t.NotaEn(1.0900, 0.08));
    }

    [Fact]
    public void Lejos_de_toda_nota_no_hay_acierto()
    {
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([1.0000, 1.0436]);
        Assert.Equal(-1, t.NotaEn(1.5, 0.08));
    }

    [Fact]
    public void El_techo_se_lee_del_JSON_del_grabador()
    {
        var t = TemaMusical.DesdeJson("""
        { "id": "x", "bpm": 172, "tiemposPorCompas": 4, "toleranciaMs": 80.0,
          "melodia": [1.0, 2.0] }
        """);
        Assert.Equal(0.08, t.VentanaMelodia, 6);
    }

    [Fact]
    public void La_cadena_aguanta_si_pulsas_notas_seguidas()
    {
        var t = ConMelodia();
        Assert.True(t.CadenaCompleta([1.00, 1.25, 1.50, 1.75]));
    }

    [Fact]
    public void Un_poco_torcido_pero_dentro_de_la_ventana_sigue_valiendo()
    {
        var t = ConMelodia();
        Assert.True(t.CadenaCompleta([1.04, 1.21, 1.53, 1.79]));
    }

    [Fact]
    public void Desafinar_corta_la_cadena_en_ese_golpe()
    {
        var t = ConMelodia();
        // El tercero cae en tierra de nadie: 1.38 no es ninguna nota.
        Assert.Equal(2, t.GolpesEnCadena([1.00, 1.25, 1.38, 1.75]));
        Assert.False(t.CadenaCompleta([1.00, 1.25, 1.38, 1.75]));
    }

    [Fact]
    public void Saltarse_una_nota_corta_la_cadena_aunque_pulses_en_tempo()
    {
        // ✅ Lo decidido: pararse la corta. 1.75 esta CLAVADO en su nota, pero
        // se dejo pasar la de 1.50 sin pulsarla.
        var t = ConMelodia();
        Assert.Equal(2, t.GolpesEnCadena([1.00, 1.25, 1.75]));
        Assert.False(t.CadenaCompleta([1.00, 1.25, 1.75]));
    }

    [Fact]
    public void La_cadena_puede_empezar_en_cualquier_nota()
    {
        var t = ConMelodia();
        Assert.True(t.CadenaCompleta([1.50, 1.75, 2.00]));
    }

    [Fact]
    public void Fallarle_al_enemigo_NO_corta_la_cadena()
    {
        // La cadena solo mira lo que PULSAS. Aqui no aparece la punteria por
        // ningun lado — es justo lo que mantiene viva a la ligera contra un
        // rival con evasion alta, que es donde mas se falla.
        var t = ConMelodia();
        Assert.True(t.CadenaCompleta([1.00, 1.25, 1.50]));
    }

    [Fact]
    public void Los_silencios_de_la_cancion_no_cortan_la_cadena()
    {
        // Si la cancion no tiene nota, no hay nada que dejar pasar. Por eso se
        // cuenta por NOTAS y no por tiempo.
        var t = Tema120();
        t.PulsosDeMelodia.AddRange([1.00, 1.25, 3.00, 3.25]);   // hueco de 1.75 s
        Assert.True(t.CadenaCompleta([1.00, 1.25, 3.00, 3.25]));
    }

    [Fact]
    public void Sin_melodia_grabada_no_hay_cadena()
    {
        Assert.Equal(0, Tema120().GolpesEnCadena([1.0, 1.25]));
        Assert.False(Tema120().CadenaCompleta([1.0, 1.25]));
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

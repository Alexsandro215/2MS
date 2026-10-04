namespace Enma.Core.Musica;

/// <summary>
/// Los numeros de la economia de Carga Musical de UN combate.
///
/// Arrancan con los del diseño (formulas.md  7, musica.md  1 y  3), que son la
/// unica fuente de estos valores, y se pueden cambiar para probar otro balance
/// sin tocar el codigo: la ventana de combate los edita.
///
/// Son por combate, no globales: dos combates con ajustes distintos no se
/// pisan, y cada simulacion sigue siendo reproducible.
/// </summary>
public sealed class AjustesCm
{
    // ---- lo que gana cada golpe que conecta: la figura del arma ----

    /// <summary>Corchea: 1/8 de CM por golpe.</summary>
    public double PorGolpeLigera { get; set; } = 1.0 / 8.0;

    /// <summary>Negra: 1/4 de CM por golpe.</summary>
    public double PorGolpeMedia { get; set; } = 1.0 / 4.0;

    /// <summary>Blanca: 1/2 de CM por golpe.</summary>
    public double PorGolpePesada { get; set; } = 1.0 / 2.0;

    // ---- en tempo: lo que se multiplica un golpe que cae en su capa ----

    /// <summary>
    /// La media se lleva el x2: es la generadora del equipo. Ligera y pesada
    /// se quedan en el suelo comun de +20%. Los documentos aun dicen x2 para
    /// todas: esta pendiente de decidir, y aqui se puede probar.
    /// </summary>
    public double TempoLigera { get; set; } = 1.0 + Constantes.BonusTempo;
    public double TempoMedia { get; set; } = BonusRitmico.CmPorPulsoDeLaMedia;
    public double TempoPesada { get; set; } = 1.0 + Constantes.BonusTempo;

    // ---- al recibir ----

    /// <summary>Al recibir un golpe se gana esta fraccion de la figura de quien pega.</summary>
    public double AlRecibir { get; set; } = Constantes.CmDefensorFactor;

    /// <summary>En guardia (DEFEND), lo que se gana al recibir se multiplica por esto.</summary>
    public double EnGuardia { get; set; } = Constantes.DefendMultiplicadorCm;

    /// <summary>CLASICAL multiplica toda la CM que gana su bando.</summary>
    public double Clasical { get; set; } = Constantes.ClasicalMultiplicador;

    // ---- lo que cuestan las teclas: precio base x descuento de la fase ----

    /// <summary>Precio base de cada tecla. El combo completo, 10.</summary>
    public double PrecioQ { get; set; } = 1;
    public double PrecioW { get; set; } = 2;
    public double PrecioE { get; set; } = 3;
    public double PrecioR { get; set; } = 4;

    /// <summary>
    /// Descuento de cada fase de la run. BASELINE, no ley: cada kit se desvia
    /// a proposito. El suelo duro es 0.33: por debajo, el recurso deja de
    /// decidir nada.
    /// </summary>
    public double FaseEarly { get; set; } = 1.00;
    public double FaseMid { get; set; } = 0.60;
    public double FaseLate { get; set; } = 0.40;

    // ---- lecturas ----

    public double PorGolpe(ClaseArma arma) => arma switch
    {
        ClaseArma.Ligera => PorGolpeLigera,
        ClaseArma.Media => PorGolpeMedia,
        ClaseArma.Pesada => PorGolpePesada,
        _ => 0.0
    };

    public double Tempo(ClaseArma arma) => arma switch
    {
        ClaseArma.Ligera => TempoLigera,
        ClaseArma.Media => TempoMedia,
        ClaseArma.Pesada => TempoPesada,
        _ => 1.0
    };

    public double Precio(char tecla) => char.ToUpperInvariant(tecla) switch
    {
        'Q' => PrecioQ,
        'W' => PrecioW,
        'E' => PrecioE,
        'R' => PrecioR,
        _ => 0.0
    };

    public double Fase(FaseCm fase) => fase switch
    {
        FaseCm.Early => FaseEarly,
        FaseCm.Mid => FaseMid,
        FaseCm.Late => FaseLate,
        _ => 1.0
    };
}

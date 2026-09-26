namespace Enma.Core.Musica;

/// <summary>
/// Clase de arma. No son numeros arbitrarios: cada una es una subdivision ritmica.
/// La ligera toca corcheas, la media negras, la pesada blancas.
/// Ver docs/musica.md  1.
/// </summary>
public enum ClaseArma
{
    /// <summary>Corchea. 1/8 de CM por impacto. Sus golpes aplican DOBLE desgaste de EVA.</summary>
    Ligera,
    /// <summary>Negra. 1/4 de CM por impacto.</summary>
    Media,
    /// <summary>Blanca. 1/2 de CM por impacto. Cobra el amplificador elemental integro.</summary>
    Pesada
}

/// <summary>Fase de la partida, que fija el precio de las habilidades.</summary>
public enum FaseCm { Early, Mid, Late }

/// <summary>
/// Carga Musical. Fuente: docs/formulas.md  7 y docs/musica.md.
///
/// La barra es COMPARTIDA de equipo. El pool NO escala con el nivel: la capacidad
/// es una ventaja de suministro, no de poder. Toda la progresion vive en el
/// COSTE: la moneda no se infla, las cosas bajan de precio.
/// </summary>
public static class CargaMusical
{
    /// <summary>CM base generada por impacto, segun la figura ritmica del arma.</summary>
    public static double BasePorImpacto(ClaseArma arma) => arma switch
    {
        ClaseArma.Ligera => 1.0 / 8.0,
        ClaseArma.Media => 1.0 / 4.0,
        ClaseArma.Pesada => 1.0 / 2.0,
        _ => 0.0
    };

    /// <summary>
    /// Golpes base por arma antes de Velocidad de Ataque. Son valores de PARTIDA,
    /// no un techo: la base de golpes se puede mejorar durante la partida.
    /// </summary>
    public static int GolpesBase(ClaseArma arma) => arma switch
    {
        ClaseArma.Ligera => 5,
        ClaseArma.Media => 3,
        ClaseArma.Pesada => 1,
        _ => 0
    };

    /// <summary>
    /// Numero de golpes tras aplicar Velocidad de Ataque.
    ///
    ///   golpes = base * (1 + AS)
    ///
    /// Se redondea HACIA ARRIBA solo si faltan 0.25 o menos para el entero
    /// siguiente. En cualquier otro caso, se trunca.
    ///   1.75 -> 2   (faltan 0.25)
    ///   1.65 -> 1   (faltan 0.35)
    /// </summary>
    public static int Golpes(int golpesBase, double velocidadAtaque)
    {
        double bruto = golpesBase * (1.0 + Math.Max(0.0, velocidadAtaque));
        double piso = Math.Floor(bruto);
        double resto = bruto - piso;
        // Faltan <= 0.25 para el siguiente entero  =>  resto >= 0.75
        return (int)(resto >= 0.75 ? piso + 1 : piso);
    }

    /// <summary>
    /// CM ganada por un impacto de basico.
    /// En tempo el basico vale el DOBLE. Si el impacto falla, CM = 0.
    /// </summary>
    public static double GananciaPorImpacto(ClaseArma arma, bool enTempo, bool impacto = true)
    {
        // Sin conectar no hay CM, y por tanto no hay nada que multiplicar.
        // Es lo que obliga a abrir con la ligera antes de que la media pueda
        // sostener: ver musica.md  1.
        if (!impacto) return 0.0;

        double cm = BasePorImpacto(arma);
        if (!enTempo) return cm;

        // La MEDIA se lleva el premio grande; el resto, el suelo comun.
        // No se acumulan: o uno o el otro.
        return arma == ClaseArma.Media
            ? cm * BonusRitmico.CmPorPulsoDeLaMedia
            : cm * (1.0 + Constantes.BonusTempo);
    }

    /// <summary>
    /// CM ganada al RECIBIR dano: la mitad de lo que genera el arma del atacante.
    /// El tempo NO aplica al recibir. DEFEND duplica. Si es invulnerable, no gana.
    /// </summary>
    public static double GananciaAlRecibir(ClaseArma armaAtacante, bool defendActivo, bool invulnerable = false)
    {
        if (invulnerable) return 0.0;
        double g = Constantes.CmDefensorFactor * BasePorImpacto(armaAtacante);
        return defendActivo ? g * Constantes.DefendMultiplicadorCm : g;
    }

    /// <summary>
    /// Capacidad del pool de equipo.
    ///   CM_max = SUM(storage de activos) + 0.5 * SUM(storage de KO)
    /// Si CM_actual supera CM_max, se pierde el excedente (clamp inmediato).
    /// </summary>
    public static double Capacidad(IEnumerable<double> storageActivos, IEnumerable<double> storageKo)
        => storageActivos.Sum() + Constantes.StorageKoFactor * storageKo.Sum();

    /// <summary>
    /// Modificadores de CM: suman linealmente, no se encadenan.
    ///   CM_final = X + X*m1 + X*m2 + ...
    /// Ejemplo: X=1 con +20% y +10% -> 1.3
    /// </summary>
    public static double AplicarModificadores(double cmBase, IEnumerable<double> modificadores)
        => cmBase + cmBase * modificadores.Sum();

    /// <summary>Precio base de cada tecla. No cambia nunca. Combo completo = 10.</summary>
    public static int PrecioBase(char tecla) => char.ToUpperInvariant(tecla) switch
    {
        'Q' => 1,
        'W' => 2,
        'E' => 3,
        'R' => 4,
        _ => 0
    };

    /// <summary>
    /// Factor de descuento por fase. BASELINE, no ley: cada kit se desvia de esta
    /// tabla a proposito, y esa desviacion ES la identidad del estilo.
    ///
    /// El suelo duro es 0.33: por debajo, el recurso deja de decidir nada.
    /// </summary>
    public static double FactorFase(FaseCm fase) => fase switch
    {
        FaseCm.Early => 1.00,
        FaseCm.Mid => 0.60,
        FaseCm.Late => 0.40,
        _ => 1.00
    };

    /// <summary>Coste real de una tecla: precio base por el factor de la fase.</summary>
    public static double Coste(char tecla, FaseCm fase, double? factorPropioDelKit = null)
        => PrecioBase(tecla) * (factorPropioDelKit ?? FactorFase(fase));

    /// <summary>
    /// Aporte de la ACCION de Clasical: el storage propio por el multiplicador de
    /// la seccion musical. Si con ese aporte la barra llega al maximo, CLASICAL
    /// se enciende durante el ciclo.
    ///
    /// El disparador es la ACCION, no "empezar el turno lleno": esa regla se
    /// autodestruia. Ver musica.md  3.
    /// </summary>
    public static double AporteAccionClasical(double storagePropio, double multiplicadorSeccion)
        => storagePropio * multiplicadorSeccion;

    /// <summary>
    /// Comprueba si la accion de Clasical enciende el modo, y devuelve la barra
    /// resultante ya recortada al maximo (el excedente se pierde).
    /// </summary>
    public static (bool Encendido, double CmResultante) IntentarClasical(
        double cmActual, double cmMax, double storagePropio, double multiplicadorSeccion)
    {
        double aporte = AporteAccionClasical(storagePropio, multiplicadorSeccion);
        double bruto = cmActual + aporte;
        bool encendido = bruto >= cmMax;
        return (encendido, Math.Min(bruto, cmMax));
    }
}

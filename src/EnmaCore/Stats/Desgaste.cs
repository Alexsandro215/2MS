namespace Enma.Core.Stats;

/// <summary>
/// Desgaste (fatiga). Fuente: docs/formulas.md  3 y manual-tecnico.md  4.
///
/// Deterioro fisico acumulado. No es dano, ni debuff, ni efecto temporal:
/// es el coste de salir a campana.
///
/// Dentro de una run NO se recupera. Entre campanas si: baja solo con los dias,
/// y mas rapido con tratamiento.
///
/// El sistema no castiga jugar normal: castiga DEPENDER de la curacion.
/// </summary>
public sealed class Desgaste
{
    /// <summary>Desgaste acumulado, en fraccion [0, 1].</summary>
    public double Acumulado { get; private set; }

    /// <summary>
    /// Acumulador interno de curacion recibida, en puntos de vida.
    /// Cada vez que cruza el 50% de la vida base efectiva, genera +1% de desgaste.
    /// </summary>
    private double _curacionAcumulada;

    public Desgaste(double acumuladoInicial = 0.0)
        => Acumulado = Math.Clamp(acumuladoInicial, 0.0, 1.0);

    /// <summary>
    /// FASE 1 - Mitigacion. La Resistencia al Desgaste reduce el desgaste actual.
    /// Ejemplo canonico: 20% actual con 10% de mitigacion -> 18% real.
    /// </summary>
    public static double Real(double acumulado, double mitigacion)
    {
        mitigacion = Math.Clamp(mitigacion, 0.0, Constantes.ResistenciaDesgasteCap);
        return acumulado * (1.0 - mitigacion);
    }

    /// <summary>
    /// FASE 2 - Supresion temporal (torniquetes). Al expirar, el desgaste vuelve.
    /// Ejemplo canonico: 18% real con torniquete de 5% -> 13% efectivo.
    /// </summary>
    public static double Efectivo(double real, double supresionTemporal)
        => Math.Max(0.0, real - supresionTemporal);

    /// <summary>
    /// FASE 3 - Multiplicador aplicado a la stat. StatOperativa = BaseRef * este valor.
    /// </summary>
    public static double Multiplicador(double efectivo)
        => 1.0 - Math.Clamp(efectivo, 0.0, 1.0);

    /// <summary>Atajo: de acumulado a multiplicador, pasando por las tres fases.</summary>
    public static double MultiplicadorDesde(double acumulado, double mitigacion, double supresion)
        => Multiplicador(Efectivo(Real(acumulado, mitigacion), supresion));

    /// <summary>
    /// Aplica curacion y genera desgaste si cruza umbrales.
    ///
    /// Cada 50% de la VIDA BASE recuperada (acumulado) -> +1% de desgaste.
    /// Es PROPORCIONAL, no un umbral fijo en puntos: curarte media barra cuesta
    /// 1% seas quien seas y estes en el nivel que estes. Asi la regla no se rompe
    /// al escalar, y no penaliza a los personajes de vida alta.
    /// </summary>
    /// <param name="curacion">Puntos de vida restaurados.</param>
    /// <param name="vidaBaseEfectiva">HP maximo efectivo del objetivo.</param>
    /// <param name="generaDesgaste">
    /// false para las fuentes exentas: skill de curacion propia sobre uno mismo,
    /// y Regeneracion de Vida. Ver manual-tecnico.md  4.1.
    /// </param>
    /// <returns>Puntos de desgaste generados por esta curacion (fraccion).</returns>
    public double AplicarCuracion(double curacion, double vidaBaseEfectiva, bool generaDesgaste = true)
    {
        if (!generaDesgaste || curacion <= 0 || vidaBaseEfectiva <= 0) return 0.0;

        _curacionAcumulada += curacion;
        double paso = Constantes.FraccionVidaPorPuntoDesgaste * vidaBaseEfectiva;

        double generado = 0.0;
        while (_curacionAcumulada >= paso)
        {
            _curacionAcumulada -= paso;
            double antes = Acumulado;
            Acumulado = Math.Clamp(Acumulado + Constantes.DesgastePorUmbral, 0.0, 1.0);
            generado += Acumulado - antes;
        }
        return generado;
    }

    /// <summary>
    /// Recuperacion entre campanas. Dentro de una run esto NO debe llamarse.
    /// </summary>
    public void Recuperar(double cantidad)
        => Acumulado = Math.Clamp(Acumulado - Math.Max(0.0, cantidad), 0.0, 1.0);

    /// <summary>
    /// Valvula deliberada: eventos concretos que quitan desgaste.
    /// Son la excepcion, no un sistema de recuperacion.
    /// </summary>
    public void EventoQuitaDesgaste(double cantidad) => Recuperar(cantidad);

    /// <summary>
    /// Fija el desgaste a un valor absoluto. Lo necesitan los efectos que lo
    /// imponen de golpe, como Kronwell R ("obtienes 70% de desgaste").
    /// No pasa por los umbrales de curacion: es una imposicion, no acumulacion.
    /// </summary>
    public void Fijar(double valor)
    {
        Acumulado = Math.Clamp(valor, 0.0, 1.0);
        _curacionAcumulada = 0.0;
    }

    /// <summary>Suma desgaste directamente, sin pasar por la curacion.</summary>
    public void Sumar(double cantidad)
        => Acumulado = Math.Clamp(Acumulado + cantidad, 0.0, 1.0);
}

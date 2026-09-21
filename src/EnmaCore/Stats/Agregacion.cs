namespace Enma.Core.Stats;

/// <summary>
/// Una fuente de modificacion de stat. Ver arquitectura.md  1: ocho sistemas
/// distintos (pasiva, gadget, emblema, emocion, elemento, terreno, comida, casilla)
/// son el MISMO esquema. El nucleo no debe saber que es un "gadget": solo conoce
/// reglas y eventos.
/// </summary>
public enum Capa
{
    /// <summary>Suma directa a la stat.</summary>
    Flat,
    /// <summary>Porcentaje. Lee siempre la StatOperativa, nunca se encadena.</summary>
    PctAdd,
    /// <summary>Capa final excepcional. Reservada: si una pasiva necesita multiplicar, lo declara ella misma.</summary>
    MultFinal
}

/// <summary>Un modificador concreto aportado por alguna fuente.</summary>
/// <param name="Capa">Como se aplica.</param>
/// <param name="Valor">Magnitud. En PctAdd y MultFinal es fraccion (0.02 = 2%).</param>
/// <param name="Origen">Etiqueta de depuracion. El nucleo no ramifica sobre ella.</param>
public readonly record struct Modificador(Capa Capa, double Valor, string Origen = "");

/// <summary>
/// Regla de agregacion oficial. Fuente: docs/formulas.md  2.
///
///   StatOperativa = BaseRef * (1 - desgasteEfectivo)
///   StatFinal     = StatOperativa + SUM(Flats) + (StatOperativa * SUM(Pcts))
///
/// REGLA CLAVE: los porcentajes NO se multiplican entre si. Todos leen la MISMA
/// StatOperativa, sin encadenarse. Esto es lo que impide las explosiones
/// exponenciales y el doble escalado, y es la propiedad mas valiosa del sistema.
/// </summary>
public static class Agregacion
{
    /// <summary>
    /// Stat operativa: la base de referencia ya mermada por el desgaste.
    /// Es el unico valor que leen los porcentajes.
    /// </summary>
    public static double StatOperativa(double baseRef, double desgasteEfectivo)
        => baseRef * Desgaste.Multiplicador(desgasteEfectivo);

    /// <summary>
    /// Stat final, con un unico punto de calculo.
    ///
    /// Ejemplo canonico (formulas.md  2):
    ///   BaseRef AD 48, desgaste 10% -> StatOperativa 43.2
    ///   Arma +25 flat, arma +2%, buff +3%
    ///   El 2% y el 3% leen 43.2, NO 48 ni 68.2
    ///   43.2 + 25 + (0.864 + 1.296) = 70.36
    /// </summary>
    public static double StatFinal(double statOperativa, IEnumerable<Modificador> modificadores)
    {
        double flats = 0.0;
        double pcts = 0.0;
        double mult = 1.0;

        foreach (var m in modificadores)
        {
            switch (m.Capa)
            {
                case Capa.Flat: flats += m.Valor; break;
                case Capa.PctAdd: pcts += m.Valor; break;
                case Capa.MultFinal: mult *= 1.0 + m.Valor; break;
            }
        }

        // Los porcentajes leen la StatOperativa, no el acumulado.
        double resultado = statOperativa + flats + (statOperativa * pcts);
        return resultado * mult;
    }

    /// <summary>Pipeline completo: de base y nivel hasta stat final.</summary>
    public static double Resolver(
        double maxAspirable,
        int nivel,
        double desgasteAcumulado,
        double mitigacionDesgaste,
        double supresionTemporal,
        IEnumerable<Modificador> modificadores)
    {
        double baseRef = Escalado.Valor(maxAspirable, nivel);
        double efectivo = Desgaste.Efectivo(
            Desgaste.Real(desgasteAcumulado, mitigacionDesgaste),
            supresionTemporal);
        return StatFinal(StatOperativa(baseRef, efectivo), modificadores);
    }
}

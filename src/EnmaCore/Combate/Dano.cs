namespace Enma.Core.Combate;

public enum TipoDano
{
    Fisico,
    Magico,
    /// <summary>Ignora ARM y RM por completo. NO ignora escudos.</summary>
    Verdadero
}

/// <summary>Parametros de un impacto concreto.</summary>
public readonly record struct Impacto(
    TipoDano Tipo,
    double AtkOfensivo,
    double MultSkill = 1.0,
    double MultContexto = 1.0,
    double MultFinalTipo = 1.0,
    /// <summary>Fraccion del dano que se trata como verdadero, en [0, 1].</summary>
    double DvPct = 0.0,
    double Penetracion = 0.0,
    bool PuedeCriticar = true);

/// <summary>Estado defensivo relevante para un impacto.</summary>
public readonly record struct Defensor(
    double Armadura,
    double ResistenciaMagica,
    double Escudo = 0.0,
    /// <summary>DEFEND activo y aun sin consumir en este impacto.</summary>
    bool DefendActivo = false);

/// <summary>Desglose del calculo, util para tests y para depurar balance.</summary>
public readonly record struct ResultadoDano(
    double Bruto,
    double Verdadero,
    double NormalReducido,
    double Final,
    double AbsorbidoPorEscudo,
    double AplicadoAVida,
    bool CriticoAplicado,
    double ReduccionUsada);

/// <summary>
/// Un golpe que pidio una regla y el motor aun no ha repartido.
///
/// Un efecto es un DATO: no alcanza al Resolvedor ni debe hacerlo. Pide el
/// golpe, y el motor lo pasa por el mismo pipeline que un basico, para que una
/// habilidad no pueda saltarse el hit-check, el escudo ni las pasivas.
/// </summary>
public readonly record struct ImpactoPendiente(
    string ObjetivoId,
    Impacto Perfil,
    string? Elemento = null,
    double ProbabilidadElemento = 0.0);

/// <summary>
/// Pipeline de dano por impacto. Fuente: docs/formulas.md  5 y  13.
///
/// El orden importa y no es negociable: aplicar el critico DESPUES de la
/// reduccion evita que critico y penetracion se retroalimenten.
/// </summary>
public static class Dano
{
    /// <summary>
    /// Penetracion efectiva: nunca supera el 80% de la defensa del objetivo.
    /// </summary>
    public static double PenetracionEfectiva(double penetracion, double defensa)
        => Math.Min(Math.Max(0.0, penetracion), Constantes.PenCap * Math.Max(0.0, defensa));

    /// <summary>
    /// Reduccion por defensa mediante formula de ratio, con techo del 90%.
    ///
    ///   Red = DEF_f / (DEF_f + 100)
    ///
    /// ARM y RM son VALORES, no porcentajes. Asi el 100% de armadura no es
    /// invulnerabilidad y no hay que vigilar el techo a mano.
    /// </summary>
    public static double Reduccion(double defensaFinal)
    {
        double d = Math.Max(0.0, defensaFinal);
        return Math.Min(Constantes.ReduccionCap, d / (d + Constantes.ReduccionK));
    }

    /// <summary>Calcula un impacto completo. No muta nada: devuelve el desglose.</summary>
    public static ResultadoDano Calcular(Impacto imp, Defensor def, bool criticoDecidido)
    {
        // --- 1. DANO BRUTO y reduccion segun tipo ---
        double bruto = imp.AtkOfensivo * imp.MultSkill * imp.MultContexto * imp.MultFinalTipo;

        double reduccion;
        if (imp.Tipo == TipoDano.Verdadero)
        {
            reduccion = 0.0;
        }
        else
        {
            double defensa = imp.Tipo == TipoDano.Fisico ? def.Armadura : def.ResistenciaMagica;
            double penEff = PenetracionEfectiva(imp.Penetracion, defensa);
            reduccion = Reduccion(Math.Max(0.0, defensa - penEff));
        }

        // --- 2. SEPARAR DV% ---
        double dv = Math.Clamp(imp.DvPct, 0.0, Constantes.DvCap);
        double dTrue = bruto * dv;
        double dNorm = bruto - dTrue;

        // --- 3. REDUCIR SOLO EL NORMAL ---
        double dReducido = dNorm * (1.0 - reduccion);

        // --- 4. CRITICO DESPUES DE LA REDUCCION ---
        bool quiereCriticar = criticoDecidido && imp.PuedeCriticar;
        double candidato = quiereCriticar
            ? dReducido * Constantes.CritMultiplicador
            : dReducido;

        // --- 5. DEFEND ANULA EL CRITICO en el primer impacto ---
        bool criticoAplicado = quiereCriticar && !def.DefendActivo;
        double dFinalNormal = def.DefendActivo ? dReducido : candidato;

        // --- 6. TOTAL ---
        double dFinal = Math.Max(0.0, dFinalNormal + dTrue);

        // --- 7. APLICACION: primero al ESCUDO, el sobrante al HP ---
        // El dano verdadero NO ignora escudos.
        double absorbido = Math.Min(def.Escudo, dFinal);
        double aVida = dFinal - absorbido;

        return new ResultadoDano(
            Bruto: bruto,
            Verdadero: dTrue,
            NormalReducido: dReducido,
            Final: dFinal,
            AbsorbidoPorEscudo: absorbido,
            AplicadoAVida: aVida,
            CriticoAplicado: criticoAplicado,
            ReduccionUsada: reduccion);
    }

    /// <summary>
    /// Dano de un ataque basico: referencia ofensiva por el multiplicador del arma.
    /// </summary>
    public static double DanoBasico(double danoOfensivoRef, double multArma)
        => danoOfensivoRef * multArma;

    /// <summary>
    /// Dano de habilidad. Escala desde la referencia ofensiva canonica del momento,
    /// NO desde un dano ya reescalado: asi se evita el double-dipping.
    /// </summary>
    public static double DanoSkill(double danoBaseSkill, double danoOfensivoRef, double coefSkill)
        => danoBaseSkill + danoOfensivoRef * coefSkill;
}

using Enma.Core.Estado;

namespace Enma.Core.Combate;

/// <summary>
/// Acciones de turno. Fuente: manual-tecnico.md  2.3.
///
/// REGLA DE MODO: el turno se inicia con basicos o con QWER. Una vez se entra
/// en QWER, no se vuelve a los basicos.
/// </summary>
public abstract record Accion
{
    public required string ActorId { get; init; }
    /// <summary>true si la accion cierra el turno del personaje.</summary>
    public abstract bool CierraTurno { get; }
}

/// <summary>
/// Ataque basico. Hasta N impactos segun arma y Velocidad de Ataque.
/// No se puede cambiar de objetivo entre golpe y golpe.
/// </summary>
public sealed record AtaqueBasico : Accion
{
    public required string ObjetivoId { get; init; }
    public override bool CierraTurno => false;
}

/// <summary>Habilidad QWER. Cada tecla se consume al usarse.</summary>
public sealed record UsarHabilidad : Accion
{
    public required char Tecla { get; init; }
    public string? ObjetivoId { get; init; }
    public override bool CierraTurno => false;
}

/// <summary>
/// DEFENSA. +50% de ARM y RM, se consume en el primer impacto recibido,
/// anula el critico de ese impacto y duplica la ganancia de CM.
/// </summary>
public sealed record Defender : Accion
{
    public override bool CierraTurno => true;
}

/// <summary>
/// Accion de CARGA MUSICAL. Aporta el storage propio por el multiplicador de
/// la seccion musical. Si con eso la barra llega al maximo, enciende CLASICAL.
/// </summary>
public sealed record CargarMusica : Accion
{
    public double MultiplicadorSeccion { get; init; } = 1.0;
    public override bool CierraTurno => true;
}

/// <summary>Moverse a otra casilla de la propia linea.</summary>
public sealed record Moverse : Accion
{
    public required Casilla Destino { get; init; }
    public override bool CierraTurno => false;
}

/// <summary>
/// SWAP. Uno del front se va al banquillo y entra otro del banquillo.
/// El que entra hereda la casilla y la linea exactas del que sale, y no
/// actua hasta el turno siguiente.
///
/// Sin <see cref="SalienteId"/> sale el propio actor. Con el, el actor gasta
/// su turno en sacar a un compañero en K.O. Y si el actor esta en el
/// banquillo, es un RELEVO: entra el mismo, y solo puede hacerlo si una
/// pasiva se lo concede (TipoFlujo.RelevoDesdeBanquillo).
/// </summary>
public sealed record Swap : Accion
{
    public required string EntranteId { get; init; }
    public string? SalienteId { get; init; }
    public override bool CierraTurno => true;
}

/// <summary>
/// HIDE. Esconderse: no targeteable para el rival, sin basicos ni skills,
/// pero con los objetos disponibles.
///
/// Con <see cref="OcultoId"/>, el actor gasta su turno en esconder a un
/// compañero en K.O.: se lo saca de la linea y deja de estar expuesto.
/// </summary>
public sealed record Esconderse : Accion
{
    public string? OcultoId { get; init; }
    public override bool CierraTurno => true;
}

/// <summary>
/// Salir del escondite. Es gratis, pero se sale SIEMPRE a la casilla
/// defensiva: para atacar ese turno hay que avanzar primero.
/// </summary>
public sealed record SalirDelEscondite : Accion
{
    public override bool CierraTurno => false;
}

/// <summary>ESCAPE. Huida de todo el equipo, con probabilidad de exito.</summary>
public sealed record Huir : Accion
{
    public override bool CierraTurno => true;
}

/// <summary>Usar comida o consumible en la ventana pre-ofensiva.</summary>
public sealed record UsarObjeto : Accion
{
    public required string ObjetoId { get; init; }
    public bool EsComida { get; init; }
    public string? ObjetivoId { get; init; }
    /// <summary>Un gadget SIEMPRE consume turno.</summary>
    public bool EsGadget { get; init; }
    public override bool CierraTurno => EsGadget;
}

/// <summary>Pasar el turno sin hacer nada. Tambien lo produce el timeout.</summary>
public sealed record Pasar : Accion
{
    public bool PorTimeout { get; init; }
    public override bool CierraTurno => true;
}

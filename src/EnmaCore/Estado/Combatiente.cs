namespace Enma.Core.Estado;

/// <summary>Bando al que pertenece un combatiente.</summary>
public enum Bando { Aliado, Enemigo }

/// <summary>
/// Casilla dentro de la linea propia. Cada combatiente del front ocupa una
/// linea de 3 casillas y solo se desplaza adelante o atras sobre ella.
/// Ver manual-tecnico.md  1.2.
/// </summary>
public enum Casilla { Defensiva = 0, Neutral = 1, Ofensiva = 2 }

/// <summary>
/// Donde esta un combatiente. Ver tablero.md  1, 5 y 6.
///
///   FRONT      peleando, en su linea de 3 casillas
///   BANQUILLO  esperando un cambio. Los enemigos no tienen
///   ESCONDITE  fuera de alcance del rival. Uno por personaje
/// </summary>
public enum Zona { Front, Banquillo, Escondite }

/// <summary>
/// Catalogo de estadisticas. Fuente: manual-tecnico.md  3.2 a  3.4.
///
/// Nota de nomenclatura: los documentos usan PRESS, PRES y ACC para la misma
/// stat. Aqui se fija en <c>Press</c>.
/// </summary>
public enum Stat
{
    // Primarias
    Hp, Ad, Ap, Arm, Rm, Press, Eva, Crit,

    // Secundarias ofensivas
    Preparacion, VelocidadAtaque, Tempo, PenArmadura, PenMagica, RoboVida,
    DominioArmas, DominioObjetos, DanoArea, DanoExplosivo, UltimaBala, Rebote,

    // Secundarias defensivas
    RegeneracionVida, DominioMedico, ResistenciaDesgaste, Inquebrantable,
    EficienciaEscudo, ExtensionEscudo, ExtensionInvulnerable, Tenacidad,
    AmplificadorBuff, ExtensionBuff, AmplificadorDebuff, ExtensionDebuff,
    DominioComida, ExtensionComida, Reflejos,

    // Stats propias de kits concretos (Willham)
    AntiArm, AntiRm,
}

/// <summary>Una marca elemental activa sobre un combatiente.</summary>
public sealed class MarcaElemental(string elemento, int nivel, int turnosRestantes)
{
    public string Elemento { get; } = elemento;
    /// <summary>1 o 2. L2 bloquea nuevos elementos.</summary>
    public int Nivel { get; set; } = nivel;
    public int TurnosRestantes { get; set; } = turnosRestantes;
}

/// <summary>
/// Un combatiente. Lleva lo que el motor de reglas necesita consultar:
/// stats, contadores propios con nombre, flags, posicion y estado elemental.
/// </summary>
public sealed class Combatiente
{
    public required string Id { get; init; }
    public string Nombre { get; init; } = "";
    public Bando Bando { get; init; }
    public Casilla Posicion { get; set; } = Casilla.Neutral;

    /// <summary>Front, banquillo o escondite.</summary>
    public Zona Zona { get; set; } = Zona.Front;

    /// <summary>
    /// Linea que ocupa dentro del bando (0..5). Tambien fija su sitio en el
    /// orden de turnos, que avanza de derecha a izquierda.
    /// </summary>
    public int Linea { get; set; }

    public int Nivel { get; set; } = 1;

    /// <summary>MAX aspirable por stat. El techo de ESTE personaje, mejorable jugando.</summary>
    public Dictionary<Stat, double> MaxAspirable { get; } = new();

    /// <summary>
    /// Contadores propios con nombre. Es lo que necesitan los kits de cargas:
    /// "cargas_kronwell", "cargas_hallen". No son stats ni modificadores.
    /// </summary>
    public Dictionary<string, double> Contadores { get; } = new();

    /// <summary>
    /// Flags de estado: "anclado", "inmovil", "inmune_ee", "invulnerable", y
    /// "atendido" —le estan dando primeros auxilios: no se le remata—.
    /// </summary>
    public Dictionary<string, int> Flags { get; } = new();

    public double Hp { get; set; }
    public double Escudo { get; set; }
    public bool EnKo { get; set; }

    /// <summary>
    /// Muerte definitiva: lo remataron estando en K.O. Sigue contando como
    /// caido, pero ya no aporta nada a la barra ni se le puede volver a rematar.
    /// </summary>
    public bool Muerto { get; set; }

    /// <summary>
    /// Turnos de su bando que ha EMPEZADO en K.O. Con uno basta: su bando ya
    /// tuvo su ventana de rescate. Ver manual-tecnico.md  10.3.
    /// </summary>
    public int TurnosEnKo { get; set; }

    /// <summary>
    /// Se le puede rematar: caido, expuesto en el front, con la ventana de
    /// rescate ya pasada, y sin nadie dandole primeros auxilios.
    /// </summary>
    public bool Rematable => EnKo && !Muerto && Zona == Zona.Front
                             && TurnosEnKo >= 1 && !TieneFlag("atendido");

    public Stats.Desgaste Desgaste { get; } = new();
    public Musica.ClaseArma Arma { get; set; } = Musica.ClaseArma.Media;

    /// <summary>Capacidad que aporta al pool de CM del equipo.</summary>
    public double Storage { get; set; } = 3.0;

    public List<MarcaElemental> Elementos { get; } = new();

    // ---- lecturas derivadas, que las reglas consultan ----

    /// <summary>Vida base efectiva a este nivel, ya mermada por el desgaste.</summary>
    public double HpMax =>
        Stats.Agregacion.StatOperativa(
            Stats.Escalado.Valor(MaxAspirable.GetValueOrDefault(Stat.Hp), Nivel),
            Desgaste.Acumulado);

    public double HpPct => HpMax <= 0 ? 0.0 : Hp / HpMax;
    public double HpFaltantePct => 1.0 - HpPct;

    public double Contador(string nombre) => Contadores.GetValueOrDefault(nombre);
    public bool TieneFlag(string flag) => Flags.GetValueOrDefault(flag) > 0;
    public bool TieneElemento => Elementos.Count > 0;
    public bool TieneElementoL2 => Elementos.Exists(e => e.Nivel >= 2);

    /// <summary>Valor base de una stat a este nivel, antes de modificadores.</summary>
    public double BaseRef(Stat s) =>
        Stats.Escalado.Valor(MaxAspirable.GetValueOrDefault(s), Nivel);
}

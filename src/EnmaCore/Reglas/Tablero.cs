using Enma.Core.Estado;
using Enma.Core.Stats;

namespace Enma.Core.Reglas;

/// <summary>
/// Lo que hace cada casilla, y vale en TODO combate.
///
/// Es contenido, no codigo del motor: una fila mas del esquema unico
/// (arquitectura.md  1: "Casilla | estar en ella | posicion = OFENSIVA |
/// +30% PRES, -30% EVA"). El motor solo se encarga de que esten puestas.
///
/// El tablero SOLO mueve PRESS y EVA. Todo lo demas que pase en una casilla
/// viene del terreno. Ver manual-tecnico.md  1.2 y tablero.md  2.
///
/// Sin portador: cada combatiente lee SU casilla. Asi la EVA que cuenta en
/// un golpe es la de la casilla del defensor, y la PRESS la del atacante.
/// </summary>
public static class Tablero
{
    public static IReadOnlyList<Regla> Reglas { get; } =
    [
        new Regla
        {
            Id = "casilla.ofensiva",
            Fuente = Fuente.Casilla,
            Disparador = Disparador.Siempre,
            Condicion = new EnCasilla(Casilla.Ofensiva),
            Efecto = new Varios(
                new ModificarStat(Stat.Press, Capa.PctAdd, Ex.K(+Constantes.BonoCasilla)),
                new ModificarStat(Stat.Eva, Capa.PctAdd, Ex.K(-Constantes.BonoCasilla))),
            Etiqueta = "Ofensiva: más precisión, menos evasión"
        },
        new Regla
        {
            Id = "casilla.defensiva",
            Fuente = Fuente.Casilla,
            Disparador = Disparador.Siempre,
            Condicion = new EnCasilla(Casilla.Defensiva),
            Efecto = new Varios(
                new ModificarStat(Stat.Press, Capa.PctAdd, Ex.K(-Constantes.BonoCasilla)),
                new ModificarStat(Stat.Eva, Capa.PctAdd, Ex.K(+Constantes.BonoCasilla))),
            Etiqueta = "Defensiva: menos precisión, más evasión"
        },
    ];

    /// <summary>Las pone en un combate, sin duplicarlas.</summary>
    public static void Instalar(EstadoCombate e)
    {
        foreach (var r in Reglas)
            if (!e.ReglasActivas.Exists(x => x.Id == r.Id))
                e.ReglasActivas.Add(r);
    }
}

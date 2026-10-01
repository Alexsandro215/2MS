using Enma.Core.Combate;
using Enma.Core.Estado;

namespace Enma.Core.Simulacion;

/// <summary>Lo que hizo un combatiente a lo largo de un combate.</summary>
public sealed class Rendimiento
{
    public int Golpes { get; set; }
    public int Aciertos { get; set; }
    public double Dano { get; set; }
    public double CmGenerada { get; set; }
    public int KosCausados { get; set; }
    public int Remates { get; set; }

    /// <summary>Como acabo: en K.O. o muerto.</summary>
    public bool Cayo { get; set; }
    public bool Murio { get; set; }
}

/// <summary>El desenlace de un combate simulado, con lo que hizo cada uno.</summary>
public sealed record ResultadoCombate(
    Desenlace Desenlace,
    int Ciclos,
    int AccionesRechazadas,
    IReadOnlyDictionary<string, Rendimiento> PorCombatiente);

/// <summary>La media de un combatiente sobre un lote de combates.</summary>
public sealed record RendimientoMedio(
    double Dano, double CmGenerada, double Acierto, double Cayo, double Murio);

/// <summary>Lo que salio de jugar muchos combates con semillas distintas.</summary>
public sealed record ResumenLote(
    int Combates,
    int Victorias,
    int Derrotas,
    int Huidas,
    int SinResolver,
    double CiclosMedios,
    int AccionesRechazadas,
    IReadOnlyDictionary<string, RendimientoMedio> PorCombatiente)
{
    public double PorcentajeVictorias => Combates == 0 ? 0 : 100.0 * Victorias / Combates;
}

/// <summary>
/// Juega combates enteros sin presentacion: es lo que hace posible balancear.
///
/// Cada bando lo mueve una <see cref="IPolitica"/>. El simulador solo pide
/// acciones, se las pasa al motor y apunta lo que pasa. Con la misma semilla,
/// el mismo combate.
/// </summary>
public static class Simulador
{
    /// <summary>Tope de acciones por turno de un personaje, por si una politica se atasca.</summary>
    private const int AccionesPorTurno = 20;

    public static ResultadoCombate Jugar(
        EstadoCombate estado, IPolitica aliados, IPolitica enemigos,
        int maxCiclos = 100, TextWriter? bitacora = null)
    {
        var m = new MotorCombate(estado);
        var registro = estado.Combatientes.ToDictionary(c => c.Id, _ => new Rendimiento());
        int rechazadas = 0;

        while (m.Desenlace == Desenlace.EnCurso && estado.Ciclo < maxCiclos)
        {
            var politica = estado.BandoActivo == Bando.Aliado ? aliados : enemigos;

            while (m.Desenlace == Desenlace.EnCurso)
            {
                var quien = m.ActorDeTurno;
                if (quien is null)
                {
                    var candidatos = m.PendientesDeActuar().ToList();
                    if (candidatos.Count == 0) break;
                    quien = politica.ElegirAbridor(m, candidatos);
                }

                int hechas = 0;
                foreach (var accion in politica.Turno(m, quien))
                {
                    var r = m.Ejecutar(accion);
                    if (!r.Permitida)
                    {
                        rechazadas++;
                        bitacora?.WriteLine($"   ✗ {quien.Id}: {accion.GetType().Name} — {r.Motivo}");
                        continue;
                    }

                    Anotar(registro, r);
                    bitacora?.WriteLine($"   {quien.Id}: {Describir(accion, r)}");
                    if (r.CierraTurno || m.Desenlace != Desenlace.EnCurso) break;
                    if (++hechas >= AccionesPorTurno) break;
                }

                // Si la politica no cerro el turno, se cierra solo: el timeout.
                if (m.Desenlace == Desenlace.EnCurso && m.PendientesDeActuar().Any(c => c.Id == quien.Id))
                    m.CerrarTurnoDe(quien.Id);
            }

            if (m.Desenlace != Desenlace.EnCurso) break;
            m.CerrarTurnoDeBando();

            if (estado.BandoActivo == Bando.Aliado)
                bitacora?.WriteLine($"— ciclo {estado.Ciclo} · " + string.Join("  ",
                    estado.Combatientes.Where(c => c.Zona != Zona.Banquillo).Select(Situacion)));
        }

        foreach (var c in estado.Combatientes)
        {
            registro[c.Id].Cayo = c.EnKo;
            registro[c.Id].Murio = c.Muerto;
        }

        bitacora?.WriteLine($"== {m.Desenlace} en {estado.Ciclo} ciclos");
        return new ResultadoCombate(m.Desenlace, estado.Ciclo, rechazadas, registro);
    }

    /// <summary>
    /// Juega <paramref name="combates"/> combates, cada uno con su semilla, y
    /// los resume. <paramref name="montar"/> recibe la semilla y devuelve un
    /// combate recien preparado.
    /// </summary>
    public static ResumenLote Lote(
        Func<int, EstadoCombate> montar, int combates,
        IPolitica aliados, IPolitica enemigos, int primeraSemilla = 1, int maxCiclos = 100)
    {
        var resultados = Enumerable.Range(primeraSemilla, combates)
            .Select(s => Jugar(montar(s), aliados, enemigos, maxCiclos))
            .ToList();

        var ids = resultados.SelectMany(r => r.PorCombatiente.Keys).Distinct();
        var medias = ids.ToDictionary(id => id, id =>
        {
            var rs = resultados.Select(r => r.PorCombatiente[id]).ToList();
            int golpes = rs.Sum(x => x.Golpes);
            return new RendimientoMedio(
                Dano: rs.Average(x => x.Dano),
                CmGenerada: rs.Average(x => x.CmGenerada),
                Acierto: golpes == 0 ? 0 : 100.0 * rs.Sum(x => x.Aciertos) / golpes,
                Cayo: 100.0 * rs.Count(x => x.Cayo) / rs.Count,
                Murio: 100.0 * rs.Count(x => x.Murio) / rs.Count);
        });

        return new ResumenLote(
            Combates: resultados.Count,
            Victorias: resultados.Count(r => r.Desenlace == Desenlace.VictoriaAliada),
            Derrotas: resultados.Count(r => r.Desenlace == Desenlace.DerrotaAliada),
            Huidas: resultados.Count(r => r.Desenlace == Desenlace.Huida),
            SinResolver: resultados.Count(r => r.Desenlace == Desenlace.EnCurso),
            CiclosMedios: resultados.Count == 0 ? 0 : resultados.Average(r => r.Ciclos),
            AccionesRechazadas: resultados.Sum(r => r.AccionesRechazadas),
            PorCombatiente: medias);
    }

    private static void Anotar(Dictionary<string, Rendimiento> registro, ResultadoAccion r)
    {
        foreach (var i in r.Impactos)
        {
            var a = registro[i.AtacanteId];
            a.Golpes++;
            if (!i.Acerto) continue;
            a.Aciertos++;
            a.Dano += i.DanoAplicado + i.AbsorbidoPorEscudo;
            a.CmGenerada += i.CmAtacante;
            if (i.ProvocoKo) a.KosCausados++;
            if (i.Remato) a.Remates++;
        }
    }

    private static string Describir(Accion a, ResultadoAccion r) => a switch
    {
        AtaqueBasico b => $"basicos a {b.ObjetivoId}: " + string.Join(" ", r.Impactos.Select(i =>
            !i.Acerto ? "·" : i.Remato ? "REMATE" : i.ProvocoKo ? $"{i.DanoAplicado:F0}!KO" : $"{i.DanoAplicado:F0}")),
        Swap s => $"saca a {s.SalienteId ?? a.ActorId}, entra {s.EntranteId}",
        Esconderse h => h.OcultoId is null ? "se esconde" : $"esconde a {h.OcultoId}",
        Moverse mv => $"a {mv.Destino}",
        _ => r.Motivo.Length > 0 ? r.Motivo : a.GetType().Name
    };

    private static string Situacion(Combatiente c)
        => $"{c.Id} {(c.Muerto ? "†" : c.EnKo ? "KO" : $"{c.Hp:F0}")}{(c.Zona == Zona.Escondite ? "(esc)" : "")}";
}

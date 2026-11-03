using Enma.Core.Estado;

namespace Enma.Core.Reglas;

/// <summary>Lo que produjo una pasada del evaluador.</summary>
public sealed record Resolucion(List<Regla> Disparadas, List<Cambio> Cambios);

/// <summary>
/// El resolvedor de reglas.
///
/// LA PRUEBA DE FUEGO (arquitectura.md  1): este archivo no debe contener
/// NUNCA un `if` sobre <see cref="Fuente"/>. Solo conoce reglas y eventos.
/// Si mañana inventas un sistema nuevo —reliquias, tatuajes, lo que sea— es
/// contenido, no codigo.
///
/// Se le entrega un estado y un evento, y devuelve los cambios. No decide
/// nada sobre presentacion.
/// </summary>
public sealed class Evaluador
{
    /// <summary>Tope de encadenamiento, por si una regla dispara otra en bucle.</summary>
    public int ProfundidadMaxima { get; init; } = 8;

    /// <summary>
    /// Resuelve todas las reglas que responden a un evento.
    /// </summary>
    public Resolucion Resolver(EstadoCombate estado, Evento evento)
    {
        var disparadas = new List<Regla>();
        var cambios = new List<Cambio>();

        var actor = evento.ActorId is null ? null : estado.Buscar(evento.ActorId);
        var objetivo = evento.ObjetivoId is null ? null : estado.Buscar(evento.ObjetivoId);

        // Se materializa la lista antes de aplicar nada: una regla puede crear
        // terreno, y no queremos que el terreno recien creado se dispare en la
        // misma pasada.
        var candidatas = estado.TodasLasReglas()
            .Where(r => r.Disparador == evento.Tipo || r.Disparador == Disparador.Siempre)
            .ToList();

        foreach (var regla in candidatas)
        {
            var portador = regla.PortadorId is null ? actor : estado.Buscar(regla.PortadorId);
            var ctx = new Contexto(portador, objetivo, evento);

            if (regla.Condicion is not null && !regla.Condicion.Cumple(estado, ctx))
                continue;

            regla.Efecto.Aplicar(estado, ctx, cambios);
            disparadas.Add(regla);
        }

        // Las de un solo uso se retiran despues, no durante la iteracion.
        foreach (var r in disparadas.Where(r => r.UnSoloUso))
            estado.ReglasActivas.Remove(r);

        return new Resolucion(disparadas, cambios);
    }

    /// <summary>
    /// Recolecta los modificadores de stat vigentes para un combatiente,
    /// sin aplicarlos. Es lo que consume <see cref="Stats.Agregacion"/>.
    ///
    /// Orden de recoleccion segun formulas.md  2: base y nivel, arma, armadura,
    /// accesorios, gadgets, pasivas, buffs, emocion, elemento, entorno, estados.
    /// El evaluador no impone ese orden — lo impone el orden de la lista de
    /// reglas, que es dato.
    /// </summary>
    public List<Stats.Modificador> ModificadoresDe(EstadoCombate estado, Combatiente quien, Stat? soloEsta = null)
    {
        var mods = new List<Stats.Modificador>();
        var cambios = new List<Cambio>();

        foreach (var regla in estado.TodasLasReglas())
        {
            if (regla.Disparador != Disparador.Siempre) continue;

            var portador = regla.PortadorId is null ? quien : estado.Buscar(regla.PortadorId);
            var ctx = new Contexto(portador, quien, null);

            if (regla.Condicion is not null && !regla.Condicion.Cumple(estado, ctx))
                continue;

            cambios.Clear();
            regla.Efecto.Aplicar(estado, ctx, cambios);

            foreach (var c in cambios)
            {
                if (c.Tipo != "stat" || c.ObjetivoId != quien.Id) continue;
                var partes = c.Detalle.Split(':');
                if (partes.Length != 2) continue;
                if (soloEsta.HasValue && partes[0] != soloEsta.Value.ToString()) continue;
                if (!Enum.TryParse<Stats.Capa>(partes[1], out var capa)) continue;
                mods.Add(new Stats.Modificador(capa, c.Valor, regla.Id));
            }
        }
        return mods;
    }

    /// <summary>
    /// Resuelve una stat completa de un combatiente: base, nivel, desgaste y
    /// todas las reglas vigentes que la tocan. Un unico punto de calculo.
    /// </summary>
    public double ResolverStat(EstadoCombate estado, Combatiente quien, Stat stat)
    {
        double operativa = Stats.Agregacion.StatOperativa(
            quien.BaseRef(stat), quien.Desgaste.Acumulado);
        return Stats.Agregacion.StatFinal(operativa, ModificadoresDe(estado, quien, stat));
    }

    /// <summary>Envejece duraciones al terminar un turno: flags, elementos y terrenos.</summary>
    public void AvanzarTurno(EstadoCombate estado)
    {
        foreach (var c in estado.Combatientes)
        {
            foreach (var clave in c.Flags.Keys.ToList())
            {
                c.Flags[clave]--;
                if (c.Flags[clave] <= 0) c.Flags.Remove(clave);
            }
            for (int i = c.Elementos.Count - 1; i >= 0; i--)
            {
                c.Elementos[i].TurnosRestantes--;
                if (c.Elementos[i].TurnosRestantes <= 0) c.Elementos.RemoveAt(i);
            }
        }

        for (int i = estado.Terrenos.Count - 1; i >= 0; i--)
        {
            estado.Terrenos[i].TurnosRestantes--;
            if (estado.Terrenos[i].TurnosRestantes <= 0) estado.Terrenos.RemoveAt(i);
        }

        // Los buffs concedidos en combate son reglas con fecha. Todo lo demas
        // —pasivas, equipo, emblemas— nace con -1 y no se toca.
        for (int i = estado.ReglasActivas.Count - 1; i >= 0; i--)
        {
            var r = estado.ReglasActivas[i];
            if (r.TurnosRestantes < 0) continue;
            r.TurnosRestantes--;
            if (r.TurnosRestantes <= 0) estado.ReglasActivas.RemoveAt(i);
        }

        estado.Historial.Reiniciar();
        estado.Turno++;
    }
}

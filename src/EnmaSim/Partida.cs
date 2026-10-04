using System.Globalization;
using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Simulacion;

namespace Enma.Sim;

/// <summary>
/// Un combate que juegas TU, por consola, contra un rival que no hace nada.
///
/// Es la primera forma de tocar el nucleo con las manos: eliges quien abre y
/// las acciones de cada personaje, y ves lo que el motor hace con ellas. Si
/// una accion no vale, el motor dice por que, y eso tambien es parte de la
/// prueba.
///
/// No hay musica en tiempo real: la Carga Musical pregunta en que seccion
/// de la cancion cae, y el ritmo del basico todavia no esta conectado.
///
/// Con <paramref name="eco"/>, cada respuesta se repite en pantalla: cuando
/// la entrada llega de un guion no hay consola que la ensene, y sin eco la
/// transcripcion no se puede seguir.
/// </summary>
sealed class PartidaConsola(EstadoCombate estado, int semilla, TextReader entrada, TextWriter salida, bool eco = false)
{
    /// <summary>La barra arrastra error de coma flotante: por debajo de esto, es cero.</summary>
    private const double Tolerancia = 1e-9;

    private readonly MotorCombate _m = new(estado);

    private readonly Dictionary<string, Rendimiento> _registro =
        estado.Combatientes.ToDictionary(c => c.Id, _ => new Rendimiento());

    /// <summary>
    /// CM que cada uno metio DE VERDAD en la barra, por cualquier via. Lo
    /// que genera un golpe con la barra llena no cabe y no cuenta.
    /// </summary>
    private readonly Dictionary<string, double> _cmALaBarra =
        estado.Combatientes.ToDictionary(c => c.Id, _ => 0.0);

    /// <summary>CM que generaron los golpes y no cupo en la barra.</summary>
    private double _cmPerdida;

    /// <summary>Los que entraron por swap este turno: no actuan hasta el que viene.</summary>
    private readonly HashSet<string> _recienEntrados = new();

    /// <summary>
    /// Quien ya empezo la ofensiva este turno. Las reglas las aplica el
    /// motor; esto solo sirve para ensenar el menu corto.
    /// </summary>
    private readonly HashSet<string> _enOfensiva = new();

    /// <summary>Alguien ha hecho algo en el ciclo en curso: cuenta en el resumen.</summary>
    private bool _cicloEmpezado;

    private bool _salir;

    public void Jugar()
    {
        Presentacion();

        while (!_salir && _m.Desenlace == Desenlace.EnCurso)
        {
            if (estado.BandoActivo == Bando.Enemigo)
            {
                TurnoRival();
                continue;
            }

            var quien = _m.ActorDeTurno;
            if (quien is null)
            {
                var candidatos = _m.PendientesDeActuar().ToList();
                if (candidatos.Count == 0)
                {
                    _m.CerrarTurnoDeBando();          // tu bando ha terminado
                    continue;
                }
                Tablero();
                quien = ElegirAbridor(candidatos);
                if (quien is null) break;
            }
            else
            {
                Tablero();
            }

            TurnoDe(quien);
        }

        Final();
    }

    // ------------------------------------------------------------ el turno

    private void TurnoDe(Combatiente quien)
    {
        while (!_salir && _m.Desenlace == Desenlace.EnCurso)
        {
            // Mientras nadie haya abierto, aun se puede cambiar de idea.
            bool puedeCambiar = _m.ActorDeTurno is null;
            var accion = Menu(quien, puedeCambiar);
            if (accion is null) return;

            double cmAntes = estado.CmActual;
            var r = _m.Ejecutar(accion);
            Mostrar(quien, accion, r, cmAntes);

            if (!r.Permitida) continue;
            if (r.CierraTurno) return;
            if (!_m.PendientesDeActuar().Any(c => c.Id == quien.Id)) return;
        }
    }

    private Combatiente? ElegirAbridor(List<Combatiente> candidatos)
    {
        salida.WriteLine(" ¿Quién abre el turno? Después sigue solo, de derecha a izquierda.");
        for (int i = 0; i < candidatos.Count; i++)
            salida.WriteLine($"   {i + 1}  {candidatos[i].Nombre} ({NombreArma(candidatos[i].Arma)}, {Donde(candidatos[i])})");

        while (true)
        {
            var txt = Leer();
            if (txt is null) return null;
            if (Numero(txt, candidatos.Count) is int n) return candidatos[n - 1];
            salida.WriteLine("   Elige un número de la lista, o s para salir.");
        }
    }

    /// <summary>Pregunta la siguiente accion. null = que abra otro, o salir.</summary>
    private Accion? Menu(Combatiente quien, bool puedeCambiar)
    {
        bool corto = _enOfensiva.Contains(quien.Id);

        while (true)
        {
            PintarMenu(quien, puedeCambiar, corto);
            var txt = Leer();
            if (txt is null) return null;
            if (puedeCambiar && txt == "v") return null;

            bool valida = corto
                ? txt is "1" or "3" or "0"
                : txt.Length == 1 && txt[0] is >= '0' and <= '9';
            if (!valida)
            {
                salida.WriteLine(corto
                    ? "   Empezada la ofensiva solo quedan 1, 3 o 0."
                    : "   Elige una opción del menú.");
                continue;
            }

            Accion? a = txt switch
            {
                "1" => Basicos(quien),
                "2" => Moverse(quien),
                "3" => Habilidad(quien),
                "4" => new Defender { ActorId = quien.Id },
                "5" => new Huir { ActorId = quien.Id },
                "6" => Carga(quien),
                "7" => Objeto(quien),
                "8" => quien.Zona == Zona.Escondite
                    ? new SalirDelEscondite { ActorId = quien.Id }
                    : new Esconderse { ActorId = quien.Id },
                "9" => Cambio(quien),
                _ => new Pasar { ActorId = quien.Id },
            };
            if (_salir) return null;
            if (a is not null) return a;
            // El submenu se cancelo: vuelve a salir el menu.
        }
    }

    private void PintarMenu(Combatiente quien, bool puedeCambiar, bool corto)
    {
        salida.WriteLine();
        salida.WriteLine($" Turno de {quien.Nombre.ToUpperInvariant()} · {NombreArma(quien.Arma)} · {Donde(quien)} · CM {Cm(estado.CmActual)} / {Cm(estado.CmMax)}");

        if (corto)
        {
            salida.WriteLine("   Ya empezó la ofensiva: solo quedan básicos, QWER o terminar.");
            salida.WriteLine("   1 Básicos              3 Habilidad QWER       0 Terminar el turno");
            salida.WriteLine("   s Salir de la partida");
            return;
        }

        string esconder = quien.Zona == Zona.Escondite ? "Salir del escondite" : "Esconderse";
        salida.WriteLine("   1 Básicos              6 Carga Musical");
        salida.WriteLine("   2 Moverse              7 Objeto");
        salida.WriteLine($"   3 Habilidad QWER       8 {esconder}");
        salida.WriteLine("   4 Defender             9 Swap con el banquillo");
        salida.WriteLine("   5 Huir                 0 Terminar el turno");
        salida.WriteLine(puedeCambiar ? "   v Que abra otro        s Salir de la partida" : "   s Salir de la partida");
    }

    // ------------------------------------------------------- submenus
    // Todos aceptan v (o una linea vacia) para volver al menu.

    private Accion? Basicos(Combatiente quien)
    {
        var rivales = estado.Combatientes
            .Where(c => c.Bando != quien.Bando && c.Zona == Zona.Front && (!c.EnKo || c.Rematable))
            .ToList();
        if (rivales.Count == 0)
        {
            salida.WriteLine("   No hay ningún rival al alcance.");
            return null;
        }
        if (rivales.Count == 1) return new AtaqueBasico { ActorId = quien.Id, ObjetivoId = rivales[0].Id };

        salida.WriteLine("   ¿A quién?   (v vuelve)");
        for (int i = 0; i < rivales.Count; i++)
            salida.WriteLine($"     {i + 1}  {rivales[i].Nombre}");
        return Elegir(rivales.Count) is int n
            ? new AtaqueBasico { ActorId = quien.Id, ObjetivoId = rivales[n - 1].Id }
            : null;
    }

    private Accion? Moverse(Combatiente quien)
    {
        salida.WriteLine("   ¿A qué casilla?   1 defensiva   2 neutral   3 ofensiva   (v vuelve)");
        return Elegir(3) is int n
            ? new Moverse { ActorId = quien.Id, Destino = (Casilla)(n - 1) }
            : null;
    }

    private Accion? Habilidad(Combatiente quien)
    {
        string costes = string.Join("   ", "qwer".Select(t => $"{t} {Cm(_m.CosteDeTecla(t))}"));
        salida.WriteLine($"   ¿Qué tecla? Cuestan   {costes}   (tienes {Cm(estado.CmActual)} CM · v vuelve)");
        salida.WriteLine("   Ojo: aún no tienen efecto, solo gastan CM.");

        while (true)
        {
            var txt = Leer();
            if (txt is null or "" or "v") return null;
            if (txt is "q" or "w" or "e" or "r") return new UsarHabilidad { ActorId = quien.Id, Tecla = txt[0] };
            salida.WriteLine("   Escribe q, w, e o r, o v para volver al menú.");
        }
    }

    private Accion? Carga(Combatiente quien)
    {
        salida.WriteLine("   ¿En qué sección de la canción cae?   1 calma ×0.5   2 base ×1.0   3 estribillo ×1.8   (v vuelve)");
        return Elegir(3) switch
        {
            1 => new CargarMusica { ActorId = quien.Id, MultiplicadorSeccion = 0.5 },
            2 => new CargarMusica { ActorId = quien.Id, MultiplicadorSeccion = 1.0 },
            3 => new CargarMusica { ActorId = quien.Id, MultiplicadorSeccion = 1.8 },
            _ => null
        };
    }

    private Accion? Objeto(Combatiente quien)
    {
        salida.WriteLine($"   ¿Qué objeto?   1 comida ({EstadoGratis(true)})   2 consumible ({EstadoGratis(false)})   3 gadget (siempre gasta el turno)   (v vuelve)");
        return Elegir(3) switch
        {
            1 => new UsarObjeto { ActorId = quien.Id, ObjetoId = "pan", EsComida = true },
            2 => new UsarObjeto { ActorId = quien.Id, ObjetoId = "vendaje" },
            3 => new UsarObjeto { ActorId = quien.Id, ObjetoId = "artilugio", EsGadget = true },
            _ => null
        };
    }

    private Accion? Cambio(Combatiente quien)
    {
        var banquillo = estado.Del(quien.Bando).Where(c => c.Zona == Zona.Banquillo).ToList();
        if (banquillo.Count == 0)
        {
            salida.WriteLine("   No queda nadie en el banquillo.");
            return null;
        }

        salida.WriteLine(quien.Zona == Zona.Front
            ? $"   ¿Quién entra? Ocupa la casilla {Donde(quien)} de {quien.Nombre}, y no actúa hasta el turno que viene.   (v vuelve)"
            : "   ¿Quién entra?   (v vuelve)");
        for (int i = 0; i < banquillo.Count; i++)
            salida.WriteLine($"     {i + 1}  {banquillo[i].Nombre} ({NombreArma(banquillo[i].Arma)}, {Vida(banquillo[i])})");
        return Elegir(banquillo.Count) is int n
            ? new Swap { ActorId = quien.Id, EntranteId = banquillo[n - 1].Id }
            : null;
    }

    // ------------------------------------------------------- resultados

    private void Mostrar(Combatiente quien, Accion accion, ResultadoAccion r, double cmAntes)
    {
        if (!r.Permitida)
        {
            salida.WriteLine($"   ✗ No se puede: {r.Motivo}");
            return;
        }

        _cicloEmpezado = true;
        Simulador.Anotar(_registro, r);
        double alaBarra = estado.CmActual - cmAntes;
        if (alaBarra > Tolerancia) _cmALaBarra[quien.Id] += alaBarra;
        if (accion is AtaqueBasico or UsarHabilidad) _enOfensiva.Add(quien.Id);

        switch (accion)
        {
            case AtaqueBasico b:
                var objetivo = estado.Buscar(b.ObjetivoId)!;
                salida.WriteLine($"   {quien.Nombre} lanza {r.Impactos.Count} golpe{(r.Impactos.Count == 1 ? "" : "s")} a {objetivo.Nombre}:");

                // Cada golpe se ensena redondeado, y el total suma lo que se ve.
                double total = 0;
                for (int i = 0; i < r.Impactos.Count; i++)
                {
                    var g = r.Impactos[i];
                    double dano = Math.Round(g.DanoAplicado + g.AbsorbidoPorEscudo);
                    if (g.Acerto) total += dano;
                    string linea = !g.Acerto ? "falla"
                        : g.Remato ? "acierta · lo REMATA"
                        : $"acierta {dano,6:F0}{(g.Critico ? "  ¡crítico!" : "")}{(g.ProvocoKo ? "  ¡K.O.!" : "")}   +{Cm(g.CmAtacante)} CM";
                    salida.WriteLine($"     {i + 1,2}  {linea}");
                }

                double generada = r.Impactos.Sum(g => g.CmAtacante);
                int aciertos = r.Impactos.Count(g => g.Acerto);
                salida.WriteLine($"   {aciertos} de {r.Impactos.Count} · {total:F0} de daño · +{Cm(generada)} CM");

                double perdida = generada - Math.Max(0, alaBarra);
                if (perdida > Tolerancia)
                {
                    _cmPerdida += perdida;
                    salida.WriteLine($"   La barra está llena: {Cm(perdida)} CM no caben y se pierden.");
                }
                break;

            case UsarHabilidad h:
                salida.WriteLine($"   Usa {char.ToUpperInvariant(h.Tecla)}: gasta {Cm(cmAntes - estado.CmActual)} CM. (Sin efecto todavía: los kits no tienen contenido.)");
                break;

            case CargarMusica:
                bool encendido = r.Cambios.Any(c => c.Tipo == "clasical");
                salida.WriteLine($"   Carga Musical: la barra pasa de {Cm(cmAntes)} a {Cm(estado.CmActual)}."
                    + (encendido ? "  ✦ ¡CLASICAL encendido! Toda la CM que ganéis vale ×1.5 hasta vuestro próximo turno." : ""));
                break;

            case UsarObjeto o:
                string que = o.EsGadget ? "un gadget" : o.EsComida ? "la comida" : "el consumible";
                if (o.EsGadget) salida.WriteLine($"   Usa {que}: gasta el turno, como siempre.");
                else if (r.CierraTurno) salida.WriteLine($"   Usa {que}, pero no era gratis: le cuesta el turno.");
                else salida.WriteLine($"   Usa {que} gratis.");
                if (r.Cambios.Any(c => c.Tipo == "compromiso"))
                    salida.WriteLine("   Desde la defensiva no arranca la espera… pero queda anclado ahí este turno.");
                break;

            case Moverse mv:
                salida.WriteLine($"   Se mueve a la casilla {mv.Destino.ToString().ToLowerInvariant()}.");
                break;

            case Defender:
                salida.WriteLine("   Se pone en guardia: +50% de ARM y RM contra el primer golpe que reciba.");
                break;

            case Esconderse:
                salida.WriteLine("   Se esconde: el rival ya no puede tocarle.");
                break;

            case SalirDelEscondite:
                salida.WriteLine("   Sale del escondite, a la casilla defensiva. Para pegar, tendrá que avanzar.");
                break;

            case Swap s:
                _recienEntrados.Add(s.EntranteId);
                salida.WriteLine($"   {quien.Nombre} se va al banquillo y entra {estado.Buscar(s.EntranteId)!.Nombre}, que actuará el turno que viene.");
                break;

            case Huir:
                bool logrado = r.Cambios.Any(c => c.Tipo == "huida" && c.Detalle == "lograda");
                salida.WriteLine(logrado ? "   ¡Huís del combate!" : "   Intentáis huir… y no hay forma. El turno se pierde.");
                break;

            case Pasar:
                salida.WriteLine($"   {quien.Nombre} termina su turno.");
                break;
        }

        if (r.CierraTurno && accion is not Pasar && _m.Desenlace == Desenlace.EnCurso)
            salida.WriteLine($"   (Esto cierra el turno de {quien.Nombre}.)");
    }

    private void TurnoRival()
    {
        salida.WriteLine();
        salida.WriteLine(" Turno del rival: el muñeco aguanta. No ataca.");
        _m.CerrarTurnoDeBando();

        _recienEntrados.Clear();
        _enOfensiva.Clear();
        _cicloEmpezado = false;
    }

    // ------------------------------------------------------- pantallas

    private void Presentacion()
    {
        salida.WriteLine();
        salida.WriteLine(" E.N.M.A. · combate de prueba contra el muñeco");
        salida.WriteLine($" Semilla {semilla}: con la misma semilla y las mismas respuestas, la misma partida.");
        salida.WriteLine();
        salida.WriteLine(" El muñeco tiene un millón de vida y no ataca nunca. Prueba armas, golpes,");
        salida.WriteLine(" casillas, CM y Clasical: en la ofensiva aciertas más, en la defensiva no puedes");
        salida.WriteLine(" pegar. Las habilidades QWER todavía no tienen efecto, y el ritmo del básico aún");
        salida.WriteLine(" no está conectado: todos los golpes cuentan como fuera de tempo.");
        salida.WriteLine(" En los submenús, v vuelve atrás. Escribe s para salir de la partida.");
    }

    private void Tablero()
    {
        var pendientes = _m.PendientesDeActuar().ToList();
        var ids = pendientes.Select(c => c.Id).ToHashSet();
        var enTurno = _m.ActorDeTurno;

        salida.WriteLine();
        salida.WriteLine($" ══ Ciclo {estado.Ciclo + 1} · tu turno ════════════════════════════════════════");
        salida.WriteLine($"  CM {Barra(estado.CmPct)}  {Cm(estado.CmActual)} / {Cm(estado.CmMax)}"
            + (estado.ClasicalActivo ? "   ✦ CLASICAL" : ""));
        salida.WriteLine($"  Comida gratis: {EstadoGratis(true)} · Consumible gratis: {EstadoGratis(false)}");
        salida.WriteLine(enTurno is null
            ? "  Orden: eliges quién abre; después sigue de derecha a izquierda (de la línea más alta a la más baja) y da la vuelta."
            : "  Orden: " + string.Join(" → ", pendientes.Select(c => c.Id == enTurno.Id ? $"▶ {c.Nombre}" : c.Nombre)));
        salida.WriteLine();

        var tuyos = estado.Combatientes.Where(c => c.Bando == Bando.Aliado).ToList();
        foreach (var c in tuyos.Where(c => c.Zona != Zona.Banquillo).OrderByDescending(c => c.Linea))
        {
            string marca = enTurno?.Id == c.Id ? "▶" : " ";
            string nota = c.EnKo ? ""
                : _recienEntrados.Contains(c.Id) ? "actúa el turno que viene"
                : ids.Contains(c.Id) ? ""
                : "ya actuó";
            salida.WriteLine($"  {marca} L{c.Linea} {c.Nombre,-9} {NombreArma(c.Arma),-7} {Casillas(c),-11} {Vida(c),-13} {nota}");
        }
        foreach (var c in tuyos.Where(c => c.Zona == Zona.Banquillo))
            salida.WriteLine($"       {c.Nombre,-9} {NombreArma(c.Arma),-7} {"banquillo",-11} {Vida(c),-13}");

        salida.WriteLine();
        foreach (var r in estado.Combatientes.Where(c => c.Bando == Bando.Enemigo))
            salida.WriteLine($"  {r.Nombre}: {r.HpMax - r.Hp:F0} de daño recibido · le quedan {r.Hp:F0}");
    }

    private void Final()
    {
        salida.WriteLine();
        string como = _m.Desenlace switch
        {
            Desenlace.VictoriaAliada => "¡El muñeco ha caído!",
            Desenlace.Huida => "Habéis huido.",
            Desenlace.DerrotaAliada => "Derrota.",
            _ => "Fin de la prueba."
        };

        // Los ciclos acabados, mas el que esta en curso si alguien llego a hacer algo en el.
        int ciclos = estado.Ciclo + (_cicloEmpezado ? 1 : 0);
        salida.WriteLine($" ══ {como} {ciclos} ciclo{(ciclos == 1 ? "" : "s")} jugado{(ciclos == 1 ? "" : "s")} ════════════════════════");

        var activos = estado.Combatientes
            .Where(c => c.Bando == Bando.Aliado && (_registro[c.Id].Golpes > 0 || _cmALaBarra[c.Id] > Tolerancia))
            .ToList();
        if (activos.Count == 0)
        {
            salida.WriteLine("  Nadie llegó a golpear ni a cargar.");
            salida.WriteLine();
            return;
        }

        salida.WriteLine($"  {"",-10} {"golpes",7} {"acierto",8} {"daño",8} {"por ciclo",10} {"CM",7}");
        foreach (var c in activos)
        {
            var r = _registro[c.Id];
            string acierto = r.Golpes == 0 ? "—" : $"{100.0 * r.Aciertos / r.Golpes:F0}%";
            string porCiclo = ciclos == 0 ? "—" : $"{r.Dano / ciclos:F0}";
            salida.WriteLine($"  {c.Nombre,-10} {r.Golpes,7} {acierto,8} {r.Dano,8:F0} {porCiclo,10} {Cm(_cmALaBarra[c.Id]),7}");
        }
        salida.WriteLine("  CM: lo que cada uno metió en la barra, con golpes y con Carga Musical.");
        if (_cmPerdida > Tolerancia)
            salida.WriteLine($"  Con la barra llena se perdieron {Cm(_cmPerdida)} CM de golpes.");
        salida.WriteLine();
    }

    // ------------------------------------------------------- utilidades

    /// <summary>Lee una respuesta. null si se acaba la entrada o se pide salir (s).</summary>
    private string? Leer()
    {
        salida.Write("  > ");
        var linea = entrada.ReadLine();
        if (linea is null)
        {
            salida.WriteLine();
            _salir = true;
            return null;
        }

        // Un BOM al principio de un guion (PowerShell lo mete) no es parte de la respuesta.
        var txt = linea.Replace("﻿", "").Trim().ToLowerInvariant();
        if (eco) salida.WriteLine(txt);

        if (txt == "s")
        {
            _salir = true;
            return null;
        }
        return txt;
    }

    /// <summary>Un numero de 1 a <paramref name="maximo"/>. null = volver al menu, o salir.</summary>
    private int? Elegir(int maximo)
    {
        while (true)
        {
            var txt = Leer();
            if (txt is null or "" or "v") return null;
            if (Numero(txt, maximo) is int n) return n;
            salida.WriteLine($"   Escribe un número del 1 al {maximo}, o v para volver al menú.");
        }
    }

    private static int? Numero(string? txt, int maximo)
        => int.TryParse(txt, out int n) && n >= 1 && n <= maximo ? n : null;

    /// <summary>La CM va en octavos y dieciseisavos: hasta tres decimales, sin ceros de sobra.</summary>
    private static string Cm(double x)
        => (Math.Abs(x) < Tolerancia ? 0.0 : x).ToString("0.###", CultureInfo.InvariantCulture);

    private string EstadoGratis(bool comida)
    {
        var (usado, espera) = _m.ObjetoGratis(Bando.Aliado, comida);
        return usado ? "ya usado este turno"
            : espera > 0 ? $"vuelve en {espera} turno{(espera == 1 ? "" : "s")}"
            : "listo";
    }

    private static string Barra(double pct)
    {
        int llenos = (int)Math.Round(Math.Clamp(pct, 0, 1) * 16);
        return "[" + new string('█', llenos) + new string('░', 16 - llenos) + "]";
    }

    private static string Casillas(Combatiente c)
    {
        if (c.EnKo) return c.Muerto ? "muerto" : "K.O.";
        if (c.Zona == Zona.Escondite) return "escondido";
        return "[" + string.Join(" ", new[] { Casilla.Defensiva, Casilla.Neutral, Casilla.Ofensiva }
            .Select(x => x == c.Posicion ? "●" : "·")) + "]";
    }

    private static string Donde(Combatiente c)
        => c.Zona == Zona.Escondite ? "escondido" : c.Posicion.ToString().ToLowerInvariant();

    private static string Vida(Combatiente c) => $"{c.Hp:F0} / {c.HpMax:F0}";

    private static string NombreArma(ClaseArma a) => a switch
    {
        ClaseArma.Ligera => "ligera",
        ClaseArma.Media => "media",
        ClaseArma.Pesada => "pesada",
        _ => a.ToString().ToLowerInvariant()
    };
}

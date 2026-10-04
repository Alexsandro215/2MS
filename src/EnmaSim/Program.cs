// SIMULADOR DE COMBATES
//
//   dotnet run --project src/EnmaSim              mil combates y su resumen
//   dotnet run --project src/EnmaSim -- 5000      cinco mil
//   dotnet run --project src/EnmaSim -- uno 7     un combate golpe a golpe, semilla 7
//   dotnet run --project src/EnmaSim -- jugar     juegas tu, contra el muñeco
//   dotnet run --project src/EnmaSim -- jugar 7   lo mismo, con la semilla 7

using Enma.Core;
using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;
using Enma.Core.Simulacion;
using Enma.Sim;

// Los aliados no rematan: a un enemigo en K.O. nadie lo rescata, asi que
// rematarlo seria gastar golpes. Los enemigos no rescatan: no tienen
// banquillo, y esconder a un caido suyo no les devuelve a nadie.
var aliados = new IABasica { Remata = false };
var enemigos = new IABasica { Rescata = false };

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

string modo = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "";

// La semilla, si viene, es el segundo argumento. Nada mas detras.
int? semillaPedida = null;
if (modo is "jugar" or "uno" && args.Length > 1)
{
    if (args.Length > 2 || !int.TryParse(args[1], out int s))
    {
        Fallo($"Después de «{args[0]}» solo va una semilla, un número entero; no «{string.Join(' ', args.Skip(1))}».");
        return;
    }
    semillaPedida = s;
}

switch (modo)
{
    case "jugar":
    {
        int semilla = semillaPedida ?? Environment.TickCount & 0xFFFF;

        // Con un guion por tuberia, el lector detecta el BOM que mete
        // PowerShell, y cada respuesta se repite para que se pueda seguir.
        bool guion = Console.IsInputRedirected;
        TextReader entrada = guion
            ? new StreamReader(Console.OpenStandardInput(), new System.Text.UTF8Encoding(false), detectEncodingFromByteOrderMarks: true)
            : Console.In;
        new PartidaConsola(Encuentro.ContraMuneco(semilla), semilla, entrada, Console.Out, eco: guion).Jugar();
        break;
    }

    case "uno":
    {
        int semilla = semillaPedida ?? 1;
        Console.WriteLine($"Combate de prueba · semilla {semilla}\n");
        Simulador.Jugar(Encuentro.DePrueba(semilla), aliados, enemigos, bitacora: Console.Out);
        break;
    }

    case "":
        Lote(1000);
        break;

    case "ayuda" or "-h" or "--help" or "/?":
        Uso();
        break;

    default:
        if (args.Length == 1 && int.TryParse(modo, out int combates) && combates > 0)
            Lote(combates);
        else
            Fallo($"No entiendo «{string.Join(' ', args)}».");
        break;
}

void Lote(int combates)
{
    var lote = Simulador.Lote(Encuentro.DePrueba, combates, aliados, enemigos);

    Console.WriteLine($"{lote.Combates} combates · encuentro de prueba\n");
    Console.WriteLine($"  Victorias   {lote.Victorias,6}   {lote.PorcentajeVictorias,5:F1}%");
    Console.WriteLine($"  Derrotas    {lote.Derrotas,6}");
    if (lote.Huidas > 0) Console.WriteLine($"  Huidas      {lote.Huidas,6}");
    if (lote.SinResolver > 0) Console.WriteLine($"  Sin acabar  {lote.SinResolver,6}   (tope de ciclos)");
    Console.WriteLine($"  Ciclos      {lote.CiclosMedios,6:F1}   de media");
    if (lote.AccionesRechazadas > 0)
        Console.WriteLine($"  ⚠️ {lote.AccionesRechazadas} acciones rechazadas: la IA propone cosas ilegales");

    Console.WriteLine();
    Console.WriteLine("  Por combate, de media:");
    Console.WriteLine($"  {"",-10} {"daño",8} {"CM",7} {"acierto",8} {"cae",6} {"muere",6}");
    foreach (var (id, r) in lote.PorCombatiente)
        Console.WriteLine($"  {id,-10} {r.Dano,8:F0} {r.CmGenerada,7:F2} {r.Acierto,7:F0}% {r.Cayo,5:F0}% {r.Murio,5:F0}%");

    Console.WriteLine();
    Console.WriteLine("  ⚠️ Stats inventadas para probar el bucle: no son de ningun personaje.");
}

void Uso()
{
    Console.WriteLine("""
        Uso:
          EnmaSim                 mil combates simulados y su resumen
          EnmaSim 5000            los que digas
          EnmaSim uno [semilla]   un combate simulado, golpe a golpe
          EnmaSim jugar [semilla] juegas tú, contra el muñeco
        """);
}

void Fallo(string motivo)
{
    Console.Error.WriteLine(motivo);
    Uso();
    Environment.ExitCode = 1;
}

/// <summary>
/// Un encuentro de prueba: tres aliados, uno por clase de arma, uno mas en
/// el banquillo, contra tres enemigos. Un escondite.
///
/// ⚠️ Los numeros son inventados. Sirven para ver que el bucle funciona, no
/// para sacar conclusiones de balance.
/// </summary>
static class Encuentro
{
    public static EstadoCombate DePrueba(int semilla)
    {
        var e = new EstadoCombate { Escondites = 1, Rng = new AleatorioSemilla(semilla) };
        e.Combatientes.AddRange(Equipo());
        e.Combatientes.AddRange([
            Pj("bruto",    Bando.Enemigo, ClaseArma.Pesada, 2, 1800, 120, 70,  95, 30),
            Pj("pillo",    Bando.Enemigo, ClaseArma.Ligera, 1, 1000,  70, 40, 110, 70),
            Pj("guardia",  Bando.Enemigo, ClaseArma.Media,  0, 1400,  90, 90, 100, 40),
        ]);
        e.RecalcularCm();
        return e;
    }

    /// <summary>
    /// El mismo equipo contra un muñeco de entrenamiento: un millon de vida,
    /// y no ataca nunca. Para probar armas, golpes, acierto, CM y Clasical.
    /// </summary>
    public static EstadoCombate ContraMuneco(int semilla)
    {
        var e = new EstadoCombate { Escondites = 1, Rng = new AleatorioSemilla(semilla) };
        e.Combatientes.AddRange(Equipo());
        e.Combatientes.Add(Pj("muneco", Bando.Enemigo, ClaseArma.Media, 0, 1_000_000, 0, 40, 0, 40, nombre: "Muñeco"));
        e.RecalcularCm();
        return e;
    }

    private static Combatiente[] Equipo() =>
    [
        Pj("ligera",   Bando.Aliado, ClaseArma.Ligera, 2,  900,  80, 40, 120, 60),
        Pj("media",    Bando.Aliado, ClaseArma.Media,  1, 1100, 100, 60, 100, 40),
        Pj("pesada",   Bando.Aliado, ClaseArma.Pesada, 0, 1300, 140, 80,  90, 20),
        Pj("suplente", Bando.Aliado, ClaseArma.Media,  0, 1000,  90, 50, 100, 40, Zona.Banquillo),
    ];

    private static Combatiente Pj(string id, Bando bando, ClaseArma arma, int linea,
        double hp, double ad, double arm, double press, double eva,
        Zona zona = Zona.Front, string? nombre = null)
    {
        var c = new Combatiente
        {
            Id = id, Nombre = nombre ?? id, Bando = bando, Arma = arma, Linea = linea,
            Nivel = Constantes.NivelMax, Zona = zona
        };
        foreach (Stat s in Enum.GetValues<Stat>()) c.MaxAspirable[s] = 0;
        c.MaxAspirable[Stat.Hp] = hp;
        c.MaxAspirable[Stat.Ad] = ad;
        c.MaxAspirable[Stat.Arm] = arm;
        c.MaxAspirable[Stat.Press] = press;
        c.MaxAspirable[Stat.Eva] = eva;
        c.MaxAspirable[Stat.Crit] = 10;
        c.Hp = c.HpMax;
        return c;
    }
}

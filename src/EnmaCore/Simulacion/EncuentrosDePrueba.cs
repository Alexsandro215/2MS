using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Musica;

namespace Enma.Core.Simulacion;

/// <summary>
/// Encuentros de PRUEBA, los mismos para el simulador, la consola y la
/// ventana de combate: tres aliados, uno por clase de arma, y uno mas en el
/// banquillo. Un escondite.
///
/// ⚠️ Los numeros son inventados. Sirven para ver que el bucle funciona, no
/// para sacar conclusiones de balance, y no son de ningun personaje. Cuando
/// haya contenido de verdad vendra de datos, no de aqui.
/// </summary>
public static class EncuentrosDePrueba
{
    /// <summary>El equipo contra tres enemigos, uno por clase de arma.</summary>
    public static EstadoCombate TresContraTres(int semilla)
    {
        var e = Nuevo(semilla);
        e.Combatientes.AddRange([
            Pj("bruto",   Bando.Enemigo, ClaseArma.Pesada, 2, 1800, 120, 70,  95, 30),
            Pj("pillo",   Bando.Enemigo, ClaseArma.Ligera, 1, 1000,  70, 40, 110, 70),
            Pj("guardia", Bando.Enemigo, ClaseArma.Media,  0, 1400,  90, 90, 100, 40),
        ]);
        e.RecalcularCm();
        return e;
    }

    /// <summary>
    /// El equipo contra un muñeco de entrenamiento: un millon de vida, y no
    /// ataca nunca. Para probar armas, golpes, acierto, casillas, CM y Clasical.
    /// </summary>
    public static EstadoCombate ContraMuneco(int semilla)
    {
        var e = Nuevo(semilla);
        e.Combatientes.Add(Pj("muneco", Bando.Enemigo, ClaseArma.Media, 0, 1_000_000, 0, 40, 0, 40, nombre: "Muñeco"));

        // La MEDIA lleva el kit de Kronwell para que QWER haga algo contra el
        // muneco. Es el unico encuentro donde se cuelga un kit a mano: el lote
        // de combates sigue sin kits, para no mezclar lo que mide.
        var media = e.Buscar("media")!;
        media.MaxAspirable[Stat.Ap] = 120;   // ⚠️ provisional, como el kit
        e.ReglasActivas.AddRange(Reglas.Kits.Kronwell("media"));

        // Y la PESADA lleva Halltus, que es lo contrario: ninguna de sus teclas
        // pega, todas van a los compañeros. Con los dos a la vez se ve ademas
        // que un kit no dispara el del otro.
        e.ReglasActivas.AddRange(Reglas.Kits.Halltus("pesada"));

        e.RecalcularCm();
        return e;
    }

    private static EstadoCombate Nuevo(int semilla)
    {
        var e = new EstadoCombate { Escondites = 1, Rng = new AleatorioSemilla(semilla) };
        e.Combatientes.AddRange([
            Pj("ligera",   Bando.Aliado, ClaseArma.Ligera, 2,  900,  80, 40, 120, 60),
            Pj("media",    Bando.Aliado, ClaseArma.Media,  1, 1100, 100, 60, 100, 40),
            Pj("pesada",   Bando.Aliado, ClaseArma.Pesada, 0, 1300, 140, 80,  90, 20),
            Pj("suplente", Bando.Aliado, ClaseArma.Media,  0, 1000,  90, 50, 100, 40, Zona.Banquillo),
        ]);
        return e;
    }

    private static Combatiente Pj(string id, Bando bando, ClaseArma arma, int linea,
        double hp, double ad, double arm, double press, double eva,
        Zona zona = Zona.Front, string? nombre = null, double ap = 0)
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
        c.MaxAspirable[Stat.Ap] = ap;
        c.MaxAspirable[Stat.Crit] = 10;
        c.Hp = c.HpMax;
        return c;
    }
}

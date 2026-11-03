using Enma.Core.Combate;
using Enma.Core.Estado;
using Enma.Core.Stats;

namespace Enma.Core.Reglas;

/// <summary>
/// Los kits de docs/estilos.md, como DATOS.
///
/// Aqui no hay codigo de combate: cada tecla es una fila del mismo esquema
/// —fuente, disparador, condicion, efecto— que las pasivas, los emblemas y el
/// terreno. Si un kit no cabe, no se añade un `if` a este fichero: se añade un
/// verbo al catalogo de <see cref="Efecto"/>. Ver arquitectura.md  1.
///
/// ⚠️ LOS NUMEROS SON PROVISIONALES. docs/estilos.md deja los valores como
/// `X%` a proposito, porque el escalado vive en las tablas por nivel y esas
/// tablas aun no estan. Lo que esta fijado aqui es la FORMA de cada habilidad
/// —que la E consuma cargas, que la R cobre desgaste—, no su potencia.
/// </summary>
public static class Kits
{
    // ---- Kronwell: valores provisionales, en un solo sitio ----
    private const double KronwellQAp      = 0.60;   // ⚠️ coef AP de la bola
    private const double KronwellCargaAp  = 0.40;   // ⚠️ coef AP por carga
    private const double KronwellCargas   = 2.0;    // ⚠️ cargas que da Q y W
    private const double KronwellConcentr = 0.30;   // ⚠️ +30% a la siguiente
    private const double KronwellDesgaste = 0.70;   // este SI esta en el doc

    /// <summary>
    /// Kronwell — *AP · Explosivo · Auto-desintegracion*. docs/estilos.md  4.
    ///
    ///   Q  bola explosiva con E.E. de X% AP, y da cargas
    ///   W  CONCENTRACION: la siguiente habilidad sube X%, y da cargas
    ///   E  estocada que explota consumiendo las cargas: n x X% AP
    ///   R  lo mismo a TODOS los enemigos, y te cobra 70% de desgaste
    ///
    /// La identidad del kit es que Q y W ACUMULAN y E y R GASTAN: no son cuatro
    /// botones, son un combo con dos mitades.
    /// </summary>
    public static List<Regla> Kronwell(string portadorId)
    {
        // La CONCENTRACION es un contador, no un flag, porque tiene que
        // multiplicar un coeficiente. Como el coeficiente se evalua al PEDIR el
        // golpe, gastarla en el mismo efecto no se lo quita al golpe que ya
        // salio: por eso Estallar va siempre antes del ConsumirContador.
        Expr conConcentracion(Expr coef) =>
            Ex.Por(coef, Ex.Mas(Ex.K(1.0), Ex.Cargas("concentracion_kronwell")));

        Efecto gastarCargas = new Varios(
            new ConsumirContador("cargas_kronwell"),
            new ConsumirContador("concentracion_kronwell"));

        Expr porCargas = Ex.Por(Ex.K(KronwellCargaAp), Ex.Cargas("cargas_kronwell"));

        Regla tecla(char t, string nota, Efecto efecto) => new()
        {
            Id = $"kronwell.{char.ToLowerInvariant(t)}",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = portadorId,
            Condicion = Cond.Tecla(t),
            Etiqueta = $"Kronwell {t}: {nota}",
            Efecto = efecto
        };

        return new List<Regla>
        {
            tecla('Q', "bola explosiva + cargas", new Varios(
                new Estallar(TipoDano.Magico, conConcentracion(Ex.K(KronwellQAp)),
                    Elemento: "Fuego", ProbabilidadElemento: 1.0),
                new ConsumirContador("concentracion_kronwell"),
                new DarContador("cargas_kronwell", Ex.K(KronwellCargas)))),

            tecla('W', "concentracion + cargas", new Varios(
                new DarContador("concentracion_kronwell", Ex.K(KronwellConcentr)),
                new DarContador("cargas_kronwell", Ex.K(KronwellCargas)))),

            tecla('E', "estocada: n x AP, consume", new Varios(
                new Estallar(TipoDano.Magico, conConcentracion(porCargas),
                    Elemento: "Fuego", ProbabilidadElemento: 1.0),
                gastarCargas)),

            // La R es la auto-desintegracion: pega a todo el frente rival y se
            // cobra el 70% de desgaste sobre TODAS tus stats. Es el unico sitio
            // del kit donde el coste no es CM.
            tecla('R', "explosion a todos + 70% desgaste", new Varios(
                new Estallar(TipoDano.Magico, conConcentracion(porCargas),
                    A: new Selector(Ambito.Enemigos, new Filtro { EnKo = false }),
                    Elemento: "Fuego", ProbabilidadElemento: 1.0),
                gastarCargas,
                new SumarDesgaste(Ex.K(KronwellDesgaste)))),
        };
    }

    // ---- Halltus: valores provisionales ----
    private const double HalltusAd       = 0.25;   // ⚠️ +AD a los compañeros
    private const double HalltusArmRm    = 0.30;   // ⚠️ +ARM y RM a los de NEUTRAL
    private const double HalltusEvaPorUno = 0.10;  // ⚠️ EVA por compañero limpiado
    private const double HalltusVidaMin  = 0.50;   // ⚠️ vida minima para la inmunidad
    private const int    HalltusCuantos  = 2;      // ⚠️ a cuantos alcanza la R
    private const int    HalltusTurnos   = 2;      // ⚠️ lo que duran los buffs

    /// <summary>
    /// Halltus — *Support · Inmunidad · Aumentos*. docs/estilos.md  3.
    ///
    ///   Q  sube el AD de tus compañeros
    ///   W  les limpia los debuffs, y TU ganas EVA por cada uno que limpiaste
    ///   E  sube ARM y RM de los compañeros que esten en NEUTRAL
    ///   R  inmunidad 1 turno a los que esten en OFENSIVA con vida suficiente
    ///
    /// Es el contrario de Kronwell: ni una sola de sus teclas pega. Todo va a
    /// los compañeros, y tres de las cuatro miran DONDE estan o COMO estan.
    /// </summary>
    public static List<Regla> Halltus(string portadorId)
    {
        var companeros = new Selector(Ambito.Aliados);

        Regla tecla(char t, string nota, Efecto efecto, Condicion? extra = null) => new()
        {
            Id = $"halltus.{char.ToLowerInvariant(t)}",
            Fuente = Fuente.Habilidad,
            Disparador = Disparador.AlUsarHabilidad,
            PortadorId = portadorId,
            Condicion = extra is null ? Cond.Tecla(t) : new Y(Cond.Tecla(t), extra),
            Etiqueta = $"Halltus {t}: {nota}",
            Efecto = efecto
        };

        return new List<Regla>
        {
            tecla('Q', "+AD a los compañeros",
                new ConcederStat(Stat.Ad, Capa.PctAdd, Ex.K(HalltusAd), HalltusTurnos,
                    companeros, "halltus-q")),

            // El orden importa y no es cosmetico: el recuento de a quien se le
            // quito algo se congela al conceder la EVA, y la limpieza va
            // DESPUES. Al reves, siempre contaria cero.
            tecla('W', "limpia debuffs, +EVA por cada uno", new Varios(
                new ConcederStat(Stat.Eva, Capa.PctAdd,
                    Ex.Por(Ex.K(HalltusEvaPorUno),
                        Ex.Contar(new Selector(Ambito.Aliados, new Filtro { ConDebuffs = true }))),
                    HalltusTurnos, Selector.Yo, "halltus-w"),
                new LimpiarDebuffs(A: companeros))),

            tecla('E', "+ARM y RM a los de NEUTRAL", new Varios(
                new ConcederStat(Stat.Arm, Capa.PctAdd, Ex.K(HalltusArmRm), HalltusTurnos,
                    new Selector(Ambito.Aliados, new Filtro { EnCasilla = Casilla.Neutral }), "halltus-e"),
                new ConcederStat(Stat.Rm, Capa.PctAdd, Ex.K(HalltusArmRm), HalltusTurnos,
                    new Selector(Ambito.Aliados, new Filtro { EnCasilla = Casilla.Neutral }), "halltus-e"))),

            // "Inmunidad durante 1 turno a X compañeros que esten en OFENSIVA y
            // tengan al menos X% de vida": tres condiciones, y las tres caben
            // en el selector. Protege a quien se expuso, no a quien se escondio.
            tecla('R', "inmunidad a los expuestos",
                new PonerFlag("invulnerable", 1,
                    new Selector(Ambito.Aliados,
                        new Filtro { EnCasilla = Casilla.Ofensiva, HpPctMinimo = HalltusVidaMin, EnKo = false },
                        Orden.MenorHpPct, Limite: HalltusCuantos))),
        };
    }
}

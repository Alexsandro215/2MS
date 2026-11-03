# E.N.M.A — Música, Cadencia, Tempo y modo CLASICAL

> La capa de identidad del proyecto. La música no es ambientación: **es el reloj, el recurso y el bonus**.

---

## 1. Tempo

**Tempo = la cantidad de CM que se obtiene por ataque básico.**

### Base por arma — son figuras rítmicas

| Arma | CM base | Figura | Básicos base | Golpes para 1 CM |
|---|---|---|---|---|
| **LIGERA** | **1/8** | corchea | **5** | 8 |
| **MEDIA** | **1/4** | negra | **3** | 4 |
| **PESADA** | **1/2** | blanca | **1** | 2 |

> ### ✅ La figura gobierna la CM, no el ritmo de ataque
>
> El 1/8 – 1/4 – 1/2 no es arbitrario: sale de que **una corchea dura la mitad que una negra, y una negra la mitad que una blanca**. Una nota corta vale menos CM que una larga. Esa proporción es cierta siempre.
>
> Lo que la figura **no** hace es dictar a qué velocidad pegas. **El número de golpes lo fijan `base` y `Attack Speed`**, que son estadísticas, no música. Siete ataques pueden caber en un compás o estirarse por dos, según cómo vaya la canción — y eso está bien.
>
> ⚠️ *Corrige una lectura anterior de esta tabla, que la trataba como si la figura marcase el ritmo de ataque. No lo marca: marca cuánto vale cada impacto en recurso.*

> ⚠️ Los básicos base **5 / 3 / 1** sustituyen a la estimación anterior de 7 / 5 / 3-1.

### Redondeo de los golpes

```
golpes = base × (1 + AS)

Se redondea HACIA ARRIBA solo si faltan 0.25 o menos para el entero siguiente.
En cualquier otro caso, se trunca.

    1.75  →  2       (faltan 0.25)
    1.65  →  1       (faltan 0.35)
```

**Golpes reales por nivel de Attack Speed:**

| AS | Ligera (5) | Media (3) | Pesada (1) |
|---|---|---|---|
| 0% | 5 | 3 | 1 |
| 15% | **6** | 3 | 1 |
| 25% | 6 | **4** | 1 |
| 35% | **7** | 4 | 1 |
| 55% | **8** | 4 | 1 |
| 60% (cap) | 8 | **5** | 1 |

**Breakpoints — el AS mínimo para ganar un golpe:**

| Arma | +1 golpe | +2 golpes | +3 golpes |
|---|---|---|---|
| **Ligera** (base 5) | AS 15% | AS 35% | AS 55% |
| **Media** (base 3) | AS 25% | AS 58.3% | *AS 91.7% — fuera del cap* |
| **Pesada** (base 1) | *AS 75% — fuera del cap* | — | — |

*(breakpoints con la base de partida; suben la base y cambian todos)*

> ### ⚠️ La pesada con base 1 no llega nunca a 2 golpes por AS
>
> Para pasar de 1 a 2 necesitaría `1 × (1 + AS) ≥ 1.75`, o sea **AS 75%** — por encima del cap del 60%. **Con base 1, el Attack Speed no le sirve de nada.**
>
> Se resuelve porque **5 / 3 / 1 son valores de PARTIDA, no fijos** (ver abajo): en cuanto la base de la pesada sube a 2, el AS vuelve a valer.

### ⭐ La base de golpes es mejorable

Los **5 / 3 / 1** son el punto de partida, no un techo. La base de golpes de cada arma **se puede mejorar durante la partida**, y a partir de ahí el AS multiplica sobre el nuevo valor.

**Efecto de subir la base (con AS al cap del 60%):**

| Base | Golpes |
|---|---|
| 1 | 1 |
| 2 | **3** |
| 3 | **5** |
| 4 | 6 |
| 5 | **8** |
| 6 | 9 |

**La pesada, según su base:**

| Base | AS 0% | AS 30% | AS 60% |
|---|---|---|---|
| 1 | 1 | 1 | 1 |
| **2** | 2 | 2 | **3** |
| 3 | 3 | 4 | 5 |

> Subir la base de la pesada de 1 a 2 es el salto más grande del juego: **desbloquea el Attack Speed para ese personaje** y triplica sus golpes en late. Antes de esa mejora, invertir en AS es dinero tirado; después, es de lo mejor que puede hacer.
>
> Es un breakpoint de construcción muy fuerte, y conviene que sea consciente: **la pesada tiene un antes y un después.**

### Cuántos golpes son alcanzables de verdad

El número de básicos **no lo fija solo la base del arma**. Depende del **armado completo**: base mejorada, AS, skills, estadísticas, emblemas, gadgets y todo lo que se ha ido mencionando.

> Por eso el ejemplo de STATS MASTER —*"VelocidadAtaque 60% → 10 hits se vuelven 16"*— **no contradice** las bases de partida 5/3/1. Describe un armado desarrollado, no un arma recién sacada.

**Postura de diseño:** subir el AS con un arma **pesada** es difícil y poco eficiente… pero **posible**.

> Y es deliberado que lo sea. Va a haber quien lo intente por gusto, y el sistema no se lo prohíbe: le pide que pague por ello. Es la diferencia entre un build subóptimo y un build imposible — el primero es contenido, el segundo es una pared.
>
> Encaja con el resto del juego: las reglas casi nunca dicen *no*, dicen *cuánto cuesta*.

### Vías para mejorar la base de golpes

| Fuente | Ejemplo |
|---|---|
| **Emblemas** | Al alcanzar X emblemas de un tipo, **todo el equipo gana +1 BA**. |
| **Arma** | **+1 BA** si alguno de tus ataques **critica**. |
| **Arma / condición** | **+1 BA** si tienes **X% de vida**. |

Encaja con el sistema de emblemas ya existente, cuyos breakpoints llegan hasta 9 con patrones variables (1-3-7-9, 2-5-7-9…). Y las versiones condicionadas —al crítico, a un umbral de vida— dan mucha variedad sin añadir sistemas nuevos.

> ### ⚠️ Un "+1 BA" no vale lo mismo para todas las armas
>
> Con AS al cap del 60%:
>
> | Arma | Base | Golpes | Con base +1 | Ganancia |
> |---|---|---|---|---|
> | **Pesada** | 1 → 2 | 1 | **3** | **+200%** |
> | **Media** | 3 → 4 | 5 | 6 | +20% |
> | **Ligera** | 5 → 6 | 8 | 9 | +12.5% |
>
> El mismo bonus **triplica** a la pesada y apenas mueve a la ligera. **Es intencional:** es la vía por la que la pesada desbloquea el Attack Speed.
>
> Consecuencia práctica: las fuentes de "+1 BA" son casi obligatorias en un build de pesada y casi irrelevantes en uno de ligera. Si son escasas, acaban dictando qué armas son viables en cada run.

### ✅ Fórmula final

```
golpes = redondeo( (base + bonos "+1 BA") × (1 + AS) )
```

Los bonos de **"+1 BA" suman a la BASE**, no a la estadística de Attack Speed. Son dos vías separadas que luego se multiplican entre sí.

**Sobre subir AS con arma pesada:** se mantiene tal cual, con su umbral del 75% para el segundo golpe.

> **No hay prohibiciones.** Es un camino malo —quince puntos de AS que no hacen nada antes del umbral— pero está abierto para quien quiera intentarlo. El sistema no impide builds raros: los cobra caros. Es la misma postura que con todo lo demás del juego.

> **Nota de balance:** la media llega a 5 golpes justo en AS 58.3%, apenas por debajo del cap. Es un breakpoint muy ajustado — si el cap bajara al 55%, la media perdería su último golpe por completo.

### ✅ El contador de CM y el bonus rítmico son DOS cosas separadas

```
CONTADOR  (motor de fondo)
    cada X básicos CONECTADOS  →  1 carga de CM
    X por arma: ligera 8 · media 4 · pesada 2
    la stat Cadencia reduce X
    ▶ PERSISTE ENTRE TURNOS — ningún golpe se desperdicia

BONUS DE TEMPO  (sobre el contador)
    básico CONECTADO y en tempo  →  esa CM vale ×2
    ▶ por GOLPE, no por turno
    ▶ sin conectar no hay CM, así que no hay nada que multiplicar

BONUS POR ARMA  (cada una en su capa, por golpe)
    pesada   →  150% del AD al caer en el primer tiempo
    media    →  CM extra al caer en el pulso
    ligera   →  la cadena de melodía → compás extra
```

> ### ⚠️ No hay puerta por turno
>
> Una versión anterior exigía que **todos** los básicos del turno fueran en
> tempo para cobrar un ×1.5 de turno. **Se retira.**
>
> No se sostiene porque **los básicos no tienen por qué encadenarse**: después
> vienen los QWER opcionales, o guardar la CM para el siguiente personaje. Una
> puerta todo-o-nada necesita un bloque cerrado, y el turno no lo es. ¿Dónde
> acabaría? ¿Contaría el hueco en el que te paraste a decidir la skill?
>
> **Por golpe es lo único que se sostiene.** Cada básico se juzga en su propio
> instante contra la capa de su arma.

> ### ⚠️ La CM exige CONECTAR — y de ahí sale la composición
>
> Un básico que va en tempo pero **falla al enemigo no genera CM**. El contador
> es de daño conectado; el tempo solo multiplica lo que ya generaste.
>
> **Eso es lo que hace obligatoria la secuencia del equipo.** Si la CM saliera
> de golpes fallados, la media generaría igual contra cualquier rival y no
> harían falta las ligeras. Al exigir conexión, contra evasión alta **hay que
> abrir primero con la ligera** para que la media pueda sostener.
>
> *Abre → sostiene → remata* deja de ser una convención y pasa a ser
> aritmética.

#### Por qué la CADENA de la ligera sí se juzga sobre lo que pulsas

Si el bonus exigiera que los golpes **conectaran** además de ir a tiempo, la puntería lo destruiría:

| Arma | Golpes | Acierto 50% | Acierto 65% | Acierto 80% |
|---|---|---|---|---|
| Ligera | 5 | **1.0%** | 3.8% | 10.7% |
| Media | 3 | 6.4% | 14.1% | 26.2% |
| Pesada | 1 | 40% | 52% | 64% |

Con arma ligera la cadena se completaría **el 1% de los turnos**. Nunca se vería — y la ligera es precisamente el arma que se lleva a los rivales evasivos, o sea justo donde más se falla.

Juzgándola sobre el input, depende **solo de la habilidad del jugador**:

| Arma | Golpes | Ritmo 70% | Ritmo 80% | Ritmo 90% |
|---|---|---|---|---|
| Ligera | 5 | 17% | 33% | 59% |
| Media | 3 | 34% | 51% | 73% |
| Pesada | 1 | **70%** | **80%** | **90%** |

> ### Dos ejes que no se pisan
>
> ```
> PRESS vs EVA  →  decide si HACES DAÑO     (azar + estadísticas)
> RITMO         →  decide si GENERAS CM      (habilidad del jugador)
> ```
>
> El jugador **nunca pierde el premio musical por mala suerte**. Y un turno con puntería nefasta nunca es un turno vacío del todo: si tocaste bien, sales con CM. Eso amortigua mucho la frustración de la evasión alta.
>
> Temáticamente también encaja: **la música no sabe si le diste al enemigo.** Tocaste en el compás o no tocaste.

#### Por qué +50% y no más

El bonus es **proporcional a la generación base del arma**, no una cantidad plana. Un bonus plano se lo llevaría la ligera solo por dar más golpes — justo el arma que **menos** debería vivir del pulso.

| Bonus | Ligera | Media | Pesada | Gradiente media>ligera>pesada |
|---|---|---|---|---|
| +25% | +8% | +13% | +20% | ✅ |
| **+50%** | **+16%** | **+26%** | **+40%** | ✅ |
| +75% | +25% | +38% | +60% | ❌ la pesada adelanta a la ligera |
| +100% | +33% | +51% | +80% | ❌ |

**El techo duro es +75%.** A partir de ahí la pesada supera a la ligera en generación y se rompe el orden decidido.

Turno perfecto en números:

| Arma | Turno normal | Turno perfecto |
|---|---|---|
| Ligera | 0.62 CM | **0.94** |
| Media | 0.75 CM | **1.12** |
| Pesada | 0.50 CM | **0.75** |

> ### ⚠️ El Attack Speed trabaja contra el bonus rítmico
>
> Más velocidad de ataque = más golpes que clavar = menos turnos perfectos.
>
> | Arma | Golpes en early | Perfecto | Golpes en late | Perfecto |
> |---|---|---|---|---|
> | Ligera | 5 | 33% | **8** | **17%** |
> | Media | 3 | 51% | **5** | **33%** |
> | Pesada | 1 | 80% | 1 | **80%** |
>
> Es un coste oculto de subir AS, y otra ventaja silenciosa para la pesada, que se queda en 1 golpe y **mantiene su 80% toda la partida**.
>
> ❓ Decidir si es deseable: puede leerse como un buen trade-off, o como que el jugador mejora y siente que toca peor.

### El pulso

> ⭐ **La capa rítmica es real.** El bonus se obtiene **al seguir el pulso** de lo que suena. No es azar ni una cadencia automática: el jugador escucha y sincroniza.

**Cada golpe se evalúa por separado.** En una ráfaga de 5 básicos puedes acertar el ritmo en 3 y fallarlo en 2.

```
Golpe fuera de tempo  →  suma 1 al contador
Golpe EN TEMPO        →  suma 2
```

Con esto, los golpes necesarios para 1 CM se reducen a la mitad si vas a tiempo:

| Arma | Fuera de tempo | En tempo |
|---|---|---|
| Ligera | 8 | **4** |
| Media | 4 | **2** |
| Pesada | 2 | **1** |

**Reglas:**
- Si el golpe **falla** el hit-check, no suma nada al contador.
- El tempo **no aplica al recibir daño**.

> ### El tempo no es un bonus: es el rendimiento esperado
>
> Los números de referencia del diseño (4 / 2 / 1) son **los de estar en tempo**. El sistema asume que **acertar el pulso es lo normal**, y fallarlo **duplica** lo que cuesta llenar la barra.
>
> No es *"si aciertas el ritmo te llevas un extra"*: es **"si pierdes el ritmo, produces la mitad"**. La música no premia — castiga. Y eso motiva mucho más a mantener el pulso.

### La estadística Cadencia

> ✅ **Renombrada (nov 2026).** Esta stat se llamaba *Tempo*. El nombre **Tempo** pasa a la stat de turno que ralentiza el tiempo de decisión (ver §2.1). Cadencia es **secundaria ofensiva**.

**Cadencia = % de ataques básicos MENOS necesarios para obtener 1 carga de CM.**

Es un **porcentaje**, no un contador. Por eso funciona igual para cualquier arma: cada arma conserva su base y Cadencia la reduce en proporción.

```
INTERNO   Cadencia = % (sube con el nivel, baja con el desgaste, como toda stat)

MOSTRADO  Golpes por CM = base_del_arma × (1 − Cadencia)
```

| Arma | Base | Cadencia 25% | Cadencia 50% (cap) |
|---|---|---|---|
| **Ligera** | 8 | 6 | **4** |
| **Media** | 4 | 3 | **2** |
| **Pesada** | 2 | 1.5 | **1** |

El **cap del 50%** que ya traía STATS MASTER da exactamente la curva buscada: alto al principio, cómodo en la etapa regular.

| Nivel | Cadencia | Golpes por CM (ligera) |
|---|---|---|
| LV1 | 2.8% | 8 |
| LV9 | 25% | 6 |
| LV18 | 50% | 4 |

> ### ⚠️ Regla general: ninguna estadística debe ser "mejor cuanto más baja"
>
> Cadencia (entonces llamada Tempo) se diseñó primero como *"golpes necesarios"*, y eso rompía dos sistemas: el escalado por nivel (`MAX × g(L)` la haría **subir**, o sea empeorar) y sobre todo el **desgaste**, que multiplica por `(1 − desgaste)` — un personaje destrozado habría necesitado **menos** golpes por carga.
>
> La solución: **guardar la estadística como eficiencia ascendente y convertirla solo al mostrarla.** El jugador lee "6 golpes por carga"; por dentro se comporta como todas las demás.
>
> Conviene aplicar este filtro a cualquier estadística futura que salga en forma de *"cuánto tarda"*, *"cuántos hacen falta"* o *"cada cuánto"*.

### Por qué la pesada se queda en 2

Decisión explícita: **la pesada no debe mejorar su generación de CM.**

> Ya cumple dos funciones —**cobrar las marcas elementales** al 100% y **pegar fuerte**—. Si además generase CM con facilidad, **la ligera se quedaría sin razón de existir**.

Generación de CM **por turno** resultante:

| Arma | Básicos/turno | Golpes por CM | CM/turno | En tempo |
|---|---|---|---|---|
| **Media** | 3 | 4 | 0.75 | **1.50** |
| **Ligera** | 5 | 8 | 0.63 | **1.25** |
| **Pesada** | 1 | 2 | 0.50 | **1.00** |

Gradiente limpio: **la media es la mejor generadora**, la ligera va detrás, la pesada es la peor — a cambio de cobrar el amplificador elemental íntegro.

### ✅ Los tres papeles, cerrados

Con el multiplicador de daño por arma ([formulas.md](formulas.md) §5), cada arma gana en su terreno:

| Arma | Daño × golpe | Su terreno | Cómo lo gana |
|---|---|---|---|
| **LIGERA** | ×0.6 | **Enemigos evasivos** | 8 golpes + **doble desgaste de EVA** |
| **MEDIA** | ×1.0 | **Todo lo demás** | Mejor daño estándar **y** mejor generación de CM |
| **PESADA** | ×1.5 | **Golpe fuerte** | Amplificador elemental íntegro · escala al subir su base (75 → 225 → 375) |

> La media no es "la sosa del medio": **gana en recurso y en daño contra objetivos normales**, y solo cede el nicho anti-evasión. Su equilibrio es una posición fuerte, no una falta de identidad.

> Que **la media sea la mejor en algo** resuelve un problema clásico: el arma intermedia suele ser la opción sosa que no destaca en nada. Aquí tiene identidad propia y una razón para elegirla que no es "no me decido".

### ⭐ Cada arma sigue una capa distinta de la música — ✅ CERRADO

Una canción tiene tres capas, y **cada arma se engancha a una**:

| Arma | Capa | Qué habilidad pide | Qué gana |
|---|---|---|---|
| **LIGERA** | **MELODÍA** | Fluir, arriesgar | Alargarse más allá del compás |
| **MEDIA** | **PULSO** | Constancia | CM extra por golpe en tiempo |
| **PESADA** | **COMPÁS** | Precisión | Daño extra al caer en el primer tiempo |

> ### Por qué ese reparto y no otro
>
> No es temático: es el único que funciona con **5 / 3 / 1 golpes**. Dentro de
> un compás, cada capa ofrece un número fijo de líneas donde encajar un golpe:
>
> | Capa | Líneas por compás | Armas a las que les caben sus golpes |
> |---|---|---|
> | **Compás** | **1** | solo la pesada (1 golpe) |
> | **Pulso** | 3 – 6, según el tramo | pesada y media (3) |
> | **Melodía** | ~el doble que el pulso | las tres |
>
> El compás ofrece **una sola línea**: a una media de 3 golpes no le caben ahí,
> y a una ligera de 5 menos. Por eso el reparto es forzado, y por eso la
> flexibilidad solo existe **hacia capas más densas**, nunca hacia las más
> escasas.
>
> Consecuencia práctica en una canción con compases variables: una ligera solo
> podría apoyarse en el pulso en tramos de **6/4** (6 líneas ≥ 5 golpes). En
> 4/4 perdería un golpe y en 3/4 dos.

> Esto es lo que hace que la música **no se pueda quitar**. Antes las figuras rítmicas —corchea, negra, blanca— eran nombres bonitos para «5, 3 y 1 golpes»: si borrabas la canción, el combate funcionaba igual. Ahora no. La música deja de ser ambientación y pasa a ser el mando.

#### LIGERA — la melodía no respeta la barra

Los básicos base van **al tempo**. Pero si la ligera **sigue la melodía**, no la cortan a mitad: **termina el compás en el que está y además el siguiente entero.**

> ⚠️ **Solo el siguiente.** Un compás extra, no dos.

#### ✅ La cadena — CERRADO

El compás extra **se gana sosteniendo la melodía sin parar**. La cadena vive
mientras cada básico caiga en la nota **inmediatamente siguiente** a la del
básico anterior.

**Se corta por dos motivos, y solo por esos dos:**

| Motivo | Qué pasa |
|---|---|
| **Desafinas** | el golpe no cae en ninguna nota → se corta ahí |
| **Te paras** | dejas pasar una nota sin pulsarla → se corta ahí |

**Lo que NO la corta:**

- **Fallarle al enemigo.** La cadena se juzga sobre lo que **pulsas**. Es lo que
  mantiene viva a la ligera contra un rival con evasión alta — que es
  exactamente donde más se falla, y para lo que existe el arma.
- **Los silencios de la canción.** Si no hay nota, no hay nada que dejar pasar.
  Por eso se cuenta **por notas, no por tiempo**.

La cadena **puede empezar en cualquier nota**: tú decides cuándo arrancarla. Lo
que no puedes es soltarla a medias.

> Esto convierte a la ligera en un **compromiso**: una vez arrancas, no te
> puedes parar a pensar si tiras la skill. Es lo que le pide su propia columna
> de la tabla — *fluir, arriesgar*.

```
COMPÁS EN CURSO           COMPÁS EXTRA              ✖ CORTE
┌───────────────┐         ┌───────────────┐
│ lo termina    │   ──▶   │ entero        │         no puede seguir
│ daño pleno    │         │ ⚠️ 25% AD base │
└───────────────┘         └───────────────┘
```

### ✅ La SEGUNDA VOZ y el compás gratis — CERRADO

Algunos tramos tienen una **segunda línea melódica de fondo**, densa. Donde la
hay, ese compás es un **BONUS EXPERTO**.

**La regla:**

| | |
|---|---|
| **Enganchar** | el primer golpe tiene que caer en la **primera nota del compás** |
| **Seguir** | cada golpe, en la nota siguiente |
| **El premio** | mientras aciertes, esos ataques **no gastan BA** |
| **El fallo** | al primero, se acaba el regalo y **empiezan a gastarse los BA reales** desde ahí |
| **El alcance** | solo **ese compás**, de principio a fin. No se encadena con el siguiente |

> ### Por qué el premio escala con la habilidad
>
> No es todo o nada. **Lo que te llevaste, te lo llevaste**: cada nota clavada
> antes del fallo es un ataque gratis que ya cobraste. Uno mediocre saca tres,
> uno bueno saca doce, y ninguno se va con las manos vacías.
>
> Es lo contrario de la cadena de la primera melodía, que sí es binaria. Dos
> mecánicas de ritmo con curvas de premio distintas.

> ### ⚠️ No se acumula con la extensión
>
> Terminar bien un compás experto **no da además el compás extra** de la
> primera melodía. Son dos premios al mismo esfuerzo.

> ### No hay nada que marcar
>
> Un compás es experto **si tiene notas de segunda voz**. La marca se deduce
> del contenido, así que no hay una lista aparte que mantener ni que se pueda
> desincronizar del audio.

**Entrar o no es una decisión del jugador.** En los tramos de semifusas el
bonus es tan goloso que se va a por él siempre; en otros menos densos, igual
compensa más guardar los BA. Eso es estrategia, no una casilla obligatoria.

*Implementado en `TemaMusical.ResolverExperto`.*

### La melodía da TIEMPO, no desborda golpes

Esto es lo que hay que entender bien, porque es contraintuitivo: **la ligera no se pasa del compás con los golpes que le sobran.** Lo que gana es **tiempo** para seguir atacando al ritmo de la melodía.

```
Base 7 BA
Sin melodía      →  los que quepan donde te corten
Con melodía      →  terminas este compás + el siguiente  →  caben 9
```

**La melodía no te limita: te da más golpes, más flojos.**

### ✅ Lo que se pierde es AD, NO Carga Musical

Aquí está la razón por la que alargarse compensa:

| En el compás extra | |
|---|---|
| **AD por básico** | ⚠️ **25%** |
| **Carga Musical** | **íntegra** |
| **Desgaste de evasión** | **íntegro** — ni siquiera depende del daño, sale de que el defensor esquive |

> La ligera **deja de ser fuente de daño y se convierte en motor de recurso y abrelatas de evasión.** No es un castigo por alargarse: es un cambio de papel dentro del mismo turno.
>
> Y es el papel que ya tenía asignado —*«las ligeras abren desgastando, la pesada remata»*— llevado al extremo y puesto en manos del jugador como una decisión del momento.

> ### Por qué renunciarías a tres cuartas partes de tu daño
>
> Porque hay dos cosas que se cobran **por impacto**, no por daño:
>
> - **Carga Musical** — cada golpe genera su 1/8, valga lo que valga
> - **Desgaste de evasión** — y la ligera lo aplica **doble**
>
> Continuar con la melodía es **renunciar a matar para abrir al enemigo**. Que es exactamente el papel que la ligera ya tenía asignado —*«las ligeras abren desgastando, la pesada remata»*— pero convertido en **una decisión del jugador en el momento**, no en una propiedad pasiva del arma.
>
> El combo de equipo se vuelve legible: la ligera se alarga y deja al objetivo sin evasión; la pesada entra en el compás siguiente y remata.

### ✅ Qué se calcula y qué se graba

Esta es la asimetría que decide cuánto trabajo de autoría cuesta cada arma:

| Capa | Arma | ¿De dónde sale? |
|---|---|---|
| **Pulso** | Media | ✅ se **calcula** con el BPM |
| **Compás** | Pesada | ✅ se **calcula** con el BPM y el compás |
| **Melodía** | Ligera | ❌ se **graba**, canción por canción |

Pero lo que se graba es **solo una lista de instantes** — cuándo empieza cada nota de la línea melódica. Ni alturas, ni duraciones, ni nombres de nota:

```json
{
  "bpm": 132,
  "tiemposPorCompas": 4,
  "secciones": [ "calma 0.0", "base 24.0", "estribillo 72.0" ],
  "melodia": [ 0.45, 0.68, 0.91, 1.13, 1.36, ... ]
}
```

> Y el archivo por canción **tiene que existir igual**: las secciones y sus multiplicadores (§4) ya había que marcarlos a mano. La melodía no es una categoría nueva de trabajo, es **una línea más** en un archivo que ya estaba previsto.
>
> Además, una lista de inicios de nota es exactamente lo que produce un **detector de onsets**. Marcar una canción pasa de transcribirla a revisar lo que la máquina ya marcó.

**Si un tema no trae melodía grabada, la ligera se alarga a su propio ritmo.** Funciona igual, solo que sin la sincronía fina. Eso permite marcar primero los temas que importan —bosses, historia— y dejar las batallas normales para después, o para nunca.

#### MEDIA — llegar al final del compás

La media va **al pulso**. Con base 3 ocupa tres tiempos del compás; si el **Attack Speed** la lleva más lejos dentro de la barra, alcanza el **último tiempo**, y ahí está el premio.

```
4/4   |  1    2    3    4  |
base  |       AB   AB   AB |        ← empieza en el 2
+AS   |  AB   AB   AB   AB |        ← llega al 4  ✔ CM extra
```

**El Attack Speed no regala el golpe: te lleva lo bastante lejos en el compás como para poder darlo.** Por eso no se suma con nada — es el vehículo, no el premio.

⚠️ El extra es **pequeño**: un X% de su CM base por golpe en tiempo. Y es deliberado que sea pequeño, porque el verdadero premio está más allá:

> ### La media es el motor de CLASICAL
>
> Lo que quieres de la media no es el goteo de CM: es **llegar antes al estado Clasical**, donde lo que toca es gastarlo todo a como dé lugar. El extra por golpe es un empujón hacia ese momento, no una recompensa en sí.

#### PESADA — un solo golpe, en el uno

La pesada cae en el **primer tiempo del compás**. Si acierta, el daño aumenta un ⚠️ X%.

**No es todo o nada.** El bonus se reparte por cercanía: clavado da el máximo, y va bajando conforme te adelantas o te retrasas.

```
        antes          EL UNO          después
    ├──────────┼──────────●──────────┼──────────┤
       parcial      ▲ máximo            parcial
```

> Esto resuelve la contradicción de que la pesada se potencie **manteniendo el clic**: mantener no está reñido con el ritmo, porque la pesada no persigue notas rápidas — persigue el acento. Cargas durante el compás y sueltas en el uno.
>
> Y le da a la pesada la vía de poder que le faltaba. Hasta ahora, con base 1, el Attack Speed no le servía de nada y el arma era *«una promesa»* hasta subir la base. Con esto tiene una fuente de daño que **no depende de esa mejora**: depende de tu oído.

#### ✅ El bonus de la pesada — CERRADO

Un básico que cae en el primer tiempo del compás aplica el **150 % del AD**.

**Plano dentro de la ventana.** Rozar el borde paga igual que clavar el centro:
la ventana ya es el margen de ayuda. Pedir además precisión fina convertiría el
arma en un juego de ritmo dentro del juego de ritmo.

> ### ⚠️ Sobre qué AD — el operativo de ese instante
>
> El 150 % se aplica al **AD que el personaje tiene en ese momento, ya con el
> desgaste descontado.** No al AD potencial que tendrá a nivel máximo.
>
> Un personaje a **nivel 9** con la mitad del AD que llegará a tener a nivel 18
> cobra el 150 % de **lo que tiene ahora**. El ritmo **nunca** te adelanta
> progresión: multiplica tu estado actual, no tu techo.
>
> Es el mismo eje que el 25 % de la ligera en el compás extra — dos
> multiplicadores del AD del momento, uno por debajo de 1 y otro por encima.

⚠️ Pendiente: **la anchura de la ventana** (provisional: ±120 ms).

#### El triángulo que sale de todo esto

| Arma | En la música | En el combate | La moneda que persigue |
|---|---|---|---|
| Ligera | melodía | **abre** | desgaste de EVA |
| Media | pulso | **sostiene** | Carga Musical → Clasical |
| Pesada | compás | **remata** | daño |

Tres armas, tres capas musicales, tres papeles de combate y tres monedas distintas. Y una gradiente de dificultad que sale gratis: quien no tenga oído puede jugar la media y le irá bien; la pesada es para quien sí.

> **Tocar la melodía, marcar el pulso y acentuar el compás son tres formas reales de acompañar una canción**, y aquí son tres formas de jugar. El arma ya no elige solo cuántos golpes das — elige **qué parte de la música eres**.

#### Las habilidades QWER quedan fuera

✅ **Los combos no tienen relación con el ritmo**, salvo que una habilidad concreta diga lo contrario.

Es la decisión correcta y encaja con la arquitectura: lo normal es que no interactúen, y las excepciones son **contenido** —una regla más— no un caso especial en el motor.

> ### Lo que esto cambia respecto a la versión anterior
>
> - La **media** tenía su capa sin especificar. Ya la tiene: el pulso.
> - La **pesada** seguía el *pulso*; pasa a seguir el **compás**, que es más preciso y musicalmente más certero — el acento estructural, no cada latido.
> - La **melodía estaba aparcada** por ser difusa: *«¿qué cuenta como seguir la melodía cuando la canción cambia?»*. Ya no es difusa: seguir la melodía es **continuar pulsando más allá de la línea de compás**. Eso es binario, medible e implementable, así que la razón para aparcarla desaparece.

---

## 2. La velocidad de la música es la velocidad del juego

> ✅ **Rediseñado (nov 2026).** Sustituye al timeout fijo de 3 s por personaje. El turno ya no se mide en segundos: **se mide en compases de la canción que suena.**

### El turno se mide en compases

```
Turno aliado = N compases de la canción          (N ⚠️ provisional, ≥ 4)
             = una BOLSA COMPARTIDA por todo el equipo

El jugador reparte ese tiempo entre sus integrantes como quiera.
Si la bolsa se agota  →  el turno se CORTA ahí y pasa el enemigo,
                         aunque falten aliados por actuar.
```

- **N sale de cómo se componen las secciones** (4, 8… compases). El valor exacto se fija en playtesting.
- **La duración real en segundos la dicta el BPM.** Una canción rápida aprieta; una lenta da margen. Si la batalla se vuelve caótica y la música acelera, la bolsa se encoge sola, sin regla adicional.
- **La presión es del jugador, no del personaje:** cuánto se apura depende de él y del tempo de la canción.

| BPM | 1 compás (4/4) | Bolsa de 4 | Bolsa de 8 |
|---|---|---|---|
| 80 | 3.0 s | 12.0 s | 24.0 s |
| 100 | 2.4 s | 9.6 s | 19.2 s |
| 120 | 2.0 s | 8.0 s | 16.0 s |
| 150 | 1.6 s | 6.4 s | 12.8 s |

> **Coincidencia de diseño:** cada arma llena un compás con los básicos que necesita para 1 CM (8 corcheas · 4 negras · 2 blancas). Un compás de básicos ≈ 1 CM, sea cual sea el arma.

Esto une en un solo eje tres cosas que estaban sueltas: la Carga Musical como recurso, el bonus por tocar a tiempo, y la presión de decisión. **El pulso de la canción es el pulso de la partida.**

### 2.1 La estadística Tempo — tiempo para pensar

**Tempo = segundos de tiempo casi detenido para decidir, que se ganan al abrir el turno.**

```
Disparador   la PRIMERA acción activa del turno del equipo
             (DEFEND, consumible, gadget, BA, QWER…)
Frecuencia   1 vez por turno de equipo
Quién cuenta el Tempo del personaje que ABRE el turno
Efecto       el tiempo va casi detenido a la vista del jugador
             → navega todo el menú con calma para planear el resto
Escala       la ralentización es FIJA; lo que escala con la stat
             son los SEGUNDOS que dura
Consumo      los segundos solo corren mientras estás en el MENÚ
Ejecución    al ejecutar (tocar básicos, etc.) la música vuelve
             a su velocidad normal — el ritmo sigue siendo un reto real
```

> ### Por qué así
>
> **Es un anti-parálisis.** Al empezar el turno el reloj corre a velocidad real: no puedes quedarte pensando sin hacer nada. Para ganarte tiempo de pensar, **primero tienes que comprometerte con algo**. Abrir con algo seguro (un DEFEND, el consumible gratis de la ventana pre-ofensiva) compra el respiro para planear lo complicado.
>
> **El orden de turnos decide cuánto piensas.** Como cuenta el Tempo de quien abre, elegir quién va primero también es elegir cuánto tiempo tendrá el equipo para decidir.
>
> **No acelera el ritmo de ejecución.** Si los básicos también se tocaran en cámara lenta, acertar el pulso sería trivial y el bonus rítmico perdería sentido. Tempo sirve para decidir, no para tocar.

**El protagonista es quien más Tempo tiene, porque no tiene Clasical.** Los demás aportan ráfaga; él aporta claridad. No aporta poder: aporta tiempo para usar bien el de los demás. Abrir el turno con él es darle al equipo el mayor margen de decisión.

❓ Pendientes:
- Valores de N y de los segundos de Tempo (MAX por personaje, cap del juego).
- Factor exacto de la ralentización ("casi detenido").
- Si la bolsa de compases se agota en mitad de una acción ya iniciada, ¿se completa o se corta?
- Duración de las acciones dentro de la bolsa: propuesta de que cada skill declare su duración en tiempos musicales (p. ej. Q = 1 tiempo · R = 1 compás) para que la animación no balancee el juego por accidente.

### Los enemigos también escuchan — o no

La relación de cada enemigo con la música es **parte de su configuración**:

| Perfil | Comportamiento |
|---|---|
| **Despistado** | Se pierde el ritmo. |
| **Enfocado** | Lo sigue. |

> Que un enemigo esté despistado o enfocado es un rasgo de diseño del bestiario, no un estado aleatorio. Da una vía limpia para hacer encuentros que se sienten distintos sin tocar sus estadísticas.

❓ ¿Estar despistado/enfocado afecta solo a su ganancia de CM, o también a su velocidad de acción?

---

## 2.5 El castigo por fallar es lo que separa el early del late

**Un golpe que falla genera 0 CM.** Y ese castigo pesa muchísimo más al principio de la partida.

| | EARLY | LATE |
|---|---|---|
| Coste de las skills | Alto | **Bajan progresivamente** |
| Obtención de CM | Baja | **Mayor** |
| Fuentes disponibles | Casi solo básicos | Básicos + Carga Musical + Defensa + skills |
| Fallar un golpe | **Devastador** | Absorbible |

> ### La curva de la run está en el castigo, no en el poder
>
> **Early:** cada básico es una fracción escasa de un recurso compartido. Un fallo con arma pesada tira el turno entero — es un golpe, y ese golpe era 1 CM completo. Lanzar una habilidad es un acontecimiento.
>
> **Late:** los costes han bajado, la generación ha subido y hay varias vías para rellenar. Fallar duele, pero no te para.
>
> El autor lo dice explícitamente: **esto existe para diferenciar los tiempos de la run.** No es que en late pegues más fuerte; es que en late **puedes permitirte equivocarte**.

**Consecuencia para la pesada:** con 1 solo básico, un fallo es literalmente todo el CM del turno. Es el arma más castigada por la varianza en early y la que mejor rinde cuando conecta.

---

## 3. Modo CLASICAL

**Se activa con la ACCIÓN de Clasical, cuando con ella la barra comunitaria llega al máximo.**

| | |
|---|---|
| **Disparador** | ✅ **La ACCIÓN de Clasical**, cuando con ella la barra llega al máximo |
| **Efecto** | **×1.5 a la obtención de CM de TODAS las fuentes**, multiplicándose con el multiplicador de la sección musical |
| **Ventana** | El turno del héroe **y el turno del villano** |
| **Fin** | Al iniciar el **siguiente turno aliado** |

### ⚠️ Por qué el disparador NO puede ser "empezar el turno lleno"

La regla original (barra llena al inicio del turno) **se autodestruye**: si tienes la barra llena, lo natural es gastar; y si gastas, ya no amaneces lleno al turno siguiente.

Simulado sobre 6 rondas, Clasical **no se activaba ni una sola vez** en ninguna fase de la partida, ni jugando avaro ni guardando a propósito. Y guardar salía peor: en late costaba 6 combos de 15 y aun así no lo encendía.

**Por eso el disparador es la acción.** Encender Clasical deja de ser algo que ocurre y pasa a ser algo que **alguien paga con su turno**.

### La acción de Clasical

```
Aporte = storage propio (3 CM)  ×  multiplicador de la sección musical

Si con ese aporte la barra llega al MÁXIMO
  →  CLASICAL se enciende durante el ciclo
```

| Sección | Aporta | vs atacar |
|---|---|---|
| Calma ×0.5 | 1.5 CM (12% del pool) | ×2.2 |
| Base ×1.0 | 3.0 CM (25%) | ×2.2 |
| Estribillo ×1.8 | **5.4 CM (45%)** | ×2.2 |

> **Encenderlo en el estribillo es mucho más barato** — 5.4 de aporte frente a 3.0 — así que cuadrar el clímax con la canción sigue siendo la jugada maestra.

**Rendimiento medido** — generación real del sistema (contador de tempo, 50% de acierto):

```
basicos 2.50 + recibir dano 2.25 + spells 1.00 + gadgets 0.30  =  6.05 CM/ronda
Pool 12  ->  2.0 rondas para llenarlo
```

| | Early (combo 12) | Mid (combo 6) | Late (combo 3) |
|---|---|---|---|
| Sin usar la acción | 2 combos | 6 | 13 |
| **Con la acción (3 CM)** | **3** · Clasical ×2 | **7** · ×1 | **14** · ×1 |

> ### El valor está en el disparo, no en la carga
>
> Entre **2 y 4 CM el resultado es idéntico** en las tres fases: lo que aporta la acción no es el recurso, es **encender el modo**. Eso da libertad para mover el número sin romper nada.
>
> Se fija en **3 CM (= el storage)** por coherencia: se autoescala, y quien más capacidad aporta al bote es también el mejor encendiéndolo.
>
> ⚠️ **Evitar 6 CM o más**: ahí la acción empieza a pagar por sí sola (+4 combos en late) en vez de por lo que enciende, y Clasical se dispara el doble de veces.

> **Robustez comprobada:** la conclusión no depende del porcentaje de acierto. Con todo acertando (8.55 CM/ronda) la acción da +2 combos en early y mid; con 50% de acierto en ambos bandos (4.42 CM/ronda) da +2 en mid y late. **En todos los escenarios sigue siendo una acción notable**, que es lo único que tiene que cumplir. No hace falta afinar más hasta tener playtesting.

Usarla rinde **+1 o +2 combos por combate** según la fase, a cambio del turno de un personaje. Es un intercambio ajustado a propósito: **la acción se paga con lo que enciende, no con lo que aporta.**

---

## 4. Los temas musicales

Cada tema aplica **un porcentaje de bonus de Clasical para todos**, y ese porcentaje **varía según la parte de la canción** en la que estés.

```
Tema musical  →  % de bonus Clasical
                 (distinto en cada sección de la canción)
```

### Escala por tipo de encuentro

| Encuentro | Capas de música | Variación de bonus |
|---|---|---|
| **Batalla normal** | Menos capas | Menor |
| **Mini boss** | ↕ | ↕ |
| **Boss** | ↕ | ↕ |
| **BOSS DE HISTORIA** | **Más capas** | **Mayor** |

> ### La música es la curva de dificultad
>
> A mayor importancia del combate, **más capas musicales y más variación de bonus**. La banda sonora no acompaña la escalada: **es** la escalada. Un jefe de historia no solo pega más fuerte — su tema tiene más capas, más secciones y picos de bonus mayores, así que la pelea tiene *estructura dramática mecánica*.
>
> Y como el bonus cambia según la sección de la canción, **el momento de tu turno importa**. Guardar la barra para que Clasical caiga en el estribillo es una decisión táctica real. No conozco otro RPG donde el jugador tenga motivos mecánicos para escuchar dónde va la canción.

❓ ¿El jugador puede **ver** en qué sección está (indicador de UI), o solo oírla?
❓ ¿Las secciones son fijas por combate o el combate se adapta a la duración de la canción?
❓ ¿Las capas se van sumando conforme avanza la pelea?

---

## 5. Acción Carga Musical

**Propuesta de trabajo:** usar Carga Musical **llena por completo la capacidad propia del personaje** y la vuelca al pool compartido.

```
CM_equipo += cmStorage del personaje que carga
```

> Es una regla limpia porque se autoescala: un personaje con storage grande aporta mucho al cargar; uno pequeño, poco. Y hace que **el personaje que más capacidad aporta al equipo sea también el mejor cargando**, lo cual le da un rol doble coherente.

- **Mejora con la habilidad de overworld de MÚSICA.**

❓ Valor no cerrado — el usuario lo da como suposición, no como decisión.
❓ ¿Se puede cargar **en tempo** para ganar más?

❓ Sistema de **habilidades de overworld**: existe y no está documentado. ¿Cuáles hay además de Música? ¿Cómo se suben?

---

## Resumen de lo que hace único a este sistema

1. **Cada arma es una figura rítmica** (corchea / negra / blanca), y eso determina su ritmo de input y su generación de recurso a la vez.
2. **El pulso de la canción marca el tiempo de decisión** del jugador.
3. **Acertar el pulso paga CM**; cuánto paga es una estadística que se sube.
4. **Los enemigos siguen o pierden el ritmo** según su diseño.
5. **Llenar la barra justo a tiempo abre Clasical**, que invierte la economía del recurso durante un ciclo completo.
6. **La sección de la canción decide el bonus**, así que hay razones mecánicas para escuchar la música.
7. **Los jefes tienen más capas musicales**, de modo que la banda sonora *es* la curva de dificultad.

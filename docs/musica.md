# E.N.M.A — Música, Tempo y modo CLASICAL

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

> No son números arbitrarios: **cada clase de arma es una subdivisión rítmica**. La ligera toca corcheas —muchas y rápidas—, la pesada toca blancas —pocas y largas—. Encaja con sus esquemas de input (spam de clics vs. mantener pulsado) y con su número de golpes por turno.

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
    la stat Tempo reduce X
    ▶ PERSISTE ENTRE TURNOS — ningún golpe se desperdicia

BONUS RÍTMICO  (premio a la habilidad, aparte del contador)
    todos los básicos del turno PULSADOS en tempo  →  ese turno genera ×1.5
    ▶ se juzga sobre el INPUT, no sobre si el ataque conectó
```

#### Por qué el ritmo se juzga sobre lo que pulsas, no sobre lo que conecta

Si el bonus exigiera que los golpes **conectaran** además de ir a tiempo, la puntería lo destruiría:

| Arma | Golpes | Acierto 50% | Acierto 65% | Acierto 80% |
|---|---|---|---|---|
| Ligera | 5 | **1.0%** | 3.8% | 10.7% |
| Media | 3 | 6.4% | 14.1% | 26.2% |
| Pesada | 1 | 40% | 52% | 64% |

Con arma ligera el bonus saltaría **el 1% de los turnos**. Nunca se vería.

Juzgándolo sobre el input, depende **solo de la habilidad del jugador**:

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

### La estadística Tempo

**Tempo = % de ataques básicos MENOS necesarios para obtener 1 carga de CM.**

Es un **porcentaje**, no un contador. Por eso funciona igual para cualquier arma: cada arma conserva su base y Tempo la reduce en proporción.

```
INTERNO   Tempo = % (sube con el nivel, baja con el desgaste, como toda stat)

MOSTRADO  Golpes por CM = base_del_arma × (1 − Tempo)
```

| Arma | Base | Tempo 25% | Tempo 50% (cap) |
|---|---|---|---|
| **Ligera** | 8 | 6 | **4** |
| **Media** | 4 | 3 | **2** |
| **Pesada** | 2 | 1.5 | **1** |

El **cap del 50%** que ya traía STATS MASTER da exactamente la curva buscada: alto al principio, cómodo en la etapa regular.

| Nivel | Tempo | Golpes por CM (ligera) |
|---|---|---|
| LV1 | 2.8% | 8 |
| LV9 | 25% | 6 |
| LV18 | 50% | 4 |

> ### ⚠️ Regla general: ninguna estadística debe ser "mejor cuanto más baja"
>
> Tempo se diseñó primero como *"golpes necesarios"*, y eso rompía dos sistemas: el escalado por nivel (`MAX × g(L)` la haría **subir**, o sea empeorar) y sobre todo el **desgaste**, que multiplica por `(1 − desgaste)` — un personaje destrozado habría necesitado **menos** golpes por carga.
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

> Esto es lo que hace que la música **no se pueda quitar**. Antes las figuras rítmicas —corchea, negra, blanca— eran nombres bonitos para «5, 3 y 1 golpes»: si borrabas la canción, el combate funcionaba igual. Ahora no. La música deja de ser ambientación y pasa a ser el mando.

#### LIGERA — la melodía no respeta la barra

Los básicos base van **al tempo**. Pero la ligera puede **seguir la melodía y continuar hasta el siguiente compás**.

```
COMPÁS 1          COMPÁS 2
[ABABAB…]  ──▶    [ababab…]     ← continúa, pero al 25% de AD base
 daño pleno        ⚠️ 25%
```

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

⚠️ Pendiente: **hasta dónde puede alargarse.** Un compás extra, o mientras se mantenga el input. Tiene que haber un tope.

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

⚠️ Pendiente: el X% del bonus y la anchura de la ventana.

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

El `timeout` de turno no es una constante: **lo marca la música.**

> Si la música va rápida, hay que **reaccionar rápido**. Si va lenta, hay más margen.

**Duración base: 3 segundos.** Es el margen por defecto para tomar una acción antes de que el turno pase al siguiente personaje.

> ✅ **El timeout lo determina la canción.** No es una constante del combate ni depende del enemigo: **lo marca la música que suena.** Los 3 segundos son la referencia por defecto; cada tema impone el suyo.

> Tres segundos es poco para deliberar y suficiente para ejecutar. Confirma que el juego no espera que pienses tu turno completo en ese margen: **la decisión se toma mientras miras el tablero, y los 3 s son para encadenar**. Encaja con la regla de "empiezas con B o con QWER y no vuelves atrás" — una vez arrancas, el turno es una cadena, no un menú.

Esto une en un solo eje tres cosas que estaban sueltas: la Carga Musical como recurso, el bonus por tocar a tiempo, y la presión de decisión. **El pulso de la canción es el pulso de la partida.**

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

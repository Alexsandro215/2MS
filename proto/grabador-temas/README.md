# Grabador de temas

Marca a mano lo único de una canción que **no se puede calcular**: la melodía.

El pulso y el compás salen solos del BPM. La melodía no sale de ningún sitio — hay que marcarla.

## Cómo se usa

1. Deja tus canciones en **`audio/`** (`.mp3`, `.ogg` o `.wav`).
2. Doble clic en `LANZAR.bat`.
3. Fase 1, fase 2, guardar.

## Una sola pantalla, sin modos

Cada tecla hace **siempre** lo mismo. No hay fases.

### La melodía se COLOCA, no se graba

Hay una línea propia debajo de la barra de tramos. Las notas se ponen como
**figuras encajadas en la rejilla**, no pulsando a tiempo real.

| | |
|---|---|
| **F** | Cambia la figura: redonda → blanca → negra → corchea → semicorchea → fusa |
| **D** | Puntillo — suma la mitad del valor |
| **clic** en la línea | Pone una figura, encajada sola en la rejilla |
| **arrastrar** | **Pinta notas seguidas** mientras mantienes pulsado |
| **clic sobre la misma figura** | La quita — volver a poner lo mismo lo apaga |
| **clic derecho** | Borra siempre, sea la figura que sea |

**La rejilla y el valor de la nota son cosas distintas**, igual que en una
partitura: el valor dice cuánto **dura** la nota, no dónde puede ir.

El paso de la rejilla es **como mucho un tiempo**:

| Figura | Paso | Dónde cae |
|---|---|---|
| redonda, blanca | un tiempo | en cualquier pulso |
| negra | un tiempo | en cualquier pulso |
| corchea | medio tiempo | también entre pulsos |
| semicorchea | cuarto de tiempo | y más fino |
| fusa | octavo de tiempo | 43,6 ms a 172 BPM |

Así **cualquier figura, por larga que sea, se puede poner en cualquier pulso**.

> ⚠️ Antes el paso era el ancho de la figura, y eso dejaba huecos imposibles:
> una blanca solo caía cada 2 tiempos desde el origen del tramo, así que **no
> se podía poner detrás de tres negras** — el sitio donde acaban, a 3 tiempos,
> no existía en esa rejilla. Y en un **3/4** la rejilla de blancas se iba
> desfasando de las barras de compás, porque 2 tiempos no caben un número
> entero de veces en 3.

Poner una figura encima de otra **la sustituye** — colocar encima es corregir,
no apilar.

**Arrastrar pinta una nota por casilla.** Temblar dentro de la misma casilla no
duplica nada, y pasar dos veces por encima tampoco: cada casilla se escribe una
sola vez. Un arrastre entero es **un paso de deshacer y un guardado**, no
ochenta.

Una vez empezado el trazo solo cuenta el movimiento horizontal, así que salirte
de la línea por arriba o por abajo no lo corta. Se cierra al soltar.

**El botón izquierdo decide el modo por lo que hay debajo:**

| Debajo del cursor | Qué hace |
|---|---|
| Nada | pinta |
| Nota de **otra** figura | la sustituye |
| Nota de **la misma** figura | la borra |

Y una vez decidido, **el trazo entero va en ese modo**. Si empiezas borrando,
sigues borrando aunque cruces notas de otro valor — si no, quitar una fila de
corcheas se cortaría en la primera negra.

> Borrar **no encaja a la rejilla**: quita lo que haya bajo el cursor. Si
> encajase con la figura activa, con una redonda seleccionada sería imposible
> borrar una corchea.

### La vista previa dice lo que va a pasar

Antes de pulsar, el fantasma bajo el cursor te dice **exactamente** qué ocurre:

| Se ve | Al pulsar |
|---|---|
| Fantasma del **color de la figura** | la pone |
| Fantasma con **«sustituir»** | cambia la que hay por esta |
| **Rojo con una X** encima de una nota | **la borra** |
| **Gris, «ya está»** | no hace nada: ahí ya hay una idéntica |

**El borrado necesita las dos cosas:** que debajo haya una nota idéntica **y**
que estés apuntando a su propia casilla. Eso parte cada nota en dos mitades:

```
┌──────────────┬──────────────┐
│  mitad izq.  │  mitad der.  │
│   la BORRA   │  pone la     │
│              │  SIGUIENTE   │
└──────────────┴──────────────┘
```

Sin zona muerta: encadenar notas es clicar siempre en la cola de la anterior.

> Sin la segunda condición, **añadir una nota detrás de otra borraba la de
> delante**: una negra mide 348,8 ms y el paso de rejilla es también 348,8, así
> que el sitio de la siguiente cae justo en su borde y clicar un pelo antes
> caía dentro de la anterior.

### El puntillo — `D`

Suma **la mitad** del valor de la figura, como en una partitura:

| | dura | |
|---|---|---|
| **blanca.** | 3 tiempos | = blanca + negra |
| **negra.** | 1,5 tiempos | = negra + corchea |
| **corchea.** | 0,75 tiempos | = corchea + semicorchea |

Se dibuja con un punto a la derecha del bloque, donde va en una partitura.

**Con puntillo, la figura encaja en medias unidades de sí misma.** Una negra sin
puntillo solo cae en tiempos enteros; con puntillo cae también en los medios —
que es donde tiene que caer, porque dura tiempo y medio.

Eso hace que el patrón de siempre salga solo:

```
negra. (1,5 tiempos)  +  corchea (0,5)  =  2 tiempos exactos
```

La corchea encaja justo donde acaba la negra con puntillo, sin hueco y sin
solape. No hay que ajustar nada a ojo.

### Seleccionar varias notas

**MAYÚS+clic** sobre una nota la ancla. **MAYÚS+clic** sobre otra estira la
selección hasta ahí y coge **todas las que haya en medio**, sean de la figura
que sean.

| | |
|---|---|
| **arrastrar** la selección | la mueve entera, encajada a la rejilla |
| **SUPR** | borra las seleccionadas |
| **CTRL+C** | las copia |
| **CTRL+V** | las pega en el cursor |
| **clic fuera** | suelta la selección |

Se copian en **relativo**, así que conservan sus huecos y sus figuras al
pegarlas en cualquier otro punto de la canción.

**Se pega tal cual.** El hueco de destino se vacía primero y luego entran las
copiadas enteras, sin tocarse entre ellas. Antes se pegaban una a una y cada
nota borraba la anterior si la cubría — el pegado **se comía las suyas propias**.

> **Mover o pegar encima SUSTITUYE lo que haya debajo.** Si no, quedarían dos
> notas en el mismo instante y dejaría de estar definido a cuál golpeó el
> jugador.
>
> Por lo mismo, poner una figura larga encima de otras cortas **se traga las
> que ocupan su sitio**: una blanca puesta sobre dos negras ocupa el de las dos.

> Con notas seleccionadas, `CTRL+C` y `CTRL+V` trabajan sobre las **notas**.
> Sin selección, siguen copiando y pegando el **patrón de compases**.

### La zona de ayuda

La banda clara alrededor de cada nota es **dónde puede pulsar el jugador y
cuenta**. Es un único valor para toda la canción: `O` y `MAYÚS+P` lo ajustan.

> ⚠️ **Si dos zonas se solapan, no está definido a qué nota golpeó el jugador.**
> El panel calcula el tope según el hueco más corto que haya y avisa en rojo si
> te pasas. Con corcheas a 172 BPM el tope son **87 ms** — menos que los 120 de
> partida.

`ESPACIO`, `RETROCESO` y `SUPR` siguen existiendo para marcar a mano, pero lo
normal es colocar figuras.

### La rejilla

Se ajusta una vez y ya.

| Tecla | Qué hace |
|---|---|
| **T** | Golpea al ritmo cuatro o más veces y deduce el BPM |
| **↑ ↓** | BPM ±1 |
| **← →** | BPM ±0,1 |
| **C** | Compás: 3/4 → 4/4 → 6/4 → 5/4 → 7/4 |
| **G** | Fija dónde cae el primer tiempo fuerte |

El metrónomo suena distinto en el uno de cada compás. **Está bien cuando el clic agudo cae siempre sobre el acento de la canción y no se desplaza con los segundos.**

En la línea de tiempo de abajo: rayas naranjas los compases, azules los pulsos, puntos verdes tus marcas.

> ### ⚠️ Por qué no hay fases
>
> La primera versión tenía dos fases y **ESPACIO hacía algo distinto en cada una**. Costó una sesión entera de trabajo: 621 pulsaciones que reajustaban la rejilla en vez de marcar notas, porque el modo activo no se veía.
>
> Un modo invisible que cambia lo que hace la tecla principal es la forma más rápida de que alguien pierda su trabajo. Por eso ahora `G` fija la rejilla y `ESPACIO` solo marca melodía.

## Se guarda solo

No hay que acordarse de nada. Autoguarda **con cada marca**, al ajustar la rejilla, al cambiar de canción, al salir con ESC y **al cerrar con la X**.

`S` sigue existiendo, pero solo para confirmar.

## Precisión

Con 6 minutos de canción en una barra de 1400 píxeles, cada píxel son casi **3 décimas de segundo**. Imposible clavar un límite de compás a ojo. Por eso hay tres herramientas.

### Ajuste a rejilla — `N`, viene activado

Es la que más resuelve. Un cambio de compás cae **siempre** sobre un tiempo del tramo anterior — no puede empezar a medio pulso. Así que los bordes de la selección se pegan solos al pulso más cercano.

Con esto, **arrastrar a ojo ya es exacto**, siempre que el BPM del tramo anterior esté bien.

### Zoom — rueda del ratón

También `Z` / `A`, y `V` para volver a ver todo. En pantalla sale cuántos milisegundos vale cada píxel.

Con zoom aparecen dos cosas más:

- **Las líneas de rejilla dentro de la barra**, gruesas en el uno de cada compás. Ahí **ves** si el metrónomo cuadra, no solo lo oyes.
- **Un minimapa** encima con la canción entera y la ventana marcada, para no perderse.

### Empujar el borde — `,` y `.`

Mueven **un tiempo exacto** el borde más cercano al cursor. Para cuando te pasaste o te faltó un compás y no quieres volver a arrastrar.

## Seleccionar un rango

**Arrastra sobre la barra.** Se marca con un borde del color del compás. Un clic suelto solo mueve el cursor.

Con la selección activa, `C`, `T` y las flechas **editan la selección**, no el tramo del cursor. Cuando esté como quieres, **ENTER** lo escribe en el mapa: el rango pasa a ser su propio tramo y justo después se restaura lo que había.

**Clic derecho** quita la selección sin aplicar nada.

## Repetir un patrón de compases

Un estribillo que va 4/4 → 2/4 → 4/4 suele aparecer tres o cuatro veces en la
canción. No hay que rehacerlo a mano cada vez.

1. **MAYÚS+clic** sobre cada tramo que se repite. Se contornean en ámbar y
   llevan un número: es el orden dentro del patrón.
2. Mueve el cursor a donde empieza la repetición.
3. **CTRL+V**. Sale la vista previa en ámbar: lo que se va a escribir y dónde.
4. **ENTER** lo escribe. **Clic derecho** cancela.

El patrón se guarda como **duraciones**, no como instantes, así que vale en
cualquier punto de la canción. Después de pegar **sigue copiado**: mueve el
cursor al siguiente estribillo y vuelve a CTRL+V.

Lo que hubiera en el hueco se sustituye, y justo detrás se restaura lo que
había. Clic derecho otra vez descarta el patrón.

> Marcar un tramo ya lo copia; `CTRL+C` está por costumbre, pero no hace falta.

## Mover el bloque marcado

Los mismos tramos que marcas para copiar **se mueven juntos**. Para cuando el
estribillo está bien montado pero entra medio compás tarde.

- **Arrástralo desde dentro.** Clicar sobre un tramo marcado lo agarra — no
  borra la marca.
- **`,` y `.`** lo empujan ±10 ms. Con **MAYÚS**, un tiempo entero.

El bloque **conserva su largo y sus compases internos**: lo que hay delante se
alarga y lo de detrás se acorta. El resto de la canción no se entera.

**Clic derecho** durante el arrastre lo deja como estaba.

> Si marcaste el 3 y el 5, el bloque es **3–5**: el 4 está físicamente en medio
> y no puede quedarse quieto mientras los otros dos se mueven.
>
> El **primer tramo no se mueve nunca** — siempre empieza en 0.

Empujar el bloque contra su vecino lo deja **sin largo**, y ahí para. Ese tramo
vacío **se conserva mientras editas**, para que arrastrar de vuelta lo
recupere; al guardar no se escribe.

## Deshacer y rehacer — `CTRL+Z` / `CTRL+Y`

Deshace lo que es trabajo: el **mapa de tempo** y la **melodía**. El zoom, el
cursor y el volumen no se guardan, porque se rehacen en un segundo.

Los cambios seguidos del mismo tipo cuentan como **uno solo**: tener pulsado
`↑` hasta subir el BPM veinte puntos es un paso, no veinte. Pero **cada nota de
melodía es suya**, porque se marcan en ráfaga y querrías quitarlas una a una.

**`CTRL+Y`** devuelve lo deshecho. También vale `CTRL+MAYÚS+Z`.

Cubre el mapa de tempo, **las figuras de melodía** y las marcas sueltas.

Guarda los últimos **80 pasos** en cada sentido. Abajo pone siempre qué va a
deshacer el siguiente `CTRL+Z` y qué va a devolver el siguiente `CTRL+Y`.

> Como en cualquier editor, **un cambio nuevo descarta lo que quedaba por
> rehacer**: a partir de ahí esa rama ya no existe.

> Al **cambiar de canción la pila se vacía**: deshacer ahí mezclaría el mapa de
> una canción con el de la otra.

## Cuando el metrónomo va a destempo — `B`

Con **rejilla continua** (`L`, viene activada) cada sección se alinea con la
primera línea de compás de la anterior. Eso evita saltos… **siempre que la
sección anterior mida un número entero de compases**.

Si no lo mide, la siguiente no puede empezar donde la pusiste: se va a la
línea de compás siguiente. Y como cada origen se calcula desde el anterior,
el error se arrastra.

El panel lo dice con números: **cuántos compases mide el tramo** y **cuánto se
desplaza el siguiente**, en ms y en pulsos. Por encima de 30 ms avisa en rojo,
porque ahí ya se oye.

**`B` cuadra el borde**: lo pega a la línea de compás más cercana de la sección
anterior y deja la desviación en cero — lo que ves es lo que suena.

> `B` **mueve el borde**. Si estás seguro de dónde cambia la música, entonces
> el que está mal es el BPM: afínalo con **↑↓←→** hasta que el tramo mida
> compases enteros, y el borde se queda donde está.

## La canción solo suena si la pides

Clicar mueve el cursor y nada más. `P` reproduce, **desde donde esté el cursor**.

## Otras teclas

| Tecla | Qué hace |
|---|---|
| **P** | Sonar / parar |
| **`[` `]`** | Canción anterior / siguiente |
| **ESC** | Salir |

## Lo que todavía no hace

- **Las secciones** (calma / base / estribillo) se guardan vacías. Hay que añadirlas a mano al JSON o ampliar la herramienta.
- **No hay detección automática** de notas. Es a mano a propósito: tú decides qué cuenta como melodía.

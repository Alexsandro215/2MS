# Grabador de temas

Marca a mano lo único de una canción que **no se puede calcular**: la melodía.

El pulso y el compás salen solos del BPM. La melodía no sale de ningún sitio — hay que marcarla.

## Cómo se usa

1. Deja tus canciones en **`audio/`** (`.mp3`, `.ogg` o `.wav`).
2. Doble clic en `LANZAR.bat`.
3. Fase 1, fase 2, guardar.

## Una sola pantalla, sin modos

Cada tecla hace **siempre** lo mismo. No hay fases.

### Lo que se graba

| Tecla | Qué hace |
|---|---|
| **ESPACIO** | Marca una nota de la melodía |
| **RETROCESO** | Borra la última |
| **SUPR** | Vacía todas |

Es lo único que se graba. Todo lo demás se calcula.

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

# Guion — Historia Principal

La Historia Principal de `BIOGRAFIAS 8de10.docx` pasada a **formato de guion para juego 3D**.

- La historia se edita en `BIOGRAFIAS 8de10 - EDITABLE.docx`. Estos `.md` son una copia formateada del 24/10/2026: si cambias la historia en el Word, no se actualizan solos.
- `NOTAS.md` tiene el resumen de cada capítulo y las sugerencias.
- `GUION - Historia Principal.docx` se genera desde estos `.md` con `python tools/build_guion.py`.

---

## Convenciones

### Estructura

```
# CAPÍTULO 1 — DONDE NACE TODO           ← un capítulo por archivo
## 23 DE ENERO DE 2017                   ← cada día, tal como lo escribe el texto
### 1.4 · INT. TREN HACIA LA E.N.M.A. — ATARDECER   ← cada escena
```

- **Encabezado de escena:** `INT.` (interior) o `EXT.` (exterior), el **lugar** y el **momento** (MAÑANA, TARDE, ATARDECER, NOCHE, o una hora: `7:30 AM`). Si el texto no dice dónde ocurre y no es obvio, `LUGAR SIN DEFINIR`.
- Las escenas se numeran por capítulo: `1.1`, `1.2`…
- Los días conservan el día de la semana que pone el texto, aunque no coincida con el calendario real (ver `NOTAS.md`).

### Acción

Párrafos cortos, en presente, con lo que se **ve** y se **oye**:

```
Itzela cae de cabeza en el asiento de al lado. Algunos pasajeros quedan aturdidos.
```

### Diálogo

```
**ITZELA**
*(emocionada, con la boca llena)*
¡Wow, un pianista!
```

- El nombre, en **MAYÚSCULAS y negrita**, en su propia línea.
- La acotación, entre paréntesis y en cursiva, debajo del nombre: solo el tono o un gesto corto. Lo que dura más va como acción.
- Pensamientos: `**ALEXTSU** *(pensando)*`. Voz sin cuerpo (altavoz, teléfono, recuerdo): `*(off)*`.
- Personajes sin nombre: `ESTUDIANTE 1`, `GUARDIA`, `???`.

### Marcadores de juego

Cada uno en su propia línea, en negrita y entre corchetes:

| Marcador | Uso |
|---|---|
| `**[CINEMÁTICA]**` | Escena sin control del jugador |
| `**[DIÁLOGO]**` / `**[FIN DEL DIÁLOGO]**` | Conversación en modo diálogo |
| `**[JUGABLE]**` | El jugador recupera el control; se añade qué puede hacer |
| `**[EVENTO OPCIONAL]**` | Escena que el jugador puede ver o no |
| `**[ELECCIÓN]**` | Decisión del jugador (ver abajo) |
| `**[LOGRO: «Nombre»]**` | Logro y su condición |
| `**[PRESTIGIO +5]**` | Cambios de prestigio u otras stats |
| `**[COMBATE: …]**` | Inicio de un combate |
| `**[TUTORIAL: …]**` | Introducción a una mecánica |
| `**[MÚSICA: …]**` / `**[SONIDO: …]**` | Pieza o efecto |
| `**[PLANO: …]**` | Indicación de cámara |
| `**[TRANSICIÓN: …]**` | Fundido, corte a negro, paso de tiempo |
| `**[FIN DEL DÍA]**` | Cierre del día |
| `**[NOTA DE PRODUCCIÓN: …]**` | Notas de trabajo del autor (storyboard…) |
| `**[PENDIENTE: …]**` | Lo que el texto marca como "aún sin definir" |

### Elecciones

```
**[ELECCIÓN]** ¿Seguir en la plaza después del dolor de cabeza?

#### Opción A — «Sí»
…bloques de guion…

#### Opción B — «No»
…bloques de guion…

**[FIN DE LA ELECCIÓN]**
```

### Bloques de diseño

Explicaciones de sistemas dentro de la historia (la lista del día, el prestigio, el menú de helados, las stats de un combate):

```
> **[DISEÑO] Lista del día**
> Texto del sistema, con sus viñetas o tablas.
```

### Referencias a la fuente

Antes de cada bloque, un comentario invisible con los párrafos del docx de los que sale: `<!-- §2467-2469 -->`. Son números del original (`BIOGRAFIAS 8de10.docx`); en el EDITABLE no coinciden (la diferencia cambia al editar), así que allí hay que buscar por el texto. Sirve para volver al original y comprobar que no se perdió nada. Si algo se quitó a propósito: `<!-- §2876 omitido: motivo -->`.

### Fidelidad

- **No cambia la historia.** Pasa exactamente lo mismo y se dice lo mismo.
- **Sí se corrige** la ortografía, las tildes y la puntuación, y se separa la acción del diálogo.
- **No se corrigen** las incoherencias de continuidad: se anotan en `NOTAS.md`.

---

## Ejemplo

Original (párrafos 2467-2476):

> Sin embargo una chica cae junto a Alextsu, ambos golpeándose en la cabeza, pero Itzela es la que cae de cabeza en el asiento de alado de Alextsu. Provocando algo de confusión.
> El Altavoz suena. - Disculpen los problemas generados por la turbulencia del Tren. […]
> Alextsu algo confundido, sin entender porque carece de dolor por el golpe de cabeza […]- (¿Acaso no le dolió el golpe contra el piso?)- ¿Estas segura? Caíste de cabeza y…

Guion:

```
<!-- §2467-2469 -->
El tren cae en un bache muy fuerte y frena en seco. Una chica que pasaba cae junto a Alextsu: los dos se golpean la cabeza, e ITZELA termina de cabeza en el asiento de al lado. Algunos pasajeros quedan aturdidos; otros, algo lastimados.

<!-- §2470 -->
**ALTAVOZ** *(off)*
Disculpen los problemas generados por la turbulencia del tren. En un momento nos pondremos en marcha de nuevo. Si requieren asistencia, pronto las azafatas les ayudarán.

<!-- §2476 -->
Alextsu la mira, confundido: a ella no parece dolerle nada.

**ALEXTSU** *(pensando)*
¿Acaso no le dolió el golpe contra el piso?

**ALEXTSU**
¿Estás segura? Caíste de cabeza y…
```

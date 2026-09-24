# Grabador de temas

Marca a mano lo único de una canción que **no se puede calcular**: la melodía.

El pulso y el compás salen solos del BPM. La melodía no sale de ningún sitio — hay que marcarla.

## Cómo se usa

1. Deja tus canciones en **`audio/`** (`.mp3`, `.ogg` o `.wav`).
2. Doble clic en `LANZAR.bat`.
3. Fase 1, fase 2, guardar.

## Fase 1 — La rejilla

Aquí no grabas nada: **configuras**. De estos tres números sale todo lo demás.

| Tecla | Qué hace |
|---|---|
| **↑ ↓** | BPM ±1 |
| **← →** | BPM ±0,1 |
| **T** | Golpea al ritmo cuatro o más veces y deduce el BPM solo |
| **C** | Cambia el compás: 3/4 → 4/4 → 6/4 → 5/4 → 7/4 |
| **ESPACIO** | Marca dónde cae el **primer tiempo fuerte** |
| **M** | Metrónomo on/off |

El metrónomo suena distinto en el primer tiempo de cada compás. **Ajusta hasta que encaje con la canción** — cuando ya no lo oigas «desfasado», la rejilla está bien.

En la línea de tiempo de abajo, las rayas naranjas son compases y las azules pulsos.

**ENTER** para pasar a la fase 2.

## Fase 2 — La melodía

Suena la canción y **pulsas ESPACIO en cada nota** de la línea melódica.

| Tecla | Qué hace |
|---|---|
| **ESPACIO** | Marca una nota |
| **RETROCESO** | Borra la última |
| **SUPR** | Vacía todas |
| **ENTER** | Volver a la rejilla |

Los puntos verdes de la línea de tiempo son tus marcas.

No hace falta clavarlo: se aplica automáticamente **la calibración medida en la prueba del reloj de audio** (−27,6 ms en la máquina principal), así que se guarda donde *oíste* la nota, no donde *pulsaste*.

## Guardar

**S**. Escribe `temas/<nombre>.json`:

```json
{
  "id": "boss_artur",
  "bpm": 132.0,
  "tiemposPorCompas": 4,
  "offset": 0.1840,
  "secciones": [],
  "melodia": [0.45, 0.68, 0.91, 1.13, ...]
}
```

Ese formato es el que consume `TemaMusical` en el núcleo. Si vuelves a abrir la misma canción, **recupera lo que ya habías grabado**.

## Otras teclas

| Tecla | Qué hace |
|---|---|
| **P** | Sonar / parar |
| **`[` `]`** | Canción anterior / siguiente |
| **ESC** | Salir |

## Lo que todavía no hace

- **Las secciones** (calma / base / estribillo) se guardan vacías. Hay que añadirlas a mano al JSON o ampliar la herramienta.
- **No hay detección automática** de notas. Es a mano a propósito: tú decides qué cuenta como melodía.

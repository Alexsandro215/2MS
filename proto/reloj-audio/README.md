# Prueba del reloj de audio

> **No es parte del juego.** Es la prueba que decide la elección de motor.
> Ver [`docs/arquitectura.md`](../../docs/arquitectura.md) §5.

Responde a la única pregunta del proyecto que no se puede contestar leyendo un documento: **¿puede este motor mantener un pulso enganchado a la música durante una canción entera, en esta máquina concreta?**

## Cómo se lanza

Doble clic en `LANZAR.bat`. O desde Godot: *Importar* → este directorio.

| Tecla | Qué hace |
|---|---|
| **ESPACIO** | Golpear al ritmo del clic |
| **R** | Reiniciar la medición |
| **S** | Guardar informe a disco (la ruta sale por consola) |

## Qué mide

Corre **tres relojes a la vez**, a propósito:

1. **Reloj de audio** — `get_playback_position() + get_time_since_last_mix() − get_output_latency()`. Es el correcto, y esa suma de tres términos es todo el secreto: la posición que reporta el reproductor está cuantizada al bloque de mezcla, así que hay que sumarle lo transcurrido desde esa mezcla y restarle lo que el sonido tarda en salir por los altavoces.
2. **Reloj del sistema** — monótono, independiente del audio.
3. **Reloj de fotogramas** — acumular `delta` cada frame. **Es el método incorrecto**, y está ahí para que veas cuánto se separa.

## Qué mirar

| Cifra | Qué significa | Qué es aceptable |
|---|---|---|
| **Latencia de salida** | Lo que reporta el driver | 10–40 ms es normal en audio integrado |
| **Desfase medio** | Tu offset personal. No es un fallo: es lo que hay que calibrar | Cualquiera, mientras sea **estable** |
| **Jitter** | Cuánto varía golpe a golpe | Por debajo de ~15 ms |
| **Deriva del reloj de frames** | Cuánto miente el método malo | Crecerá sin parar. Ese es el punto |
| **Deriva del sistema vs audio** | Si el audio se descuelga del tiempo real | Debe quedarse plana |

**La cifra que decide es la última.** Si a los tres minutos el reloj de audio sigue pegado al reloj del sistema, Godot sirve. Si se va, no sirve — y mejor saberlo ahora que en el año tres.

## La pista

`audio/clics_120bpm.wav` está generada con `generar_clics.py` y es **exacta a la muestra**: el pulso N cae en `round(N × 60/BPM × 44100)`, con 0.000000000 s de error acumulado a los 3 minutos.

Por eso se mide con clics y no con una canción: para medir hace falta saber con precisión de muestra dónde cae cada pulso, y mapear una canción real mete error justo donde queremos medir el hardware.

Otros tempos:

```
python generar_clics.py --bpm 128 --seg 240
```

## Fase 2: con música de verdad

Cuando la fase 1 dé buenos números, se mete una canción real en `audio/` y se cambia `PISTA_POR_DEFECTO` y `BPM_POR_DEFECTO` en `RelojAudio.gd`. Ahí ya no se mide: se juzga si **se siente** bien.

## Las tres máquinas

Ejecútalo en las tres y guarda el informe de cada una con **S**. Tres equipos con hardware de sonido distinto dan tres latencias distintas — y esa variación es exactamente lo que hay que absorber con calibración. Es una ventaja que casi ningún desarrollador en solitario tiene a mano.

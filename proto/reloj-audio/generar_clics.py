# -*- coding: utf-8 -*-
"""
Genera una pista de clics con BPM exacto para medir el reloj de audio.

Por que no una cancion: para MEDIR hace falta saber con precision de muestra
donde cae cada pulso. En una cancion real hay que mapearla primero, y ese
mapeo introduce error justo donde queremos medir el hardware.

Aqui el pulso N cae exactamente en la muestra round(N * 60/BPM * SR).
Cero ambiguedad.

Uso:
    python generar_clics.py                 # 120 BPM, 3 minutos
    python generar_clics.py --bpm 128 --seg 240
"""
import argparse
import math
import os
import struct
import wave

import numpy as np

SR = 44100          # frecuencia de muestreo
CANALES = 1
ANCHO = 2           # 16 bits


def click(sr, freq, dur_ms, amp):
    """Un clic corto: seno con caida exponencial. Ataque nitido, sin cola."""
    n = int(sr * dur_ms / 1000.0)
    t = np.arange(n) / sr
    env = np.exp(-t * (6000.0 / dur_ms))
    return (np.sin(2 * math.pi * freq * t) * env * amp).astype(np.float32)


def generar(bpm, segundos, compas, ruta):
    total = int(SR * segundos)
    pista = np.zeros(total, dtype=np.float32)

    # Dos timbres: el 1 del compas mas agudo, para que se oiga la estructura
    fuerte = click(SR, 1760.0, 25, 0.85)   # La6
    debil = click(SR, 880.0, 18, 0.55)     # La5

    periodo = 60.0 / bpm
    pulsos = int(segundos / periodo)
    marcas = []

    for i in range(pulsos):
        # Posicion exacta en muestras. Sin acumular error de coma flotante:
        # se calcula desde el indice, no sumando periodos.
        muestra = int(round(i * periodo * SR))
        c = fuerte if (i % compas == 0) else debil
        fin = min(muestra + len(c), total)
        if muestra >= total:
            break
        pista[muestra:fin] += c[: fin - muestra]
        marcas.append((i, muestra, muestra / SR))

    # Normaliza sin recortar
    pico = float(np.max(np.abs(pista))) or 1.0
    pista = (pista / pico) * 0.9

    pcm = (pista * 32767).astype(np.int16)
    with wave.open(ruta, "wb") as w:
        w.setnchannels(CANALES)
        w.setsampwidth(ANCHO)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())

    return marcas


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--bpm", type=float, default=120.0)
    p.add_argument("--seg", type=int, default=180)
    p.add_argument("--compas", type=int, default=4)
    p.add_argument("--salida", default=None)
    a = p.parse_args()

    base = os.path.dirname(os.path.abspath(__file__))
    ruta = a.salida or os.path.join(base, "audio", "clics_%gbpm.wav" % a.bpm)
    os.makedirs(os.path.dirname(ruta), exist_ok=True)

    marcas = generar(a.bpm, a.seg, a.compas, ruta)

    print("Generado: %s" % ruta)
    print("  BPM         : %g" % a.bpm)
    print("  Periodo     : %.6f s  (%.3f ms)" % (60.0 / a.bpm, 60000.0 / a.bpm))
    print("  Duracion    : %d s" % a.seg)
    print("  Pulsos      : %d" % len(marcas))
    print("  Compas      : %d/4" % a.compas)
    print()
    print("  Primeros pulsos (indice, muestra, segundo):")
    for i, m, s in marcas[:5]:
        print("    %3d  %9d  %8.6f" % (i, m, s))
    print("  Ultimo pulso:")
    i, m, s = marcas[-1]
    print("    %3d  %9d  %8.6f" % (i, m, s))
    print()
    print("  Error acumulado al final: %.9f s" % abs(s - (len(marcas) - 1) * 60.0 / a.bpm))


if __name__ == "__main__":
    main()

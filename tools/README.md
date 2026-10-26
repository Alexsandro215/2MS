# Herramientas

Scripts que insertan la documentación de diseño dentro del `.docx`.

> **Desde el 24/10/2026, el documento en el que se trabaja es `BIOGRAFIAS 8de10 - EDITABLE.docx`.** Es una copia del TRABAJO que se edita a mano en Word, y ningún script la toca. `build_docx.py` sigue regenerando el TRABAJO, pero esos cambios ya no llegan al EDITABLE: si cambias un `.md` de `docs/` y lo quieres en el Word, hay que pasarlo a mano.

## `build_docx.py` — el que se usa

Reconstruye `BIOGRAFIAS 8de10 - TRABAJO.docx` **desde cero** a partir del original en cada ejecución, así que es **idempotente**: se puede correr las veces que haga falta y nunca duplica contenido.

```
python tools/build_docx.py
```

Qué hace:

1. Copia `BIOGRAFIAS 8de10.docx` → `BIOGRAFIAS 8de10 - TRABAJO.docx`
2. Localiza los títulos ancla por texto y **borra el bloque antiguo** entre ellos
3. Convierte el markdown a OOXML y lo inserta en su sitio:

| Ancla en el docx | Sustituido por |
|---|---|
| ESTRUCTURA DIARIA DEL TIEMPO | `docs/dia.md` |
| FUNDAMENTOS DEL SISTEMA DE COMBATE | `docs/manual-tecnico.md` |

4. Añade títulos H1 nuevos: **PASIVAS Y RECUERDOS**, **EFECTOS EMOCIONALES (E.EM.)**, **LAS RUNS**

> ⚠️ **El original `BIOGRAFIAS 8de10.docx` no se toca nunca.** Todo el trabajo va sobre la copia.

## `md2docx.py`

El conversor de markdown a OOXML que usa el anterior: títulos, tablas, listas, negrita, código y citas.

## Requisitos

```
pip install python-docx
```

## `memoria/`

Notas de contexto del proyecto — respaldo para no perderlas al reinstalar el sistema.

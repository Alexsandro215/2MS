---
name: enma-flujo-docs-a-word
description: En E.N.M.A los .md de docs/ son la fuente de verdad; el .docx TRABAJO es artefacto generado por tools/build_docx.py
metadata: 
  node_type: memory
  type: project
  originSessionId: 3cdceb12-f523-4def-9422-6d0aafd2ce30
  modified: 2026-09-21T00:37:33.605Z
---

En E.N.M.A el flujo es **una sola dirección**: `docs/*.md` → `BIOGRAFIAS 8de10 - TRABAJO.docx`, vía `tools/build_docx.py`. El original `BIOGRAFIAS 8de10.docx` **no se toca nunca**; el script lo copia desde cero en cada ejecución, así que es idempotente.

**Why:** es fácil confundirse y tratar el Word como fuente, porque es donde el usuario lee. No lo es: editarlo a mano se pierde en la siguiente reconstrucción. Y un valor caducado en un `.md` se imprime en el Word sin avisar — pasó con el `0.01 × stacks`, que llegó al papel porque `manual-tecnico.md` iba por detrás de `formulas.md`.

**How to apply:** corregir siempre el `.md`, luego reconstruir. Si el usuario dice "el Word está actualizado", suele tener razón sobre la sincronización — el desfase real casi siempre está **entre documentos `.md`**, no entre `.md` y Word. Antes de afirmar que falta algo en el Word, medirlo: extraer el texto del docx y comparar frases contra el `.md`. Cuidado con los falsos positivos al comparar (enlaces markdown, `###` dentro de blockquotes, `~~tachado~~`).

Requiere `pip install python-docx`. El script inserta 7 documentos: `dia` y `manual-tecnico` en anclas existentes, y `pasivas`, `emociones`, `impacto`, `runs`, `equipo` en secciones propias. Los demás `.md` (`formulas`, `musica`, `tablero`, `elementos`, `estilos`, `arquitectura`, `conflictos`, `gdd`) **no van al Word** a propósito.

Ver también [[enma-proyecto-de-vida]].

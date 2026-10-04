# E.N.M.A. — *Two Souls Musicals*

RPG táctico por turnos con capa musical. Combate posicional en línea, habilidades tipo QWER y un sistema de recurso (**Carga Musical**) que depende del ritmo.

> **Empieza por [`docs/gdd.md`](docs/gdd.md)** — es el índice de todos los documentos y dice cuál es canónico y cuál está superado.

---

## Documentación de diseño

| Documento | Contenido |
|---|---|
| [`docs/manual-tecnico.md`](docs/manual-tecnico.md) | **Manual formal completo** — la referencia principal |
| [`docs/formulas.md`](docs/formulas.md) | Cálculo: stats, acierto, daño, CM, desgaste |
| [`docs/arquitectura.md`](docs/arquitectura.md) | Forma del sistema, capas, esquema único de reglas |
| [`docs/tablero.md`](docs/tablero.md) | Casillas, movimiento, alcance, anclaje |
| [`docs/musica.md`](docs/musica.md) | Tempo, ritmo, modo CLASICAL |
| [`docs/impacto.md`](docs/impacto.md) | Efectos de Impacto (desde el arma) |
| [`docs/elementos.md`](docs/elementos.md) | Los 9 efectos elementales |
| [`docs/emociones.md`](docs/emociones.md) | E.EM. — efectos emocionales |
| [`docs/equipo.md`](docs/equipo.md) | Arma · Armadura · Botas · Accesorio |
| [`docs/pasivas.md`](docs/pasivas.md) | Pasivas y recuerdos |
| [`docs/dia.md`](docs/dia.md) | El bucle fuera del combate |
| [`docs/runs.md`](docs/runs.md) | Estructura de las expediciones |
| [`docs/estilos.md`](docs/estilos.md) | Los 20 kits QWER |
| [`docs/lore-contexto.md`](docs/lore-contexto.md) | Mundo, distritos, estructura social |
| [`docs/conflictos.md`](docs/conflictos.md) | Divergencias pendientes de decidir |

## Código

| Ruta | Qué es |
|---|---|
| `src/EnmaCore` | El núcleo determinista: stats, reglas, combate, música |
| `src/EnmaCore.Tests` | Los tests (`dotnet test src/Enma.slnx`) |
| `src/EnmaSim` | Simulador: juega combates enteros (`dotnet run --project src/EnmaSim`) |
| `proto/combate` | Ventana de combate en Godot: el equipo de prueba contra un muñeco (`LANZAR.bat`) |
| `proto/grabador-temas` | Editor de temas musicales en Godot |

## Fuentes originales

| Ruta | Qué es |
|---|---|
| `BIOGRAFIAS 8de10.docx` | **Original — no tocar.** Historia y sistema |
| `BIOGRAFIAS 8de10 - TRABAJO.docx` | Copia de trabajo, con el manual técnico ya insertado |
| `drive-download-.../*.txt` | Los 7 documentos MASTER en PlantUML |
| `Calculador de Stats OFICIAL*.xlsx` | Hoja de cálculo de escalado |

---

## Convenciones

- ✅ **RESUELTO** · 🔨 **POR CONSTRUIR** · ❓ pendiente de decisión · ⚠️ valor provisional
- Los **MASTER en PlantUML** son canónicos cuando contradicen al levantamiento hablado.
- Todo valor numérico marcado con ⚠️ está sujeto a balanceo.

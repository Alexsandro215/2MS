# E.N.M.A — Arquitectura

> Sin código todavía. Este documento fija **la forma** del sistema para que añadir contenido nunca obligue a tocar el núcleo.

---

## 1. El hallazgo: casi todo el juego tiene la misma forma

Mirando los sistemas ya diseñados, **todos son la misma estructura escrita de maneras distintas**:

| Sistema | Disparador | Condición | Efecto |
|---|---|---|---|
| **Pasiva** | siempre | HP < X% | +X% AD |
| **Gadget** | `ON_BEING_HIT` | villano y CM > 80% | fijar CM al 50% |
| **Emblema** | conteo de equipo | breakpoint 3 / 6 / 9 | +1 BA al equipo |
| **Emoción** | evento del día | personaje = Kali | −50% puntería turno 1 |
| **Elemento** | marca aplicada | 2 marcas, 2 personajes | detonar L2 |
| **Terreno** | actuar en la casilla | estar ahí al actuar | −X% PRES, AD, AP |
| **Comida** | usar objeto | ventana pre-ofensiva | buff / debuff |
| **Casilla** | estar en ella | posición = OFENSIVA | +30% PRES, −30% EVA |

**Ocho sistemas, un solo esquema.** Si se escribe una vez y todo lo demás son filas de esa tabla, añadir contenido **nunca toca el núcleo**.

```
Regla = { fuente · disparador · condición · efecto · capa · duración · alcance }
```

Esto no es una propuesta nueva: RESOLUTIONATOR ya lo tenía a medias con `ModifierRule` (`sourceType`, `trigger`, `mode`, `statKey`, `value`). Lo que falta es **comprometerse a que sea el único mecanismo**.

> ### La prueba de fuego
>
> **El núcleo no debe saber qué es un "gadget".** Solo conoce reglas y eventos. Si mañana inventas un sistema nuevo —reliquias, tatuajes, lo que sea— es contenido, no código.
>
> Si en algún momento hay un `if (esGadget)` en el resolvedor, la arquitectura ya se rompió.

---

## 2. La segunda categoría: reglas que cambian el FLUJO

No todo encaja en "modificar un valor". Hay efectos que alteran **la estructura del turno**:

| Ejemplo | Qué rompe |
|---|---|
| **Artens W** — *"puedes hacer el doble de habilidades o básicos este turno"* | El número de acciones |
| **Jaens Jeans R** — *"invencible 3 turnos, no puedes hacer nada"* | El turno entero |
| **Lein-Xein R** — *"un básico que se repite hasta que falles"* | El bucle de impactos |
| **Anclaje** | El movimiento |
| **Preparación** | Aplaza la resolución N turnos |
| **Movimiento forzado** | La posición de otro |

Conviene **nombrarlas aparte** y que sean pocas y explícitas. Son la parte cara: cada una es un caso real en la máquina de estados.

> Si se meten a la fuerza en el esquema de modificadores, ese esquema se pudre. Mejor dos categorías limpias que una que lo abarca todo mal.

---

## 3. Capas

```
┌─ CONTENIDO ───────────────────────────────────────────┐
│  kits · elementos · emociones · emblemas · gadgets    │
│  recuerdos · temas musicales · enemigos               │   ← datos, se editan sin programar
└───────────────────────────────────────────────────────┘
┌─ REGLAS ──────────────────────────────────────────────┐
│  el esquema único de §1 + las de flujo de §2          │   ← el contrato
└───────────────────────────────────────────────────────┘
┌─ NÚCLEO DETERMINISTA ─────────────────────────────────┐
│  stats → acierto → daño → CM → estados → KO           │   ← puro, testeable sin gráficos
└───────────────────────────────────────────────────────┘
┌─ PRESENTACIÓN ────────────────────────────────────────┐
│  render · audio · input rítmico · UI                  │   ← lo único que necesita motor
└───────────────────────────────────────────────────────┘
```

**El núcleo no debe importar nada de las capas de arriba ni de abajo.** Se le entrega un estado y una acción, y devuelve un estado nuevo más una lista de eventos. La presentación *observa* esos eventos; nunca los produce.

### Por qué esto importa aquí en concreto

- **Se puede simular sin juego.** Miles de combates automáticos para balancear los 20 kits. Con este diseño, imprescindible: a mano es inviable.
- **Los enemigos usan el mismo motor.** Ya lo decidiste (usan las mismas spells). Si el núcleo es puro, la IA es *otro cliente* del mismo resolvedor, no una copia paralela que se desincroniza.
- **Los tests son tablas.** Cada número de [formulas.md](formulas.md) es un caso de prueba listo.

---

## 4. Lo que ya está bien y hay que proteger

| Propiedad | Por qué es valiosa |
|---|---|
| **Una sola regla de agregación** | `StatFinal = StatOperativa + Σflats + (StatOperativa × Σpcts)`. Un único punto de cálculo. Ya se rechazó la excepción multiplicativa de las pasivas justamente por esto. |
| **Orden de resolución explícito** | El pipeline por impacto está escrito paso a paso. Elimina la ambigüedad más cara de un juego de combate. |
| **Jerarquía de autoridad declarada** | *"Ante contradicción prevalece STATS MASTER"*. Con varios documentos vivos, esto es lo que evita la deriva. |
| **Ninguna stat es "mejor cuanto más baja"** | Regla ya adoptada con Tempo. Mantenerla para toda stat futura. |
| **Pool de CM fijo** | La capacidad no escala; escala el poder adquisitivo. Un número que el jugador aprende una vez. |

---

## 5. Los dos relojes

Es la única dificultad técnica real del proyecto.

```
NÚCLEO POR TURNOS     discreto · determinista · sin prisa
CAPA RÍTMICA          continua · tiempo real · atada al audio
```

**El reloj debe ser el audio, no los fotogramas.** Si el pulso se deriva del frame rate o de un temporizador del sistema, deriva respecto a la canción y el jugador siente que el juego miente. Es el fallo clásico del género y aquí hundiría la identidad entera.

**Consecuencia para la elección de motor:** el criterio no es qué motor conoces mejor, sino **cuál te da un reloj de audio fiable con calibración de latencia**. Esa decisión debería elegir el motor, y no al revés.

> **Prueba antes de comprometerse:** una canción, un metrónomo visual, detectar si el input cae dentro de la ventana. Sin combate, sin personajes. Media tarde de trabajo, y decide la pieza más cara de revertir del proyecto.

---

## 6. Decisiones técnicas pendientes

❓ Plataforma (navegador · escritorio · móvil)
❓ Motor y lenguaje
❓ 2D o 3D · tipo de vista
❓ Formato de los datos de contenido (JSON, tablas, hoja de cálculo exportada…)
❓ Si el Excel actual se convierte en la fuente de datos o sigue siendo solo herramienta de comprobación

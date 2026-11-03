# E.N.M.A — Pasivas y Recuerdos

> **Una pasiva es un modificador condicionado.** No se activa, no gasta turno, no gasta CM: existe y se comprueba.

```
CONDICIÓN  →  EFECTO
```

> ### ✅ Sistema único
> **No existen los "rasgos" como sistema aparte.** Todo lo que antes se llamaba *rasgo* es una **pasiva**. Lo que cambia entre unas y otras es **de dónde vienen** y **cuánta potencia tienen**, no su mecanismo.
>
> *(La palabra "rasgo" queda obsoleta. Aparece en el documento de biografías y en `SourceType.TRAIT` de RESOLUTIONATOR; léase como pasiva.)*

---

## 1. Reglas base

| Regla | Detalle |
|---|---|
| **Personajes jugables** | **1 pasiva**, la del **recuerdo** equipado |
| **Enemigos** | **1 o más** pasivas |
| **Jefes** | Pasivas **más potentes que la media** — no son otra categoría, solo son más fuertes |
| **Origen (jugables)** | Objetos: los **RECUERDOS**. No son innatas |
| **Intercambio** | Sí, cambiando el recuerdo equipado |
| **Escalado** | El `X%` escala con el nivel como cualquier stat (`MAX × g(L)`) |
| **Apilamiento** | **Aditivo.** Leen la misma StatOperativa ([formulas.md](formulas.md) §2) |

> ### La pasiva es un slot de build, no la identidad del personaje
> Como viene de un objeto y solo hay una, **el mismo personaje juega distinto según el recuerdo que lleve**. Es una decisión de construcción por run, igual que los emblemas de club.
>
> Y como los enemigos pueden llevar varias, **la dificultad de un encuentro se expresa en pasivas**, no solo en estadísticas. Un enemigo con tres pasivas fuertes es un problema distinto a uno con el doble de vida.

**Encaje con el canon:** los recuerdos ya existían. RESOLUTIONATOR los lista en el orden de recolección de modificadores (*"accesorios / recuerdos / emblemas"*) y `SourceType` incluye `MEMORY`.

---

## 2. Qué puede ser una pasiva

Cualquier cosa manipulable matemáticamente o sobre el campo de juego.

| Familia | Ejemplo |
|---|---|
| **Modificador de stat** | +X% de AD · +X% de AP |
| **Modificador de sistema** | Reducir los básicos necesarios por carga de CM |
| **Meta / run** | +X% de EXP al terminar la batalla |
| **Control de campo** | **Muralla**: nadie puede atacar a otros aliados excepto este |
| **Inmunidad** | Inmune a ataques físicos, mágicos, elementos o emociones |
| **Reacción** | Al recibir daño, al dejar K.O., al evadir, al criticar |

La familia de **control de campo** es la que sostiene la protección de la retaguardia. Y es **condicional**, así que se desactiva sin hacerle un punto de daño al portador — bajándolo del umbral de vida, o sacándolo de su casilla si la condición es posicional.

> ### Las pasivas son el pegamento del juego
> *"Otro mago contigo en la defensiva"* cruza **arquetipo** + **posición** + **composición de equipo** en una sola línea. *"¡NO CEDAN!!!"* toca directamente el contador de CM.
>
> Son donde los sistemas ya existentes se tocan entre sí **sin inventar reglas nuevas**. Por eso tenía sentido dejarlas para el final: cuanto más completo está el resto, más ganchos hay de los que tirar.

---

## 3. Apilamiento: ADITIVO

Misma regla de agregación que todo lo demás:

```
StatFinal = StatOperativa + Σ Flats + (StatOperativa × Σ Pcts)
```

**Ejemplo** — AD operativo 100, pasiva +30%, buffs +50%:

| | Resultado |
|---|---|
| **Aditivo (oficial)** | 100 + 30 + 50 = **180** |
| ~~multFinal~~ | ~~(100 + 50) × 1.30 = 195~~ |

> **Por qué aditivo.** La diferencia es de un 8% con una sola pasiva — marginal. A cambio, mantener **una única regla de agregación** evita el bug silencioso que aparece cuando dos capas conviven y alguien olvida cuál aplica.
>
> `multFinal` sigue reservada para casos excepcionales. Si una pasiva concreta necesita multiplicar, **lo declara ella misma**.

---

## 4. Potencia

Las pasivas **no se clasifican por categoría, sino por potencia**. Una pasiva de jefe no es otra cosa: es una pasiva **más fuerte que la media**.

| Portador | Cuántas | Potencia |
|---|---|---|
| Jugable | 1 (del recuerdo) | Media |
| Enemigo común | 1 o más | Media |
| Jefe | 1 o más | **Por encima de la media** |

> Por eso la dificultad de un encuentro se expresa **en pasivas**. Un enemigo con tres pasivas fuertes es un problema distinto a uno con el doble de vida — y mucho mejor herramienta de diseño que subir números.

---

## 5. Catálogo — POR CONSTRUIR

🔨 **Se hará desde cero.**

El catálogo heredado del documento de biografías (las ~40 que figuraban como *"rasgos"*) **queda descartado**. Razones:

- Usa **nomenclatura antigua**: "fuerza" por AD, "especial" por skill QWER, carga musical individual en vez del pool compartido.
- Cinco de ellas estaban escritas para **"evasión/precisión" como una sola estadística**, que ya no existe — ahora son EVA y PRESS por separado.
- Varias asumen sistemas que han cambiado desde entonces.

Sigue estando en el documento de biografías si hace falta consultarlo como referencia histórica.

### Lo único que se conserva: los recuerdos con nombre

Son posteriores y ya usan la nomenclatura vigente.

| Recuerdo | Pasiva | Condición | Efecto |
|---|---|---|---|
| **Medalla del Ejército Korkakov** *(Emilia)* | **¡NO CEDAN!!!** | Permanente | Reduce un X% los BA necesarios para obtener una carga de CM |
| — | **Corazón de Acero** | HP por debajo de X% | +X% de AD a ataques **y** skills |
| — | **Aficionado a la experiencia laboral** | Al terminar la batalla | +X% de EXP |
| — | **¿Mago? ¡MAGOOOOO!** | Otro mago contigo en **DEFENSIVA** | +X% de AP |
| — | *(el "muro")* | Por encima de X% de vida | **Impide atacar a los demás enemigos** |

### Qué debe cubrir el catálogo nuevo

Para que el sistema haga su trabajo, conviene que haya pasivas de estas familias:

| Familia | Para qué sirve |
|---|---|
| **Modificador de stat** | La base. +X% de AD, AP, ARM… |
| **Modificador de sistema** | Tocar contadores: CM por básico, coste de preparaciones, EXP |
| **Reacción** | Al recibir daño, al dejar K.O., al evadir, al criticar |
| **Control de campo** | El *muro* y familia — sostiene la protección de la retaguardia |
| **Inmunidad** | La respuesta a cada vía de ataque: físico, mágico, E.E., E.EM. |
| **Meta / run** | Efectos fuera del combate |

> **Las inmunidades son las más importantes de calibrar.** Hay cuatro vías de ataque —físico, mágico, elemental y emocional— y una inmunidad por cada una es una **declaración de qué estrategia no sirve aquí**. Una que anule los E.EM. invalida de golpe toda la preparación emocional de la semana del jugador.

---

## Pendientes

❓ ¿De dónde salen los **recuerdos**? ¿Botín, historia, lazos, misiones?
❓ ¿Persisten entre runs, como los emblemas guardados, o son temporales?
❓ ¿Un recuerdo lo puede llevar cualquier personaje, o están atados a alguien?
❓ ¿Se pueden **anular o suprimir** las pasivas enemigas?
✅ **Etiquetas de arquetipo:** definidas en [roles.md](roles.md) — 6 familias, rol principal · secundario · curva. (*Mago* sigue siendo válido como etiqueta de tipo de daño, aparte del rol.)
❓ ¿Cuántas pasivas lleva un enemigo normal frente a un jefe? ¿Hay tope?

---

## Nota de tono

El nombrado de los recuerdos es deliberadamente informal y con humor — *"¿Mago? ¡MAGOOOOO!"*, *"¡NO CEDAN!!!"*. Merece la pena mantenerlo: es de las pocas superficies donde se ve la voz del proyecto, frente a la densidad de siglas del resto.

Y los recuerdos **llevan lore**: *Medalla del Ejército Korkakov* referencia a la vez un estilo del roster y a un personaje, Emilia. El objeto cuenta de dónde viene la pasiva, no solo lo que hace.

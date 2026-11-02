# E.N.M.A — Las runs

> Una **run** es una expedición de combate. Vive dentro del día, ocupa **1 bloque** ([dia.md](dia.md)) y contiene **varias oleadas** encadenadas en un camino de nodos.
>
> Referente declarado: la estructura de camino de *Angry Birds Epic*.

---

## 1. Las dos estructuras

| | **ZONA DE OVERWORLD** | **ACUEDUCTOS (Historia Principal)** |
|---|---|---|
| Forma | **Camino fijo**, lineal | **Bifurcaciones** y caminos paralelos |
| Se libera | **Por secciones** | Cerrando la **fuente de corrupción** |
| Decisión central | **¿Hasta dónde sigo?** | **¿Dónde pago la dificultad: en la rama o en el jefe?** |
| Escalado | El nivel enemigo **sube conforme avanzas** | Depende de si la fuente está cerrada |

---

## 2. Zonas del overworld — empuja tu suerte

La corrupción ataca el overworld. Cada zona se libera **por secciones**, y cada sección es **un camino fijo de nodos**.

**Dónde y cuándo aparecen:** las zonas se generan **en días concretos**, siempre **en zonas abiertas o públicas** como calles, plazas o parques. **Nunca dentro de edificios**: sería modelar dos veces el mismo interior.

```
ENTRAS  →  ●──●──●──●  sección 1 completada  →  ¿sigues?
                          │                        │
                        SALIR                   SECCIÓN 2
                                                ●──●──●──●  →  ¿sigues?
                                                                  │
                                                             SECCIÓN 3…
```

### El límite lo decides tú

Al terminar una sección puedes **salir** o **seguir a la siguiente**. Nadie te obliga.

Pero seguir cuesta:

| Al encadenar secciones | Consecuencia |
|---|---|
| **Desgaste** | Sigue acumulándose. No baja hasta el amanecer |
| **Nivel enemigo** | **Sube conforme avanzas**, hasta el tope de **LV 18** |
| **Recursos** | El pool de CM, los consumibles y los escondites no se reponen |

> **Lo recomendable es irse.** Pero el que aguanta, libera más zona de una vez.

### La regla que lo hace tenso

> **Si no completas la sección en la que estás, esa sección no se libera.**

Es decir: puedes retirarte **entre secciones**, pero no **a mitad de una**. Si entras a la sección 3 y te retiras en el tercer nodo, esa sección sigue corrupta.

Eso convierte cada frontera de sección en una apuesta real: *tengo 60% de desgaste y el equipo tocado — ¿me llevo lo ganado, o entro a una más sabiendo que si me rajo la pierdo entera?*

### El escalado de nivel es el freno

Los enemigos no se quedan en LV1 mientras avanzas: **suben contigo**. Al llegar al tope están en **LV 18**.

Así que no es solo el desgaste lo que te echa — es que **cada sección extra es más dura que la anterior**, mientras tú estás cada vez peor.

---

## 3. Acueductos de la Historia Principal — el orden importa

Aquí el camino **se bifurca**. Hay una **zona donde nace la corrupción** y hay **caminos paralelos** que salen de ella.

```
              ┌── rama A ──●──●
              │
   ENTRADA ───┼── FUENTE DE CORRUPCIÓN ──●──●
              │
              └── rama B ──●──●
```

### Cerrar la fuente lo cambia todo

| Orden | Qué pasa |
|---|---|
| **Fuente primero** | Al cerrarla, **se libera el camino**. Las ramas quedan **mucho más manejables** — puedes recorrerlas **empezando en LV 1**. Incluso puedes salir después de cerrar la fuente y volver luego |
| **Ramas primero** | La corrupción **sigue manando**. Los caminos paralelos son **bastante más difíciles** |

### La rama no es un tesoro: es una LLAVE

Lo que hace que la bifurcación sea una decisión de verdad no es la recompensa, sino esto:

> **El jefe de la fuente puede exigir cierto artilugio, arma u objeto.** Y ese objeto está en una de las ramas.

Sin él, el jefe es **mucho más duro**. Con él, es abordable.

### La dificultad es constante — eliges dónde pagarla

| Orden | Dónde duele |
|---|---|
| **Rama primero, luego fuente** | La rama es dura (corrupción abierta), pero llegas al jefe **con la llave** |
| **Fuente primero, luego rama** | El jefe es duro (sin la llave), pero después la rama está **trivializada** |

> Las dos rutas son viables. Lo que cambia es **en qué punto del camino te toca sufrir**: en el desvío o en el jefe.
>
> Y como el desgaste no se recupera dentro de la run, la ruta larga —rama y luego jefe— llega al combate final **más gastada aunque mejor equipada**. Ahí está el equilibrio.

### El problema de información

Para que sea una elección y no una adivinanza, el jugador tiene que poder **saber o sospechar qué pide el jefe** antes de decidir.

❓ ¿Cómo se entera? Opciones naturales dentro de lo que ya existe:
- Un **lazo** que conoce la zona te lo cuenta
- El **periódico** o las peticiones de cartel lo insinúan
- **Explorar** la rama lo revela
- **Intentarlo y fallar** — el jefe te enseña lo que te falta, y vuelves preparado

---

## 4. Qué se lleva y qué se pierde

Recordatorio de [dia.md](dia.md) y EMBLEM MASTER:

| | Al terminar la run |
|---|---|
| **Emblemas guardados** | Persisten |
| **Emblemas temporales de run** | **Desaparecen** |
| **Emblemas antiguos** hallados | **Desaparecen** |
| **Objetos creados** en la run | Persisten hasta usarse |
| **Progreso de gadget `persistence = RUN`** | Se pierde |
| **Desgaste** | **Persiste** — solo baja al amanecer o en el hospital |
| **Emoción activa** | Sobrevive a la run; muere al final del día |
| **% de zona liberado** | Persiste |

> Esto es una capa **roguelite dentro del día**: entras con lo guardado, acumulas poder que solo existe ahí dentro, y al salir se evapora todo menos el progreso permanente.

---

## 5. Por qué las dos estructuras funcionan juntas

**La zona de overworld** es de **gestión de recursos**: sabes lo que hay, y la única pregunta es cuánto aguantas.

**El acueducto** es de **información y orden**: la pregunta es por dónde empezar.

Y las dos comparten el mismo precio: el **desgaste** que te vas a llevar a casa, y que se paga en **días** del calendario.

---

## Pendientes

❓ **¿Cuántos nodos tiene una sección?** Tres, cinco, variable.

❓ **¿Hay nodos que no sean combate?** Cofre, tienda, descanso, evento.

❓ **¿Qué determina el % de zona** que aporta cada sección? ¿Todas suman igual, o las más profundas dan más?

❓ **¿Cuánto sube el nivel enemigo por sección?** Y con qué curva hasta el tope de LV 18.

❓ **Si te retiras a mitad de sección**, ¿pierdes también el botín de esa sección, o solo la liberación?

❓ **El banquillo**, ¿viaja contigo a la run o se fija al entrar?

❓ ¿Se reponen **consumibles y escondites** entre secciones, o el camino entero comparte los mismos recursos?

❓ ¿**Cómo se entera el jugador** de qué objeto pide el jefe? (lazos, periódico, explorar, o fallar y volver)

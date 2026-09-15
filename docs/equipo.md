# E.N.M.A — Equipamiento

> Cuatro slots, cuatro dominios. Todo en **porcentaje**, todo con **contrapartida**.

---

## 1. Los cuatro slots

Cada slot responde **una pregunta**. Esa es la forma de recordar el sistema sin mirar la tabla.

| Slot | Pregunta | Dominio |
|---|---|---|
| **ARMA** | ¿Cuánto pego? | Ofensiva |
| **ARMADURA** | ¿Cuánto aguanto? | Defensa bruta |
| **BOTAS** | ¿Me dan? ¿Doy? | Posición y ritmo |
| **ACCESORIO** | ¿Con qué lo pago? | Recurso y sostenimiento |

Junto a ellos, fuera de este documento: **1 recuerdo** (pasiva), **1 gadget**, **4 emblemas**.

---

## 2. Regla del porcentaje

**Todo bonus de equipo es porcentual. Nunca plano.**

Un `+15 de ARM` vale mucho en LV1 y nada en LV18. Un `+20% de ARM` vale lo mismo, relativamente, toda la partida.

### La propiedad que lo hace funcionar

```
StatFinal = StatOperativa + Σ Flats + (StatOperativa × Σ Pcts)
```

Los porcentajes leen la **StatOperativa**, que ya escala con el nivel (`MAX × g(L)`).

> **Por eso el porcentaje de un objeto puede quedarse fijo para siempre y no envejecer nunca.** El objeto no escala: escala la base sobre la que se aplica. Una espada de +20% de AD vale lo mismo en LV1 que en LV18.
>
> Eso significa que **no hace falta versionar el equipo por nivel**. Un objeto es un objeto.

### La consecuencia a tener presente

El porcentaje **amplifica lo que ya eres; no arregla lo que te falta**.

| Personaje | ARM base | Armadura +20% | Le da |
|---|---|---|---|
| Tanque | 100 | +20% | **+20** |
| Caster | 10 | +20% | **+2** |

Un caster no se vuelve duro poniéndose una coraza: le multiplica una armadura que no tiene. **Esto refuerza arquetipos a propósito.**

### La excepción: conversión

Cuando haga falta tapar un hueco sin romper la regla, la herramienta es **leer una stat y dar otra**:

> *"+X% de tu HP base como ARM"*

Lee lo que sí tienes y lo convierte. Es la única vía de flexibilidad, y debe ser rara por eso mismo.

---

## 3. Regla del intercambio

**Toda pieza da y quita.** Sin excepción.

Precedente en el canon:
- *Lanza Simple* — **+20%** Daño Físico / **−5%** Precisión
- *Espada del Distrito 4* — **+30%** Daño Mágico / **−30%** Daño Físico

Sin contrapartida, la mejor pieza es la de números más altos y no hay decisión que tomar.

**El intercambio puede cruzar slots.** Unas botas pueden dar TEMPO y quitar ARM: la stat final es una sola, da igual de dónde venga cada parte.

### Frente a las pasivas

| | Qué hace |
|---|---|
| **Equipo** | Modificadores porcentuales **incondicionales**, con contrapartida |
| **Recuerdo (pasiva)** | `condición → efecto` |

Si una pieza de equipo lleva condiciones, se convierte en una pasiva y vuelven a existir dos sistemas haciendo lo mismo.

---

## 4. Reparto completo de estadísticas

Las 34 estadísticas tienen **una sola casa**. Ninguna huérfana, ninguna duplicada.

### ARMA — *¿cuánto pego?*

| Primarias | Secundarias |
|---|---|
| **AD** · **AP** · **CRÍTICO** | Velocidad de Ataque · Penetración de Armadura · Penetración Mágica · Dominio de Armas · Daño de Área · Daño Explosivo · Última Bala · Rebote · Preparación |

Todo lo que convierte tu acción en daño.

### ARMADURA — *¿cuánto aguanto?*

| Primarias | Secundarias |
|---|---|
| **ARM** · **RM** · **HP** | Eficiencia de Escudo · Extensión de Escudo · Extensión de Invulnerabilidad · Inquebrantable |

Todo lo que pasa **después** de que te acierten.

### BOTAS — *¿me dan? ¿doy?*

| Primarias | Secundarias |
|---|---|
| **PRESS** · **EVA** | **TEMPO** · Reflejos |

> ### Por qué las botas son el slot más interesante
>
> **PRESS y EVA son exactamente el eje del tablero.** Las casillas no mueven ninguna otra cosa ([tablero.md](tablero.md)). Las botas son, literalmente, el equipo que decide **cuánto vale tu posición**: unas de precisión hacen que adelantarte valga el doble.
>
> **Y el TEMPO son las botas.** Marcar el compás se hace con los pies. Mecánicamente: el arma te da el daño, **las botas te dan el ritmo para convertirlo en CM**. Se puede construir un personaje que pegue flojo pero mantenga el pulso y alimente el pool del equipo — un rol entero que nace de un slot.
>
> **Reflejos** encaja sin forzar: interceptar el golpe mortal dirigido a un aliado es **llegar a tiempo**.

### ACCESORIO — *¿con qué lo pago?*

| Primarias | Secundarias |
|---|---|
| **CM** · **Gen. CM** | Robo de Vida · REG.HP · Dominio Médico · **Resistencia al Desgaste** · Tenacidad · Dominio y Extensión de Objetos · Dominio y Extensión de Comida · Amplificador y Extensión de Buff · Amplificador y Extensión de Debuff |

Es el slot de la **economía**: cuánto aportas al pool compartido, cuánto te sostienes sin generar desgaste, y cuánto rinden tus objetos y tu comida.

Aquí vive el build de **campaña larga**: la Resistencia al Desgaste decide si encadenas tres secciones o te vas al hospital.

---

## 5. Clases por slot

Dentro de cada slot, la **clase** define qué eje toca. La **rareza** define cuánto.

### Armas — las clases ya existen

Son las tres del sistema rítmico ([musica.md](musica.md)). El reparto de stats debe **reforzar** su identidad, no contradecirla.

| Clase | Ritmo | Da | Quita |
|---|---|---|---|
| **Ligera** | Melodía · 1/8 | Velocidad de Ataque · Crítico | AD por golpe |
| **Media** | ❓ · 1/4 | AD equilibrado · Penetración | — moderado en ambos |
| **Pesada** | Pulso · 1/2 | AD alto · Daño Explosivo · Última Bala | Velocidad de Ataque |

### Armaduras

| Clase | Da | Quita |
|---|---|---|
| **Placa** | ARM alta | RM |
| **Runa** | RM alta | ARM |
| **Vital** | HP alto | ARM y RM medias |
| **Barrera** | Eficiencia y Extensión de Escudo | ARM directa |

> **Placa y Runa se oponen a propósito.** Obligan a decidir contra qué te blindas, y hacen que un enemigo mágico y uno físico pidan preparativos distintos.

### Botas

| Clase | Da | Quita |
|---|---|---|
| **Precisas** | PRESS | EVA |
| **Ligeras** | EVA | PRESS |
| **Rítmicas** | **TEMPO** | PRESS **y** EVA |

> Las **Rítmicas** son las botas del músico: pagas precisión *y* evasión a cambio de alimentar el pool del equipo. Son la pieza que permite construir un personaje cuyo trabajo no es pegar, sino **sostener el compás**.

### Accesorios

| Clase | Da | Quita |
|---|---|---|
| **Resonador** | CM · Gen. CM | HP |
| **Sanitario** | REG.HP · Dominio Médico | Gen. CM |
| **Peregrino** | **Resistencia al Desgaste** | CM |
| **Gourmet** | Dominio y Extensión de Comida · de Objetos | Robo de Vida |

> El **Gourmet** tiene un gancho que no es obvio: la comida es la vía de **activar emociones** ([emociones.md](emociones.md) §5). Amplificar los efectos de la comida es amplificar tu capacidad de manipular el ánimo del equipo en combate.
>
> Y el **Peregrino** es el que decide tu semana, no tu combate: menos desgaste son menos días de hospital.

---

## 6. Efectos de Impacto

✅ **RESUELTO — ver [impacto.md](impacto.md) para el sistema completo.**

Es la **tercera familia** de efectos aplicables, junto a las dos existentes:

| Familia | Sigla | Se aplica desde | Disparador | Inmunidad de jefe |
|---|---|---|---|---|
| Efecto **Elemental** | E.E. | Skills, artilugios | Probabilidad 25% | *Elemental* |
| Efecto **Emocional** | E.EM. | Flags del día y del combate | Condición cumplida | *Sin alma* |
| Efecto de **Impacto** | — | **Arma (AD)** | **Acumulación, sin dado** | ***Inamovible*** |

### Lo que aporta al slot de arma

Sin esto, dos armas ligeras solo se distinguen por sus porcentajes. **Cada arma lleva un efecto de impacto**, y eso convierte la elección de arma en una decisión táctica y no solo numérica.

### Resumen de una línea

Cada golpe que conecta deposita en el objetivo **la misma figura rítmica que genera CM** (1/8 ligera, 1/4 media, 1/2 pesada). Cuando la barra llega al umbral del efecto, el efecto entra — **sin azar**.

| Efecto | Umbral | Clases | Qué hace |
|---|---|---|---|
| **SANGRADO** | 1 | Ligera, Media | 3% HP máx por turno, acumula hasta 5 |
| **TRABA** | 1 | Ligera | No puede cambiar de casilla 1 turno |
| **QUIEBRE** | 1 | Media, Pesada | −8% ARM y RM, acumula hasta 3 |
| **DESAJUSTE** | 3/2 | Media, Pesada | 1 turno sin tempo ni generación de recurso |
| **EMPUJE** | 1/2 | Pesada | Mueve 1 casilla hacia su DEFENSIVA |
| **RESONANCIA** | 1 | Todas | +1/2 CM al bote del equipo |

> **El empuje entra aquí.** Hasta ahora el movimiento forzado solo salía de skills; con esto **el arma queda conectada al tablero**.

---

## 7. Rareza — cuánto da cada pieza

⚠️ Bandas **provisionales**, pendientes de balanceo.

| Rareza | Bonus principal | Contrapartida |
|---|---|---|
| **Común** | +5 a 10% | −5% |
| **Rara** | +10 a 18% | −8% |
| **Épica** | +18 a 28% | −12% |
| **Legendaria** | +28 a 40% | **−20%** |

> **La contrapartida crece con la rareza.** Una legendaria no es una épica con más números: es una decisión más extrema. Así el equipo de alto nivel sigue siendo una elección y no un simple ascenso.

Mismas cuatro rarezas que los gadgets, para no inventar una escala nueva.

---

## 8. Lo que habilita el reparto

Cada slot abre un rol distinto sin que haya que escribir ninguna regla extra:

| Si inviertes en… | Te vuelves |
|---|---|
| **Arma** | El que mata |
| **Armadura** | El que no cae |
| **Botas** | El que marca el ritmo y alimenta al equipo |
| **Accesorio** | El que aguanta la semana |

Y como cada pieza quita algo, **especializarse cuesta**: el que marca el ritmo falla más golpes, y el que aguanta la campaña tiene menos recurso para gastar.

---

## 9. Conjuntos (kits)

**Si llevas todas las piezas de un mismo conjunto, obtienes:**

1. Una **pasiva** propia del conjunto
2. Un **bonus porcentual** de estadística

### Por qué esto crea una decisión real

Los cuatro slots están pensados para **mezclar**: coges el arma que te conviene, las botas que te convienen, y armas un personaje a medida. El conjunto empuja en la dirección contraria — **llevarlo todo del mismo sitio**.

Esa tensión es el sistema:

| | Ganas | Pierdes |
|---|---|---|
| **Mezclar** | Optimizas cada slot por separado | El bonus de conjunto |
| **Conjunto completo** | Pasiva + bonus de stat | La libertad de elegir pieza a pieza |

> ### Y hay una segunda capa que lo hace más interesante
>
> **Toda pieza quita algo** (§3). Un conjunto completo significa **tragarse cuatro contrapartidas** — las que el conjunto imponga, no las que tú habrías elegido.
>
> Así que el conjunto no es "la opción buena": es un **compromiso**. Aceptas cuatro penalizaciones ajenas a cambio de una pasiva extra. Si el bonus no compensa esas cuatro, nadie lo lleva; si compensa de sobra, nadie mezcla.

### El riesgo a vigilar

Si los conjuntos son demasiado fuertes, **matan el sistema de mezcla** y todo el mundo acaba llevando el conjunto de su rol. El freno natural es hacerlos **estrechos**: un conjunto optimiza un eje **muy** a fondo y es malo en todo lo demás.

Un conjunto debería ser la respuesta obvia a **una** situación concreta, no la respuesta por defecto.

### El gancho narrativo

Los conjuntos son el sitio natural para meter **lore en el equipo**. Un conjunto puede pertenecer a:

- Un **camino o estilo** del roster
- Un **distrito** o un **club**
- Un **Clasical histórico** — la cuarta generación, los creadores de las cuerdas, los portadores anteriores

Igual que los **recuerdos** ya cuentan de dónde vienen (*Medalla del Ejército Korkakov*), un conjunto cuenta **a quién perteneció**.

### Forma canónica de una pasiva de conjunto

**Ejemplo real:**

> **LEGENDARIO — "Gran mago de Pofi-Pofi!!"  `4/4`**
> Si tienes algún miembro con un **adorno Pofi-Pofi**, aumenta su **RM** en **X% de su stat total** cuando se le aplica un **E.E.**
>
> *(**Adorno** = el slot de **accesorio**.)*

De ahí salen tres reglas:

#### 1. La pasiva de conjunto puede leer a OTRO personaje

Este bonus **no buffea a quien lleva el conjunto**: buffea a un compañero que lleve una pieza emparentada.

> Eso convierte los conjuntos en **sinergia de composición**, no solo en build individual. Mi conjunto mejora tu equipo. Es la misma familia de pensamiento que los emblemas de club: **lo que llevan los demás importa**.
>
> Y abre un tipo de pieza nuevo: el **adorno emparentado**, que por sí solo no hace nada especial pero **habilita** el conjunto de otro.

#### 2. Las legendarias sí usan el % del total

*"X% de su stat total"* — no de la StatOperativa. Es la capa `multFinal`, la excepcional.

**Y ese es su sitio.** Reservada para conjuntos legendarios, no para equipo corriente. Así el "% del total" es una rareza reconocible en vez de una segunda norma compitiendo con la primera.

#### 3. La pasiva de conjunto es condicional y reactiva

`Si se le aplica un E.E. → sube su RM`

No es un bonus permanente: **responde a algo que pasa en combate**. Un conjunto puede ser la respuesta directa a una amenaza concreta — este es un contador anti-elemental.

> Eso encaja con la recomendación de que los conjuntos sean **estrechos**: la respuesta obvia a **una** situación, no la opción por defecto.

#### 4. Una pasiva de conjunto puede ser de cualquier naturaleza

No están limitadas a dar estadísticas. Pueden ser **ofensivas, pasivas o reactivas** — y eso las vuelve el sistema más expresivo del equipamiento.

**No hace falta inventar una taxonomía nueva: los gadgets ya tienen la suya**, y conviene reutilizarla para no tener dos clasificaciones que digan lo mismo.

| Modo | Qué es | Ejemplo de conjunto |
|---|---|---|
| **PASSIVE** | Existe y se comprueba | *Mientras lleves el conjunto, +X% de RM* |
| **REACTIVE** | Responde a un suceso | *Gran mago de Pofi-Pofi!!* — reacciona a que te apliquen un E.E. |
| **ACTIVE** | El jugador la dispara | Un efecto que se usa una vez por combate |
| **AURA** | Afecta al equipo entero | *Todos los que lleven adorno emparentado…* |

Y la **polaridad** también se hereda de los gadgets: `POSITIVE`, `NEGATIVE`, `MIXED`. Un conjunto puede perjudicar a propósito — encaja con la regla de que todo da y quita.

> ### Y todo esto ya cabe en la arquitectura existente
> Según [arquitectura.md](arquitectura.md) §1, cualquier regla del juego es
> `fuente · disparador · condición · efecto · capa · duración · alcance`.
>
> Una pasiva de conjunto es **una regla más**, con `fuente = CONJUNTO`. No necesita maquinaria nueva: solo una fila distinta en la misma tabla.

#### 5. Escalado por piezas — ✅ DECIDIDO

**El bonus porcentual escala linealmente. La pasiva es exclusiva del conjunto completo.**

| Piezas | Bonus de stat | Pasiva del conjunto |
|---|---|---|
| `1/4` | **Nada** | ❌ |
| `2/4` | **50%** | ❌ |
| `3/4` | **75%** | ❌ |
| `4/4` | **100%** | ✅ |

**Los conjuntos empiezan a las dos piezas.** Una pieza suelta no aporta nada de conjunto.

> ### Por qué esta combinación funciona
>
> **El porcentaje te acompaña desde la segunda pieza**, así que nunca llevas algo muerto encima: dos piezas de conjunto ya pagan.
>
> **Pero la pasiva —lo que hace memorable al conjunto— solo llega al completarlo.** Así hay gradiente *y* hay meta. Con un escalado puramente lineal, el `4/4` no sería más que un número un poco mayor y dejaría de ser un objetivo.

### Lo que elimina: el precipicio

En los conjuntos clásicos, llevar 3 de 4 piezas **no vale nada**: o completas o no tienes conjunto. Eso obliga a decidir de golpe y castiga duro la mezcla.

Aquí puedes llevar **dos piezas de conjunto y dos optimizadas** y quedarte con medio bonus. La tensión entre *mezclar* y *comprometerse* deja de ser un interruptor y se vuelve **un dial**.

Y encaja con la regla del intercambio: a `2/4` te tragas **dos** contrapartidas ajenas en vez de cuatro, y cobras la mitad. **El precio y el premio escalan juntos.**

> **Por qué desde dos y no desde una:** así una pieza suelta **se juzga por sí misma**, por sus propios porcentajes, y no por el conjunto al que pertenece. Evita que el jugador guarde piezas mediocres solo porque llevan una etiqueta.

❓ **La potencia del efecto de la pasiva**, ¿es fija al completar el conjunto, o también escala con algo? *(Como solo existe a `4/4`, lo más simple es que sea fija.)*

### Nota de nombrado

*"Gran mago de Pofi-Pofi!!"* mantiene la voz de los recuerdos (*"¿Mago? ¡MAGOOOOO!"*, *"¡NO CEDAN!!!"*) y conecta con la familia de cosas con nombre tierno del lore — los **pluf-pluf** que Alextsu le promete a Itzela. Merece la pena mantener ese registro: es donde se ve la personalidad del proyecto.

### Pendientes del sistema de conjuntos

❓ **¿Cuántas piezas son un conjunto?** La notación `4/4` apunta a los cuatro slots. Confirmar si el recuerdo o el gadget cuentan.


❓ **¿La pasiva del conjunto se suma a la del recuerdo?** Si sí, un personaje con conjunto completo lleva **dos pasivas**, y eso hace los conjuntos muy atractivos. Si no, hay que elegir cuál cuenta.

❓ **¿Cómo se consigue un conjunto completo?** ¿Botín de una zona concreta, recompensa de un lazo, fabricación, o piezas sueltas por todo el juego?

❓ **¿Existen conjuntos de conversión** (§2)? Sería el sitio ideal: un conjunto que te convierte HP en ARM es lo bastante raro y lo bastante costoso.

---

## Pendientes

❓ **¿Cuántas piezas por clase?** ¿Un puñado o un catálogo grande?

❓ **¿De dónde salen?** Muro Mercantil, botín de runs, recompensas de lazos, concursos, historia.

❓ ¿Hay **requisito de nivel** para equipar, o cualquier pieza sirve desde el principio?

❓ ¿El equipo es **por personaje** o compartido en un inventario de equipo?

❓ ¿Persisten entre runs? *(Presumiblemente sí, a diferencia de los emblemas temporales.)*

❓ **Arma media**: falta decidir si sigue melodía o pulso ([musica.md](musica.md) §7.2). Su identidad de stats depende de eso.

❓ ¿Existen piezas de **conversión** (§2) y con qué frecuencia?

❓ **Precios.** Dependen de la economía, que no existe todavía.

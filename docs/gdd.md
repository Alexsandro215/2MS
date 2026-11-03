# E.N.M.A — GDD (documento vivo)

> ⚠️ **LEER PRIMERO: [conflictos.md](conflictos.md)**
> Este documento recoge el **levantamiento hablado**. Los PlantUML MASTER son **canónicos** y contradicen varias partes de lo que hay aquí — sobre todo el sistema de casillas (§1.2, §1.4) y los turnos de ventaja (§1.3). No tomar este documento como verdad sin contrastar.

**Documentos del proyecto:**

| Archivo | Contenido | Autoridad |
|---|---|---|
| [**manual-tecnico.md**](manual-tecnico.md) | **Manual formal completo — sustituye págs. 143-161 del docx** | **Canónico · v2.0** |
| [formulas.md](formulas.md) | Cálculo: stats, acierto, daño, CM, desgaste, elementos | **Canónico** (STATS MASTER prevalece) |
| [tablero.md](tablero.md) | Front/banquillo, casillas, movimiento, alcance, HIDE/SWAP, terreno, anclaje | **Canónico** |
| [musica.md](musica.md) | Tempo, ritmo, modo CLASICAL, temas musicales | **Canónico** |
| [**emociones.md**](emociones.md) | **E.EM. — flags, perfiles por personaje, efectos en día y combate** | **Canónico** |
| [**equipo.md**](equipo.md) | **Arma · Armadura · Botas · Accesorio — reparto de stats y clases** | **Canónico** |
| [**impacto.md**](impacto.md) | **Efectos de Impacto — la tercera familia de efectos, aplicada desde el arma** | **Canónico** |
| [**runs.md**](runs.md) | **Estructura de las expediciones: secciones, nodos, bifurcaciones** | **Canónico** |
| [**dia.md**](dia.md) | **El bucle fuera del combate: bloques, lazos, clase, historia, emoción** | **Canónico** |
| [lore-contexto.md](lore-contexto.md) | Mundo, distritos, estructura del día, lazos, entrenamiento | Contexto |
| [divergencias-biografias.md](divergencias-biografias.md) | Qué corregir en `BIOGRAFIAS 8de10.docx` | Lista de trabajo |
| [**roles.md**](roles.md) | **Roles / etiquetas de arquetipo: 6 familias, asignación de los 20 estilos, huecos** | **Canónico** |
| [**progresion.md**](progresion.md) | **Capítulos ↔ elementos: presentar en 3 meses, dominar después** | Parcial (intro y cap 1 confirmados) |
| [**lazos.md**](lazos.md) | **Los 81 secundarios: organizaciones, HPS y HS** | Parcial |
| [produccion.md](produccion.md) | Notas para la fase de creación (animación, retargeting) | Notas |
| [pasivas.md](pasivas.md) | Pasivas como modificadores condicionales | **Canónico** |
| [arquitectura.md](arquitectura.md) | Forma del sistema, capas, esquema único de reglas | **Canónico** |
| [elementos.md](elementos.md) | Los 9 efectos elementales, estados y amplificadores | Notas del autor (desactualizadas) |
| [conflictos.md](conflictos.md) | Divergencias canon ↔ levantamiento | Índice de decisiones pendientes |
| [estilos.md](estilos.md) | 20 kits QWER (¿= caminos/árboles?) | Borrador |
| este archivo | Levantamiento hablado del combate | **Superado en parte** |

> Estado: **borrador / levantamiento de requerimientos**
> Lo marcado con ❓ está pendiente de decisión.

---

## 0. Concepto

RPG por turnos con combate táctico posicional en línea y habilidades tipo MOBA (QWER).
Hay un componente **musical** en el sistema de recursos (ver *Carga Musical*).

❓ Nombre del proyecto (por ahora "TMS")
❓ Plataforma objetivo (navegador / escritorio / móvil)
❓ 2D o 3D · vista (lateral, isométrica, frontal…)
❓ Estructura: campaña / roguelike / otra
❓ ¿Cuánto pesa la temática musical? (¿solo el recurso, o identidad completa?)

---

## ⚠️ Nota de terminología

**DEFENSA** y **DEFENSIVA** son cosas distintas. No confundirlas:

| Término | Qué es |
|---|---|
| **Defensa** | Una **acción de turno**. Mitiga daño y termina el turno. Se puede usar **desde cualquier casilla**. |
| **DEFENSIVA** | La **casilla trasera** de la línea. Es una posición, no una acción. Otorga sus propios bonus y restricciones. |

---

## 1. Combate

### 1.1 Formaciones

| Lado | Personajes |
|---|---|
| Aliados | máx. **4** |
| Enemigos | máx. **6** |

Son **máximos, no cifras fijas**: un encuentro puede tener menos enemigos. El 6 es el techo del formato, no el tamaño típico.

**Dónde está cada personaje:**

| Zona | Definición |
|---|---|
| **FRONT** | Personajes peleando activamente. **Aquí viven las casillas** (§1.2). |
| **BANQUILLO** | Personajes fuera del combate activo, esperando un cambio. *(Término oficial: "reserva" queda descartado.)* |
| **ESCONDITE** | Lugar donde ocultarse. **Solo disponible para quienes están en el front.** |

| Acción | Efecto |
|---|---|
| **HIDE** | Esconderse en un escondite. |
| **SWAP** | Cambiar un personaje del front por uno del banquillo. |
| **ESCAPE** | Huir de la batalla, con **% de éxito**. |

### 1.2 Posicionamiento

Cada personaje tiene su **propia línea de 3×1**. Solo se mueve **adelante** o **atrás** sobre ella.
Las líneas son independientes: nadie compite por casillas, nadie se cruza.

```
ALIADOS                                    ENEMIGOS
[DEF][NEU][OFE]  ─┐                    ┌─  [OFE][NEU][DEF]
[DEF][NEU][OFE]  ─┤                    ├─  [OFE][NEU][DEF]
[DEF][NEU][OFE]  ─┤    ← frente →      ├─  [OFE][NEU][DEF]
[DEF][NEU][OFE]  ─┘                    ├─  [OFE][NEU][DEF]
                                       ├─  [OFE][NEU][DEF]
                                       └─  [OFE][NEU][DEF]
```

**Propiedades de cada casilla:**

| Casilla | Efectos |
|---|---|
| **OFENSIVA** (adelante) | ▲ PRESS (precisión) · ▼ EVA |
| **NEUTRAL** (centro) | ❓ intermedio |
| **DEFENSIVA** (atrás) | ▲ EVA · ❓ ▼ PRESS · **2 turnos de ventaja** · **no puede atacar** · **skills AD bloqueadas** |

Lectura táctica: avanzar a OFENSIVA es la forma de **alcanzar enemigos con evasión alta** o replegados a su DEFENSIVA, a costa de volverte más fácil de golpear.

> ⚠️ El sistema real es **más complejo** que este resumen; PRESS/EVA es solo uno de los ejes. Pendiente de detallar.

**Coste del movimiento:** gratis, no consume nada.
**Bloqueo:** ver 1.4 — el 2º turno de ventaja congela al personaje en DEFENSIVA.

❓ ¿Se puede recorrer más de una casilla por turno?
❓ ¿Qué otros ejes además de PRESS/EVA?
❓ ¿NEUTRAL es solo "sin bonus" o tiene ventaja propia?
❓ En DEFENSIVA no se puede atacar: ¿aplica también a personajes a distancia / mágicos, o el bloqueo de ataque básico es solo físico?

### 1.3 Acciones disponibles

**Acciones de turno** (se elige UNA, cierra el turno):

| Acción | Regla |
|---|---|
| **Atacar** | Hasta *N* ataques básicos (BA), *N* = **Attack Speed máx**. Con el mouse. Habilita la fase de **Combos**. No disponible desde DEFENSIVA. |
| **Defensa** | ✅ **DECIDIDO:** **+50% de ARM y RM** · se **consume en el primer impacto recibido** · **anula el crítico** de ese impacto · **duplica** la ganancia de CM al recibirlo. Cierra el turno. **Disponible desde cualquier casilla.** |
| **Carga Musical** | Estructura igual a Defensa, pero enfocada en **generar CM**. ❓ ¿mitiga algo? ❓ ¿desde cualquier casilla? |

**Turnos de ventaja** (se usan ANTES de la acción de turno):

| Tipo | Regla |
|---|---|
| **Consumible** | Objeto benéfico (aliado) u ofensivo (enemigo). |
| **Comida** | Potencia a un personaje. Efectos **positivos o negativos**. |
| **Artilugio** | **Ofensivo** (enemigo) · **Defensivo/benéfico** (aliado) · **Pasivo** (no se usa directamente: se activa al cumplirse una condición y **no consume turno de ventaja**). |

**Reglas — MODELO UNIFICADO (canon + excepción de casilla):**

```
1 COMIDA gratis por EQUIPO   ┐
1 CONSUMIBLE gratis por EQUIPO ┘  independientes · solo en ventana pre-ofensiva

Recarga: CD de 3 turnos
Usar con CD > 0  →  consume el turno (y NO resetea el CD)
Gadget           →  SIEMPRE consume turno

▶ Estando en la casilla DEFENSIVA, el CD NO aplica.
```

> La casilla defensiva **no regala un objeto extra: elimina la espera.** El beneficio de replegarse no es cantidad, es **tempo**.

❓ En DEFENSIVA sin CD: ¿se puede repetir en el mismo turno, o sigue siendo 1 de cada uno por ventana?
❓ ¿Inventario compartido de equipo o por personaje? ¿Se gastan las unidades?
❓ ¿La comida dura X turnos, todo el combate, o es permanente?

### 1.4 Flujo de turno y regla de compromiso

El movimiento **no está fijado al inicio**: el personaje sigue libre de reposicionarse hasta que se compromete.

```
                    ┌──────────────────────────────────┐
   Movimiento  ◄────┤  sigue libre                     │
       │            └──────────────────────────────────┘
       ▼
  1er turno de ventaja  ──► el personaje SIGUE pudiendo moverse
       │
       ▼
  2do turno de ventaja  ──► SOLO existe en la casilla DEFENSIVA
       │                    ⇒ QUEDA ANCLADO ahí
       ▼
  Acción de turno:
       ├─ desde OFE/NEU: Ataques básicos → Combos ─────► FIN
       ├─ desde DEFENSIVA: Combos no-AD ───────────────► FIN
       ├─ Defensa (cualquier casilla) ─────────────────► FIN
       └─ Carga Musical ───────────────────────────────► FIN
```

**Salidas desde la casilla DEFENSIVA — CONFIRMADO.** Estando atrás hay tres salidas válidas, todas independientes entre sí:
1. **Lanzar combos no-AD** (curación, apoyo, buff, terreno, daño AP) como turno completo, sin necesidad de acompañarlos de nada más.
2. **Defensa** (mitigar y cerrar).
3. **Carga Musical** (generar CM y cerrar).

Es decir: DEFENSIVA es la **postura de apoyo/lanzador**. Se va ahí a curar, potenciar y hacer daño mágico — o a aguantar y a cargar maná. Lo único vetado es todo lo que sea pegar físicamente (ataque básico y skills AD).

**Reglas de compromiso:**
1. El movimiento es gratis y reversible mientras no se gaste el 2º turno de ventaja.
2. Tras usar **1** turno de ventaja el personaje **sigue libre de moverse**.
3. El **2º** turno de ventaja solo existe estando en **DEFENSIVA**; al usarlo **ya no puede moverse** de esa casilla.
4. Si tras 1 turno de ventaja el personaje se queda en NEUTRAL u OFENSIVA, **no puede usar el 2º**. Sus salidas: atacar desde ahí, o retroceder a DEFENSIVA.

❓ Desde DEFENSIVA, sin ataques básicos posibles: ¿se puede hacer un turno de **solo combos** (no-AD) sin elegir Defensa ni Carga Musical? ¿O los combos van encadenados a una de esas dos acciones?

### 1.5 Orden de turnos

**Por bandos, secuencial.** Un aliado ejecuta su flujo completo; al terminar pasa al siguiente; cuando **todos los aliados** han actuado, el turno pasa al bando enemigo.

Dentro del bando aliado, **el jugador elige quién empieza**, y a partir de ahí el orden avanza **de derecha a izquierda**.

✅ **RESUELTO:** el jugador solo escoge quién abre. Desde ahí el orden es automático, de derecha a izquierda, con vuelta al extremo, y **nadie se salta su vez**. Se vuelve a elegir en cada turno de bando. Ver [manual-tecnico.md](manual-tecnico.md) §1.4.
❓ ¿Quién abre la ronda, aliados o enemigos? ¿Depende de algo (emboscada, iniciativa)?
❓ ¿Se puede pasar/aplazar el turno de un personaje?

### 1.6 Combos (habilidades QWER)

- Habilidades estilo MOBA en **Q / W / E / R**.
- Consumen **CM**. Se encadenan **todas** las que alcance el CM disponible.

**Restricción de la casilla DEFENSIVA:**
Sí se pueden usar combos estando en DEFENSIVA, **excepto los de daño físico (AD)**.

| Tipo de skill | ¿Permitida en DEFENSIVA? |
|---|---|
| Curación | ✅ |
| Apoyo | ✅ |
| Aumento / buff | ✅ |
| Modificación de terreno | ✅ |
| Daño **AP** (mágico) | ✅ |
| Daño **AD** (físico) | ❌ |

En la UI, las skills AD aparecen **bloqueadas** al estar en DEFENSIVA — y si el personaje es de estilo puramente físico, **todas** sus skills quedan bloqueadas.

**Rol de la casilla:** DEFENSIVA existe sobre todo *para* lanzar estos combos. El corte no es "combos sí / combos no", sino **físico / no físico**: si el combo no busca dañar con AD, se puede lanzar desde atrás.

**Secuencias y tecla EVOL — CONFIRMADO.**
El orden de encadenado **sí importa**. Ejecutar una **secuencia concreta de teclas** desbloquea una tecla **EVOL**, disponible al completarse la secuencia.

> Esto es lo que le da identidad a las cuatro teclas: no basta con gastar CM en la habilidad más eficiente, hay que ejecutar patrones para acceder a la capa EVOL. El QWER deja de ser un menú de costes y pasa a ser un lenguaje.

❓ ¿Cuántas secuencias EVOL tiene un personaje? ¿Una por personaje o varias?
❓ ¿La secuencia debe completarse **en un solo turno** o puede arrastrarse entre turnos?
❓ ¿EVOL cuesta CM aparte, o la secuencia ya lo pagó?
❓ ¿Se rompe la secuencia si intercalas una tecla equivocada?
**Cadena de turno — CONFIRMADO:**
- Se empieza con **básicos** o con **QWER**. Si entras en QWER, **no vuelves a los básicos**.
- Cada tecla base **se consume al usarse**: en un turno puedes llegar a usar **Q, W, E y R** si te alcanza el CM.
- Las **subskills de secuencia** (QT1, ET1…) quedan disponibles **tras cumplir la secuencia temporal**. Uses o no la subskill, **todo vuelve a su estado normal al turno siguiente.**

> Las secuencias no añaden teclas: **reexpresan** las que ya tienes. Y como se resetean cada turno, no se pueden guardar — hay que rehacer el patrón cada vez.

### Otras reglas de combate confirmadas

| Regla | Detalle |
|---|---|
| **Preparación** | El personaje **no puede hacer nada** mientras prepara. Se le puede **interrumpir dejándolo en K.O.** |
| **Última Bala** | Es un **umbral de ejecución del atacante**: si dejas al objetivo por debajo de X% de su vida base, pasa directo a **K.O.** No cuenta escudos, solo HP real. |
| **Objetivo de los básicos** | No se puede cambiar de enemigo entre golpe y golpe (ver [formulas.md](formulas.md)). |
| **Orden de turnos** | Habrá un **menú para consultar el orden del equipo** tras cada pelea. |
| **IA enemiga** | Sin diseñar todavía, pero juega con **exactamente las mismas reglas** que el jugador. |
| **Kits enemigos** | Los enemigos usan **las mismas spells que el jugador** (excepto los jefes, que tienen las suyas). Pueden llevar un estilo completo, otro, o **mezclar habilidades de varios** según el contexto del encuentro. |

> ### Los enemigos no son un sistema aparte
>
> No hay que diseñar un bestiario con kits propios: **el roster de estilos ES el bestiario**. Un encuentro se compone eligiendo qué habilidades lleva cada enemigo, y **la mezcla entre kits** es la herramienta principal de variedad.
>
> Ventajas: el balance es recíproco (lo que es fuerte contra ti lo es a tu favor), el jugador aprende a leer al enemigo con lo que ya conoce, y **cada habilidad nueva que escribes amplía los dos lados a la vez**.
>
> Los **jefes** son la excepción: tienen habilidades propias, y son también los que traen más capas musicales ([musica.md](musica.md) §4).

❓ ¿Hay límite a cuántos kits puede mezclar un enemigo? ¿Un enemigo mezclado tiene EVOL y secuencias, o solo habilidades sueltas?
❓ ¿Los enemigos comparten un pool de CM entre ellos, como el equipo aliado?

### 1.7 Fórmula de acierto (PRESS vs EVA)

**Nomenclatura:** `PRESS` = precisión del atacante · `EVA` = evasión del defensor.

```
Hit% = 5 + 90 × ( PRESS / (PRESS + EVA_efectiva) )
```

La forma de la fórmula garantiza matemáticamente que el acierto quede **entre 5% y 95%**: nunca hay acierto ni fallo garantizado.

**Evasión efectiva (desgaste):**

```
EVA_efectiva = EVA × max( 0.10 , 1 − factor_desgaste_evasión )
```

Conforme el defensor recibe intentos de ataque y **consigue esquivarlos**, su evasión efectiva **baja gradualmente**, evitando que una unidad con EVA alta esquive indefinidamente. El suelo del multiplicador es **0.10**: la EVA efectiva nunca baja de un **10% de su valor original**.

**Tabla de referencia** (PRESS del atacante = 50):

| Situación | PRESS | EVA efectiva | Acierto |
|---|---|---|---|
| Enemigo fresco | 50 | 100 | 35% |
| EVA reducida | 50 | 75 | 41.0% |
| EVA reducida | 50 | 50 | 50% |
| EVA reducida | 50 | 25 | 65% |
| EVA muy desgastada | 50 | 10 | 80% |
| EVA mínima | 50 | 5 | ~87% (clamp máx. 95%) |

> El 95% es el **techo** de la fórmula, no el resultado de PRESS 50 / EVA 5.

Consecuencia de diseño: un **Attack Speed alto** es la respuesta natural a un enemigo de evasión alta — los primeros golpes fallan pero desgastan su EVA y abren paso a los siguientes. Un personaje de AS 1 nunca llega a desgastar nada.

**Disparador del desgaste — CONFIRMADO:** la EVA baja **cuando el defensor esquiva con éxito**. Un ataque que acierta no desgasta nada; el castigo lo genera el propio esquive.

> Nota de diseño: esto hace que la EVA se **autorregule**. Un tanque evasivo se vuelve progresivamente golpeable precisamente porque está esquivando bien. No se puede "sobrevivir a base de EVA" un turno entero contra un atacante de AS alto.

**Valor provisional** (a afinar con playtesting):

```
factor_desgaste_evasión = 0.15 × (nº de esquives logrados en la ventana)
```

Progresión resultante con EVA base 100 y PRESS 50:

| Esquives logrados | Multiplicador | EVA efectiva | Acierto del siguiente ataque |
|---|---|---|---|
| 0 | 1.00 | 100 | 35.0% |
| 1 | 0.85 | 85 | 38.3% |
| 2 | 0.70 | 70 | 42.5% |
| 3 | 0.55 | 55 | 47.9% |
| 4 | 0.40 | 40 | 55.0% |
| 5 | 0.25 | 25 | 65.0% |
| 6+ | 0.10 (suelo) | 10 | 80.0% |

❓ ¿Cuándo se restaura? (dicho antes: "al final del turno aliado" — ¿fin del turno del personaje atacante, o fin de la ronda completa del bando?)
❓ ¿El desgaste es global del defensor o se cuenta por atacante?
❓ ¿Los combos también tiran PRESS, o solo los BA?

### 1.8 Sistema de CM (maná)

> ⭐ **Pilar del juego.** El maná **no es individual: es un pool compartido de todo el equipo.**

| Concepto | Definición |
|---|---|
| **CM** | Estadística individual = cuánta **capacidad de almacenamiento** aporta ese personaje al pool común. |
| **Pool de equipo** | La **suma** del CM aportado por todos los miembros. |
| **Gen. CM** | Capacidad de **obtener** CM de distintas fuentes. |

**Cálculo de la capacidad máxima:**

```
Pool máx = Σ ( CM de cada miembro × estado )

    estado:  vivo = ×1.0
             KO   = ×0.5   (aporta la mitad)
             muerto = ×0    (capacidad perdida)
```

**Ejemplo dado:** dos personajes con CM 3 y CM 4 → pool de equipo = **7 CM**.
Si uno de ellos queda **KO**, su aporte se **reduce a la mitad**.
Si **muere**, esa capacidad **se pierde**.

> ### Por qué esto cambia todo el juego
>
> 1. **Cada combo es un gasto que le quitas a tus compañeros.** No hay "mi maná". Lanzar es una decisión económica de equipo, no personal.
> 2. **El orden de turnos —que el jugador elige— pasa a ser una decisión de reparto.** Quien actúa primero come del mismo plato que los que vienen después.
> 3. **Perder gente no solo te quita un cuerpo: te encoge el techo de recursos.** Una baja degrada la capacidad estratégica del equipo entero, no solo su daño. La espiral de derrota es explícita y buscada.
> 4. **Los personajes tanque de CM tienen valor sin pegar.** Un personaje puede justificar su hueco en el equipo por lo que *permite lanzar a los demás*.

**Fuentes de generación:**
1. Daño hecho con **ataques básicos (BA)**
2. Daño hecho con **combos**
3. **Recibir** daño
4. **Carga Musical** (acción de turno dedicada)

**Sobre Carga Musical:** su valor depende de la **fase de partida** y del **estilo**. La gestión de CM no es "gasta todo lo que tengas": vaciar el pool en un combo **sin dejar recurso a los aliados** es mala jugada. Es un recurso a administrar, no a quemar.

**Gasto sin freno — CONFIRMADO.** Un solo personaje **puede vaciar el pool entero**. No hay límite individual. Hay combos que **exigen muchísimo CM**, y pagarlos significa dejar al equipo seco.

**Los costes de CM bajan con el LV** — poco o mucho **según el kit de habilidades** de cada personaje.

> ### Aquí está el motor de la curva de partida
>
> Este es el mecanismo que explica EARLY → ULTRA LATE (§5):
>
> - **Early:** el pool es pequeño y los costes altos. Un solo combo consume el recurso de **todo el equipo**. Lanzar es un evento, no una rutina.
> - **Late:** los costes han bajado y el pool ha crecido. Lo que antes era el gasto del turno entero ahora se encadena.
>
> Y como la rebaja de coste depende del **kit**, hay personajes que se abaratan mucho y otros poco.
>
> ⚠️ **El coste es uno de los ejes, no el único.** La curva de cada personaje se dibuja combinando **coste, daño y demás parámetros del kit**. Ejemplos de perfiles posibles:
> - Pega poco **y** cuesta mucho en early → **monstruo** en late.
> - Pega fuerte en early → **mediocre** en late.
> - …y muchas combinaciones más.
>
> La idea es que cada kit tenga su propia forma de curva, no que todos escalen igual.

❓ Si el pool **máximo baja** por un KO y el CM **actual** lo supera, ¿se recorta el excedente o se conserva?
❓ ¿El **Gen. CM** también se suma en equipo, o cada personaje genera al pool común según su propia tasa?
❓ ¿Se empieza el combate con 0 CM o con algo cargado? ¿El CM persiste entre combates?

### 1.9 Armas y ataques básicos

El número y el ritmo de los ataques básicos **los define el arma**, no solo una estadística. Cada clase de arma tiene su propia interacción de mouse:

| Clase | Nº de BA | Ritmo | Daño | Interacción |
|---|---|---|---|---|
| **Ligera** | ~7 | Rápido, spameable | Bajo por golpe | Clics rápidos |
| **Mediana** | ~5 | Esporádico, más espaciado | Medio (> ligera) | Clics con cadencia |
| **Pesada** | 3 → 1 | Lento | Alto | **Mantener el clic potencia el golpe** |

> El arma no es un modificador de daño: es un **esquema de input distinto**. El arma ligera premia la velocidad de clic; la pesada premia la contención (sostener en vez de repetir). Eso convierte el ataque básico en una mecánica activa, no en un botón de confirmación.

Nota de sistema: como el desgaste de EVA se acumula por cada esquive del defensor, el arma **ligera** es estructuralmente la mejor para **romper evasión** (muchos intentos), mientras la **pesada** depende de que sus pocos golpes conecten. Esa tensión es intencional y sana.

❓ ¿"3 → 1" en la pesada significa que al mantener el clic los 3 golpes se funden en 1 cargado?
❓ ¿Cuánto potencia el mantener el clic? ¿Hay un punto óptimo de carga (timing) o es lineal?
❓ ¿El Attack Speed del personaje modifica los números del arma, o el arma los fija?
❓ ¿Hay más clases de arma? ¿A distancia / mágicas?
❓ ¿El arma condiciona desde qué casilla se puede atacar (alcance)?

---

## 2. Personajes y estadísticas

Glosario completo extraído de los kits (ver [estilos.md](estilos.md)):

| Stat | Función |
|---|---|
| **HP** | Vida. Muchas habilidades escalan con **HP faltante**. |
| **AD** | Ataque físico. |
| **AP** | Ataque mágico. |
| **DV** | **Daño verdadero** — ❓ presumiblemente ignora ARM y RM. |
| **ARM** | Armadura (defensa contra AD). |
| **RM** | Resistencia mágica (defensa contra AP). |
| **PRES** | Precisión. Entra en la fórmula de acierto (§1.7). |
| **EVA** | Evasión. Entra en la fórmula de acierto · se degrada al evadir. |
| **CRÍTICO** | Probabilidad de golpe crítico. ❓ multiplicador sin definir. |
| **VA / Attack Speed** | Velocidad de ataque = nº de ataques básicos por turno. |
| **CM** | Aporte individual al **pool compartido** de maná (§1.8). |
| **Gen. CM** | Tasa de obtención de maná. |
| **REG. HP** | Regeneración de vida. |
| **Pen. ARM / Pen. RM** | **Penetración**: ignora parte de la defensa del objetivo. |
| **Anti-ARM / Anti-RM** | Stat propio, distinto de la penetración. Willham lo trasvasa de uno a otro y sus habilidades **escalan con él**. ❓ relación exacta con la penetración sin definir. |
| **Desgaste** | ❓ **Sin definir.** Kronwell "obtiene 70% de desgaste" al usar su R (estilo *auto-desintegración*), y Halltus reacciona a que "un compañero tenga X% de desgaste". Parece un **coste acumulado sobre uno mismo**, distinto del desgaste de EVA. |
| **Mitigación %** | Reducción de daño (acción Defensa). |

**Tipos de daño:** **AD** (físico) · **AP** (mágico) · **VERDADERO** ❓ (mencionado en el remate de K.O.; presumiblemente ignora defensas — pendiente de definir)
**Estilos de personaje:** hay personajes puramente físicos (todas sus skills se bloquean en DEFENSIVA) y presumiblemente híbridos o mágicos.

❓ Faltan: HP, AD/AP como stats ofensivos, defensas (armadura / resistencia mágica), crítico, clases/roles, elementos…
❓ **Fórmula de daño**: aún sin definir. Aplazada a propósito hasta cerrar las mecánicas.

### 2.1 Estados del personaje

Hay una **escalera de tres peldaños** hacia la pérdida definitiva:

```
   VIVO  ──►  K.O.  ──►  MUERTE  ──►  ELIMINACIÓN PERMANENTE
            (×0.5 CM)   (×0 CM)      (borrado de la historia)
```

| Estado | Qué significa | Recuperación | Aporte al pool de CM |
|---|---|---|---|
| **Vivo** | Normal. | — | ×1.0 |
| **K.O.** | Caído pero recuperable. | Reanimable **por un compañero** o **automáticamente** (objetos, skills, etc). | ×0.5 |
| **Muerte** | Estado grave. Se tienen **3 oportunidades de revivir**. | 3 intentos. | ×0 |
| **Eliminación permanente** | Si fallan las 3 oportunidades, el personaje es **eliminado del juego y de la historia** de forma definitiva. | **Ninguna.** | — |

> ### Lectura de diseño
>
> Esto no es un sistema de muerte, es un **sistema de deterioro**. Cada peldaño no solo acerca a la pérdida: **degrada la economía del equipo en el camino** (§1.8). Un aliado en K.O. ya te está recortando el techo de maná, así que reanimarlo no es caridad, es recuperar capacidad de lanzar.
>
> Y con **permadeath real** al final de la escalera, las decisiones de recursos tienen consecuencias que sobreviven al combate. Gastar el pool entero en un combo puede costarte un personaje para siempre — literalmente, desaparece de la narrativa.

#### Cómo se pasa de K.O. a MUERTE — CONFIRMADO

La muerte **no es un umbral de daño: es un evento de remate con ventana de gracia.**

```
Turno enemigo N      →  un aliado cae en K.O.
        │
   ── TURNO ALIADO ──   ← ventana de rescate: aquí se le puede reanimar
        │
Turno enemigo N+1    →  ¿sigue en K.O.?
                          ├─ NO  → a salvo
                          └─ SÍ  → los enemigos lo atacan
                                     └─ si el daño CONECTA → MUERTE
```

Reglas:
- El aliado debe haber caído en K.O. **durante el turno enemigo**.
- Si **sigue en K.O.** al llegar el **siguiente** turno enemigo, será atacado.
- Si ese ataque **logra hacer daño**, pasa a **MUERTE**.
- Sirve **cualquier tipo de daño**: **AD**, **AP** o **verdadero**.

**Consecuencias de diseño:**
1. Un personaje en K.O. **sigue en el tablero y puede ser rematado**.
2. Hay exactamente **un turno aliado de margen** para levantarlo. La urgencia es concreta y legible, no difusa.
3. El daño que remata **puede fallar** — sigue pasando por la fórmula de acierto. La EVA del caído (si conserva alguna) todavía cuenta.
4. Reanimar compite directamente con atacar: el turno que gastas levantando a alguien es un turno que no presionas. Y el pool de CM es compartido, así que la reanimación también se paga del bote común.

❓ ¿Un personaje en K.O. conserva su EVA, o es blanco fácil (¿acierto garantizado?)?
❓ ¿Los enemigos **siempre** rematan al caído, o es decisión de su IA / estilo?
❓ ¿Qué son exactamente las **3 oportunidades** de revivir tras la MUERTE? ¿Se consumen en combate o entre combates? ¿Son tiradas de azar o intentos activos?
❓ ¿Las 3 oportunidades son **por muerte** o un contador vitalicio del personaje?
❓ ¿Un personaje en K.O. o muerto libera su línea, o la sigue ocupando (bloqueando el hueco)?
❓ Si un personaje es eliminado permanentemente, **¿qué pasa con la historia?** ¿Hay ramas narrativas que lo contemplan?

## 3. Sistemas revelados por los kits

> Fuente: [estilos.md](estilos.md) — 19 kits completos + 1 incompleto (Zadrick).

### 3.1 Modificación de terreno — RESUELTO

Las **casillas se pueden encantar**. Una habilidad marca una casilla concreta de una línea concreta, y quien esté ahí recibe el efecto mientras dure.

**Terreno de buff** (Floristas del Hollins):

| Casilla objetivo | Terreno | Efecto | Duración |
|---|---|---|---|
| OFENSIVA | Pasto de guerra | ▲ AD y AP | 3 turnos |
| MEDIA | Campo de flores | ▲ PRES y CRÍTICO | 3 turnos |
| DEFENSIVA | Rocas y hierba | ▲ RM y ARM | 3 turnos |

**Terreno de debuff** (Madrerick):

| Terreno | Efecto | Duración |
|---|---|---|
| Tierra quemada | ▼ PRES, AD, AP | 2 turnos |
| Ortigas y cicutas | ▼ EVA, CM (por ataque), CRÍTICO | 2 turnos |
| Pradera de guerra | ▼ DV, ARM, RM | 2 turnos |

Además, el terreno **interactúa con otros sistemas**: la R de Madrerick ancla a los enemigos que pisen sus casillas debuff, y su Q EVOL **elimina una casilla de buff enemiga**. Existe, por tanto, una guerra de terreno.

❓ ¿El terreno pertenece a la casilla o al personaje que la ocupa? Si el enemigo se mueve, ¿escapa del efecto?
❓ ¿Se pueden apilar terrenos en la misma casilla? ¿Uno sustituye al otro?
❓ ¿Se puede encantar terreno del bando propio y del enemigo indistintamente?

### 3.2 Movimiento forzado — SISTEMA NUEVO

**La posición del enemigo es disputable.** Varias habilidades mueven a otros por la línea:

| Verbo | Efecto | Ejemplo |
|---|---|---|
| **Asediar** | Empujar / presionar | Drimur Q, R · Lein-Xein E |
| **Enganchar / Atraer** | Traer al enemigo hacia el frente | Drimur E · Setto W |
| **Lanzar** | Empujar al enemigo a su casilla defensiva | Drimur Q |
| **Anclar** | **Impedir que se mueva** | Korkakov Q/E/R · Madrerick R |

> Esto cambia la lectura de todo el sistema posicional. La casilla no es solo tuya: **el rival puede sacarte de ella**.

**Para qué se usa — CONFIRMADO.** El movimiento forzado se ejecuta **en tu propio turno**, como herramienta de **preparación del tablero**. Los dos usos canónicos:

1. **Apartar a un tanque que "obstruye la entrada" a la back line.**
2. **Traer a la back line al frente**, donde sí se la puede castigar.

> ### La protección de la back line es por PASIVAS, no por geometría
>
> ⚠️ *Corrección de una inferencia previa: no existe una regla de línea de visión que impida golpear la casilla trasera si hay alguien delante.*
>
> El mecanismo real: **hay pasivas que redirigen los ataques hacia un personaje si está en el frente** (y variantes de esa idea). Es un **taunt**, no un muro.
>
> **Consecuencia:** para matar a los **casters y glass cannons** de la retaguardia no hay que derribar la pared — hay que **sacar de su sitio al que la sostiene**, o arrastrar al objetivo hacia delante. Por eso el reposicionamiento forzoso no es un lujo táctico: es la vía de acceso a la back line.

**Anclaje — CONFIRMADO.** Es un efecto **dirigido y unilateral**, no un bloqueo mutuo:
- Se le aplica **al objetivo**; quien lo lanza **sigue moviéndose con libertad** (o al revés, si te lo aplican a ti).
- La unidad anclada **no puede moverse en su turno** ni **ser movida en el del rival**.
- Es **inmune incluso a los efectos de masas** (movimiento forzado en área).
- Dura **hasta que pase el turno enemigo**.

> Anclar a un tanque en el frente lo deja expuesto y sin poder replegarse; anclar a un enemigo trasero impide que lo rescaten. En ambos casos, quien ancla conserva su propia movilidad.

Las habilidades también **mueven al propio lanzador**: Lein-Xein Q y E regresan a NEUTRAL, su W lo manda a DEFENSIVA, Bladeus W EVOL retrocede a la defensiva, Artens E EVOL retrocede a MEDIA, Winchesster Q sale y vuelve al frente.

❓ **Conflicto con la regla de compromiso (§1.4):** si el 2º turno de ventaja te ancla a la casilla defensiva, ¿qué pasa cuando una habilidad propia te mueve (Lein-Xein E, Artens E EVOL)? ¿El movimiento por habilidad es ajeno a esa restricción?
❓ ¿El anclaje impide solo moverse, o también actuar?
❓ ¿Hay resistencia o tirada contra el movimiento forzado, o siempre entra?

### 3.3 Preparación (habilidades diferidas)

Habilidades que **se lanzan ahora y resuelven en N turnos**. Aparecen en Ultimatum (todo su kit), Winchesster, Kawamato y Jaens Jeans.

| Duración | Ejemplos |
|---|---|
| 1 turno | Kawamato E, Winchesster Q y W, Setto E |
| 2 turnos | Ultimatum Q |
| 3 turnos | Ultimatum W y E, Winchesster R, Jaens Jeans R |

El caso extremo: **Jaens Jeans R** te vuelve invencible 3 turnos durante los cuales **no puedes hacer nada**, y al terminar todo el equipo gana CONCENTRACIÓN. Es un sacrificio de tempo puro.

❓ ¿Se puede cancelar o interrumpir una preparación? ¿Al recibir daño?
❓ ¿El personaje puede actuar normalmente mientras prepara (salvo casos como Jaens Jeans R)?
❓ ¿El CM se paga al iniciar la preparación o al resolverla?
❓ Si el objetivo muere o se mueve antes de resolver, ¿qué ocurre?

### 3.4 Cargas (recursos propios de personaje)

Algunos kits tienen su **propio contador**, aparte del CM compartido: *cargas de Kronwell*, *cargas de Hallen*. Se acumulan con unas teclas y se **consumen de golpe** en otras, escalando el efecto por `n × X%`.

Es el patrón clásico de acumular-y-detonar, y es **lo que da sentido a las secuencias EVOL**: el orden importa porque construyes recurso antes de gastarlo.

❓ ¿Las cargas persisten entre turnos? ¿Entre combates? ¿Tienen tope?

### 3.5 Estados y efectos

Catálogo extraído de los kits — todos **pendientes de definir numéricamente**:

| Estado | Qué hace | Aparece en |
|---|---|---|
| **E.E.** | **Efecto Elemental.** Sistema de acumulación — ver §3.9. | Kronwell, Lein-Xein, Willham, Winchesster, Setto, Ultimatum, Drimur… |
| **E.EM.** | **Efecto Emocional.** Sistema aparte, por disparadores — ver §3.10. | Kawamato E EVOL |
| **Sangrado / Sangrado II** | Daño en el tiempo, con **niveles**. | Rudentlein Q · Kawamato W |
| **Escudo** | Absorbe daño **antes que el HP**. No genera desgaste (§3.11). A menudo escala con **HP faltante**. | Halltus R EVOL · Floristas R · Setto E |
| **Inmunidad** | Bloquea efectos. | Halltus R · Drimur R EVOL |
| **Invencibilidad** | Intocable, pero **inmóvil**. | Jaens Jeans R |
| **Concentración** | Potencia la **siguiente** habilidad. | Kronwell W · Jaens Jeans R |
| **Anclaje** | Impide moverse. | Korkakov · Madrerick |
| **Anticuración** | Bloquea o reduce la curación recibida. | Ordeus Q EVOL (se libera de ella) |
| **Robo de vida** | Convierte daño en curación. | Bladeus · Hallen |
| **Ejecución** | Mata directo por debajo de X% de vida. | Rudentlein Q EVOL |
| **Desgaste** | **Reducción porcentual de TODAS las estadísticas.** Se recupera con **vendajes**. Ver §3.11. | Kronwell R · Halltus R EVOL |
| **Debuffs (genérico)** | Limpiables por Halltus W y Ordeus E. | — |

❓ **Definir E.E. es prioritario.** Es el término más repetido de todo el documento y no está explicado en ninguna parte.

### 3.6 Niveles de habilidad

Cada habilidad escala en **5 niveles** con valores propios por nivel (tablas en el apéndice de [estilos.md](estilos.md)).

❓ ¿Los niveles de habilidad se suben por separado del LV del personaje? ¿Hay puntos que repartir?
❓ ¿Cómo se relacionan con las fases EARLY → ULTRA LATE (§5)?
❓ Falta asignar cada bloque de la tabla a su personaje y su tecla.

### 3.7 Confirmaciones sobre EVOL

De los 19 kits completos: cada personaje tiene **una sola habilidad EVOL**, atada a **una tecla concreta** (Q, W, E o R) — no es una quinta tecla libre, es la *evolución de una de las cuatro*.

Muchas EVOL se disparan **"al finalizar el combo"** (Kronwell, Floristas, Artens), lo que confirma que la secuencia se resuelve **dentro de un mismo turno**.

❓ Setto y Mertinoli aparecen **sin EVOL** en el documento. ¿Falta escribirla o no tienen?

### 3.8 Pasivas de personaje

Existen **pasivas propias del personaje**, distintas de los artilugios pasivos (§1.3). La familia confirmada hasta ahora:

- **Redirección de ataques:** los ataques se dirigen a un personaje **si está en el frente**, con variantes de la misma idea.

Son las que sostienen la protección de la retaguardia (§3.2) y, por tanto, las que definen quién es alcanzable en cada momento.

**Estado del diseño:** las pasivas **aún no están escritas**. Decisión deliberada — primero se cierran los estilos para poder **variarlas** en función de ellos, no al revés.

❓ ¿Cada personaje tiene una pasiva, o solo algunos?
❓ ¿Se pueden anular, robar o suprimir?
❓ ¿Qué otras familias de pasivas habrá además de la redirección?

### 3.9 Efectos Elementales (E.E.)

Sistema de **acumulación con dos salidas distintas**:

| Situación | Resultado |
|---|---|
| E.E. aplicado **2 veces** | **Se detona el efecto elemental** propiamente dicho. |
| E.E. aplicado **1 vez**, seguido de daño **AP o AD** | El daño se aplica **aumentado**. |

> ### Por qué esto está bien construido
>
> Una sola marca de E.E. nunca es basura: si no llegas a la segunda, la primera sigue amplificando tu daño normal. **No hay stacks desperdiciados.** Y crea una decisión real dentro del combo: ¿gasto otra tecla en detonar, o aprovecho la marca para pegar más fuerte con lo que ya tengo?
>
> Esto explica de golpe medio roster: **Winchesster E** (sus spells aplican E.E. **dos veces** → detonación instantánea) pasa de ser una mejora menor a ser el eje de su kit. **Lein-Xein E** y **Willham** escalan con el *número de enemigos marcados*. **Mertinoli**, que reparte al azar entre varios enemigos, siembra marcas por todo el campo.

❓ **Catálogo de elementos pendiente** (el usuario lo explicará más adelante): cuáles son, qué hace cada detonación, si hay ventajas entre ellos.
❓ ¿Las marcas caducan? ¿Se acumulan más allá de 2?
❓ ¿Los elementos de distintos personajes se mezclan en el mismo objetivo?
❓ La Q de Floristas habla de **"efecto elemental propio"** — ¿cada personaje tiene su elemento asignado?

### 3.10 Efectos Emocionales (E.EM.)

Sistema **distinto e independiente** del elemental.

Un efecto emocional se aplica **cuando sucede algo concreto** — es decir, funciona por **disparadores**, no por acumulación. Y lo importante:

> **La emoción hace cosas distintas en cada personaje.** El *enojo* provoca una cosa en un personaje y otra en otro. No es una tabla global de estados: es una capa de **identidad por personaje**.

❓ ¿Qué emociones existen? (confirmada: **enojo**)
❓ ¿Qué las dispara — recibir daño, ver caer a un aliado, fallar ataques, quedarse sin CM?
❓ ¿Son siempre negativas, o hay emociones beneficiosas?
❓ ¿Duran turnos, o hasta que algo las resuelve?
❓ ¿Se pueden provocar deliberadamente en el enemigo?

> Nota: junto con la Carga Musical y el CM, esta capa emocional refuerza una identidad temática clara — **música y emoción como sistemas de recurso**, no como decorado.

### 3.11 Desgaste

**Reducción porcentual de TODAS las estadísticas a la vez.** No es daño ni un debuff puntual: es una degradación global y **persistente** del personaje.

#### Fuente principal: CURARSE

⚠️ *Corrección de una lectura previa: los vendajes **no quitan** desgaste. Lo **generan**.*

```
Cada X de vida curada  →  +1% de desgaste
```

**Ejemplo confirmado** (X = 50): un personaje que lleva **750 de vida curada** en un cuarto de la run acumula **15% de desgaste** — todas sus estadísticas al 85%.

#### Qué curación genera desgaste — CONFIRMADO

**Regla:** solo se libra la curación que viene de una **skill propia sobre uno mismo**. Tiene que ser **a fuerza una skill** — cualquier otra vía de recuperar vida desgasta.

| Origen de la curación | ¿Genera desgaste? |
|---|---|
| **Skill** de curación **sobre uno mismo** | ❌ **No** |
| **Robo de vida** propio (te autoinfliges daño y te curas) | ❌ **No** |
| **REG. HP** | ❌ **No** |
| **Skill** de curación lanzada por **otro personaje** | ✅ Sí |
| **Vendajes** | ✅ Sí |
| **Objetos** (consumibles, comida) | ✅ Sí |
| **Artilugios** | ✅ Sí |

> El eje no es "propia vs. ajena" sino **skill propia vs. todo lo demás**. Curarte con un objeto en tu propio turno **también desgasta**, aunque lo uses tú.

> ### La autosuficiencia es la sostenibilidad limpia
>
> Esto le da la vuelta a lo que parecería intuitivo: **que te curen sale caro; curarte tú, no.**
>
> - **Bladeus** puede sostener su bucle de auto-daño → robo de vida **indefinidamente sin degradarse**. Su estilo *auto-daño / robo de vida* no es una excentricidad: es la forma más barata de sobrevivir del juego.
> - **Hallen**, en cambio, cura a los demás — y **cada punto de vida que reparte desgasta a quien lo recibe**. Un sanador no es gratis: cambia HP de ahora por estadísticas para siempre, y la factura la paga el aliado.
> - Los **escudos y la mitigación** siguen siendo lo más limpio de todo: el daño evitado nunca se cura, así que **nunca genera desgaste**.
>
> - Los **objetos de curación desgastan igual**, aunque los uses tú. Y como además consumen un **turno de ventaja** (§1.3), curarse con un consumible se paga **dos veces**: en tempo y en estadísticas permanentes. El botiquín es el último recurso, no el primero.
>
> Jerarquía de sostenibilidad resultante, de más barata a más cara:
> **evitar daño** · **REG.HP** → **skill de curación propia** → **que te cure un aliado** → **objetos y vendajes**.
>
> Y encaja con Halltus R EVOL: escudar al que ya está desgastado es exactamente la respuesta correcta, porque curarlo solo empeoraría su estado a largo plazo.

> ### El coste real de sobrevivir
>
> Curarse no es gratis: **es un préstamo contra el resto de la run.** Cada vendaje te devuelve vida ahora y te cobra estadísticas para siempre. Un equipo que aguanta a base de curación llega al late game entero pero **degradado**.
>
> Esto convierte la mitigación en algo estructuralmente mejor que la curación: **el daño evitado no genera desgaste, el daño curado sí.** De golpe, la acción Defensa, los escudos, la evasión y la ARM/RM dejan de ser alternativas a curar y pasan a ser la forma *barata* de sobrevivir. Curar es la cara.
>
> Y como es acumulativo a lo largo de la run, la presión no está en un combate: está en **la campaña entera**.

#### Fuente secundaria: habilidades

Algunas habilidades lo aplican directamente como coste. **Kronwell R** otorga **70% de desgaste** de golpe — deja al personaje al 30% de todas sus estadísticas. De ahí su etiqueta de *auto-desintegración*.

> El CM es el coste **compartido y recuperable**; el desgaste es el coste **personal y permanente**. Una habilidad devastadora puede pagarse con recurso de equipo, con la integridad del propio personaje, o con ambas.
>
> **Halltus R EVOL** —escudo a un compañero con X% de desgaste— confirma que es un estado que el equipo **gestiona**: hay soportes cuya función es sostener a los que se degradan.

#### Anti-desgaste (mitigación del desgaste)

El desgaste es **definitivo**: no se limpia ni se cura. La **única** forma de reducirlo es la estadística **Anti-desgaste**, que actúa como un **multiplicador sobre el desgaste total acumulado**:

```
Desgaste efectivo = Desgaste acumulado × (1 − Anti-desgaste)
```

**Ejemplo dado:** `30% de desgaste × 0.89 = 26.7%` efectivo (Anti-desgaste del 11%).

> Es una mitigación **multiplicativa**, así que su valor **crece con el desgaste acumulado**: invertir en Anti-desgaste vale poco al principio de la run y mucho al final. Es una estadística de late game por construcción.

❓ ¿Cómo se sube el Anti-desgaste? (¿LV, equipo, objetos, eventos?)
❓ ¿Tiene tope? ¿Y el desgaste — al 100% el personaje quedaría a cero en todo?
❓ ¿El valor X (vida curada por 1% de desgaste) es fijo, o varía por personaje o dificultad?
#### Escudos: la sostenibilidad sin factura — CONFIRMADO

El escudo **se resta siempre antes que el HP**. Mientras quede escudo, la vida no baja:

```
daño  →  ESCUDO  →  (si lo agota)  →  HP
```

Da igual el estado del personaje: con la vida a tope o a medias, **el escudo es lo primero que se consume**.

> **Por eso el escudo no genera desgaste: no cura nada.** Impide que el HP baje, así que no hay vida que reponer después. Es la única forma de sostenerse que no cuesta ni turno de ventaja, ni estadísticas permanentes, ni CM ajeno.
>
> Con esto la jerarquía queda cerrada y es muy nítida:
> **prevenir daño es estructuralmente superior a repararlo.** Escudos, mitigación y evasión son gratis a largo plazo; la curación, no.

**Reglas del escudo — CONFIRMADAS:**

| Regla | Detalle |
|---|---|
| **Duración** | En turnos, según diga la habilidad de origen. |
| **No se apilan** | Un escudo nuevo **sustituye** al anterior si tiene **más turnos** de duración. |
| **Absorción total** | El **daño verdadero NO atraviesa el escudo**: se absorbe igual que el AD y el AP. |

> El daño verdadero ignora ARM y RM, pero **no ignora el escudo**. Eso convierte al escudo en la única defensa que funciona contra absolutamente todo — y le da una razón de ser clara frente a la armadura, que sí es esquivable por la vía del DV.
>
> Y como los escudos **se sustituyen en vez de apilarse**, dos soportes escudando al mismo aliado se pisan el uno al otro. Es un freno natural contra las composiciones de doble soporte.

❓ ¿El desgaste afecta también al **aporte de CM** al pool compartido? Sería coherente — y muy severo.
❓ Si el escudo nuevo tiene **más absorción pero menos turnos**, ¿se pierde? La regla actual mira solo la duración.

## 4. Objetos

❓ Pendiente: catálogo de consumibles, comida y artilugios; rareza; obtención.

## 5. Progresión y curva de partida

Los personajes **escalan por LV**, y el juego reconoce **fases de partida**:

```
EARLY  →  MID  →  LATE  →  ULTRA LATE
```

**Consecuencia de diseño buscada:** un estilo de juego puede ser **horrible en early** y volverse fuerte al alcanzar cierto LV. Es decir, los arquetipos no están balanceados de forma plana: están balanceados **a lo largo de la curva**. Hay estilos que pagan un peaje temprano a cambio de un techo alto.

Esto conecta con:
- **Carga Musical**, cuyo valor cambia según la fase (§1.8).
- **Combos EVOL** (§1.6), que presumiblemente se abren con el nivel.

❓ ¿Las fases son un estado formal del juego (con efectos mecánicos) o solo un lenguaje de diseño para hablar de la curva?
❓ ¿Qué escala con LV exactamente? (stats, nuevas skills, nuevas teclas EVOL, más turnos de ventaja…)
❓ ¿LV por personaje o de equipo? ¿Hay tope?
❓ ¿Dónde ocurre la progresión — mazmorra, campaña, entre combates?

## 6. Técnico

❓ Pendiente hasta cerrar alcance.

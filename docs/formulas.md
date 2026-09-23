# E.N.M.A — Referencia de cálculo

> Consolidado de **STATS MASTER**, **RESOLUTIONATOR MASTER**, **CM MASTER** y **BASE MASTER**.
> **Jerarquía canónica declarada:** ante contradicción sobre cálculo de stats, agregación, caps, escalado o desgaste, **prevalece STATS MASTER**.

---

## 0. Constantes

| Constante | Valor |
|---|---|
| `HIT_MIN` / `HIT_MAX` | 5 / 95 |
| `CRIT_CAP` | 100 |
| `ARM_CAP` / `RM_CAP` | 90 / 90 |
| `PEN_CAP` | 80 (% de la defensa del objetivo) |
| `DV_CAP` | 100 |
| Multiplicador de crítico | **×1.8** |

---

## 1. Escalado por nivel (1–18)

```
g(L) = L / 18          (lineal)

Valor(L) = MAX × g(L)
Pct(L)   = CAP × g(L)
```

### Qué es un MAX — CONFIRMADO

**El MAX es la aspiración del personaje en esa run**: el techo al que ese personaje concreto puede llegar en esa estadística. **No es un valor global del juego.**

- Es **variable por personaje**. Dos personajes pueden tener MAX de crítico muy distintos.
- Puede ser un número pequeño: un personaje con MAX de crítico de ~3.9% es perfectamente válido. **El crítico no es abundante en este juego.**

**El porcentaje de MAX alcanzado mide el progreso de la partida.**

> Si un personaje va por el **90% de su aspiración**, el juego está casi terminado — y en ese punto su crítico podría ser un 3.5%, si su MAX era 3.9%.

Es una lectura elegante: no hace falta mirar el nivel para saber dónde estás. **Lo cerca que está cada personaje de su propio techo te dice cuánto queda de historia.**

### ✅ El MAX es mejorable — las cuatro capas del sistema

**El MAX no es fijo: en el juego se puede mejorar la aspiración de un personaje.** Eso reconcilia dos tablas que parecían contradecirse — no compiten, son capas distintas:

```
1) CAP DEL JUEGO        techo absoluto que esa stat puede alcanzar jamás
   (STATS MASTER)       p. ej. Velocidad de Ataque = 0.60

2) MAX ASPIRABLE        el techo de ESTE personaje ahora mismo.
   (por personaje)      Empieza por debajo del CAP y SE MEJORA JUGANDO.
                        p. ej. 0.12 → puede crecer hacia 0.60

3) VALOR POR NIVEL      MAX_aspirable × g(L)

4) VALOR OPERATIVO      × (1 − desgasteEfectivo)
```

> Por eso hay **dos ejes de progresión simultáneos**: subir de nivel te acerca a tu techo actual, y mejorar el MAX **levanta el techo**. Un personaje puede estar al 100% de su aspiración y aun así ser débil si nunca la subió.
>
> Y explica Ultra Late (abajo): al llegar a LV18, `g(L) = 1.0` y el eje del nivel se agota — pero **mejorar el MAX sigue vivo**. Es la única progresión que queda cuando ya no quedan niveles.

❓ ¿Cómo se mejora el MAX? (¿eventos, objetos, emblemas, historia?) ¿Tiene coste? ¿Es permanente entre runs?

### Sobre el *Calculador de Stats OFICIAL*

⚠️ **El Excel es una herramienta de comprobación, no una fuente de datos.**

Metes en la columna B los MAX de un personaje, un nivel en `Lv.Ejemplo` y un porcentaje en `DESGASTE`, y las columnas `Resolucionador PRE/Post` te devuelven el valor real antes y después del desgaste. Las tablas inferiores muestran la curva de HP y AD por nivel.

**Los números que tenga la columna B son valores de prueba de la última sesión de trabajo, no caps canónicos.** No citarlos como si fueran oficiales.

**Estructura verificada** (útil para replicar el cálculo en código):

| Columna | Contenido |
|---|---|
| B | MAX aspirable (entrada) |
| C | Valor a LV1 = `MAX × (1/18)` |
| E / F | Resuelto a nivel máximo, pre / post desgaste |
| G | Nivel de ejemplo (entrada) |
| H / I | Resuelto a ese nivel, pre / post desgaste |

El desgaste se aplica como `× (100 − desgaste)/100`, **sin** el paso de mitigación por Anti-Desgaste — es una vista simplificada.

| Fase | Niveles | Lectura |
|---|---|---|
| **Early** | 1 – 5 | bonus pequeño / setup |
| **Mid** | 6 – 12 | el bonus ya se nota |
| **Late** | 13 – 18 | cercano al techo |
| **ULTRA LATE** | **> 18** | **nivel máximo alcanzado, la partida continúa** |

Picos de referencia: **LV6** inicio del Mid · **LV11** Mid alto · **LV16** Late alto · **LV18** techo.

### Ultra Late — la fase después del tope

**Ultra Late no es un tramo de niveles: es lo que pasa cuando ya no quedan niveles.** Se ha alcanzado el LV18 (salvo que algo diga lo contrario) y **todavía quedan batallas por delante**.

Mecánicamente eso significa que `g(L) = 1.0` y **todas las estadísticas están congeladas en su MAX**. La progresión por nivel se acabó.

> ### Lo que queda cuando ya no se puede crecer
>
> En Ultra Late las únicas variables vivas son las que **no** dependen del nivel:
> - Emblemas y sus breakpoints
> - Gadgets y sus mejoras
> - Niveles de habilidad y evoluciones
> - Objetos y equipo
> - …y el **desgaste**, que solo sube
>
> **Es la única fase en la que el equipo únicamente puede empeorar.** Las estadísticas ya no suben, pero el desgaste sigue acumulándose con cada curación. Un personaje al 30% de desgaste en Ultra Late no puede compensarlo subiendo de nivel: está al 70% de su techo y ahí se queda.
>
> ### ✅ El declive ES el punto — CONFIRMADO
>
> **No hay vía de recuperación en Ultra Late. Es el clímax.**
>
> El tramo final de la run es una **guerra de desgaste literal**: las estadísticas están congeladas, el desgaste solo sube, y lo único que queda es aguantar con lo que tienes. Cada curación que aceptas te acerca al final peor de lo que estabas.
>
> Con permadeath encima, eso significa que el último tercio de la partida se juega **con lo que quedó**, no con lo que podrías conseguir. La tensión no viene de enemigos más fuertes: viene de que **tú eres más débil cada vez** y lo sabes.
>
> Es coherente con todo el resto del sistema. Desde el primer bloque de diseño, este juego dice lo mismo de muchas formas: prevenir es mejor que reparar, bastarse solo es mejor que depender, y todo recurso es finito y compartido. Ultra Late es esa tesis llevada a su conclusión — **el juego termina cuando ya no puedes permitirte nada más.**

**Válvula:** habrá **eventos concretos que quitan desgaste**. Son la excepción deliberada, no un sistema de recuperación.

❓ Pendiente: qué eventos, con qué frecuencia y cuánto desgaste retiran.

---

## 2. Pipeline de stats (orden canónico)

```
1. Stats base del personaje
2. Escalado por nivel        → BaseRef
3. Mitigación de desgaste
4. Desgaste efectivo
5. Aplicar desgaste a BaseRef → StatOperativa
6. Flats y porcentajes de buffs/debuffs
7. Multiplicadores finales (multFinal)
8. Derivados (evasión efectiva, reducción…)
9. Clamps finales
```

**Fórmula oficial de agregación:**

```
StatOperativa = BaseRef × (1 − desgasteEfectivo)

StatFinal = StatOperativa + Σ Flats + (StatOperativa × Σ Pcts)
```

> ### Regla clave: los porcentajes NO se multiplican entre sí
> Todos los porcentajes leen **la misma StatOperativa**, sin encadenarse. Esto es lo que impide las explosiones exponenciales y el doble escalado.
>
> **Ejemplo canónico:** BaseRef AD 48, desgaste 10% → StatOperativa 43.2. Arma +25 flat, arma +2%, buff +3%.
> El 2% y el 3% leen **43.2**, no 48 ni 68.2.
> `43.2 + 25 + (0.864 + 1.296) = 70.36`

**Capas de modificador:** `flat` · `pctAdd` · `multFinal` (capa final excepcional) · `conditional` (por trigger) · `flags` (inmunidades/overrides).

**Orden de recolección de fuentes:** base/nivel → arma → armadura → accesorios/recuerdos/emblemas → gadgets → pasivas (personaje, arma, armadura, gadget) → rasgos → buffs/debuffs → emoción → elemento → entorno/clima/música/tempo → estados temporales.

---

## 3. Desgaste (fatiga)

Deterioro físico acumulado. No es daño, ni debuff, ni efecto temporal: es el coste de salir a campaña.

**Dentro de una run no se recupera.** Entre campañas sí: baja solo con los días, y más rápido con tratamiento (hospital). La velocidad depende de la gravedad.

```
desgasteReal      = desgasteActual × (1 − mitigacionDesgaste)
desgasteEfectivo  = max(0, desgasteReal − supresionTemporal)
multiplicadorDesgaste = 1 − desgasteEfectivo
```

**Ejemplo:** 20% actual, 10% mitigación → 18% real. Con torniquete de 5% → 13% efectivo. Al expirar vuelve a 18%.

### Generación — ✅ DECIDIDO

```
Cada 20% de la VIDA BASE recuperada (acumulado)  →  +1% de desgaste
```

Es **proporcional**, no un umbral fijo de puntos. Curarte un quinto de barra cuesta 1% seas quien seas y estés en el nivel que estés, y **una barra entera cuesta 5%**.

> ### ✅ El 1/5 sale de un objetivo de diseño, no de una intuición
>
> **Que un jugador normal llegue a LATE con ~35% de desgaste acumulado.**
>
> Con una run de ~40 combates, late empieza sobre el combate 27. Un jugador que se cure un 25% de barra por combate habrá curado 6,75 barras para entonces, y a 1/5 eso son **34%**.
>
> | Cómo juegas | Al llegar a late | Al final de la run |
> |---|---|---|
> | Curándote poco | 20% | 30% |
> | **Normal** | **34%** | 50% |
> | Apoyado en el curandero | 68% | 100% antes de acabar |
>
> La curva hace lo que el sistema dice en todas partes: **no castiga jugar normal, castiga depender de la curación.** El que previene llega entero; el que repara revienta su margen antes de terminar.

> ### Por qué proporcional y no plano
>
> La vida crece con el nivel (`HP_base(L) = HP_MAX × g(L)`). Con un umbral fijo en puntos, curarte "un quinto de barra" costaría cada vez más desgaste conforme subes — **el mismo acto castigaría cinco veces más en LV18 que en LV1** sin que el jugador hiciera nada distinto.
>
> Y penalizaría a los personajes de vida alta: un tanque curándose un quinto de su barra pagaría mucho más que un caster curándose un quinto de la suya. Los tanques ya reciben más daño y ya necesitan más curación; no hace falta cobrarles además una tarifa peor por unidad.
>
> Es el mismo principio que se aplicó a Tempo: **ninguna regla debe romperse cuando las estadísticas escalan.**

**Proyección a final de run:**

| Curación media por combate | 20 combates | 40 combates | 60 combates |
|---|---|---|---|
| Muy poca (10% de barra) | 10% | 20% | 30% |
| **Moderada (25%)** | 25% | **50%** | 75% |
| Alta (media barra) | 50% | **100%** | 100% |
| Intensiva (una barra) | 100% | 100% | 100% |

Con la Resistencia Desgaste al tope (cap 50%), un 50% acumulado baja a **25% real** — invertir en ella deja de ser opcional para quien juegue apoyado en la curación, y sigue siendo prescindible para quien previene.

> **Dial de ajuste:** esta fracción. Está en **1/5**. Historial: 1/2 → se probó 1/3 → se volvió a 1/2 → 1/5, este último ya calibrado contra el objetivo de los 35% en late. Es un solo número en `Constantes.cs` y los tests dicen al instante qué cambia.

> ⚠️ **De qué depende el 1/5.** De que una run sean ~40 combates, que es lo que da por supuesto esta tabla pero [dia.md](dia.md) aún tiene como pendiente. Si la run resulta más larga, la fracción sube: 30 combates → 1/7 · **40 → 1/5** · 50 → 1/4 · 60 → 1/3,5.

```
applyFastHeal(target, healAmount):
    hpCurrent = min(hpCurrent + healAmount, hpBase_eff)
    fastHealAccum += healAmount
    step = 0.2 × hpBase_eff
    while fastHealAccum >= step:
        fastHealAccum -= step
        fatiguePct += 0.01     (clamp 0..1)
```

### Alcance

| Afecta | No afecta |
|---|---|
| Todas las **primarias**: HP, DF, DM, ARM, RM, ACC, EVA, CRIT | **CM actual** |
| Todas las **secundarias ofensivas** | **CM máximo** |
| Todas las **secundarias defensivas** | **Cooldowns** |
| | **Flags de estado** |

**Interacción con vida:**
```
HPMaxEfectivo = HP_agregado × multiplicadorDesgaste
Si sube el desgaste en combate:  HPActual = min(HPActual, HPMaxEfectivo)
```

**Nota:** la **Regeneración de Vida no genera desgaste** por sí misma, y el Dominio Médico no añade desgaste extra.

---

## 4. Acierto

```
EVA_eff = EVA × max(0.10, 1 − 0.03 × stacksEvasionDown)

Si (ACC + EVA_eff) <= 0:   Hit% = HIT_MIN
Si no:                     Hit% = 5 + 90 × ( ACC / (ACC + EVA_eff) )

Hit% = clamp(Hit%, 5, 95)
hit  = RNG(0..100) < Hit%
```

**Reglas:**
- Un fallo **no corta la secuencia**.
- Si falla un hit: no aplica daño, ni elemento, ni efectos on-hit de ese impacto.
- Si la acción consume recurso, **se consume aunque falle**.

> ✅ **El valor por stack es 3%** (el canon original decía 1%). Elegido para que el **acierto máximo suba ~+50% en late** contra un objetivo evasivo. Ver el desglose abajo.

### Calibración del desgaste de evasión

Objetivo de diseño: **+50% de acierto máximo en late.**

| Fase | Golpes en la ronda | Acierto máximo | Ganancia |
|---|---|---|---|
| **Early** | ~12 | 39.9% | +14% |
| **Late** | ~32 | **51.9%** | **+48%** |

*(objetivo evasivo: PRESS 50 vs EVA 100, base 35%)*

**El mismo número produce las dos cosas** porque en late hay más ataques por ronda. La mecánica escala sola con la curva de la partida, sin reglas adicionales.

**Y se apunta sola al arquetipo que debe castigar:**

| Objetivo | Base | Máximo en late | Ganancia |
|---|---|---|---|
| Evasivo (EVA 100) | 35% | 51.9% | **+48%** |
| Igualado (EVA 50) | 50% | 61.8% | +24% |

Cuanta más evasión tenga el objetivo, más margen hay para erosionar y mayor es el beneficio relativo. Un enemigo que ya era fácil de golpear no puede volverse mucho más fácil. **No hace falta declarar que esto es anti-evasión: lo es por construcción.**

Valores descartados: 1% (no mueve nada, +13% en late) · 4-5% (se pasan del objetivo: +72% y +96%) · 8-10% (tocan el suelo del 10% a media ronda y borran la evasión).

### Dos fuentes de `stacksEvasionDown` — ambas cuentan

| Fuente | Cuándo se quita |
|---|---|
| **Desgaste por esquivar** — el defensor esquiva y su EVA baja | Al **terminar el turno del BANDO atacante** — es decir, persiste durante toda la ronda y lo aprovechan todos los aliados. |
| **Debuffs de skill/terreno** — Rudentlein W, casilla de ortigas de Madrerick… | Cuando **expiran sus turnos**, o al **limpiarse** con skill, objeto, etc. |

> Las dos alimentan la misma `EVA_eff`, pero tienen vidas distintas: el desgaste por esquivar dura **toda la ronda del bando atacante**; el debuff de skill es un estado con duración propia que sobrevive varias rondas.
>
> **El desgaste por esquivar es acumulativo de equipo.** Los stacks que genera el primer aliado siguen ahí cuando entra el cuarto. Eso convierte el orden de turnos —que el jugador elige— en la herramienta que hace funcionar la erosión de evasión: las **armas ligeras abren** desgastando, y la **pesada remata** con mucha mejor precisión.
>
> Encaja con los otros dos sistemas que también viven en el ciclo: el **pool de CM compartido** y las **marcas elementales**, que las pone uno y las cobra otro.
>
> Consecuencia: reducir la EVA con una habilidad **antes** de que entren tus atacantes multiplica el valor de todos los golpes de la ronda. Ahí está la sinergia entre un Rudentlein y un arma ligera.

### Objetivo durante los básicos

**No se puede cambiar de objetivo entre golpe y golpe.** Todos los básicos de un turno van al mismo enemigo.
Solo reparten daño entre varios los que sean **skills de dos o más objetivos**.

> Esto es lo que hace funcionar el desgaste de evasión: como no puedes repartir, cada turno de básicos es una **apuesta concentrada** sobre un solo objetivo, y el desgaste se acumula donde importa.

---

## 5. Daño (por hit)

```
0) CRIT (decidido ANTES)
   critWouldApply = skill.canCrit AND RNG(0..100) < atk.critPct

1) DAÑO BRUTO
   FÍSICO:  D_raw = physAtk × multSkill × multContext × damagePhysicalFinalMult
            PenArmor_eff = min(penArmor, 0.8 × def.armorValue)
            Armor_f = max(0, def.armorValue − PenArmor_eff)
            Red_f   = min(0.90, Armor_f / (Armor_f + 100))

   MÁGICO:  D_raw = magAtk × multSkill × multContext × damageMagicalFinalMult
            PenRM_eff = min(penResist, 0.8 × def.resistValue)
            RM_f  = max(0, def.resistValue − PenRM_eff)
            Red_f = min(0.90, RM_f / (RM_f + 100))

   VERDADERO: Red_f = 0 (ignora ARM y RM por completo)

2) SEPARAR DV%
   DV     = clamp(skill.trueDamagePct, 0, 1)
   D_true = D_raw × DV
   D_norm = D_raw − D_true

3) REDUCIR SOLO EL NORMAL
   D_red_saved = D_norm × (1 − Red_f)

4) CRÍTICO DESPUÉS DE LA REDUCCIÓN
   D_red_candidate = critWouldApply ? D_red_saved × 1.8 : D_red_saved

5) DEFEND ANULA EL CRÍTICO en el primer impacto
   Si isDefendActiveThisHit:  D_red_final = D_red_saved ; critApplied = false
   Si no:                     D_red_final = D_red_candidate

6) TOTAL
   D_final = max(0, D_red_final + D_true)

7) APLICACIÓN
   D_final va primero al ESCUDO, el sobrante al HP.
   El daño verdadero NO ignora escudos.
```

**Básico vs habilidad:**
```
DañoBásico = DañoOfensivoRef × multiplicador del ARMA

DañoSkill  = DañoBaseSkill + (DañoOfensivoRef × CoefSkill)
```

### ✅ Multiplicador de daño por arma

| Arma | Golpes (LV18) | Daño × golpe | Daño/turno | CM/turno | Extra |
|---|---|---|---|---|---|
| **LIGERA** | 8 | **×0.6** | 252 | 1.08 | **sus golpes aplican DOBLE desgaste de EVA** |
| **MEDIA** | 5 | **×1.0** | 253 | **1.45** | — |
| **PESADA** | 1 | **×1.5** | 75 | 0.70 | cobra el amplificador elemental íntegro |

> ### Cada arma gana en su terreno
>
> Los tres papeles declarados —**ligera anti-evasión · media equilibrio · pesada golpe fuerte**— se cumplen numéricamente:
>
> | EVA del objetivo | Ligera | Media | Gana |
> |---|---|---|---|
> | 0.7 (normal) | 252 | 253 | **media**, por un pelo |
> | 1.4 (evasivo) | 183 | 179 | **LIGERA** |
> | 2.1 (muy evasivo) | 146 | 141 | **LIGERA** |
> | 2.8 (extremo) | 124 | 119 | **LIGERA** |
>
> Empate casi exacto contra objetivos normales, que se rompe a favor de la ligera **en cuanto el objetivo esquiva**.

**Por qué hacen falta las dos cosas juntas.** Se probaron por separado y ninguna bastaba:
- Solo el **doble desgaste** (con daño ×0.5): la ligera perdía en *todos* los escenarios — su daño base era demasiado bajo.
- Solo **más precisión** (+40% PRESS): empataba, no ganaba.
- Subir el daño **sin** el desgaste doble: la ligera ganaba **en todas partes**, no solo en su nicho.

El daño la pone a la altura; el desgaste doble le da el nicho.

**Por qué el desgaste doble es de la ligera.** El desgaste de EVA existe para que muchos golpes pequeños abran a un objetivo escurridizo — pero **la media también lo aprovecha** con sus 5 golpes, así que no diferenciaba nada. Dárselo doble a la ligera convierte una mecánica compartida en la firma de un arma. Y encaja con la ficción: golpes encadenados que no dejan recomponerse.

### La pesada depende de mejorar su base

| Base de golpes | Golpes a LV18 | Daño/turno |
|---|---|---|
| 1 | 1 | **75** |
| **2** | 3 | **225** |
| 3 | 5 | **375** |

⚠️ **Sin mejorar su base, la pesada no es jugable — es una promesa.** Si las fuentes de "+1 BA" son escasas, esa arma no existe durante media partida. Ver [musica.md](musica.md) §1 sobre las vías para subir la base.

La habilidad escala desde la referencia ofensiva canónica del momento, **no** desde un daño ya reescalado — así se evita el double-dipping.

---

## 6. Defensa (DEFEND)

| Regla | Detalle |
|---|---|
| Bonus | **+50% de defensa** vs físico y mágico |
| Consumo | Se gasta en el **primer impacto recibido** |
| Crítico | Queda **anulado** en ese primer impacto |
| Después | El bonus se pierde |
| CM | **Duplica** la ganancia de CM al recibir ese hit |

---

## 7. Carga Musical (CM)

### Capacidad

```
CM_max = Σ storage(Activos) + 0.5 × Σ storage(KO)
```
Barra **compartida de equipo**. Si `CM_current > CM_max`, **se pierde el excedente** (clamp inmediato al entrar alguien en KO o morir).

> ### ✅ El CM máximo NO escala con el nivel
>
> **`storage` es una ventaja de suministro, no de poder.** Tener más CM máximo no te hace más fuerte: te da más margen logístico. Por eso **no sube con el nivel** y no se puede mejorar progresivamente.
>
> Solo aumenta con **cosas específicas que suman de forma unitaria** (+1, +2), nunca con una curva. Así el apartado queda controlado: sin escalado, la capacidad no se dispara a números grandes y siempre se sabe cuánto bote hay.

**Pool de referencia:** `storage 3 × 4 personajes = 12 CM`, constante toda la run.

### Generación por básico (por hit)

| Arma | Base | En tempo |
|---|---|---|
| **LIGERA** | 1/8 | 2/8 |
| **MEDIA** | 1/4 | 2/4 |
| **PESADA** | 1/2 (a 1) | — |

```
En tempo:         CM_total = CM_base × 2
Fuera de tempo:   CM_total = CM_base
Si falla el hit:  CM = 0
```

> ⚠️ **El bonus de tempo es ×2**, no el `+1/3` de CM MASTER — ese valor queda **superado**.
> Es lo que produce la tabla de arriba (`1/8 → 2/8`) y lo confirma la aritmética de golpes por CM (4/2/1). Ver [conflictos.md](conflictos.md) §D2.

### Generación por recibir daño

```
gainDef = 0.5 × cmGainPerHit(arma del ATACANTE, tempo = false)
gainDef *= (DEFEND ? 2 : 1) × modificadores
```
El **tempo no aplica** al recibir. Si es invulnerable, no gana.

### Modificadores (suma lineal)

```
CM_final = X + X·m1 + X·m2 + ...
Ejemplo: X=1, +20% y +10%  →  1.3
```
Ejemplos de modificador: **Enfermo ×2**, **Terror ×0.75**.

### Fuentes válidas

✅ básicos · habilidades · gadgets · recibir daño · otros eventos directos de combate
❌ debuffs pasivos · efectos indirectos sin interacción directa

### Coste de habilidades — donde vive TODA la progresión

Como el pool es constante (12), **la única variable que crece es el poder adquisitivo**: las habilidades se abaratan. La moneda no se infla; las cosas bajan de precio.

```
coste real = precio base × FACTOR de la fase
```

**Precios base** (no cambian nunca): `Q = 1 · W = 2 · E = 3 · R = 4` → combo completo **10**

**Factor por fase — BASELINE:**

| Tecla | Early ×1.00 | Mid ×0.60 | Late ×0.40 |
|---|---|---|---|
| Q | 1.0 | 0.6 | 0.4 |
| W | 2.0 | 1.2 | 0.8 |
| E | 3.0 | 1.8 | 1.2 |
| R | 4.0 | 2.4 | 1.6 |
| **Combo completo** | **10.0** | **6.0** | **4.0** |

**Lo que eso permite** (pool 12):

| Fase | Combos que caben en el pool | Combos sostenibles por ronda |
|---|---|---|
| Early | 1 | 0.4 |
| Mid | 2 | 0.7 |
| Late | **3** | **1.6** |

> ### ⚠️ El suelo duro es 0.33 — por debajo es spam
>
> | Factor | Coste | Combos por ronda | |
> |---|---|---|---|
> | 0.25 | 2.5 | **2.5** | ❌ el recurso deja de decidir nada |
> | 0.33 | 3.3 | 1.9 | límite |
> | **0.40** | **4.0** | **1.6** | ✅ uno por ronda y sobra un poco |
> | 0.50 | 5.0 | 1.2 | más justo |
>
> La recompensa al jugador hábil **no es poder pulsar más veces**: es elegir cuándo desatar la ráfaga guardada y cuadrarla con el estribillo y con Clasical.

> ### Dos cosas distintas: la ráfaga y el sostenido
>
> - **El POOL es la ráfaga.** Con 12 guardados sueltas 3 combos de golpe. Es el momento dramático.
> - **La GENERACIÓN es el sostenido.** Da para ~1 combo por ronda. Es lo que puedes hacer siempre.

### Esta tabla es el BASELINE, no la ley

Sirve para **construir los kits y calcular su coste promedio**. Cada kit se desvía de ella a propósito:

| Perfil | Curva de descuento |
|---|---|
| **Kit de early** | Baja poco — ya es asequible desde el principio, pero nunca despega |
| **Kit de late** | Baja mucho — impagable al principio, devastador al final |
| **Kit de ultra late** | Baja tardísimo — solo rinde cuando ya no quedan niveles |

**La desviación respecto al baseline ES la identidad del estilo.** El ritmo global lo marca esta tabla; la curva de cada personaje la marca su precio.

### Calendario de desbloqueo de teclas

| Nivel | Teclas disponibles |
|---|---|
| **LV1–3** | Q, y se van desbloqueando **W y E** |
| **LV6** | **R** — ya se puede ejecutar el combo completo |

> Esto explica por qué la escasez de CM en early **no es un problema**: no tienes el kit completo, así que no necesitas pagarlo. Con lo que generas en LV1-3 alcanza para **Q+W cada ronda**, que es todo lo que tienes.
>
> Y crea un buen momento en **LV6**: desbloqueas la R y el combo entero cuesta 10 cuando generas ~4 por ronda. **Tienes el juguete a la vista y aún no puedes permitírtelo.** Subir a mid deja de ser algo que pasa y se convierte en un logro.

❓ Entre LV1-3 y LV6 el número de teclas lanzables por ronda no cambia (Q+W). Si ese tramo se hace largo puede sentirse plano.

### CLASICAL

⚠️ **Regla vigente en [musica.md](musica.md) §3**, que sustituye a la de CM MASTER.

**Disparador: la ACCIÓN de Clasical**, cuando con ella la barra llega al máximo.
La condición antigua —*empezar el turno con la barra llena*— **se autodestruía**: si estás lleno gastas, y entonces no amaneces lleno. Simulado sobre 6 rondas no se activaba nunca, en ninguna fase.

```
Acción de Clasical:
    aporte = storage propio (3 CM) × multiplicador de la sección musical
    si la barra llega al MÁXIMO  →  CLASICAL se enciende durante el ciclo

Efecto: ×1.5 a toda obtención de CM, MULTIPLICÁNDOSE con el multiplicador musical
Ventana: turno aliado + turno enemigo · termina al iniciar el siguiente turno aliado
```

El excedente al desbordar **se pierde**, lo que limita solo los multiplicadores altos sin necesidad de un tope artificial.

**Historial del disparador** — las dos versiones anteriores quedan **superadas**:

| Versión | Disparador | Estado |
|---|---|---|
| CM MASTER | *Overflow*: `CM_actual + ganancia > CM_max` | ❌ superada |
| Levantamiento | Iniciar el turno con la barra comunitaria llena | ❌ superada — se autodestruye (ver arriba) |
| **Vigente** | **La acción de Clasical**, si con ella la barra llega al máximo | ✅ |

Lo que no cambió entre versiones: el efecto dura el **turno aliado y el enemigo**, y termina al empezar el siguiente turno aliado.

---

## 8. Elementos

**Nueve tipos:** `DARK · ICE · ELEC · NATURE · WIND · LIGHT · FIRE · SOUL · CRYSTAL`

> Efectos concretos de cada uno: **[elementos.md](elementos.md)**

| Nivel | Duración |
|---|---|
| **L1** | 1 turno |
| **L2** | 3 turnos |

```
P_element = base n% + modificadores
```

**Regla de L2 — requiere coordinación de equipo:**
- Ciclo = turno completo de ambos bandos.
- `elementSuccessActorsThisCycle[targetId] = { actorIds con éxito }`
- **L2 se produce si hay 2 éxitos en el MISMO ciclo, por 2 personajes DISTINTOS, sobre el mismo objetivo.**

**Bloqueo:** si el objetivo ya tiene **L2 activo**, se ignoran nuevos elementos.
**L1 → L2:** con L1 activo se sigue intentando; si no se cumple la condición, L1 se mantiene hasta expirar.
**Reset de ventana:** al terminar el turno del villano.

---

## 9. Estados especiales

| Sistema | Regla |
|---|---|
| **PREPARING** | +50% de defensa (ARM y RM) · +20% de precisión y evasión. `CD_prep--` al final del turno. |
| **Última Bala** | Post-daño, **no altera el daño**. No cuenta escudos, solo HP real. Si `HP_final ≤ umbral% de Vida Base` → **KO**. |
| **KO** | Deja de ser targeteable. Debe atenderse antes del siguiente turno del villano; si llega y sigue en KO → **remate → DEAD**. |
| **Inquebrantable** | Probabilidad de **autorevivirse** tras KO (cap 25%). |
| **Reflejos** | Intercepta daño letal dirigido a un aliado (cap 20%). |
| **Tenacidad** | Al inicio del turno limpia debuffs — **excepto emociones** (cap 30%). |

---

## 10. Stats secundarias — caps y escalado

### Ofensivas

| Stat | Efecto | Cap |
|---|---|---|
| Preparación | Reduce 1 turno de preparación al llegar al umbral | 75% |
| Velocidad de Ataque | `hits = base × (1 + AS)` | 60% |
| Tempo | `CM_gain = CM_base × (1 + TMP)` | 50% |
| Pen. Armadura | `Armor_f = Armor − PenArmor_eff` | ≤80% ARM objetivo |
| Pen. Mágica | `RM_f = RM − PenRM_eff` | ≤80% RM objetivo |
| Robo de Vida | `Heal = Damage × LS` | 25% |
| Dominio de Armas | `P_final = P_base × (1 + WM)` | 50% |
| Dominio de Objetos | `D_item = D_base × (1 + IM)` | 60% |
| Daño de Área | `× (1 + AOE)` | 80% |
| Daño Explosivo | `× (1 + EXPL)` | 100% |
| Última Bala | Umbral de KO | 10–15% |
| Rebote | Chance de rebotar daño a enemigo cercano | 30% |

### Defensivas

| Stat | Efecto | Cap |
|---|---|---|
| Regeneración de Vida | `HP_regen = HP_base(L) × RGN` por turno | 10% |
| Dominio Médico | `Heal_total = Heal_base × (1 + MM)` | 50% |
| **Resistencia Desgaste** | Reduce el desgaste actual en % | **50%** |
| Inquebrantable | Chance de autorevivir tras KO | 25% |
| Eficiencia Escudo | `Shield × (1 + SHD)` | 50% |
| Extensión de Escudo | Al expirar: `rnd < SHD+` → +1 turno | 30% |
| Extensión Invulnerable | Al expirar: +1 turno | 25% |
| Tenacidad | Limpia debuffs (no emociones) | 30% |
| Amplificador / Extensión de Buff | | 50% / 25% |
| Amplificador / Extensión de Debuff | | 50% / 25% |
| Dominio / Extensión de Comida | | 50% / 30% |
| Reflejos | Intercepta daño letal a aliado | 20% |

**Picos de referencia:** AS 60% → 10 hits pasan a 16 · Tempo 50% → ×1.5 de CM · Daño Explosivo 100% → 200 pasa a 400.

---

## 11. Objetos gratis (ventana pre-ofensiva)

- **1 COMIDA gratis por equipo** y **1 CONSUMIBLE gratis por equipo**, independientes entre sí.
- Solo **antes de la ofensiva**. Al iniciar la ofensiva se cierra la ventana.
- Recarga cada **3 turnos**.
- Reutilizar con `CD > 0` **consume el turno** y **no** resetea el CD.
- **Gadget: siempre consume turno.**

---

## 12. EXP por participación

**Puntos por combate:**

| Categoría | Evento | Pts |
|---|---|---|
| Presencia | Entrar al frontline | +2 |
| Presencia | Permanecer toda la pelea en frontline | +2 |
| Táctico | Realizar swap | +1 |
| Eliminación | Último golpe | +2 |
| Eliminación | KO / derribo | +3 |
| Eliminación | Asistencia | +1 |
| Rescate | Salvar aliado | +2 |
| Rescate | Revivir aliado | +3 |
| Soporte | Curación significativa | +2 |
| Control | Acción decisiva | +2 |
| Reserva | Permanecer en banca | +1 |
| Sigilo | Permanecer oculto | +1 |

**Cap anti-exploit:**
```
Pts_eff = min(Pts, 10) + max(0, Pts − 10) × 0.5
```

**Conversión:**
```
EXP_p = EXP_combat × (Pts_eff / promedio del equipo)
```

---

## 13. Orden de resolución por hit (canónico)

```
0) Pre-chequeos: locks + CM + costos
1) Hit-check (si falla, no corta la secuencia)
2) Daño:
   base → modificadores ofensivos → separar DV% →
   reducir por ARM/RM (con penetración) →
   CRÍTICO después de la reducción → escudos → HP
3) Post-hit inmediato:
   CM atacante (por arma / hit / tempo)
   CM defensor (según arma del atacante; DEFEND duplica; tempo NO aplica)
   Modificadores de CM (emociones, rasgos)
   Clamp CM_actual ≤ CM_max (pierde excedente)
4) Pasivas on-hit / on-being-hit
5) Elementos (L2 bloquea nuevos; L2 requiere 2 PJs distintos en la ventana)
6) KO / Muerte
7) Última Bala (condicionador post-daño, no altera daño)
8) Si KO/Muerte: recalcular CM_max + clamp inmediato
```

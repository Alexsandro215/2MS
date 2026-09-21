# E.N.M.A — MANUAL TÉCNICO DEL SISTEMA DE COMBATE

| | |
|---|---|
| **Versión** | 2.0 |
| **Última revisión** | Septiembre 2026 |
| **Sustituye a** | *BIOGRAFIAS 8de10.docx*, págs. 143–161 (versión 1.x, obsoleta) |
| **Autoridad** | Ante contradicción sobre cálculo, agregación, caps, escalado o desgaste, **prevalece este documento** |

> **Convención de marcas**
> ✅ Regla cerrada · ⚠️ Valor provisional, pendiente de balanceo · ❓ Decisión pendiente

---

## ÍNDICE

| § | Contenido |
|---|---|
| 1 | Estructura del combate |
| 2 | El turno |
| 3 | Estadísticas |
| 4 | Desgaste |
| 5 | Resolución del combate |
| 6 | Carga Musical |
| 7 | Música y Tempo |
| 8 | Elementos |
| 9 | Emociones |
| 10 | Estados del personaje |
| 11 | Consumibles y soporte |
| 12 | Rasgos y pasivas |
| 13 | Entorno de combate |
| A | Hoja rápida de fórmulas |
| B | Glosario de siglas |

---

# 1. ESTRUCTURA DEL COMBATE

## 1.1 Formaciones

| Zona | Capacidad | Definición |
|---|---|---|
| **FRONT** | 1 – 4 personajes | Combatientes activos. Aquí existen las casillas. |
| **BANQUILLO** | El resto del equipo | Fuera del combate activo, disponibles para relevo. |
| **ESCONDITE** | 0 – 3 según el entorno | Refugio. Uno por personaje. |

**Bando enemigo:** hasta **6** combatientes. **No tiene banquillo**; sí tiene escondites, con las mismas reglas.

```
        ALIADOS  (1-4)                          ENEMIGOS  (1-6)
   ┌───────────────────────┐              ┌───────────────────────┐
   │ [DEF][NEU][OFE]       │              │       [OFE][NEU][DEF] │
   │ [DEF][NEU][OFE]       │   ← FRENTE → │       [OFE][NEU][DEF] │
   │ [DEF][NEU][OFE]       │              │       [OFE][NEU][DEF] │
   │ [DEF][NEU][OFE]       │              │       [OFE][NEU][DEF] │
   └───────────────────────┘              │       [OFE][NEU][DEF] │
   ┌───────────────────────┐              │       [OFE][NEU][DEF] │
   │ BANQUILLO             │              └───────────────────────┘
   └───────────────────────┘                    (sin banquillo)

   ESCONDITES: 0 a 3 · 1 personaje cada uno · ambos bandos
```

## 1.2 El tablero

Cada combatiente del front ocupa una **línea propia de 3 casillas**. Solo se desplaza adelante o atrás sobre ella. Las líneas son independientes: nadie compite por casillas.

### Efectos de casilla

| Casilla | PRESS | EVA | Restricciones y ventajas |
|---|---|---|---|
| **OFENSIVA** | **+30%** ⚠️ | **−30%** ⚠️ | — |
| **NEUTRAL** | — | — | Sin bonus ni penalización |
| **DEFENSIVA** | **−30%** ⚠️ | **+30%** ⚠️ | **Prohibido todo AD** · los objetos gratis **no generan cooldown** |

> **El tablero solo modifica PRESS y EVA.** Cualquier otra alteración en una casilla proviene del **terreno** (§13), nunca de la casilla en sí.

### Restricción de la casilla DEFENSIVA

| Acción | Desde DEFENSIVA |
|---|---|
| Ataque básico | ❌ Prohibido |
| Skill de daño **AD** | ❌ Prohibida |
| Skill de daño **AP** | ✅ Permitida |
| Skill de curación, apoyo, buff o terreno | ✅ Permitida |
| Defensa · Carga Musical | ✅ Permitidas |

Un personaje de estilo puramente físico **no tiene ninguna skill disponible** en esta casilla.

> **No existen armas a distancia.** El único ataque a distancia proviene de **gadgets** (p. ej. *Ballesta anti-curación*).

## 1.3 Alcance

**Todo objetivo es alcanzable**, salvo que una regla concreta diga lo contrario (pasivas de redirección, estar escondido, estar en K.O.).

No hay reglas geométricas de rango. La dificultad de golpear la retaguardia enemiga es **matemática**: allí la EVA sube y el `Hit%` cae.

| Vía para alcanzar la retaguardia | Eficacia |
|---|---|
| Situarse en OFENSIVA (+PRESS) | Alta |
| Aumentar PRESS por buffs, terreno o emblemas | Alta |
| Movimiento forzado (arrastrar al objetivo al frente) | Alta |
| Potenciar el arma | Baja |

## 1.4 Orden de turnos

Por **bandos, secuencial**. Cada aliado ejecuta su turno completo; al agotarse el bando, juega el enemigo.

```
CICLO = turno completo del bando aliado + turno completo del bando enemigo
```

El jugador **elige qué personaje abre**; desde ahí el orden avanza **de derecha a izquierda**. Existe un menú de consulta del orden de equipo tras cada combate.

---

# 2. EL TURNO

## 2.1 Diagrama de flujo

```
┌─────────────────────────────────────────────────────────────┐
│  INICIO DE TURNO                                            │
│  · Tick de elementos, emoción, buffs y debuffs               │
│  · Tenacidad (solo efectos negativos)                       │
│  · Regeneración                                             │
│  · Recálculo de LOCKS                                       │
│  · Evaluación de evoluciones de skill                       │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  MOVIMIENTO (libre y reversible · sin coste)                │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  VENTANA PRE-OFENSIVA                                       │
│  1 COMIDA gratis de equipo  ·  1 CONSUMIBLE gratis de equipo│
│  (independientes · CD 3 turnos · sin CD desde DEFENSIVA)    │
└──────────────────────────┬──────────────────────────────────┘
                           │  ← al iniciar la ofensiva, la ventana se cierra
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  ACCIÓN DE TURNO — se elige UNA vía                         │
│                                                             │
│   desde OFE/NEU ──► Básicos (hasta VA) ──► Combos QWER ──┐  │
│   desde DEFENSIVA ─────────────────────► Combos no-AD ───┤  │
│   Defensa (cualquier casilla) ───────────────────────────┤  │
│   Carga Musical ─────────────────────────────────────────┤  │
│   HIDE · SWAP · ESCAPE ──────────────────────────────────┤  │
└──────────────────────────────────────────────────────────┼──┘
                                                           ▼
┌─────────────────────────────────────────────────────────────┐
│  FIN DE TURNO                                               │
│  · Si prepara: CD_prep −−                                   │
│  · Recalcular CM_max si hubo K.O. o muerte, y hacer clamp   │
└─────────────────────────────────────────────────────────────┘
```

**Regla de modo:** el turno se inicia con **básicos** o con **QWER**. Una vez se entra en QWER, **no se vuelve a los básicos**.

**Timeout:** si no se toma una acción dentro del margen, el turno **pasa al siguiente personaje**. El margen lo marca la música (§7).

## 2.2 Regla de compromiso

```
Movimiento LIBRE
      │
      ▼
¿Se usan los objetos gratis estando en DEFENSIVA?
      ├─ NO ──► el personaje sigue libre de moverse
      └─ SÍ ──► queda ANCLADO a la casilla defensiva este turno
```

**Beneficio de DEFENSIVA:** usar un objeto gratis desde ahí **no arranca su cooldown de 3 turnos**, por lo que sigue disponible el turno siguiente vaya donde vaya el personaje. Sigue siendo **1 comida + 1 consumible por ventana**; el beneficio es de ritmo, no de volumen.

## 2.3 Acciones de turno

| Acción | Efecto | Cierra turno |
|---|---|---|
| **Ataque básico** | Hasta *N* impactos según arma y Velocidad de Ataque. Habilita la fase de combos. | No |
| **Combos QWER** | Habilidades que consumen CM. Encadenables mientras alcance el recurso. | Sí |
| **Defensa** | +50% de ARM y RM · se consume en el primer impacto recibido · **anula el crítico** de ese impacto · **duplica** la ganancia de CM al recibirlo. | Sí |
| **Carga Musical** | Acción dedicada a generar CM. Mejora con la habilidad de overworld de Música. ❓ valores | Sí |
| **HIDE** | Esconderse: **no targeteable**. Permite consumibles, comida y curación. **Prohibidos skills y básicos.** | Sí |
| **SWAP** | Cambio por un personaje del banquillo. **El entrante hereda la casilla exacta del saliente.** | Sí |
| **ESCAPE** | Huida con probabilidad de éxito. Sujeto a flag por combate. | Sí |
| **Gadget** | **Siempre consume el turno.** | Sí |

## 2.4 Combos: teclas, secuencias y evoluciones

| Concepto | Regla |
|---|---|
| **Consumo de tecla** | Cada tecla base se consume al usarse. En un turno pueden usarse Q, W, E y R si alcanza el CM. |
| **Coste de CM** | Q = 1 · W = 2 · E = 3 · R = 4–5 ⚠️ |
| **Subhabilidades de secuencia** | QT1, WT1, ET1… Quedan disponibles tras cumplir la secuencia temporal. **Consumen su tecla base.** Se usen o no, **todo vuelve a su estado normal al turno siguiente.** |
| **Ruptura de secuencia** | Si el jugador se desvía, la continuación desaparece y **no puede retomarse**. No se recuperan teclas. |
| **Evolución de slot** | Reemplaza la habilidad base conservando su nivel. **La primera evolución válida gana** y el slot ya no vuelve a cambiar. Se evalúa al inicio del turno. |
| **Evoluciones simultáneas** | Gana la asociada a la stat con **mayor valor absoluto**. Empate exacto → azar. |

---

# 3. ESTADÍSTICAS

## 3.1 Escalado por nivel

Toda stat con techo definido escala **linealmente**:

```
g(L) = L / 18

Valor(L) = MAX × g(L)          Pct(L) = CAP × g(L)
```

| Fase | Niveles | Lectura de poder |
|---|---|---|
| **Early** | 1 – 5 | Bonus pequeño, fase de preparación |
| **Mid** | 6 – 12 | El bonus ya se nota |
| **Late** | 13 – 18 | Cercano al techo |

**Picos de referencia:** LV6 inicio del Mid · LV11 Mid alto · LV16 Late alto · LV18 techo.

### Tabla de factor por nivel

| LV | g(L) | LV | g(L) | LV | g(L) |
|---|---|---|---|---|---|
| 1 | 0.056 | 7 | 0.389 | 13 | 0.722 |
| 2 | 0.111 | 8 | 0.444 | 14 | 0.778 |
| 3 | 0.167 | 9 | 0.500 | 15 | 0.833 |
| 4 | 0.222 | 10 | 0.556 | 16 | 0.889 |
| 5 | 0.278 | 11 | 0.611 | 17 | 0.944 |
| 6 | 0.333 | 12 | 0.667 | 18 | 1.000 |

## 3.2 Estadísticas primarias

| Stat | Sigla | Tipo | Función |
|---|---|---|---|
| Vida Base | HP | Número | Salud. A 0 → K.O. |
| Daño Físico | AD | Número | Referencia de daño físico |
| Daño Mágico | AP | Número | Referencia de daño mágico |
| Armadura | ARM | Número | Reduce daño físico vía fórmula de ratio |
| Resistencia Mágica | RM | Número | Reduce daño mágico vía fórmula de ratio |
| Precisión | PRESS | Número | Entra en la fórmula de acierto |
| Evasión | EVA | Número | Entra en la fórmula de acierto |
| Crítico | CRIT | % | Probabilidad de golpe crítico. Cap 100% |
| Carga Musical | CM | Recurso | Aporte individual al pool compartido |
| Daño Verdadero | DV | % | Porción del daño que ignora ARM y RM |
| Desgaste | — | % | Deterioro acumulado. 0–100% |

> ⚠️ **Cambio respecto a la versión 1.x:** ARM y RM ya **no son porcentajes de reducción**, son **valores** que pasan por la fórmula de ratio (§5.3).

## 3.3 Estadísticas secundarias ofensivas

| Stat | Fórmula / efecto | Cap |
|---|---|---|
| Preparación | Reduce 1 turno de preparación al alcanzar el umbral | 75% |
| Velocidad de Ataque | `hits = base × (1 + VA)` | 60% |
| Tempo | `CM_gain = CM_base × (1 + TMP)` | 50% |
| Penetración de Armadura | `Armor_f = ARM − PenArm_eff` | ≤ 80% de la ARM objetivo |
| Penetración Mágica | `RM_f = RM − PenRM_eff` | ≤ 80% de la RM objetivo |
| Robo de Vida | `Heal = Daño × LS` | 25% |
| Dominio de Armas | `P_final = P_base × (1 + WM)` | 50% |
| Dominio de Objetos | `D_item = D_base × (1 + IM)` | 60% |
| Daño de Área | `× (1 + AOE)` | 80% |
| Daño Explosivo | `× (1 + EXPL)` | 100% |
| Última Bala | Umbral de K.O. automático | 10 – 15% |
| Rebote | Probabilidad de rebotar daño a un enemigo cercano | 30% |

**Picos de referencia:** VA 60% → 10 impactos pasan a 16 · Tempo 50% → ×1.5 de CM · Daño Explosivo 100% → 200 pasa a 400.

## 3.4 Estadísticas secundarias defensivas

| Stat | Fórmula / efecto | Cap |
|---|---|---|
| Regeneración de Vida | `HP_regen = HP_base(L) × RGN` por turno. **No genera desgaste** | 10% |
| Dominio Médico | `Heal_total = Heal_base × (1 + MM)` | 50% |
| **Resistencia al Desgaste** | Reduce el desgaste acumulado (§4) | 50% |
| Inquebrantable | Probabilidad de autorevivirse tras K.O. | 25% |
| Eficiencia de Escudo | `Shield × (1 + SHD)` | 50% |
| Extensión de Escudo | Al expirar: `rnd < SHD+` → +1 turno | 30% |
| Extensión de Invulnerabilidad | Al expirar → +1 turno | 25% |
| Tenacidad | Limpia debuffs al inicio del turno. **No limpia emociones** | 30% |
| Amplificador / Extensión de Buff | Potencia y duración de buffs recibidos | 50% / 25% |
| Amplificador / Extensión de Debuff | Potencia y duración de debuffs aplicados | 50% / 25% |
| Dominio / Extensión de Comida | Potencia y duración de alimentos | 50% / 30% |
| Reflejos | Intercepta el daño letal dirigido a un aliado | 20% |

## 3.5 Orden canónico de cálculo

```
 1. Stats base del personaje
 2. Escalado por nivel                      → BaseRef
 3. Calcular mitigación de desgaste
 4. Obtener desgaste efectivo
 5. Aplicar desgaste a BaseRef              → StatOperativa
 6. Aplicar flats y porcentajes
 7. Aplicar multiplicadores finales
 8. Calcular derivados (EVA efectiva, reducción…)
 9. Aplicar clamps finales
```

## 3.6 Regla de agregación

```
StatOperativa = BaseRef × (1 − desgasteEfectivo)

StatFinal = StatOperativa + Σ Flats + (StatOperativa × Σ Pcts)
```

> ### ✅ Los porcentajes NO se multiplican entre sí
>
> Todos los porcentajes leen **la misma StatOperativa**, sin encadenarse. Esta es la regla que impide las explosiones exponenciales y el doble escalado. Aplica también a las **pasivas**.

**Ejemplo trabajado**

| Paso | Cálculo | Resultado |
|---|---|---|
| BaseRef de AD | — | 48 |
| Desgaste efectivo 10% | `48 × 0.90` | **43.2** ← StatOperativa |
| Flat del arma | `+25` | 25 |
| Arma +2% | `43.2 × 0.02` | 0.864 |
| Buff +3% | `43.2 × 0.03` | 1.296 |
| **StatFinal** | `43.2 + 25 + 2.16` | **70.36** |

El 2% y el 3% leen **43.2** — ni 48, ni 68.2.

### Capas de modificador

| Capa | Significado |
|---|---|
| `flat` | Suma directa a la stat |
| `pctAdd` | Porcentaje aditivo sobre la StatOperativa |
| `multFinal` | Multiplicador final **excepcional**. Reservado |
| `conditional` | Regla que solo aplica si se cumple su disparador |
| `flags` | Lógica especial, inmunidades, overrides |

**Orden de recolección de fuentes:** base y nivel → arma → armadura → accesorios, recuerdos y emblemas → gadgets → pasivas → rasgos → buffs y debuffs → emoción → elemento → entorno → estados temporales.

---

# 4. DESGASTE

**Deterioro físico acumulado.** No es daño, ni debuff, ni efecto temporal: es el coste de salir a campaña.

> **Dentro de una run el desgaste no se recupera.** Entre campañas, **sí**: baja solo con el paso de los días, y más rápido con tratamiento (§4.5).
>
> Así se lee la frase de STATS MASTER *"el desgaste real de la run nunca desaparece"*: nunca **dentro** de la run. Como una campaña dura 1 bloque, durante la misión lo que tienes es lo que llevas.

## 4.1 Generación

```
Cada 1/3 de HP base restaurada (acumulado)  →  +1% de desgaste
```

```
applyFastHeal(objetivo, curación):
    hpActual = min(hpActual + curación, hpBase_eff)
    acumulado += curación
    paso = (1/3) × hpBase_eff
    mientras acumulado ≥ paso:
        acumulado −= paso
        desgaste += 1%        (clamp 0–100%)
```

### Qué curación desgasta

| Origen | ¿Genera desgaste? |
|---|---|
| **Skill de curación sobre uno mismo** | ❌ No |
| **Robo de vida propio** | ❌ No |
| **Regeneración de Vida** | ❌ No |
| Skill de curación de **otro** personaje | ✅ Sí |
| Vendas y botiquines | ✅ Sí |
| Consumibles y comida | ✅ Sí |
| Gadgets | ✅ Sí |

> Solo se libra **la skill propia sobre uno mismo**. Todo lo demás pasa factura.

## 4.2 Cálculo en tres fases

```
FASE 1 — MITIGACIÓN
   desgasteReal = desgasteActual × (1 − Resistencia al Desgaste)

FASE 2 — SUPRESIÓN TEMPORAL  (torniquetes)
   desgasteEfectivo = max(0, desgasteReal − supresión)
   Al expirar el efecto, vuelve a desgasteReal.

FASE 3 — MULTIPLICADOR
   multiplicadorDesgaste = 1 − desgasteEfectivo
```

**Ejemplo trabajado**

| Variable | Valor |
|---|---|
| Desgaste acumulado | 20% |
| Resistencia al Desgaste | 10% |
| `desgasteReal` | `20 × 0.90` = **18%** |
| Torniquete | −5% |
| `desgasteEfectivo` | **13%** |
| `multiplicadorDesgaste` | **0.87** |

## 4.3 Recuperación

```
CAMPAÑA (1 bloque)      →  acumula desgaste · durante la run NO baja
        │
        ▼
DÍAS SIN CAMPAÑA        →  baja solo, con el paso del tiempo
        │
        ▼
HOSPITAL / TRATAMIENTO  →  baja MÁS RÁPIDO
                           (el jugador acude, o manda al personaje)
```

### Valores ⚠️ provisionales

| Concepto | Valor |
|---|---|
| Campaña media | **+35%** |
| **Amanecer** | **−10%**, automático e incondicional — no lo cancela nada |
| Hospital (día completo, consume los 3 bloques) | **−25%** total |

**Simetría resultante:** campaña 35% → amanecer 25% → un día de hospital → 0%. O cuatro amaneceres sin pagar bloques.

**Encadenar campañas** deja un saldo neto de **+25% por día**: cuatro seguidas llegan al tope del 100%. El sistema se autolimita sin reglas extra.

> ⚠️ **La tasa de generación sigue sin cuadrar con el 35% por campaña.** Bajó de 1/2 a 1/3 de la barra, lo que reduce la curación necesaria de 17,5× a **11,7× la barra** — sigue siendo inalcanzable en un bloque. `dia.md` §11 propone **10% de HP base = 1%** (3,5× la barra). Pendiente de decidir cuál manda.

> ### El desgaste es el ritmo de la campaña, no un castigo
> Salir a misión se paga con **días**, y esos días compiten con los lazos, el entrenamiento y la historia. Encadenar campañas sin descanso es posible, pero se llega a la siguiente con las estadísticas mermadas.
>
> **El equipo se desgasta y hay que rotarlo.** Ahí está el sentido real del banquillo.

❓ ¿Cuánto baja por día sin campaña? ¿Cuántos días cuesta cada nivel de gravedad en el hospital?

## 4.4 Alcance

| Afecta | No afecta |
|---|---|
| Todas las primarias: HP, AD, AP, ARM, RM, PRESS, EVA, CRIT | **CM actual** |
| Todas las secundarias ofensivas | **CM máximo** |
| Todas las secundarias defensivas | **Cooldowns** |
| | **Flags de estado** |

**Interacción con la vida:**
```
HPMaxEfectivo = HP_agregado × multiplicadorDesgaste
Si el desgaste sube en combate:  HPActual = min(HPActual, HPMaxEfectivo)
```

## 4.5 Presentación al jugador

La barra de vida no se acorta de golpe. El jugador **nota** que falla más, esquiva menos y recibe más daño. Se muestra un icono tenue de **Desgaste activo**.

---

# 5. RESOLUCIÓN DEL COMBATE

## 5.1 Constantes

| Constante | Valor |
|---|---|
| `HIT_MIN` / `HIT_MAX` | 5 / 95 |
| `CRIT_CAP` | 100 |
| `ARM_CAP` / `RM_CAP` | 90 / 90 |
| `PEN_CAP` | 80% de la defensa objetivo |
| Multiplicador de crítico | **×1.8** |

## 5.2 Acierto

```
EVA_eff = EVA × max(0.10, 1 − 0.03 × stacks)

Hit% = 5 + 90 × ( PRESS / (PRESS + EVA_eff) )

Hit% = clamp(Hit%, 5, 95)
```

> La forma de la fórmula garantiza que el acierto quede **siempre entre 5% y 95%**. Cuando `PRESS = EVA_eff`, el acierto es exactamente **50%**.

> ### ✅ El valor por stack es 0.03
>
> El `0.01` del canon original queda **superado**. El 3% está calibrado para que el acierto máximo contra un objetivo evasivo suba **~+50% en late**, y lo hace solo: el mismo número da +14% en early y +48% en late, porque en late hay más golpes por ronda.
>
> Descartados: `0.01` (no mueve nada) · `0.04–0.05` (se pasan) · `0.08–0.10` (tocan el suelo del 10% a media ronda y borran la evasión). Desglose completo en [formulas.md](formulas.md) §4.

**Tabla de referencia — PRESS del atacante = 50**

| EVA efectiva | Hit% |
|---|---|
| 100 | 35.0% |
| 75 | 41.0% |
| 50 | 50.0% |
| 25 | 65.0% |
| 10 | 80.0% |

### Fuentes de `stacks`

| Fuente | Vida del efecto |
|---|---|
| **Esquive con éxito** — el defensor esquiva y su EVA baja | Se reinicia al terminar el turno del **BANDO** atacante — persiste toda la ronda y lo aprovechan todos los aliados |
| **Debuffs de skill o terreno** | Expiran por turnos, o se limpian con skill u objeto |

### Reglas de impacto

- Un fallo **no corta la secuencia**.
- Si falla un impacto: no aplica daño, ni elemento, ni efectos on-hit **de ese impacto**.
- El recurso de la acción **se consume aunque falle**.
- **No se puede cambiar de objetivo entre impactos básicos.** Solo reparten las skills multiobjetivo.

## 5.3 Daño

```
┌── 0 · CRÍTICO (se decide antes) ───────────────────────────┐
│   critWouldApply = canCrit AND RNG(0..100) < CRIT          │
└────────────────────────────┬───────────────────────────────┘
                             ▼
┌── 1 · DAÑO BRUTO Y REDUCCIÓN ──────────────────────────────┐
│   FÍSICO:   D_raw = AD × multSkill × multContexto          │
│             PenArm_eff = min(PenArm, 0.8 × ARM_objetivo)   │
│             Armor_f    = max(0, ARM_objetivo − PenArm_eff) │
│             Red_f      = min(0.90, Armor_f/(Armor_f+100))  │
│                                                            │
│   MÁGICO:   D_raw = AP × multSkill × multContexto          │
│             PenRM_eff  = min(PenRM, 0.8 × RM_objetivo)     │
│             RM_f       = max(0, RM_objetivo − PenRM_eff)   │
│             Red_f      = min(0.90, RM_f/(RM_f+100))        │
│                                                            │
│   VERDADERO: Red_f = 0   (ignora ARM y RM)                 │
└────────────────────────────┬───────────────────────────────┘
                             ▼
┌── 2 · SEPARAR DAÑO VERDADERO ──────────────────────────────┐
│   D_true = D_raw × DV                                      │
│   D_norm = D_raw − D_true                                  │
└────────────────────────────┬───────────────────────────────┘
                             ▼
┌── 3 · REDUCIR SOLO EL DAÑO NORMAL ─────────────────────────┐
│   D_red = D_norm × (1 − Red_f)                             │
└────────────────────────────┬───────────────────────────────┘
                             ▼
┌── 4 · CRÍTICO, DESPUÉS DE LA REDUCCIÓN ────────────────────┐
│   si crítico:  D_red = D_red × 1.8                         │
└────────────────────────────┬───────────────────────────────┘
                             ▼
┌── 5 · DEFENSA ANULA EL CRÍTICO ────────────────────────────┐
│   si DEFEND activo en este impacto:  crítico NO se aplica  │
└────────────────────────────┬───────────────────────────────┘
                             ▼
┌── 6 · TOTAL Y APLICACIÓN ──────────────────────────────────┐
│   D_final = max(0, D_red + D_true)                         │
│   D_final va primero al ESCUDO, el sobrante al HP          │
└────────────────────────────────────────────────────────────┘
```

> **El crítico se aplica DESPUÉS de la reducción.** Es lo que impide que crítico y penetración se retroalimenten y disparen el daño.

### Básico frente a habilidad

```
DañoBásico = DañoOfensivoRef

DañoSkill  = DañoBaseSkill + (DañoOfensivoRef × CoefSkill)
```

La habilidad escala desde la referencia ofensiva del momento, **no** desde un daño ya reescalado. Así se evita el double-dipping.

## 5.4 Escudos

| Regla | Detalle |
|---|---|
| **Prioridad** | El escudo se consume **siempre antes que el HP** |
| **Daño verdadero** | **NO atraviesa el escudo.** Se absorbe igual que AD y AP |
| **Duración** | En turnos, según la habilidad de origen |
| **Apilamiento** | **No se apilan.** Un escudo nuevo sustituye al anterior si tiene **más turnos** |

> El escudo es la única defensa eficaz contra los tres tipos de daño. Y **no genera desgaste**, porque impide la pérdida de vida en lugar de repararla.

## 5.5 Orden de resolución por impacto

```
0) Pre-chequeos: locks, CM, costes
1) Chequeo de acierto
2) Daño (pipeline §5.3)
3) Post-impacto inmediato:
     · CM del atacante (arma · impacto · tempo)
     · CM del defensor (arma del atacante · DEFEND duplica · sin tempo)
     · Modificadores de CM (emociones, rasgos)
     · Clamp CM ≤ CM_max
4) Pasivas on-hit / on-being-hit
5) Elementos
6) K.O. / Muerte
7) Última Bala
8) Si hubo K.O. o muerte: recalcular CM_max y hacer clamp
```

---

# 6. CARGA MUSICAL

> ⚠️ **Cambio respecto a la versión 1.x:** la Carga Musical **ya no es individual**. Es una **barra compartida de equipo**.

## 6.1 Capacidad

```
CM_max = Σ storage(activos) + 0.5 × Σ storage(K.O.)
```

| Estado del personaje | Aporte |
|---|---|
| **Activo** | ×1.0 |
| **K.O.** | ×0.5 |
| **Muerto** | ×0 |

Al cambiar el estado de cualquier miembro, `CM_max` **se recalcula de inmediato** y el excedente **se pierde**.

**Ejemplo:** dos personajes con CM 3 y CM 4 → pool de equipo = **7**. Si el de CM 4 cae en K.O. → `3 + 2 = 5`. Si muere → `3`.

> ### Implicación de diseño
> Gastar CM es **quitárselo a los compañeros**. Y como el jugador elige el orden de turnos, decidir quién actúa primero es decidir **quién dispone del recurso**.

## 6.2 Generación por ataque básico

| Arma | CM base por impacto | Figura rítmica | En tempo ⚠️ |
|---|---|---|---|
| **Ligera** | **1/8** | Corchea | 2/8 |
| **Media** | **1/4** | Negra | 2/4 |
| **Pesada** | **1/2** | Blanca | — |

Si el impacto **falla**, CM = 0.

## 6.3 Generación al recibir daño

```
gainDef = 0.5 × CM_base(arma del ATACANTE, sin tempo)
gainDef × (DEFEND ? 2 : 1) × modificadores
```

El **tempo no aplica** al recibir. Un personaje invulnerable no genera.

## 6.4 Modificadores

Se aplican en **suma lineal** sobre la base:

```
CM_final = X + X·m1 + X·m2 + …
```

Ejemplo: `X = 1`, con +20% y +10% → `1.3`.
Modificadores conocidos: **Enfermo ×1.5** · **Terror −25%**.

## 6.5 Fuentes válidas

| ✅ Generan | ❌ No generan |
|---|---|
| Ataques básicos | Debuffs pasivos |
| Habilidades | Efectos indirectos sin interacción directa |
| Gadgets | |
| Recibir daño | |
| Otros eventos directos de combate | |

## 6.6 Modo CLASICAL

| | |
|---|---|
| **Disparador** | ✅ **La ACCIÓN de Clasical**, cuando con ella la barra comunitaria llega al máximo |
| **Aporte de la acción** | `storage propio (3 CM) × multiplicador de la sección musical` |
| **Efecto** | ✅ **×1.5 a la obtención de CM de todas las fuentes**, multiplicándose con el multiplicador de la sección |
| **Ventana** | Turno del bando aliado **y** turno del bando enemigo |
| **Fin** | Al iniciar el siguiente turno aliado |

> ### ⚠️ El disparador NO es "empezar el turno lleno"
>
> Esa regla **se autodestruye**: si tienes la barra llena lo natural es gastar, y si gastas no amaneces lleno. Simulada sobre 6 rondas no se activaba **ni una sola vez** en ninguna fase, ni jugando avaro ni guardando a propósito.
>
> Por eso el disparador es la acción: encender Clasical deja de ser algo que ocurre y pasa a ser algo que **alguien paga con su turno**. Encenderlo en el estribillo (×1.8) es mucho más barato que en calma. Ver [musica.md](musica.md) §3.

> ### Clasical invierte la economía del recurso
> Durante la partida el CM se administra. En Clasical **se gasta todo**. Y como la ventana cubre también el turno enemigo —donde recibir daño genera CM— el equipo **vacía la barra atacando y la rellena aguantando**, con la ganancia amplificada en ambos sentidos.
>
> El objetivo deja de ser acumular: es **llegar al tope justo antes del turno propio**.

---

# 7. MÚSICA Y TEMPO

## 7.1 Concepto

**Tempo = la cantidad de CM que se obtiene por ataque básico.**

```
CM por impacto = BASE (según el arma) + BONUS (por seguir el pulso o la melodía)
```

La **estadística Tempo** es el tamaño del bonus, no la probabilidad de conseguirlo. Acertar el ritmo es **habilidad del jugador**; cuánto se paga por acertarlo es estadística.

**Cada impacto se evalúa por separado.** En un multi-hit de 7 básicos pueden acertarse 4 y fallarse 3.

## 7.2 Qué sigue cada arma

| Arma | Sigue | Razón |
|---|---|---|
| **Ligera** | La **MELODÍA** | No tiene carga que la frene: corre por encima de las notas |
| **Pesada** | El **PULSO** | Pocos impactos, cada uno en el tiempo fuerte. Se carga manteniendo el clic y se suelta en el beat |
| **Media** | ❓ Intermedio | Sin especificar |

## 7.3 La música como reloj y como bonus

| Aspecto | Regla |
|---|---|
| **Velocidad de decisión** | El margen de turno lo marca la música. Si acelera, hay menos tiempo para decidir |
| **Bonus de Clasical** | Cada tema aporta un **% de bonus** que **varía según la sección de la canción** |
| **Escala del encuentro** | A mayor importancia del combate, **más capas musicales y más variación de bonus** |

| Encuentro | Capas | Variación |
|---|---|---|
| Batalla normal | Menos | Menor |
| Mini jefe | ↕ | ↕ |
| Jefe | ↕ | ↕ |
| **Jefe de historia** | **Más** | **Mayor** |

> La banda sonora **es** la curva de dificultad. Y como el bonus depende de la sección, **el momento del turno importa**: reservar la barra para que Clasical caiga en el clímax de la canción es una decisión táctica.

## 7.4 Los enemigos y el ritmo

| Perfil | Comportamiento |
|---|---|
| **Despistado** | Pierde el ritmo |
| **Enfocado** | Lo sigue |

Es un **rasgo de diseño del enemigo**, no un estado aleatorio.

---

# 8. ELEMENTOS

## 8.1 Aplicación y niveles

| Nivel | Duración | Condición |
|---|---|---|
| **L1** | 1 turno | Una aplicación con éxito |
| **L2** | 3 turnos | **Dos éxitos en el MISMO ciclo, por DOS personajes DISTINTOS, sobre el mismo objetivo** |

**Probabilidad base:** 25% uniforme para los nueve elementos. Se diferencian por efecto, no por fiabilidad.

**Bloqueo:** con L2 activo, se **ignoran** nuevos elementos.
**Reset de ventana:** al terminar el turno del bando enemigo.

## 8.2 Tabla de elementos

| Elemento | Estado L1 | Efecto del estado | Amplificador AD |
|---|---|---|---|
| **Oscuridad** | Aterrado | **Solo puede defenderse** | +50% del daño como **DAÑO VERDADERO** |
| **Hielo** | Congelamiento | No puede usar ataques que consuman CM | **120%** |
| **Electricidad** | Electrificado | No puede hacer ataques físicos | **140%** |
| **Naturaleza** | Envenenamiento | −20% AD | **120%** |
| **Viento** | Aturdimiento | −20% ARM y RM | **140%** |
| **Luz** | Ceguera | −25% EVA y PRESS | **130%** |
| **Fuego** | Quemadura | −20% de curación recibida | **140%** |
| **Alma** | Terror | −25% de obtención de CM | **130%** |
| **Cristal** | Desorientación | −30% de crítico | **150%** |

## 8.3 La regla que define el sistema

> **El AD cobra la marca; el AP la detona.**

| Vía | Quién | Qué hace con la marca |
|---|---|---|
| **AD — cobrar** | Físicos | La marca L1 **amplifica su golpe** (120–150%, o daño verdadero con Oscuridad) |
| **AP — detonar** | Casters | Al aplicar E.E. ponen la **segunda marca**, disparando el **L2 de 3 turnos** |

El amplificador **solo aplica al daño AD**. Es una decisión de balance: impide que los casters siembren marcas y se las cobren ellos mismos.

---

# 9. EMOCIONES

## 9.1 Reglas del sistema

| Regla | Detalle |
|---|---|
| **Cantidad** | Solo **una emoción activa** a la vez |
| **Sustitución** | **No se reemplaza directamente.** Hay que pasar por NEUTRAL |
| **Duración** | Todo el **día / run** |
| **Limpieza** | Por evento específico, dentro o fuera del combate |
| **Fin del día** | Se eliminan todas y se vuelve a NEUTRAL |
| **Ventana de combate** | **Cada emoción define la suya** al entrar en combate |
| **Tenacidad** | Limpia debuffs, **pero no emociones** |

> ⚠️ **Cambio respecto a la versión 1.x:** las emociones ya no duran 3 turnos ni se activan por condiciones de combate genéricas. Duran el día, se disparan por **reglas propias de cada personaje**, y se originan tanto **dentro** como **fuera** del combate.

## 9.2 Origen de los disparadores

| Fuera del combate | Dentro del combate |
|---|---|
| Calendario, festivales, fechas señaladas | Estado de la batalla, vida propia y relativa |
| Clima | Acciones: básicos, skills, defensa, relevo, fallos |
| Rutina social: salidas con lazos, días sin salir, compromisos | Sucesos: aliado en K.O., golpes recibidos, elementos, buffs y debuffs |
| Comida y bebida: postres, favoritos, excesos | Entorno: zona, clima, elemento dominante, lugar conocido |
| Estado físico: enfermedad, cansancio, malestar | |

## 9.3 Tabla de emociones

| Emoción | Efecto |
|---|---|
| **Furia** | **+50%** de daño físico · **no elige objetivo** durante 1 turno |
| **Alegría** | Gana **1 turno extra** al inicio de cada batalla |
| **Tristeza** | Turno 1: no puede atacar ni defender · **+10% a +40%** a pasivas y gadgets según nivel |
| **Miedo** | Turno 1: **defensa automática** · la defensa **no se rompe** en el primer impacto |
| **Enamorado** | Pierde el primer turno · toma la **mejor stat secundaria** de un villano al azar y obtiene el **50%** de esa categoría sobre sus propias bases · dura todo el combate, sin recálculo |
| **Alterado** | **−50%** de puntería en el turno 1 · **+50%** relativo a la probabilidad de activar elemento |
| **Serio** | **+5%** de crítico por cada **10%** de vida base faltante |
| **Enfermo** | Todas las stats a **2/3** durante 3 turnos (excepto vida y desgaste) · **CM ×1.5** sobre toda fuente |
| **Neutral** | Estado base, sin efectos |

## 9.4 Flujo de resolución

```
1. Detectar evento
2. Buscar las reglas del personaje
3. ¿Está en NEUTRAL?
      ├─ NO  → bloquear nueva emoción
      │        (salvo que el evento sea de limpieza → pasar a NEUTRAL)
      └─ SÍ  → si alguna regla coincide, activar la emoción
4. Registrar el estado para toda la run
5. Aplicar la manifestación según la ventana de la emoción
```

> **Cada personaje tiene sus propias reglas.** La misma emoción se dispara por causas distintas y se limpia por causas distintas en cada uno.

---

# 10. ESTADOS DEL PERSONAJE

## 10.1 La escalera

```
   VIVO  ──►  K.O.  ──►  MUERTE  ──►  ELIMINACIÓN PERMANENTE
             ×0.5 CM     ×0 CM         borrado del juego
```

| Estado | Aporte al pool | Recuperación |
|---|---|---|
| **Vivo** | ×1.0 | — |
| **K.O.** | ×0.5 | Primeros auxilios, relevo, o esconderlo |
| **Muerte** | ×0 | Desfibrilador · **3 oportunidades** |
| **Eliminación permanente** | — | **Ninguna** |

## 10.2 K.O.

- Se entra al llegar a **HP 0**, o por **Última Bala**.
- El personaje **deja de ser targeteable** para acciones normales.
- **Sigue ocupando su casilla y su línea.**
- Puede autorevivirse por **Inquebrantable** (cap 25%).

**Salidas del K.O.:**

| Vía | Detalle |
|---|---|
| **Primeros auxilios** | Un compañero lo reanima. Recupera **la mitad de su vida y de su CM** |
| **SWAP** | Se va al banquillo y entra un recambio en su casilla |
| **Esconderlo** | Se lo saca de la línea y deja de estar expuesto |

| Primeros auxilios | Turnos necesarios |
|---|---|
| Barato | 4 |
| Regular | 3 |
| Costoso | 2 |
| Caro | 1 |

## 10.3 Muerte

```
Turno enemigo N     →  el aliado cae en K.O.
       │
  ── TURNO ALIADO ──   ventana de rescate
       │
Turno enemigo N+1   →  ¿sigue en K.O.?
                         ├─ NO  → a salvo
                         └─ SÍ  → es rematado
                                   └─ si el daño CONECTA → MUERTE
```

Sirve **cualquier tipo de daño**: AD, AP o verdadero. El remate **pasa por la fórmula de acierto**, así que puede fallar.

## 10.4 Revivir tras la muerte

```
Probabilidad = (daño realizado / vida base) × 100  +  probabilidad del desfibrilador
```

**Tres oportunidades.** Al tercer fallo, el personaje se pierde **definitivamente**.

| Desfibrilador | Probabilidad |
|---|---|
| Usado | Baja |
| Básico | Media |
| Especial | Alta |
| Completo | Segura |

> ❓ **Revisar:** tal como está, cuanto **más brutal** fue el golpe que mató al personaje, **más fácil es revivirlo**. Conviene confirmar si es intencional o si el término debería restar.

---

# 11. CONSUMIBLES Y SOPORTE

## 11.1 Reglas de uso

| Regla | Detalle |
|---|---|
| **Gratis por equipo** | 1 comida + 1 consumible, independientes |
| **Momento** | Solo en la **ventana pre-ofensiva** |
| **Cooldown** | 3 turnos · **sin cooldown si se usan desde DEFENSIVA** |
| **Con CD activo** | Se puede usar igual, pero **consume el turno** y no resetea el CD |
| **Gadget** | **Siempre consume turno** |

## 11.2 Catálogo base

| Familia | Niveles |
|---|---|
| **Kits de vendas** (individual) | Básico · Mediano · Grande · Completo |
| **Kits de analgésicos** (grupal) | Pequeño · Mediano · Grande · Completo |
| **Primeros auxilios** (reanimar K.O.) | Barato · Regular · Costoso · Caro |
| **Desfibriladores** (revivir muerte) | Usado · Básico · Especial · Completo |
| **Torniquetes** (suprimir desgaste) | Aficionado · Novato · Veterano · Experto |

> **Toda curación por consumible genera desgaste.** Los torniquetes lo **suprimen temporalmente**; al expirar, vuelve.

---

# 12. RASGOS Y PASIVAS

## 12.1 Pasivas (recuerdos)

**Una pasiva es un modificador condicionado.** No se activa, no gasta turno, no gasta CM.

```
CONDICIÓN  →  EFECTO
```

| Regla | Detalle |
|---|---|
| **Cuántas** | **1 por personaje** |
| **Origen** | Objetos: los **RECUERDOS** |
| **Intercambio** | Sí, cambiando el recuerdo equipado |
| **Escalado** | El `X%` escala con el nivel como cualquier stat |
| **Apilamiento** | **Aditivo.** Leen la misma StatOperativa (§3.6) |

**Familias:**

| Familia | Ejemplo |
|---|---|
| Modificador de stat | +X% de AD o AP |
| Modificador de sistema | Reducir los básicos necesarios por carga de CM |
| Meta / run | +X% de EXP al terminar la batalla |
| **Control de campo** | El *muro*: mientras esté por encima de X% de vida, no se puede atacar a otros enemigos |

## 12.2 No existen los "rasgos" — todo es pasiva

✅ **Sistema único.** Lo que el documento de biografías llamaba *rasgos* son **pasivas**. Mismo mecanismo (`condición → efecto`); cambia el origen y la potencia.

| Portador | Cuántas | Origen | Potencia |
|---|---|---|---|
| **Jugable** | 1 | Recuerdo equipado, intercambiable | Media |
| **Enemigo común** | 1 o más | Propias, fijas | Media |
| **Jefe** | 1 o más | Propias, fijas | **Por encima de la media** |

> Un jefe no tiene "pasivas especiales": tiene **pasivas más fuertes**. La dificultad de un encuentro se expresa en pasivas, no solo en estadísticas.

🔨 **Catálogo por construir desde cero.** El heredado queda descartado por usar nomenclatura antigua. Ver [pasivas.md](pasivas.md) §5.

---

# 13. ENTORNO DE COMBATE

| Factor | Efecto |
|---|---|
| **Música** | Marca el tempo, el margen de decisión y el bonus de Clasical (§7) |
| **Ambientación** | Potencia o reduce elementos según el escenario (un entorno en llamas potencia fuego y reduce hielo) |
| **Clima** | Modifica la probabilidad de efectos elementales (frío, tormenta eléctrica…) |
| **Lugar conocido** | Leve aumento de rendimiento si los personajes ya conocen el sitio |
| **Escondites** | De 0 a 3. Un entorno sin escondites es un entorno agresivo |
| **Flag de escape** | Por combate. Define si se puede huir |

## 13.1 Terreno

Las casillas pueden **encantarse**. El terreno pertenece a la casilla.

| Regla | Detalle |
|---|---|
| **Aplicación** | Solo si el personaje **ejecuta su acción** en la casilla afectada. Atravesarla no basta |
| **Apilamiento** | **No se apilan.** Un terreno nuevo **sustituye** al anterior |
| **Alcance** | Es la única excepción a que las casillas solo modifiquen PRESS y EVA |

**Ejemplos**

| Terreno | Casilla | Efecto | Duración |
|---|---|---|---|
| Pasto de guerra | Ofensiva | ▲ AD y AP | 3 turnos |
| Campo de flores | Media | ▲ PRESS y CRIT | 3 turnos |
| Rocas y hierba | Defensiva | ▲ ARM y RM | 3 turnos |
| Tierra quemada | — | ▼ PRESS, AD, AP | 2 turnos |
| Ortigas y cicutas | — | ▼ EVA, CM por ataque, CRIT | 2 turnos |
| Pradera de guerra | — | ▼ DV, ARM, RM | 2 turnos |

## 13.2 Movimiento forzado y anclaje

| Concepto | Regla |
|---|---|
| **Movimiento forzado** | Asediar, enganchar, atraer, lanzar. **Entra siempre**: no hay tirada de resistencia |
| **Contador único** | **Enganche propio**: un personaje anclado a su casilla es **inarrastrable** |
| **Anclaje (sobre el enemigo)** | Efecto dirigido y unilateral. Quien lo lanza conserva su movilidad |
| **Efecto del anclaje** | Niega las **skills que dependen del desplazamiento** (empiezan en casilla X y terminan en Y). **Sigue pudiendo hacer básicos** |
| **Duración** | Hasta que pase el turno enemigo |

---

# APÉNDICE A — HOJA RÁPIDA DE FÓRMULAS

| Módulo | Fórmula |
|---|---|
| **Escalado** | `g(L) = L / 18` · `Valor(L) = MAX × g(L)` |
| **Stat operativa** | `StatOperativa = BaseRef × (1 − desgasteEfectivo)` |
| **Stat final** | `StatFinal = StatOperativa + ΣFlats + (StatOperativa × ΣPcts)` |
| **Desgaste real** | `desgasteActual × (1 − Resistencia al Desgaste)` |
| **Desgaste efectivo** | `max(0, desgasteReal − supresión)` |
| **Multiplicador de desgaste** | `1 − desgasteEfectivo` |
| **Generación de desgaste** | Cada 1/3 de HP base curada → +1% |
| **Evasión efectiva** | `EVA × max(0.10, 1 − 0.03 × stacks)` |
| **Acierto** | `5 + 90 × (PRESS / (PRESS + EVA_eff))` · clamp 5–95 |
| **Daño bruto** | `ATK × multSkill × multContexto × multFinal` |
| **Daño verdadero** | `D_true = D_raw × DV` · ignora ARM y RM · **no** ignora escudos |
| **Penetración** | `Pen_eff = min(Pen, 0.8 × DEF_objetivo)` |
| **Defensa final** | `DEF_f = max(0, DEF_objetivo − Pen_eff)` |
| **Reducción** | `Red = DEF_f / (DEF_f + 100)` · cap 90% |
| **Crítico** | ×1.8 · **después** de la reducción · anulado por DEFEND |
| **Escudos** | `D_final` → escudo → HP sobrante |
| **Preparación** | +50% de defensa · +20% de PRESS y EVA |
| **Capacidad de CM** | `Σ storage(activos) + 0.5 × Σ storage(K.O.)` |
| **CM por básico** | Ligera 1/8 · Media 1/4 · Pesada 1/2 |
| **CM al recibir** | `0.5 × CM_base(arma atacante)` · ×2 si defiende |
| **Elementos** | L1 = 1 turno · L2 = 3 turnos · L2 = 2 personajes distintos, mismo ciclo |
| **Revivir tras muerte** | `(daño realizado / vida base) × 100 + prob. del desfibrilador` |

---

# APÉNDICE B — GLOSARIO DE SIGLAS

| Sigla | Significado |
|---|---|
| **HP** | Vida base |
| **AD** | Daño físico |
| **AP** | Daño mágico |
| **DV** | Daño verdadero |
| **ARM** | Armadura |
| **RM** | Resistencia mágica |
| **PRESS** | Precisión |
| **EVA** | Evasión |
| **CRIT** | Crítico |
| **VA** | Velocidad de ataque |
| **CM** | Carga Musical |
| **E.E.** | Efecto Elemental |
| **E.EM.** | Efecto Emocional |
| **L1 / L2** | Nivel 1 / Nivel 2 de efecto elemental |
| **K.O.** | Fuera de combate, recuperable |
| **BA** | Ataque básico |
| **QWER** | Las cuatro habilidades del kit |
| **QT / WT / ET / RT** | Subhabilidades de secuencia |
| **EVOL** | Evolución de una habilidad de slot |

---

# APÉNDICE C — DECISIONES PENDIENTES

## C.1 Sistemas sin diseñar

| Sistema | Estado |
|---|---|
| ~~Efecto Emocional (E.EM.)~~ | ✅ **Sistema escrito** — flags, perfiles por personaje, efectos en el día y en combate, flujo de resolución. Ver §9 y [emociones.md](emociones.md). Queda pendiente el **catálogo** de flags por personaje. |
| **Enemigos e IA** | Juegan con las mismas reglas, pero falta: cómo deciden, perfiles de comportamiento, bestiario y construcción de encuentros. |
| ~~Rasgos vs. pasivas~~ | ✅ **RESUELTO.** Sistema único: todo es **pasiva**. Los jefes solo llevan pasivas más fuertes. Catálogo **por construir desde cero**. |

## C.2 Decisiones estructurales — una frase cada una

| § | Decisión |
|---|---|
| 4 | **Horizonte del desgaste.** Como la run vive dentro del día, *"nunca desaparece"* debe significar **a lo largo de toda la campaña**. Confirmar. |
| 3.3 | **Dominio de arma** (Amateur 5 / Normal 7 / Avanzado 9) frente a **Velocidad de Ataque** (`× (1+VA)`). Falta definir cómo se combinan. |
| 6.6 | **"Clasical" significa tres cosas**: el poder del mundo, el modo de combate y (antes) los ataques especiales. Fijar una. |
| 10.4 | La **fórmula de reanimación** premia que te maten de un golpe brutal. ¿Intencional o signo cambiado? |
| — | **¿Cuántos distritos hay?** El lore dice cuatro; la tabla de entrenamiento menciona el 5 y el 6. |

## C.3 Números provisionales ⚠️

| § | Valor | Provisional |
|---|---|---|
| 1.2 | PRESS / EVA por casilla | 30 / 30 |
| 7.2 | Arma **media**: ¿melodía o pulso? | — |

### Ya no son provisionales — ✅ cerrados

| § | Valor | Cerrado en |
|---|---|---|
| 7.1 | Bonus de tempo · **×2** (el `+1/3` de CM MASTER queda superado) | [conflictos.md](conflictos.md) §D2 |
| 5.2 | Desgaste de evasión · **0.03 por stack** | [formulas.md](formulas.md) §4 |
| 6.6 | Magnitud de Clasical · **×1.5**, multiplicándose con la sección | [musica.md](musica.md) §3 |
| 2.3 | Acción de Carga Musical · aporta **el storage propio (3 CM)** × multiplicador de sección | [musica.md](musica.md) §3 |
| 2.4 | Coste de CM por tecla · **Q1 · W2 · E3 · R4**, con factor de fase ×1.00 / ×0.60 / ×0.40 | [formulas.md](formulas.md) §7 |

## C.4 Contenido pendiente

| Qué | Estado |
|---|---|
| Los **20 estilos** | Todos sus valores siguen en `X%` |
| Tablas de escalado del Excel | Sin asignar a personaje y tecla |
| **Zadrick** | Kit incompleto |
| **Setto** y **Mertinoli** | Sin habilidad EVOL |
| **Recuerdos** | Origen, persistencia entre runs, si están atados a un personaje |
| **Habilidades de overworld** | Solo se conoce la de MÚSICA |
| Sección *LISTA COMPLETA DE ARMAS, ARMADURAS, AMULETOS Y RASGOS* | ✅ El **sistema** ya está escrito ([equipo.md](equipo.md): slots, clases, rareza, conjuntos). Falta el **catálogo** de piezas concretas con día y método de obtención. |

## C.5 Técnico

Ver [arquitectura.md](arquitectura.md) §6: plataforma, motor y lenguaje, 2D o 3D, formato de los datos de contenido, y si el Excel pasa a ser la fuente de datos.

**Prueba previa recomendada:** una canción, un metrónomo visual y detección de ventana de input. Decide la elección de motor, que es la pieza más cara de revertir.

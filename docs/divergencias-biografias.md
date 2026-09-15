# Divergencias — `BIOGRAFIAS 8de10.docx` vs. canon

> Sección afectada: **FUNDAMENTOS DEL SISTEMA DE COMBATE** (págs. 143–161).
> Es una versión **anterior** a los PlantUML MASTER. Buena parte ya no es válida.
> Los elementos (§EFECTOS DE ELEMENTO) sí coinciden: ya están alineados con [elementos.md](elementos.md).

---

# 🔴 CAMBIOS OBLIGATORIOS

## 1. Fórmula de acierto

**Dice:** `(precisión propia − evasión enemiga) + 50`
**Y en EVASIÓN:** `Evasión efectiva = Evasión propia − (Precisión enemiga × 10)`

Son dos fórmulas distintas para lo mismo dentro del mismo documento. Además la segunda puede dar negativo (EVA 90 vs PRESS 10 → −10).

**Sustituir por:**
```
EVA_eff = EVA × max(0.10, 1 − 0.01 × stacks)
Hit%    = 5 + 90 × ( PRESS / (PRESS + EVA_eff) )     → siempre entre 5% y 95%
```

## 2. Armadura y Resistencia Mágica

**Dice:** reducción porcentual plana. *"Armadura = 25% · Daño 100 → 75"*

Así, 100% de armadura sería invulnerabilidad, y hay que vigilar el techo a mano.

**Sustituir por:**
```
Red = DEF_f / (DEF_f + 100)        cap 90%
```
ARM y RM pasan a ser **valores**, no porcentajes.

## 3. Penetración

**Dice:** resta puntos porcentuales. *"40% de armadura − 25% de penetración = 30%"*
(el propio ejemplo está mal: 40 − 25 = 15, no 30)

**Sustituir por:**
```
Pen_eff = min(Pen, 0.8 × DEF_objetivo)
DEF_f   = max(0, DEF_objetivo − Pen_eff)
```

## 4. Crítico

**Dice:** 200% del daño físico base.

**Sustituir por:** **×1.8**, aplicado **después** de la reducción por defensa — no sobre el daño base. Es lo que evita que crítico y penetración se retroalimenten.

## 5. "Clasical" cambió de significado — el cambio más importante

**Dice:** Clasical = los **ataques especiales** (de ataque, mágico, apoyo, preparaciones). *"Carga de Clasical"*, *"medidor de Clasical"*.

**Ahora:** los especiales son las **habilidades QWER**, y **CLASICAL es un modo**: se activa al empezar el turno con la barra comunitaria llena, aumenta la obtención de CM durante el turno aliado y el enemigo, y termina al iniciar el siguiente turno aliado.

> ⚠️ En el documento viejo "Clasical" significa cuatro cosas a la vez: el poder del mundo, los ataques especiales, el recurso y el medidor. Conviene que en el juego signifique **una**.

## 6. La Carga Musical es COMPARTIDA

**Dice:** individual. *"Carga máxima = 5. Ataque especial nivel medio = 3 cargas"*

**Sustituir por:**
```
CM_max = Σ storage(activos) + 0.5 × Σ storage(KO)     ← barra única de equipo
```
Un muerto aporta 0. Si baja el máximo, el excedente se pierde.

Es el cambio con más consecuencias narrativas: **gastar es quitarle al compañero**.

## 7. ~~El desgaste NO se cura~~ → **EL DOCUMENTO VIEJO TENÍA RAZÓN**

⚠️ *Corrección de una lectura mía anterior.*

El documento dice que el desgaste se cura con reposo, con el Hospital de Félix y con tratamiento. **Es correcto.** El desgaste **sí se recupera**:

- **Baja solo** con el paso de los días sin ir a campaña.
- **Más rápido** con hospital o tratamiento, según la gravedad.

La frase de STATS MASTER —*"el desgaste real de la run nunca desaparece"*— se refiere a **dentro de la run**. Como una campaña dura 1 bloque, durante la misión no se recupera nada.

**Lo único que sí conviene revisar** del texto viejo es la lista de curas exóticas (*"eventos especiales con Graves o Meiko, artefactos únicos o melodías legendarias"*): habría que decidir si siguen existiendo o si la recuperación es solo tiempo + hospital.

Y se mantienen las dos capas ya canonizadas:
- **Resistencia al Desgaste** (stat) → `desgasteReal = desgasteActual × (1 − mitigación)`
- **Torniquetes** → supresión temporal; al expirar, vuelve

## 8. Contradicción interna sobre el desgaste

El documento da **dos tasas distintas**:

| Dónde | Qué dice | |
|---|---|---|
| §DESGASTE (stats básicas) | Cada **50% de vida base** restaurada = **1%** de desgaste | ✅ correcto |
| §DESGASTE FISICO (consumibles) | *"la vida base se verá reducida en 10 por cada 100 de vida que recupere y pierda"* | ❌ borrar |

## 9. Las emociones son otras

Los ocho efectos del documento **no coinciden con EMOTION MASTER**:

| Emoción | Documento viejo | Canon |
|---|---|---|
| **Furia** | +20% físico, −20% PRESS, −20% EVA | **+50%** daño físico · **no elige objetivo** 1 turno |
| **Alegría** | Turno extra · solo ataques físicos | Turno extra **al inicio de cada batalla** |
| **Tristeza** | +20% defensa, −20% daño | Turno 1 sin atacar ni defender · +10–40% a pasivas y gadgets |
| **Miedo** | +20% mágico, −20% físico | Turno 1 **defensa automática** · la defensa no se rompe al primer impacto |
| **Enamorado** | Regala un objeto y el receptor pierde turno | Pierde el primer turno · copia el 50% de la mejor stat secundaria de un villano al azar |
| **Alterado** | +20% EVA, +20% PRESS, −20% crítico | −50% puntería turno 1 · +50% activación de elemento |
| **Serio** | +30% crítico, −20% PRESS/EVA | +5% crítico **por cada 10% de vida faltante** |
| **Enfermo** | ×2 carga musical | Stats a **2/3** tres turnos · CM **×1.5** |

**Y la duración cambió:** el documento dice 3 turnos; el canon dice **todo el día/run**, con una sola emoción activa, obligación de pasar por NEUTRAL para cambiar, y una ventana de aplicación propia al entrar en combate.

## 10. Esconderse

**Dice:** reduce el daño recibido, los enemigos se centran en los que siguen fuera, permite relevo *"según el tamaño del escondite"*, y la ventaja numérica da stats al equipo.

**Ahora:** esconderse te vuelve **no targeteable**; puedes usar consumibles, comida y curación, pero **ni skills ni ataques básicos**. Hay de **0 a 3 escondites** por entorno, **uno por personaje**, y los enemigos usan las mismas reglas.

❓ Lo de la **ventaja numérica dando stats** no está en el canon. ¿Se conserva o se descarta?

## 11. Escapar

**Dice:** permitido salvo contra subjefes, jefes o villanos, y *"el equipo no debe estar demasiado dañado"*.
**Ahora:** flag por combate + **probabilidad de éxito**.
❓ ¿Sigue existiendo la condición de daño del equipo?

## 12. "Maestro de Armas" describe gadgets

El nombre dice armas; el texto habla de *"eficacia al usar gadgets, cantidad de disparos, daño de gadget"*. En el canon, `weaponMastery` es `P_final = P_base × (1 + WM)`.

❓ ¿Es maestría de **armas** o de **gadgets**? ¿O son dos stats distintas?

---

# ✅ LO QUE YA COINCIDE (no tocar)

| Tema | Estado |
|---|---|
| **Los nueve elementos** | Coinciden con [elementos.md](elementos.md): estados, 25% uniforme, amplificadores, L2 de 3 turnos. |
| **Defensa** | +50% de defensa, anula críticos, se pierde tras el primer impacto, da CM al recibir. Exacto. |
| **Preparación** | +50% defensa y +20% PRESS/EVA durante la preparación; la stat reduce un turno. Exacto. |
| **Daño verdadero** | Ignora ARM y RM, **no** ignora escudos. Exacto. |
| **Última Bala** | Umbral de vida que provoca K.O. automático. Exacto. |
| **Regeneración** | No genera desgaste. Exacto. |
| **El desgaste no afecta al CM** | Exacto. |
| **Reflejos** | Interceptas el golpe letal dirigido al aliado. Exacto. |
| **Tenacidad · Aumentador · Disminuidor · Degustador · Eficiencia de Escudo · Invulnerabilidad** | Coinciden. |
| **Música del entorno** | Atacar al inicio del compás da carga extra. Es el sistema de Tempo. |
| **Ambientación y clima** | Bonos elementales por entorno y clima. Compatible: entran como reglas de contexto. |

---

# 🟢 LO QUE EL LORE APORTA Y NOS FALTABA

No está en los MASTER y cierra preguntas abiertas de nuestros documentos.

## Las 3 oportunidades de revivir — RESUELTAS

Estaban pendientes desde el primer día:

```
Probabilidad de revivir = (daño realizado / vida base) × 100  +  probabilidad del desfibrilador
```

Tres intentos. Al tercer fallo, el personaje se pierde definitivamente.

| Desfibrilador | Probabilidad |
|---|---|
| Usado | Baja |
| Básico | Media |
| Especial | Alta |
| Completo | Segura |

> ⚠️ La fórmula depende del **daño que provocó la muerte**: cuanto más brutal el golpe que te mató, **más fácil es revivirte**. Es contraintuitivo. ¿Intencional, o el término debería restar?

## Reanimar un K.O. — RESUELTO

Con **primeros auxilios**, y la calidad decide cuántos turnos tarda:

| Primeros auxilios | Turnos |
|---|---|
| Barato | 4 |
| Regular | 3 |
| Costoso | 2 |
| Caro | 1 |

Al reanimarse recupera **la mitad de su vida y de su carga musical**.

## Dominio de arma — SISTEMA NUEVO

La Velocidad de Ataque no era solo un porcentaje: **el arma tiene niveles de dominio**.

| Dominio (arma ligera / katana) | Ataques | Con +20% AS |
|---|---|---|
| Amateur | 5 | 6 |
| Normal | 7 | 8 |
| Avanzado | 9 | 11 |

> De aquí salía el "7 golpes" de la ligera: es el nivel **Normal**. El dominio no aparece en ningún MASTER.

## Rasgos — SISTEMA ENTERO SIN DOCUMENTAR

Lista larga por familia (curación, ataque, defensa, evasión, crítico, carga musical, preparaciones), más rasgos **exclusivos de jefes**: Acorazado (inmune a físico), Antimágico, Sin alma (inmune a emociones), Elemental (inmune a elementos), Anticuración.

> **Aquí está el "muro" de [pasivas.md](pasivas.md):** el rasgo **Muralla** — *"nadie puede atacar a otros aliados excepto este"*. Ya existía.
>
> ❓ ¿Rasgos y pasivas son **el mismo sistema** o dos distintos? En RESOLUTIONATOR, `TRAIT` y `MEMORY` son fuentes separadas.

## Estructura del día — LA BASE DE LA RUN

- El día se divide en **tres bloques de tiempo libre**.
- Las stats **se entrenan** gastando bloques, en ubicaciones concretas por distrito.

| Stat | Dónde | Bloques |
|---|---|---|
| Vida Base | Circuito de ejercicio, Distrito 4 | 1 |
| Daño Físico | Gimnasio, Distrito 1 | 1 |
| Daño Mágico | Aulas prácticas, Distrito 5 (prof. Graves) | 1 |
| Armadura | Hospital de Félix | 1 |
| Resistencia Mágica | Defensa mágica, Distrito 2 (Charles) | 1 |
| Precisión | Campo de arcos de Oyvin, Distrito 6 | 2 |
| Evasión | Campo de flores de Merie, Distrito 3 | 2 |
| Crítico | Cordillera de Antony | **3 (día completo)** |
| Carga Musical | Parques de Meiko | 1 |

> Es exactamente la **estructura de run** que faltaba para poder calibrar el desgaste y el permadeath. Y explica por qué los lazos importan: los profesores y las ubicaciones **son personajes**.

## Consumibles — CATÁLOGO BASE

Kits de vendas y de analgésicos en cuatro tamaños (poco / medio / mucho / completo), individuales y grupales. Primeros auxilios en cuatro calidades. Desfibriladores en cuatro calidades. Torniquetes en cuatro niveles.

Cierra el ❓ de "catálogo de consumibles" de [gdd.md](gdd.md) §4.

# Conflictos canon ↔ levantamiento — estado

> Los PlantUML MASTER son canónicos para lo que cubren. El **tablero de casillas es posterior** a esos documentos y se va a añadir: no los contradice, los amplía.

---

# ✅ RESUELTOS

## R1. El tablero se añade — no sustituye nada

Las casillas (OFENSIVA / NEUTRAL / DEFENSIVA) son **posteriores** a los MASTER y **se van a incorporar**. Conviven con la estructura de front/banquillo, que queda definida así:

| Término | Definición | Nota |
|---|---|---|
| **Front** | Personajes que están **peleando activamente**. | Es donde viven las casillas. |
| **Banquillo** | Personajes **fuera del combate activo**, esperando un cambio. | ⚠️ **Término oficial.** "Reserva" queda **descartado** — era lo mismo. |
| **Escondite** | Lugar donde un personaje puede ocultarse. | **Solo para quienes están en el front.** |

**Acciones asociadas:**

| Acción | Qué hace |
|---|---|
| **HIDE** | Esconderse en un escondite. |
| **SWAP** | Cambiar un personaje del front por uno del **banquillo**. |
| **ESCAPE** | Huir de la batalla, con **% de éxito**. |

> Nota de nomenclatura: en el canon PlantUML aparece `reserva` en varios sitios. **Debe leerse como *banquillo*.**

---

## R2. Turnos de ventaja — modelo unificado

Gana el modelo del canon, **con una excepción por posición**:

```
1 COMIDA gratis por equipo   ┐
1 CONSUMIBLE gratis por equipo ┘  independientes · solo ventana pre-ofensiva

Recarga: CD de 3 turnos
Usar con CD > 0  →  consume el turno (y no resetea el CD)
Gadget           →  SIEMPRE consume turno

▶ EXCEPCIÓN: estando en la casilla DEFENSIVA, el CD NO aplica.
```

> Así sobrevive la idea original de "más objetos si te repliegas", pero expresada dentro de la economía canónica: la casilla defensiva **no da un objeto extra, elimina la espera**. Es más elegante — el beneficio no es cantidad, es **tempo**.

✅ **Resuelto:** sigue siendo **1 comida + 1 consumible por ventana**. Lo que da la casilla defensiva no es repetir dentro del turno, sino que **no se arranca el CD**, así que siguen disponibles el turno siguiente.

---

## R3. El timeout: ritmo, no tiempo real

El `timeout` **no** convierte el juego en acción en tiempo real. Es un **límite de inactividad**: si no tomas una acción en cierto periodo, el turno **pasa al siguiente personaje**.

Sigue siendo un RPG por turnos. Pero:

> **Hay batallas donde el ritmo de la música puede volverse caótico y acelerarse.** El tiempo disponible para decidir es una variable del combate, no una constante.

Eso conecta el `tempoPct`, la Carga Musical y el contexto de entorno (`Música/Tempo`) en un mismo eje: **la música marca el pulso real de la partida**. Es, con diferencia, la mejor idea de identidad del proyecto.

✅ **Duración base: 3 segundos.**
❓ Pendiente: qué lo acelera exactamente, y si perder el turno por inactividad tiene penalización adicional.

---

## R4. Estilos ≠ personajes

Confirmado. Son dos capas distintas:

**ESTILOS** (los 20 kits de [estilos.md](estilos.md)): Bladeus, Rudentlein, Halltus, Kronwell, Artens, Floristas del Hollins, Hallen, Ultimatum, Drimur, Lein-Xein, Korkakov, Willham, Jaens Jeans, Kawamato, Madrerick, Ordeus, Winchesster, Setto, Mertinoli, Zadrick.

**PERSONAJES PREDETERMINADOS** (9):

| # | Personaje |
|---|---|
| 1 | **Alextsu** |
| 2 | **Itzela** |
| 3 | **Ren** |
| 4 | **Kali** |
| 5 | **Dorokan** |
| 6 | **Amy** |
| 7 | **Richard** |
| 8 | **Ares** |
| 9 | **Haruka** |

Los personajes son quienes tienen **emociones propias, emblemas y gadgets**; los estilos son los kits QWER que equipan.

Ejemplo de personaje completo, reconstruido del canon — **Ren**: emblema de Procedencia *Distrito 4*, emblema de Arma *Ligero*, clubes *Cocina* y *Administración*, secuencia propia `W → ET1 → ET2 → Q → R` ("Secuencia de Chef"), gadget *Tostadora Quemadora* (crea Tostadas de mermelada al consumir 3 comidas).

✅ **Estilos:** cada personaje trae uno **predeterminado** —sirve para presentarlo— pero **se puede cambiar por otro**.

✅ **Almis, Riko y demás nombres: NO son jugables.** Los únicos jugables son los **9**. El resto son **vínculos**: personajes con los que te relacionas fuera del combate y que te dan cosas — mejoras, ayuda para subir estadísticas, herramientas, objetos.

> Los vínculos son la bisagra entre el simulador de vida y el combate. Ya aparecían en el sistema emocional (*salir con un lazo*, *el helado favorito de Riko*), y ahora se confirma que además son una **fuente de progresión mecánica**. Cultivar una relación no es solo narrativa: es equipamiento.

---

# 🟠 PENDIENTE DE DECISIÓN

## ~~D1. La acción Defensa~~ → ✅ **DECIDIDO: modelo del canon**

**DEFENSA (oficial):**

| Aspecto | Regla |
|---|---|
| Efecto | **+50% de ARM y RM** |
| Duración | Se **consume en el primer impacto recibido** |
| Crítico | **Anulado** en ese impacto |
| CM | **Duplica** la ganancia al recibir ese golpe |

*(Descartado: el modelo de "mitiga un X% durante todo el turno".)*

Razones de la elección:

1. **Escala con el personaje.** Al pasar por `Red = DEF/(DEF+100)`, un +50% sobre una armadura alta vale mucho más que sobre una baja. Defender es *mejor para los que defienden*, sin necesidad de una regla aparte.
2. **Crea un triángulo con las armas.** Contra un arma **pesada** (1–3 golpes gordos) defender es excelente; contra una **ligera** (7 golpes) absorbe uno de siete y casi no sirve. Eso hace que la clase de arma importe también en defensa, gratis.
3. **Se conecta con la economía de CM** al duplicar la ganancia: defender no es pasividad, es *aportar al bote del equipo*.
4. **Anular el crítico** le da una función precisa: protege contra el pico de daño, que es lo que mata.

El modelo A es un número plano que no interactúa con nada.

⚠️ **Lo que hay que vigilar si eliges B:** si la mayoría de enemigos pegan multi-hit, Defensa se vuelve opción trampa. La contramedida es que el bestiario tenga variedad real de clases de arma.

---

## D2. Divergencias numéricas menores

| Tema | Levantamiento | Canon | Decidir |
|---|---|---|---|
| ~~**Tasa de desgaste**~~ | ~~1% cada 50 de vida~~ | **1% cada 50% de la vida base** | ✅ **Proporcional**, para que no se rompa al escalar. ⚠️ La fracción exacta sigue en disputa: ver [dia.md](dia.md) §11 |
| ~~**Desgaste de EVA**~~ | ~~−15%~~ | ~~−1% por stack~~ | ✅ **RESUELTO — 3% por stack**, calibrado para +50% de acierto máximo en late |
| ~~**Bonus de tempo**~~ | ~~+1/3~~ | **×2** | ✅ **RESUELTO** — confirmado por aritmética (4/2/1 golpes por CM) |
| ~~**Fases de run**~~ | ~~Ultra Late~~ | ~~3 fases~~ | ✅ **RESUELTO — son 4.** Ultra Late = superado el LV18 y aún quedan batallas |

## D3. Reglas habladas sin reflejo en el canon

- **Desgaste solo por curación ajena** (skill propia sobre uno mismo exenta). El canon solo exime la Regeneración de Vida.
- **E.E. aplicado 1 vez + daño AD/AP → daño aumentado.** El canon documenta la detonación L2 (2 personajes distintos, mismo ciclo) pero **no** el amplificador de una sola marca.
- **Bloqueo de skills AD en la casilla DEFENSIVA.** Depende del tablero, que aún no está escrito en el canon.
- **Movimiento forzado y anclaje** (asediar, enganchar, atraer, lanzar). Igual: parte del tablero pendiente de canonizar.
- **Pasivas de redirección** que protegen la back line. Sin escribir todavía (decisión deliberada).

---

# Lo que el canon resuelve bien

- **No hay cadena de multiplicadores.** STATS MASTER: *"los porcentajes NO se multiplican entre sí; todos leen la misma stat operativa"*. Mata las explosiones exponenciales por diseño.
- **La fórmula de daño está completa y bien ordenada** — aplicar el crítico *después* de la reducción evita que crítico y penetración se retroalimenten.
- **El desgaste tiene tres fases limpias** y un alcance definido con precisión (no toca CM ni cooldowns).
- **Hay jerarquía de autoridad declarada** entre documentos.

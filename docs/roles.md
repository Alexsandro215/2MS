# E.N.M.A — Roles (etiquetas de arquetipo)

> ✅ **Catálogo inicial (nov 2026).** Los 20 estilos se diseñaron con libre albedrío; este documento los **cataloga a posteriori** y señala qué sistemas del juego se quedaron sin nadie que viva en ellos.
> Responde al pendiente de [pasivas.md](pasivas.md): *"¿Qué etiquetas de arquetipo existen?"*

---

## 0. Cómo se etiqueta un estilo

Cada estilo lleva **tres etiquetas**:

```
ROL PRINCIPAL  ·  ROL SECUNDARIO  ·  CURVA
   (qué hace)       (qué más hace)     (cuándo rinde)
```

- **Rol** = qué hace en el combate. Sale del catálogo de abajo.
- **Curva** = en qué fase rinde: **Early · Mid · Late · Ultra Late**. Es otro eje, independiente del rol: un Asesino de early y un Asesino de late son el mismo rol con distinta curva (ver [formulas.md](formulas.md) §7, *Kit de early / late / ultra late*).

> **Regla de diseño:** el rol no se decide mirando otros RPG. Se decide preguntando **¿cada sistema propio del juego tiene al menos un estilo que viva en él?** Un sistema sin dueño es un hueco — salvo que se deje vacío a propósito (§3).

Los **enemigos usan las mismas etiquetas** (comparten motor y spells — ver [arquitectura.md](arquitectura.md)). El jugador aprende los roles con su propio equipo y así lee al enemigo sin manual.

---

## 1. Catálogo — 6 familias

### ⚔️ OFENSIVA — hacer daño

| Rol | Qué hace | Sistema donde vive |
|---|---|---|
| **Asesino** | Daño concentrado a un solo objetivo | Daño único, crítico |
| **Rompedefensas** | Atraviesa ARM/RM o hace daño verdadero | Penetración, DV% |
| **Arrasador** | Daño a muchos, en área o al azar | Daño de Área, Explosivo |
| **Preparador** | Golpe diferido que carga turnos | Preparación |
| **Temerario** | Se daña o se destruye a cambio de poder | Auto-daño |
| **Duelista** | Marca a un enemigo y gana bonus contra él | Objetivo fijo de los básicos |

### 🌀 CONTROL — condicionar al enemigo

| Rol | Qué hace | Sistema donde vive |
|---|---|---|
| **Asediador** | Mueve enemigos: empuja, engancha, atrae, lanza | Casillas, movimiento forzado |
| **Ancla** | Fija enemigos e impide que se muevan | Anclaje |
| **Terraformador** | Crea casillas de terreno | Terreno |
| **Marcador** | Pone marcas elementales (E.E.) | Elementos |
| **Detonador** | Cobra las marcas: detona, completa el L2 | Elementos |
| **Debilitador** | Baja stats del enemigo (EVA, PRES, AD…) | Debuffs, erosión de EVA |

> **Marcador y Detonador van separados a propósito.** El L2 exige **dos personajes distintos** en el mismo ciclo: uno pone, otro cobra. La pesada ya es detonadora por diseño (cobra el amplificador elemental íntegro).

### 🛡️ PROTECCIÓN — evitar que el daño llegue

| Rol | Qué hace | Sistema donde vive |
|---|---|---|
| **Tanque** | Aguanta el castigo él mismo | ARM/RM, HP, regeneración |
| **Guardián** | Protege a **otros**: provoca, intercepta, redirige | Redirección, Reflejos |
| **Escudero** | Escudos que **previenen** el daño | Escudos |
| **Espejo** | Castiga que lo golpeen: rebota daño o efectos | Rebote, pasivas *on-being-hit* |

> El **Escudero** es el support que mejor expresa la tesis del juego: **prevenir es mejor que reparar**. El escudo no genera desgaste; la curación sí.

### ✨ SOPORTE — mejorar al propio equipo

| Rol | Qué hace | Sistema donde vive |
|---|---|---|
| **Potenciador** | Buffs, inmunidad, limpia debuffs | Buffs |
| **Sanador** | Cura y roba vida | Curación — ⚠️ **genera desgaste** en quien la recibe |

### 🕯️ VIDA Y MUERTE — jugar con el KO y el permadeath

| Rol | Qué hace | Sistema donde vive |
|---|---|---|
| **Inmortal** | Regresa a la vida si se cumplen **condiciones** | Muerte, permadeath |
| **Rescatista** | Atiende el KO y evita el remate | KO, remate |
| **Mártir** | Absorbe el desgaste o el daño de los aliados | Desgaste |
| **Vengador** | Se hace más fuerte cuando un aliado cae o muere | KO, muerte |
| **Verdugo** | Remata enemigos con poca vida o en KO | Última Bala |

> **Inmortal ≠ Inquebrantable.** Inquebrantable es una stat de **azar** (cap 25%). El Inmortal vuelve por **condiciones**, para que el jugador pueda planear con él. Las condiciones pueden ser musicales (*"vuelve cuando entre el estribillo"*, *"si el equipo enciende Clasical antes del remate"*).
> ❓ Definir las condiciones y si vuelve también de DEAD o solo de KO.

### 🎵 RECURSOS Y RITMO — jugar con la economía, no con el daño

| Rol | Qué hace | Sistema donde vive |
|---|---|---|
| **Músico** | CM, Clasical, secciones de la canción | Carga Musical, música |
| **Estratega** | Altera el orden de turnos, da Tempo, gana compases | Turno por compases, Tempo |
| **Intendente** | Saca más de comida, consumibles y gadgets | Dominio de Objetos / Comida, gadgets `ITEM_GENERATION` |
| **Ladrón** | Roba buffs, CM o stats al enemigo | Semilla: la emoción *Enamorado* copia stats de un villano |

---

## 2. Asignación de los 20 estilos

❔ = asignación dudosa, a confirmar. La **curva** está pendiente para todos.

| # | Estilo | Principal | Secundario | Curva |
|---|---|---|---|---|
| 1 | Bladeus | Temerario | Sanador | ❓ |
| 2 | Rudentlein | Asesino | Debilitador | ❓ |
| 3 | Halltus | Potenciador | Guardián ❔ | ❓ |
| 4 | Kronwell | Arrasador | Temerario | ❓ |
| 5 | Artens el Gladiador | Rompedefensas | Asesino | ❓ |
| 6 | Floristas del Hollins | Terraformador | Marcador (y Escudero por su R) | ❓ |
| 7 | Hallen | Sanador | — | ❓ |
| 8 | Ultimatum | Preparador | Temerario | ❓ |
| 9 | Drimur | Tanque | Asediador | ❓ |
| 10 | Lein-Xein | Rompedefensas | Asediador | ❓ |
| 11 | Korkakov | Ancla | Asesino | ❓ |
| 12 | Willham | Asesino | Rompedefensas | ❓ |
| 13 | Jaens Jeans | Potenciador | Músico | ❓ |
| 14 | Kawamato | Arrasador | — | ❓ |
| 15 | Madrerick | Terraformador | Ancla / Debilitador | ❓ |
| 16 | Ordeus | Tanque | Ancla | ❓ |
| 17 | Winchesster | Preparador | Detonador (su E aplica E.E. dos veces) | ❓ |
| 18 | Setto | Rompedefensas | Escudero | ❓ |
| 19 | Mertinoli | Arrasador | — | ❓ |
| 20 | Zadrick | Marcador ❔ | Potenciador | ❓ |

---

## 3. Lectura del catálogo

### Cobertura

| Familia | Estilos con rol principal aquí |
|---|---|
| ⚔️ Ofensiva | **12** — saturada (4 Rompedefensas, 3 Arrasadores) |
| 🌀 Control | 4 |
| 🛡️ Protección | 2 (solo Tanques) |
| ✨ Soporte | 3 |
| 🕯️ Vida y Muerte | **0** |
| 🎵 Recursos y Ritmo | **0** |

### Huecos (roles sin dueño principal)

- **Guardián, Escudero, Espejo** — nadie protege a otros como función principal.
- **Toda la familia Vida y Muerte** — la regla más dura del juego (permadeath) no tiene ningún estilo dedicado.
- **Toda la familia Recursos y Ritmo** — en un RPG musical, nadie vive de la música.
- **Detonador** — solo como secundario (Winchesster).

> **Vía barata para cubrir huecos:** Hallen, Kawamato y Mertinoli **no tienen rol secundario**. Darles uno de los roles vacíos cubre el hueco sin crear estilos nuevos. Ej.: Hallen como **Rescatista** — un sanador que, en vez de curar mucho y desgastar, se especializa en sacar a la gente del KO.

> **Un hueco del jugador puede ser solo del enemigo.** Si ningún estilo toca la música pero un enemigo Músico sí, el jugador siente que el enemigo hace algo que él no puede.

### ✅ Huecos a propósito — NO rellenar

| Sistema | Por qué queda sin rol |
|---|---|
| **Banquillo / SWAP** | Son **botones de emergencia**. Su fuerza está en ser universales y escasos. Si un rol viviera de ellos (*Relevista*), pasarían de salvavidas a rutina y perderían la tensión. |
| **Escondites / HIDE** | Igual. Un rol que atacara escondido (*Acechador*) convertiría el escondite en una posición de combate más, no en un refugio. |

> Descartados explícitamente: **Relevista** y **Acechador**. Si en el futuro aparece la tentación de un estilo que viva del banquillo o del escondite, esta es la razón para no hacerlo.

### Huecos menores, sin rol propio de momento

- **Emociones** — ningún estilo las provoca ni las calma.
- **Orden de turnos** — lo cubriría el **Estratega** si se usa.

---

## Pendientes

❓ Confirmar las asignaciones marcadas con ❔ (Halltus, Zadrick).
❓ Asignar la **curva** (Early / Mid / Late / Ultra Late) a cada estilo.
❓ Condiciones del **Inmortal**; ¿regresa de DEAD o solo de KO?
❓ ¿Qué huecos se cubren con estilos nuevos, cuáles con secundarios de estilos existentes y cuáles se dejan para enemigos?

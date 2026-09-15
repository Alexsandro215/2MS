# Efectos de Impacto

> Tercera familia de efectos, junto al **E.E.** (elemental) y al **E.EM.** (emocional).
> Se aplican **solo desde el arma**, **solo en ataques básicos (AD)**, y **no dependen del azar**.

---

## 1. La diferencia con el elemento

| | Efecto Elemental (E.E.) | Efecto de Impacto |
|---|---|---|
| **Fuente** | Skills, artilugios | **Arma** (AD) |
| **Disparador** | Probabilidad **25%** uniforme | **Acumulación** — sin dado |
| **Niveles** | L1 / L2 (L2 exige 2 personajes distintos) | **Sin niveles** — tiene barra |
| **Naturaleza** | Se cobra en AD, **detona en AP** | Se cobra en AD, **detona solo** |
| **Inmunidad de jefe** | *Elemental* | ***Inamovible*** |

> **El elemento es suerte; el impacto es constancia.**
> Es la razón de ser del sistema: le da al jugador una vía de efecto **que sí puede planificar**, y le da al slot de arma una identidad que hoy no tiene.

---

## 2. La carga — reutiliza la figura rítmica

**Cada golpe que conecta deposita carga en el objetivo. La cantidad es el mismo valor de nota que genera CM.**

| Arma | Figura | Carga por golpe | Golpes/turno (base → LV18) | Carga/turno (LV18) |
|---|---|---|---|---|
| **LIGERA** | corchea | **1/8** | 5 → 8 | **1** |
| **MEDIA** | negra | **1/4** | 3 → 5 | **1 1/4** |
| **PESADA** | blanca | **1/2** | 1 → 1 | **1/2** |

**Un solo número sirve a dos sistemas.** El arma no tiene una estadística de CM y otra de impacto: tiene **una figura rítmica**, y de ahí salen las dos.

### Las tres reglas heredadas — sin excepciones

| Regla | Ya existía en | Aplica igual al impacto |
|---|---|---|
| **Si el golpe falla, carga 0** | Generación de CM | ✅ Golpe evadido no acumula |
| **En tempo, vale el doble** | Bonus de tempo | ✅ `1/8 → 2/8` también en carga |
| **Movimiento forzado siempre entra** | Tablero | ✅ Salvo autoanclaje |

> El golpe a tiempo **pesa el doble para todo**. El ritmo deja de ser solo economía de recurso y pasa a ser control.

---

## 3. El umbral — lo declara el efecto, no el arma

Cada efecto tiene su **umbral en compases**. Cuando la carga lo alcanza, el efecto se dispara y **se descuenta el umbral** (el sobrante queda acumulado, no se pierde).

```
  LIGERA  ····•····•····•····•  8 golpes de 1/8   →  1 compás por turno
  MEDIA   ···•···•···•···•···•  5 golpes de 1/4   →  1 compás + 1/4 sobrante
  PESADA  •·······•·······•···  1 golpe  de 1/2   →  medio compás por turno
```

### Qué arma puede llevar qué efecto

**No todos los efectos están disponibles para todas las clases.** Esto no es una restricción arbitraria: es lo que impide que un arma ligera dispare un umbral de 1/2 cuatro veces por turno.

| Efecto | Umbral | LIGERA | MEDIA | PESADA | Acumula |
|---|---|:---:|:---:|:---:|---|
| **SANGRADO** | 1 | ✅ | ✅ | — | hasta 5 |
| **TRABA** | 1 | ✅ | — | — | no |
| **QUIEBRE** | 1 | — | ✅ | ✅ | hasta 3 |
| **DESAJUSTE** | 3/2 | — | ✅ | ✅ | no |
| **EMPUJE** | 1/2 | — | — | ✅ | no |
| **RESONANCIA** | 1 | ✅ | ✅ | ✅ | no |

**Cada arma lleva exactamente un efecto de impacto.** Es lo que distingue dos armas de la misma clase con los mismos porcentajes.

---

## 4. Catálogo ⚠️ *valores provisionales*

### SANGRADO — *la ligera desgasta*
Al final del turno del objetivo pierde **3% de su HP máxima** por acumulación. Dura **3 turnos** por acumulación, con temporizador independiente cada una. Máximo 5 → **15% por turno**.

- No es daño verdadero: **el escudo lo absorbe**.
- No lo reduce DEF ni ARM.

### TRABA — *la ligera te clava*
El objetivo **no puede cambiar de casilla en su próximo turno**. No acumula, dura 1 turno.

> Es la contraparte exacta del Empuje: **la pesada te mueve, la ligera te deja quieto.** Un objetivo trabado en OFENSIVA no puede retroceder a cubrirse.

### QUIEBRE — *abrir la armadura*
**−8% ARM y RM** del objetivo. Acumula hasta 3 → **−24%**. Dura **3 turnos** desde la última aplicación.

- Entra como **porcentaje sobre la StatOperativa**, en la capa de porcentajes normal.
- Se suma con la Penetración, no la sustituye. El tope de `Pen_eff = min(Pen, 0.8 × DEF)` se calcula **sobre la DEF ya quebrada**.

### DESAJUSTE — *sacarlo de compás*
Durante **1 turno**, el objetivo queda fuera de compás: **sus golpes no reciben bonus de tempo y no generan recurso**. No acumula.

> El único efecto que ataca la capa musical. Contra un enemigo que depende de su bote, vale más que el daño.

### EMPUJE — *la pesada mueve*
Mueve al objetivo **1 casilla hacia su DEFENSIVA**. No acumula, instantáneo.

- Sigue la regla de movimiento forzado del tablero: **siempre entra**, salvo autoanclaje.
- Responde a la pregunta abierta del §6 de `equipo.md`: **sí, el empuje entra aquí.** Es lo que conecta por fin el slot de arma con el sistema posicional — hasta ahora el movimiento forzado solo salía de skills.

### RESONANCIA — *el arma que paga*
Al dispararse, **devuelve 1/2 CM al bote del equipo**. No acumula.

> El efecto neutro. Un arma sin utilidad táctica, pero que financia al equipo — la opción para quien ya tiene control de sobra y le falta recurso.

---

## 5. Dónde vive la carga

**La carga es del objetivo, por efecto, y es compartida entre todos los aliados.**

```
  Objetivo  →  [ SANGRADO 5/8 ] [ QUIEBRE 2/8 ] [ EMPUJE 0/4 ]
                     ↑                ↑
              Alextsu (ligera)   Dorokan (pesada)
```

Dos personajes con armas de sangrado **llenan la misma barra**. Es la misma lógica colectiva que el L2 elemental, pero sin exigir que sean distintos.

| Situación | Qué pasa con la carga |
|---|---|
| El objetivo cae **K.O.** | Se conserva |
| El objetivo **muere** | Se pierde |
| El objetivo **se esconde** o va al banquillo | **Se pierde entera** |
| Fin del turno / fin de la oleada | Se conserva |

> Esconderse deja de ser solo evasión: **también borra el trabajo acumulado del jugador.** Es el coste real del escondite.

---

## 6. Inmunidad de jefe

Los jefes tienen una inmunidad por cada vía de ataque. El impacto es la quinta:

| Vía | Inmunidad |
|---|---|
| Físico | Acorazado |
| Mágico | Antimágico |
| Elemental (E.E.) | Elemental |
| Emocional (E.EM.) | Sin alma |
| **Impacto** | **Inamovible** |

Un jefe *Inamovible* sigue recibiendo el daño del arma; **lo que no recibe es el efecto**. Contra él, un arma se juega solo por sus porcentajes — y eso es una decisión de preparación, no una sorpresa en combate.

---

## 7. Resumen en esquema de regla

Según `arquitectura.md`, todo se expresa como `Regla = { fuente · disparador · condición · efecto · capa · duración · alcance }`:

| Campo | Valor |
|---|---|
| **fuente** | Arma equipada |
| **disparador** | Golpe básico (AD) que conecta |
| **condición** | Carga acumulada ≥ umbral del efecto · objetivo no *Inamovible* |
| **efecto** | El del catálogo |
| **capa** | Porcentajes (Quiebre) · HP directa (Sangrado) · Tablero (Empuje, Traba) · Música (Desajuste) · Recurso (Resonancia) |
| **duración** | 1 o 3 turnos según efecto; instantáneo en Empuje y Resonancia |
| **alcance** | El objetivo golpeado |

---

## 8. Simetría

**Los enemigos usan exactamente las mismas reglas.** Un enemigo con arma pesada empuja; uno con ligera sangra y traba. El jugador aprende el sistema jugándolo desde los dos lados.

---

## 9. Lo que queda por decidir

⚠️ Todos los valores numéricos son provisionales: 3% de sangrado, −8% de quiebre, los topes de acumulación, las duraciones de 3 turnos y el 1/2 CM de resonancia.

❓ **¿La rareza del arma escala el efecto?** Encajaría con la regla de porcentajes del equipamiento — un arma legendaria con sangrado haría 5% en vez de 3%. Alternativa: que la rareza solo toque los porcentajes y el efecto sea siempre igual, para que un arma temprana con el efecto correcto siga siendo válida en late.

❓ **¿Los conjuntos (kits) pueden dar un séptimo efecto?** El 4/4 ya da una pasiva; podría dar en cambio un efecto de impacto exclusivo.

❓ **¿Hay contrajuego?** Hoy solo se limpia escondiéndose. Faltaría decidir si algún consumible o alguna pasiva puede vaciar carga.

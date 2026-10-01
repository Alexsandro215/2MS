# E.N.M.A — El tablero

> Sistema **posterior** a los PlantUML MASTER. Este documento lo canoniza.

---

## 1. Estructura

| Zona | Capacidad | Notas |
|---|---|---|
| **FRONT** | **1 a 4** personajes | Peleando activamente. Aquí viven las casillas. |
| **BANQUILLO** | Todos los que lleves de más | Esperando un cambio. |
| **ESCONDITE** | Del entorno | Solo para quien está en el front. |

**Asimetría con el enemigo:** los enemigos **no tienen banquillo**. Sí tienen **escondites**.

> Consecuencia: tú puedes rotar y ellos no. Es una ventaja estructural del jugador que compensa en parte el que puedan ser hasta 6.

**Emblemas:** el conteo es **la suma de todo el equipo**. Las tres casillas son front, así que replegarse **no desactiva** emblemas.

---

## 2. Las casillas

Cada personaje del front tiene su **línea propia de 3×1**. Solo se mueve adelante o atrás sobre ella. Las líneas son independientes.

```
[ DEFENSIVA ]   [ NEUTRAL ]   [ OFENSIVA ]   →   enemigos
```

### Qué hace cada una

| Casilla | PRESS | EVA | Extra |
|---|---|---|---|
| **OFENSIVA** | **+30%** | **−30%** | — |
| **NEUTRAL** | — | — | Ni buff ni debuff |
| **DEFENSIVA** | **−30%** | **+30%** | **Sin AD** · los turnos de ventaja **no generan CD** |

> ⚠️ **30/30 es provisional.** Falta balancear; trátese como `X%` simétrico hasta que se fije.

> ### El tablero solo mueve PRESS y EVA
>
> **Los bonus y debuffs de casilla son exactamente eso: PRESS y EVA. Nada más** — salvo que el **terreno** diga lo contrario.
>
> Es mucho más simple de lo que parecía, y es una virtud: toda la lectura posicional se reduce a un eje —**precisión contra evasión**— y todo lo demás que ocurra en una casilla viene de fuera (terreno, skills, pasivas).

**NEUTRAL no tiene identidad propia por diseño.** Es la posición sin compromiso: no ganas nada y no pierdes nada. Se elige por lo que *no* te quita — mantienes PRESS y EVA intactas, puedes usar AD, y sigues libre de moverte.

### Restricción de la casilla DEFENSIVA

Desde DEFENSIVA **no se puede usar nada AD**:
- ❌ Ataques básicos
- ❌ Skills de daño físico (AD)
- ✅ Skills de curación, apoyo, buff, terreno y daño **AP**

Si el personaje es de estilo puramente físico, **todas** sus skills quedan bloqueadas ahí.

> **No hay armas a distancia en el juego.** Por eso el bloqueo de AD en la casilla trasera no tiene excepciones por tipo de personaje. Lo único que dispara desde lejos son **gadgets** — p. ej. la *Ballesta anti-curación*: lanza una flecha que inflige X% de daño y **reduce la curación** del objetivo X%.

---

## 3. Movimiento

**Gratis y sin límite de casillas.** Un personaje puede recorrer su línea entera en un turno.

### Regla de compromiso

```
Movimiento LIBRE
     │
     ▼
¿Usas turnos de ventaja estando en DEFENSIVA?
     ├─ NO  →  sigues libre de moverte
     └─ SÍ  →  QUEDAS ANCLADO a la casilla defensiva
```

> El disparador del compromiso es **usar los turnos de ventaja desde la casilla defensiva**. El trato es explícito: *usas objetos sin coste de cooldown, pero renuncias a moverte ese turno.*

### El beneficio real de DEFENSIVA: no arrancar el cooldown

Usar un objeto gratis **normalmente pone su CD a 3 turnos**. Usarlo **desde DEFENSIVA no arranca ese cooldown**.

```
Turno N   · en DEFENSIVA · usas comida/consumible  →  NO se activa CD
Turno N+1 · vayas donde vayas (neutral, ofensiva)  →  siguen DISPONIBLES
```

**Sigue siendo 1 comida + 1 consumible por ventana**, no se repiten dentro del mismo turno. Lo que ganas no es cantidad: es que **el recurso queda listo para el turno siguiente**, estés donde estés. Y si te quedas en DEFENSIVA, puedes encadenar turno tras turno sin esperar nunca.

> Replegarse no te da más objetos — te da **acceso continuo** a ellos. Es un beneficio de ritmo, no de volumen, y es exactamente lo que hace que la casilla trasera sea la posición del soporte.

---

## 4. Alcance y objetivos

**Todo el mundo es alcanzable, salvo que algo diga lo contrario.**

No existe una regla geométrica de rango. Lo que hace difícil llegar a la retaguardia enemiga es **la matemática de acierto**: un objetivo en su casilla defensiva tiene ▲ EVA, así que el `Hit%` contra él se desploma.

**Cómo se llega de verdad a la back line:**

| Vía | Efecto |
|---|---|
| Ponerte en **OFENSIVA** | ▲ PRESS propia |
| Subir **PRESS** por otras vías | Buffs, terreno, emblemas, kit |
| **Arrastrarlos al frente** | Movimiento forzado (Drimur, Setto) |
| Aumentos del arma | Ayudan, pero **solo ligeramente** |

> Con arma **ligera** llegar a la back es **difícil de por sí**, y potenciarla apenas cambia eso. La respuesta real es **posición + PRESS**, no equipo.
>
> Esto es coherente y elegante: la posición no es geometría, es **economía de precisión**. Nadie está fuera de alcance; simplemente hay objetivos a los que no aciertas.

---

## 5. HIDE (esconderse)

Un personaje del front puede ocultarse en un escondite del entorno.

| | Estando escondido |
|---|---|
| **Ser objetivo** | ❌ No es targeteable |
| Skills | ❌ Prohibidas |
| Ataques básicos | ❌ Prohibidos |
| **Turnos de ventaja** | ✅ **Todos disponibles** |
| Consumibles | ✅ |
| Comida | ✅ |
| Curación | ✅ |

> Esconderse es la postura de **soporte puro**: te sales de la ecuación ofensiva por completo a cambio de intocabilidad y acceso libre a objetos. Es el sitio donde estabilizar al equipo sin recibir a cambio.

### Disponibilidad

| Regla | Detalle |
|---|---|
| Cantidad | **Depende del entorno: de 0 a 3.** Puede no haber ninguno. |
| Capacidad | **Uno por personaje.** |
| Enemigos | **Mismas reglas.** También se esconden. |

> ⚠️ **Y eso es deliberadamente frustrante.** Un enemigo puede esconderse justo cuando ibas a rematar a su glass cannon o a su caster de AP. El escondite no es una comodidad del jugador: es un recurso del entorno que **ambos bandos** pelean, y perder la ejecución por un turno es parte del juego.
>
> Un entorno con 0 escondites es, por tanto, un entorno **agresivo**: nadie se puede refugiar, ni tus KO ni sus casters.

### Salir del escondite

✅ **RESUELTO.** Salir es **gratis**, pero se sale **siempre a la casilla DEFENSIVA**. Desde ahí no hay AD, así que para atacar ese mismo turno hay que avanzar primero, y el movimiento ya es libre.

> Esconderse no te deja reaparecer donde más daño haces: vuelves por la puerta de atrás.

---

## 6. SWAP (cambio)

El personaje que hace swap **se va al banquillo** y el seleccionado ocupa su lugar en el front.

**El que entra aparece en la casilla exacta donde estaba el que salió.** El swap no reposiciona: hereda la posición.

> Consecuencia táctica: puedes usar el swap para **meter a alguien directamente en ofensiva o en defensiva** sin gastar el movimiento. Y al revés — sacar a un herido de la ofensiva mete al recambio en esa misma casilla expuesta.

**El que entra no actúa hasta el turno siguiente.** Si actuara, cambiar sería gratis.

---

## 7. Terreno

El terreno pertenece a **la casilla**, y su efecto se aplica según **dónde actúa** el personaje:

> **Si no ejecuta su acción en la casilla afectada, el efecto no se aplica.**

Es decir, no basta con pasar por encima: hay que **actuar desde ahí**. Un personaje que atraviesa una casilla de tierra quemada y termina actuando en otra, no sufre el debuff.

El terreno es también la **única excepción** a la regla de que las casillas solo modifican PRESS y EVA — un terreno puede alterar AD, AP, ARM, RM, crítico, CM, etc.

### Los terrenos NO se apilan: se sustituyen

Un terreno nuevo **reemplaza** al que hubiera en esa casilla. Nunca conviven dos.

> **La casilla es un espacio en disputa.** Si Madrerick riega tierra quemada donde Floristas del Hollins había puesto pasto de guerra, el pasto **desaparece**: no se compensan ni se suman. Gana el último que actúa.
>
> Eso convierte el control de terreno en una guerra de tempo — importa *cuándo* riegas, no solo *dónde*. Y explica por qué la Q EVOL de Madrerick (borrar una casilla de buff enemiga) existe como herramienta aparte: sustituir cuesta una habilidad de terreno, borrar es más barato.

---

## 8. Anclaje

El anclaje **niega el acceso a las skills que dependen del movimiento**, pero no desarma al personaje.

| Estando anclado | |
|---|---|
| Skills que requieren moverse | ❌ **No se pueden usar** |
| Ataques básicos | ✅ **Sí puede** |
| Ser movido por el rival | ❌ Inmune |

**Regla de aplicación:** solo cuenta si la skill **empieza en una casilla X y termina en una casilla Y** después del casteo. Si la habilidad no implica desplazamiento, el anclaje no la afecta.

> ### Por qué el anclaje es tan fuerte
>
> El usuario lo explica con el caso que lo motiva: llenas el tablero de **casillas buenas** para un personaje de movimiento, de modo que caiga donde caiga sus skills salgan potenciadas. El anclaje **rompe esa estrategia entera** — le niegas el acceso a la skill, no solo el desplazamiento.
>
> Y el matiz importante: **no lo deja inútil**. Sigue pegando básicos. El anclaje no es un aturdimiento, es una **negación de plan**.

### Enganche propio = inmunidad al movimiento forzado

**No hay tirada de resistencia contra el movimiento forzado.** Entra siempre… salvo por un contador duro:

> **Si un personaje tiene enganche propio a una casilla, el control de levantamientos no le aplica.**

Es decir: **anclarte tú mismo te vuelve inarrastrable.** No es probabilidad, es binario — o estás enganchado, o te mueven.

Esto le da sentido mecánico completo a **Ordeus** (*Tanque · Anti-Movimiento · Recuperación de Vida*), cuya Q dice literalmente *"te mueves inmóvil en una casilla"*. No es una rareza de redacción: es **plantarse a propósito** para negar a los Drimur y Korkakov del otro lado.

> Diseño limpio: el contador del control posicional no es un stat de resistencia genérico, es **una decisión que se toma en el turno** — renuncias a tu movilidad para que nadie pueda dictártela.

---

---

## 9. Personajes en K.O. sobre el tablero

**Un K.O. sigue ocupando su casilla y su línea.** No se retira solo.

Solo deja de ocuparla si el equipo hace algo al respecto:

| Salida | Efecto |
|---|---|
| **SWAP** | Se va al banquillo y entra un recambio en su casilla. |
| **Reanimación** | Por alguien del banquillo o del front. |
| **Ocultarlo en un escondite** | Se lo saca de la línea y deja de ser objetivo. |

> Esto es lo que le da dientes a la ventana de remate (§2.1 del GDD): el caído **se queda ahí, visible y golpeable**, hasta que alguien gaste recursos en sacarlo. Y como el K.O. además recorta el pool de CM del equipo, ignorarlo cuesta por partida doble.

### Quién lo saca

✅ **RESUELTO.** Por defecto, **un aliado del front gasta su turno** en cambiar al caído por alguien del banquillo, o en esconderlo. A un compañero en pie no se le puede cambiar desde fuera: el swap de un personaje sano lo decide él en su turno.

**Una pasiva puede cambiar eso.** Lo previsto es que alguien del banquillo entre **por su cuenta** a relevar a un caído, sin que nadie del front gaste su turno. Es una regla de flujo (`RelevoDesdeBanquillo`), no una excepción del motor: otra pasiva puede decir otra cosa.

Sacado del front, al banquillo o al escondite, **ya no está expuesto y no se le remata**.

---

## Pendientes

❓ Valores definitivos de PRESS/EVA por casilla (provisional: 30/30).

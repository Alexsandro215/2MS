# E.N.M.A — Contexto del mundo y estructura de la partida

> Extraído de `BIOGRAFIAS 8de10.docx`. **Solo lo que el diseño necesita** — el lore completo vive aparte.
> Título del documento fuente: *TWO SOULS MUSICALS*.

---

## 1. El mundo en cuatro líneas

La música **no es solo arte: es una fuente de poder**. No manifiesta los poderes por sí misma, pero **interpretarla fortalece a quien los usa**, tanto en combate como en su desarrollo personal.

Hay **dos grandes fuerzas**:

| Fuerza | |
|---|---|
| **Poder Puro** | La fuerza ancestral e indivisible: la forma más poderosa de esta energía. El de la Oscuridad no existía en la Línea Base. |
| **Clasical** | Una versión fragmentada del Poder Puro que creó Webell Eisner para que fuera más accesible y controlable. Solo lo heredan músicos, cada portador tiene un elemento, se activa a voluntad y la música lo potencia. Siempre es inferior al Poder Puro. |

> ⚠️ **"Clasical" es a la vez el poder del mundo y el nombre del modo de combate.** Antes también era el nombre de los ataques especiales. Conviene decidir qué significa en cada capa — ver [divergencias-biografias.md](divergencias-biografias.md) §5.

**Protagonista:** Alextsu Rousvelt Field.
**Antagonista / fundador:** Artur Hosenfield, creador de la E.N.M.A. En secreto: para el público es solo el rector actual (ver §6).
**Arranque:** preludio el 24 de diciembre de 2012; la trama principal empieza el **23 de enero de 2017**.

---

## 2. La E.N.M.A.

Una **academia** fundada por Artur en **1615**, en la región de Hiroshima. Nació defendiéndose del ejército japonés tras la negativa del gobierno a apoyarla (la primera guerra de la E.N.M.A., ver §6); la alianza posterior le dio a Artur libre albedrío para construirla sin intervención ni impuestos.

### Los distritos

| Distrito | Identidad | Arquitectura |
|---|---|---|
| **1** | Diplomacia y administración. Decide en secreto el rumbo de la academia. | Medieval + sintoísta; torres y muros de piedra |
| **2** | Arte y artesanía. Decide el sistema educativo. Construido en torno a un lago. | Neoclásica y rococó |
| **3** | Revolución industrial. Energía, investigación, transporte (tranvía, cablebús). | Metal, puentes y arcos del s. XVIII |
| **4** | Naturaleza y deporte. Bosque, jardín de Fa, cúpula, lago. **Escenario del Año Rojo y la Noche Oscura.** | Siglo XX |
| **Muro Mercantil** | Antiguo muro defensivo, hoy el centro comercial. Colinda con todos los distritos. | — |

**Rivalidad establecida:** Distrito 2 (club de Electricidad y Circuitos) contra Distrito 3 (club de Mecatrónica).

> **Son cuatro distritos** (confirmado el 01/11/2026). Los Distritos 5 y 6 de la tabla de entrenamiento venían de la documentación vieja.
> ❓ Falta reubicar las **aulas prácticas de Graves** y el **campo de arcos de Oyvin** dentro de los cuatro.

### El plano

Hay bocetos a lápiz por distrito en `G:\MAPAS` (01/11/2026). El plano limpio y unido está en https://claude.ai/artifact/WBiaQ5mn3WauWJzGwMxQyb.

```
        ┌────────────┬────────────┐
        │  DISTRITO  │  DISTRITO  │
        │     3    ┌─┴─┐    4     │
        │          │   │          │
        ├──────────┤ C ├──────────┤
        │          │   │          │
        │  DISTRITO└─┬─┘ DISTRITO │
        │     1      │      2     │
        └────────────┴────────────┘
```

- **El Distrito Central es todo el rectángulo amurallado del centro.** Los muros marcan su límite. Se entra por **arcos**, donde terminan las cuatro avenidas.
- **Las avenidas** separan los distritos y son un poco más anchas que una calle normal.
- **Las líneas de colores** de los bocetos eran las **rutas del tranvía**. El autor pidió rehacerlas para el plano nuevo (02/11/2026):
  - **Dos líneas por distrito** (8 en total), una de cada tipo:
    - la **académica** pasa por dormitorios, comedor, aulas, carreras, centro y auditorio;
    - la **de ocio y exploración** pasa por lo que no es académico: parque, lugares propios, hospital, el servicio del distrito y las aficiones.
  - Las 10 líneas tienen colores distintos. En el plano:

    | Distrito | Académica (continua) | Ocio (punteada) |
    |---|---|---|
    | 1 | roja | rosa |
    | 2 | morada | verde |
    | 3 | amarilla | naranja |
    | 4 | café | turquesa |

    La azul cruza la E.N.M.A. y la negra la rodea.
  - **Una línea no recorre todo el distrito** (regla del autor, 02/11/2026): pasa solo por los lugares más importantes de su tipo, con pocas estaciones y pocos giros. Cada una empieza o termina en la línea azul, que también tiene sus paradas fijas en cada distrito.
  - El archivo del plano está en [plano/plano-enma.html](plano/plano-enma.html); las rutas y paradas de cada línea están a mano en `LINEAS_DISTRITO` y las de la azul en `RUTAS`.
  - **La línea azul** atraviesa toda la E.N.M.A. y **la negra** la rodea.
  - **La línea cian une los distritos pasando por el centro:** sale de un distrito, va por la avenida al Distrito Central, pasa por él y regresa por la misma vía, en sentido contrario, para entrar al siguiente. Por ejemplo, del 1 al Central y de vuelta al 3. Así con todos.
  - **El tranvía de la orilla** recorre los alrededores de la E.N.M.A. siguiendo exactamente sus límites. Nunca entra a los distritos.
  - **Estaciones (propuesta de Claude, 02/11/2026):** son los puntos de viaje rápido y el lugar de las charlas de la mañana ([dia.md](dia.md) §3).
    - **Cada línea tiene sus propias paradas** a lo largo de su recorrido (regla del autor). Las líneas largas tienen más.
    - **Donde una línea toca a otra**, porque la cruza, comparte calle con ella o termina en ella, la parada es **de correspondencia** (regla del autor).
    - En el plano, la parada simple es un círculo pequeño del color de su línea, y la de correspondencia, un círculo blanco con borde oscuro.
    - La estación central del circuito del centro sería **Muros del Pasado**, donde bajan los trabajadores en la introducción.
    - El tranvía de la orilla tiene 4 paradas, donde terminan las avenidas. La del sur sería la **entrada principal**, por la «avenida central del sur» del guion.
    - En el Distrito 4, la «Parada al Distrito 4» de la introducción queda junto a la plaza principal 1, el parque del arroyo y el Edificio 2.
- **Requisito para más adelante (01/11/2026): el mapa gana detalle al hacer zoom.** De lejos se ven distritos, avenidas y líneas de tranvía. Más cerca, calles, tramos y edificios. Muy cerca, los caminos, patios y entradas dentro de cada manzana.
  - **La escala tiene que cuadrar con el relato.** Es una institución de más de 10 000 personas: 11 000 inscritos, según la introducción, más profesores y trabajadores. Viven ahí, así que hacen falta residencias, comedores y aulas suficientes.
  - Primero hay que aclarar qué es cada edificio.
- Cada distrito repite una base (aulas, residencias, enfermería, centro de información, auditorio principal, almacén, parque) y tiene sus propios clubes.
- **Lo que tiene cada distrito** (fijado por el autor el 01/11/2026; el Distrito Central queda aparte por ahora):
  - **Un centro del distrito.** Es grande y tiene una plaza central. Ahí están el ministro, el biministro y los administradores. El guion lo llama *centro principal del distrito* o *edificio principal* (1.9, 2.8, 2.10). La **administración** está en todos los distritos.
  - **Un parque principal**, **un hospital**, **un comedor** y **un auditorio**.
  - **Cuatro carreras secundarias**, numeradas como en el listario:

    | Distrito | Carreras |
    |---|---|
    | 4 | 3 Medicina · 4 Botánica · 5 Gastronomía · 6 Historia Universal |
    | 3 | 7 Matemáticas · 8 Imprenta · 9 Mecatrónica · 10 Química |
    | 2 | 11 Pintura y Arte · 12 Electricidad · 13 Textiles · 14 Teatro |
    | 1 | 15 Derecho y Ley · 16 Economía · 17 Agronomía · 18 Tecnología |

  - El Consejo (1) y el Mercantil (2) no son carreras de distrito.
  - **Reparto propuesto (02/11/2026, por revisar):** en el plano, cada distrito tiene ubicados su centro, su plaza, su parque, su hospital, su comedor, su auditorio y sus cuatro carreras. Lo que eligió Claude va marcado como «propuesta».
  - **Numeración del plano, igual en todos los distritos** (fijada por el autor el 02/11/2026):

    | Número | Edificio |
    |---|---|
    | 1 | Centro del distrito, con su plaza |
    | 2 | Parque principal |
    | 3 | Hospital |
    | 4 | Comedor |
    | 5 | Auditorio |
    | 6–9 | Sus cuatro carreras, en el orden del listario |
    | 10 | Dormitorios: los edificios del mismo tamaño, lejos del centro. El número es propuesta de Claude |
    | 11 | Aulas: no son de ninguna carrera, sirven para distintos usos y están cerca de las carreras |
    | 12 | Lo que el distrito maneja para toda la E.N.M.A. (ver abajo) |
    | 13 | Módulo de seguridad: uno por distrito (02/11/2026), cerca de una entrada |
    | R | Residencias de maestros, trabajadores y gente del Muro Mercantil. Número provisional, se ajusta al final |

  - **Lo que cada distrito maneja para toda la E.N.M.A.** (fijado por el autor el 02/11/2026):

    | Distrito | Se encarga de | Edificio en el plano |
    |---|---|---|
    | 1 | Suministro de insumos: comida y bienes materiales | el edificio grande al sur del centro (propuesta) |
    | 2 | Suministro de electricidad | la «Central de Energía» de su hoja |
    | 3 | Transporte: el almacén de tranvías | el edificio al este del octágono, en su hoja «Central de Tranvías» |
    | 4 | Logística de la E.N.M.A. | el edificio justo al sur del centro (propuesta) |

    El suministro del Distrito 1 se movió a la «Fragua» de su hoja, junto a las granjas y el molino.

  - **Lugares propios de cada distrito** (fijados por el autor el 02/11/2026; en el plano van con letras):
    - **Distrito 1:** el Museo de Historia de la E.N.M.A., los campos de recreación antiguos, las Torres del Último Batallón (las «Torre Guardián» de la hoja) y el mercado antiguo, que va al centro del distrito, junto a la plaza del hexágono.
    - **Distrito 2:** el Museo de Cristal, el parque auditorio al aire libre de Rudentlein y las mesetas de Janna.
    - **Distrito 3:** el Parque Levantado del Progreso, el parque de girasoles y los caminos y monumentos del pensamiento. **La franja rayada de la hoja es un río** que pasa por el distrito.
    - **Distrito 4:** el jardín de flores, que es el **Jardín de Fa** del guion; el deportivo Haser (el centro deportivo del guion) y la cúpula de Mudrick (la «cúpula principal» del guion).

  - **Dormitorios** (02/11/2026). Todos los distritos tienen dormitorios: son los edificios iguales en tamaño y lejanos al centro del distrito.
    - **Los que salen en las hojas:** Distrito 1, 4; Distrito 2, 4; Distrito 3, 6; Distrito 4, 7, contando el Edificio 2. En total, 21.
    - **Cuentas:** 11 000 inscritos, dos por cuarto (como Alextsu e Itzela) y unos 50 cuartos por piso (por el «cuarto 248»). Con 21 edificios tocan unos 524 por edificio, así que hacen falta edificios de 5 a 6 pisos.
    - **El número de pisos varía según la época de cada distrito** (fijado por el autor). No hay un edificio único para todos.
    - **Mixtos o de un solo género:** depende de cuántos inscritos haya. Los que sobran se juntan, mixtos por defecto.
  - **Residencias** (fijado por el autor el 02/11/2026). Son para maestros, trabajadores y gente del Muro Mercantil, no para estudiantes. En la hoja del Distrito 2 son los tres edificios con el número 4.
    - **Proporción:** 1 por cada 5 alumnos, según el autor. Con 11 000 alumnos son unas 2 200 personas.
    - **Una residencia por distrito** (propuesta del autor, 02/11/2026). Va en un edificio de tamaño parecido a los dormitorios, no en uno grande.
    - Con la proporción 1:5 salen unas 550 personas por residencia. O son complejos grandes, o parte del personal vive fuera y entra en tranvía, como los trabajadores que bajan en Muros del Pasado en la introducción.
    - Los ministros y biministros viven en el centro de su distrito (Ren, en los apartamentos del edificio central, 2.12).
  - **Aulas** (fijado por el autor el 02/11/2026). Cada distrito controla sus aulas. No pertenecen a ninguna carrera, sirven para distintos usos y están cerca de las carreras.
    - Pendiente: en las hojas, el número 2 parece querer decir «Aulas». En los Distritos 1 y 2, esos edificios se usaron como dormitorios. El autor decidió dejarlo así por ahora y redistribuir más adelante si hace falta. Las aulas del Distrito 2 quedaron en tres edificios libres del suroeste, cerca de Electricidad: dos que su hoja llamaba «Residencias» y uno sin número. Junto a Textiles y Teatro no quedaba ningún edificio libre.
- **El trazado de cada distrito** (fijado por el autor el 01/11/2026).
  - Todas las calles tienen el mismo ancho.
  - Las manzanas grandes de los bocetos se conservan: una calle no las parte para forzar un cruce, sino que se corta y queda en T.
  - Las construcciones son rectangulares; las curvas son de las calles, no de los edificios.
  - **Distrito 1:** diagonales y caminos que llevan al hexágono central, con cruces y caminos irregulares.
  - **Distrito 2:** parecido al 4, pero con calles curvas que rodean subidas.
  - **Distrito 3:** muchas diagonales con cruces irregulares.
  - **Distrito 4:** calles que se cruzan en cruz casi siempre, con manzanas de tamaño parecido para que la cruz siempre se forme.

### Jerarquía interna

Rector o Director · Consejero de la E.N.M.A. · Director de Muro Mercantil · Director de Distrito · Controlador de Zonas · Administrador Principal · Bi-Ministrador · **Líder de Club** · Líder de Equipo · Bi-Líder de Equipo · Controlador de Documentación · Auxiliar Administrativo

> Los **clubes** de los emblemas (Cocina, Administración…) son **clubes escolares reales** de esta jerarquía. La sinergia mecánica y la pertenencia narrativa son la misma cosa.

---

## 3. La estructura del día — la capa fuera del combate

El día va de 7:00 a 21:00. **No es tiempo real:** el reloj solo avanza al hacer actividades y caminar no gasta tiempo. Las reglas vigentes están en [dia.md](dia.md).

| Franja | Qué pasa |
|---|---|
| **7:00 – 9:00** | Charlas camino a clase o en el tranvía. Eventos prioritarios con personajes. |
| **9:00 – 12:00/13:00** | **Clases.** Aquí se desarrollan los **lazos** con los **81 personajes secundarios**. Participar en debates y responder bien **sube la reputación**; hacerlo mal la baja. |
| **12:00 – 15:00** | Medio día ┐ |
| **15:00 – 18:00** | Tarde  ├ tiempo libre |
| **18:00 – 21:00** | Noche  ┘ |

> Resuelto en [dia.md](dia.md) §1: la clase es de 9:00 a 12:00.

### Actividades y su coste en tiempo

> ⚠️ **Costes en horas de la documentación vieja.** Los vigentes son en bloques y están en [dia.md](dia.md) §2.

| Actividad | Coste | Efecto |
|---|---|---|
| **Estudiar música** (solo o con 2+ compañeros) | 3 h | Mejora la **carga musical de todos los participantes** |
| **Tarea de lazos** | 4 h | Refuerza un lazo. Desde recados hasta peleas |
| **Ejercicio** | 2 h | Mejora una stat principal |
| **Concursos o batallas** | 1–5 h | Cupones, consumibles, stats, **emblemas**, armaduras, armas |
| **Invitar a salir o estudiar** | — | Reduce los **días de espera** del lazo, según el lugar y sus preferencias |
| **Peticiones de cartel** | — | Reputación + reduce la espera de lazos relacionados |
| **Comprar objetos** | **0 h** | Deliberadamente gratis |

**Regla pragmática:** una misión de combate cuenta **siempre como 1 bloque**, dure lo que dure en tiempo real. *(La versión vieja decía 4 horas.)*

### Entrenamiento de stats por ubicación

> ⚠️ **Sustituido el 02/11/2026** por las aficiones: ver [aficiones.md](aficiones.md). La tabla de abajo queda solo como historial.

| Stat | Dónde | Bloques |
|---|---|---|
| Vida Base | Circuito de ejercicio, Distrito 4 | 1 |
| Daño Físico | Gimnasio, Distrito 1 | 1 |
| Daño Mágico | Aulas prácticas — prof. **Graves** · distrito ❓ (decía 5) | 1 |
| Armadura | Hospital de **Félix** — el mismo que trata el desgaste ([dia.md](dia.md) §11) | 1 |
| Resistencia Mágica | Defensa mágica, Distrito 2 — **Charles** | 1 |
| Precisión | Campo de arcos de **Oyvin** · distrito ❓ (decía 6) | 2 |
| Evasión | Invernadero de **Merie**, Distrito 4 | 2 |
| Crítico | Cordillera de **Antony** | **3 — día completo** |
| Carga Musical | Parques de **Meiko** | 1 |

> Cada stat se entrena **en casa de alguien**. Subir crítico cuesta un día entero. Eso convierte la mejora de personaje en una decisión de agenda **y** de relación social: los lugares son personas.

### Dormir tarde tiene consecuencias

| Hábito | Consecuencia |
|---|---|
| 4:00 o más, **1 día** | Se duerme en clase · pierde los eventos de la mañana · **se acuesta obligatoriamente tras clase y despierta a las 18:00** |
| 2:00 o más, **2 días seguidos** | Se duerme en clase · pierde la mañana · la tarde queda normal |
| 24:00 o más, **3 días seguidos** | Sin bonificaciones en preguntas ni debates · **la clase de música se vuelve más difícil** |
| 22:00 o más, **5 días seguidos** | Somnolencia: **reduce cuántos días puede descontar de los lazos** |

### Estado de los personajes

Los principales pueden estar **heridos, enfermos o emocionalmente afectados**, y entonces son **menos propensos a ayudarte** en lazos, peticiones o planes. Depende de lo que les haya pasado — **tus decisiones tienen consecuencias sobre su estado**.

> Es el enganche directo del [sistema emocional](gdd.md): el clima, el calendario, la rutina social y la comida entran al combate por aquí.

---

## 4. Lo que esto resuelve de nuestro diseño

| Pregunta abierta | Respuesta |
|---|---|
| ¿Qué pasa entre combates? | Un día escolar completo con agenda, lazos y entrenamiento |
| ¿Cómo se sube de nivel? | Entrenando stats por bloques de tiempo en ubicaciones concretas |
| ¿De dónde salen los emblemas? | De concursos, batallas y lazos — y son clubes escolares reales |
| ¿Por qué importan los lazos? | Reducen los días de espera y desbloquean tareas, emblemas y recompensas |
| ¿Dónde encaja la Carga Musical fuera del combate? | **Estudiar música** (3 h) mejora la carga musical de todos los participantes |
| ¿Qué es la habilidad de overworld de MÚSICA? | Presumiblemente lo que se sube estudiando música y en la clase de música |

---

## 5. Qué es una "run" — RESUELTO

El ciclo canónico de EMOTION MASTER lo define:

```
InicioDelDía → DuranteElDía → InicioDeRun → Combate → FinDeRun → FinDelDía → día siguiente
```

**La run vive DENTRO del día.** `FinDeRun` y `FinDelDía` son pasos distintos: la emoción activa **sobrevive al final de la run** y solo se elimina **al final del día**.

| Nivel | Qué es | Qué se reinicia al terminar |
|---|---|---|
| **Combate** | Un encuentro | Stacks de evasión, buffs de combate, estados temporales |
| **Run** | El bloque de expedición que contiene combates. Ocupa **1 bloque** del día | — (la emoción continúa) |
| **Día** | 7:00 a 21:00, con tres bloques libres | **Todas las emociones → NEUTRAL** |

### Y el desgaste encaja aquí — RESUELTO

Una **campaña dura 1 bloque**. El desgaste se acumula durante esa run y **no baja mientras dura**. Entre campañas **sí baja**: solo con el paso de los días, y más rápido con hospital o tratamiento según la gravedad.

Por eso el desgaste es un sistema **del día**, no del combate: se paga con tiempo, y ese tiempo compite con los lazos y la historia. Ver [dia.md](dia.md) §11.

---

## 6. Las reglas de la historia: la línea temporal 4

> Reglas fijadas por el autor el 24/10/2026. **Si las biografías o sus cronogramas las contradicen, mandan estas reglas y la Historia Principal**; los cronogramas se revisarán al final. El resumen de cada capítulo y las sugerencias están en [guion/NOTAS.md](../guion/NOTAS.md).

### Las líneas temporales

| Cuándo | Qué pasa |
|---|---|
| **Línea 3, 2012** | Alextsu, corrompido por el Clasical de la Oscuridad, hace daño a los ocho principales. |
| **24/12/2012** | Después del concierto, el **atentado**, que ejecuta **Charles Leoroux** por orden de Artur. Mueren los padres de Alextsu y él queda en coma. **Mike e Itzela** lo salvan y lo esconden en una choza de los bosques de coníferas de la ciudad de Itzela. Artur lo da por muerto. Itzela despierta su Clasical cuidándolo. |
| **2013** | Leopold, desde el futuro, le da a Itzela una carta que **confirma lo que ella ya sospechaba**. Itzela recorre muchos países entregando cartas a las víctimas, y **nadie le cree**. La última visita es a Haruka. |
| **Hacia finales de 2013** | Empieza la **línea 4**, más o menos un año después del atentado. Artur ha descuidado la línea 3 mientras preparaba el cambio. |
| **23/01/2017** | Empieza la Historia Principal. |

### Qué cambia en la línea 4

- **Los principales tienen bloqueados los recuerdos de la línea 3**, y les asignaron otros nuevos.
- **El bloqueo borra el *quién*, no el *qué*.** El daño sigue ahí (Ren perdió a Remi, Ares vuelve a acabar preso, la familia de Dorokan sigue arruinada) y los sentimientos sobreviven, pero no saben que fue Alextsu.
- **Para todos, Alextsu está muerto.** Era una estrella del piano, y por eso lo reciben como a una leyenda que vuelve.
- **Recordar es despertar el Clasical.** Quien despierta recupera los recuerdos de la línea 3.
- **Las cartas sobreviven al cambio de línea** porque Leopold las escribió con **la pluma que le regaló Artur**, un artefacto con parte de su alma. Quien la tiene no sabe a quién menciona, y lo entenderá al despertar. Ren nunca recibió carta; Kali guarda la suya sin abrir.

### Quién recuerda desde el principio

- **Itzela.** Antes de entrar en la E.N.M.A. le pidió a **Mike** que le cerrara los recuerdos y el Clasical, para que Artur no notara su libre albedrío. **La llave es que Alextsu le diga su nombre él mismo** (en el tren, el 23/01/2017). En ese momento lo recuerda todo y le devuelve la bofetada de 2012. Desde entonces lo sabe y lo oculta, y su "despertar" del 28/01 es una puesta en escena.
- **Amy.** Despertó el Clasical de la Luz a los 8 años, con la rabia, en la puerta de sus padres biológicos, con Kali y Dorokan delante, y se teletransportó a un bosque lejano. Recuerda las dos líneas, pero no sabe qué es Alextsu y cree que él cambió la historia: por eso es la *fake-villain*. No sabe activar su Clasical a voluntad hasta el Capítulo 5. Solo ella sabe que Kali es su hermana; Dorokan solo vio el resplandor.

### Las voces: los recuerdos de la línea 3

Fijado por el autor el 01/11/2026.

- **Los principales que no han despertado oyen voces.** Son sus recuerdos de la línea 3, que intentan salir a pedazos: el *qué* sin el *quién*, como marca la regla del bloqueo.
  - Suenan dos o tres veces por arco y se vuelven más nítidas al acercarse el clímax.
  - Son frases de su propio recuerdo de 2012: lo que se oye ahogado en el Preludio.
  - **Las voces solo suenan en la Historia Principal.** En el overworld hay solo guiños, como que a alguien le duela la cabeza al ver un escenario parecido a uno de la línea 3.
- **Al despertar su Clasical y vencer al jefe del arco, los recuerdos se conectan.** Viene un flashback frente a ellos y su punto de vista del Preludio suena por fin completo. Saben que fue Alextsu. Eso explica la euforia, mezcla de poder recuperado, verdad y odio, como la de Ren frente a Alextsu en el Capítulo 2.
- **Las excepciones son Itzela y Amy.**
  - **Itzela** lo recuerda todo desde el tren; su destello al oír el nombre es su conexión.
  - **Amy** recuerda las dos líneas desde los 8 años.
  - Las voces las oyen siete: Ren, Kali, Dorokan, Richard, Ares, Haruka y **Alextsu**.
- **Alextsu es el último en conectar.** Su amnesia es física y su Clasical no despierta hasta el final. Su flashback sería el 2012 visto desde él.

### Artur, la FDPH y el mundo

- **La guerra.** Artur alargó la guerra de 1935 hasta **1985** para desgastar al mundo, jugando a los dos bandos. Vendió las armas y después los materiales para reconstruir. La gente culpa a sus gobiernos y acepta su ayuda.
- **Los tratados de 1985** frenan el avance tecnológico y le ceden poder. Eso explica el nivel tecnológico de 2017, los acueductos prohibidos y la ejecución de Ares.
- **La FDPH.** A través de ella, Artur asigna a cada persona "el mejor futuro", y los gobiernos lo aceptan a regañadientes. Es un poder descentralizado que controla naciones para "ayudarlas".
  - **Para el público** es una fundación internacional que pertenece a la E.N.M.A.; nadie la relaciona con Artur.
  - **Por dentro** solo aplica los protocolos de Artur: es él quien decide el resultado de cada persona según lo que ve.
  - **Quien se desvía** no es castigado: simplemente deja de recibir los apoyos que la FDPH le asignaría. En un mundo en reconstrucción, y con una FDPH casi omnipresente, eso lo es todo. Por eso la gente está "más que feliz", aunque no sea con lo que quería.
  - **No todos lo viven igual.** Cambia de dónde vienen: unos lo perdieron todo en la guerra, otros nacieron después, y a algunas familias les fue bien. Y cambia qué hacen con su destino:

    | Postura | Desviarse, al hacer lazo con Alextsu, es… |
    |---|---|
    | No quería ese destino | una liberación, con miedo a perder los apoyos |
    | Lo agradece con el alma | traicionar a quien lo salvó |
    | Le da igual: va por el beneficio | un cálculo de costos |
    | Alineado con la E.N.M.A. (p. ej. Robert: lo acepta, sin ser rígido) | una crisis de fe |
    | Vive el destino que añoraba sin saber que la E.N.M.A. se lo da | perder justo lo que quería |

  - **La posguerra se cuenta desde la gente, no desde la E.N.M.A.**, que es la burbuja. Que la FDPH los aceptara es el milagro de los residentes. Alextsu entró directo por el piano y ni sabe que hubo guerra, así que su ignorancia suena casi a burla.
  - **Alrededor de Alextsu, la FDPH falla:** como Artur no ve nada cerca de él, quienes hacen lazo con Alextsu empiezan a desviarse de su destino. Se profundiza en las historias secundarias y en la HP de expedición.
- **La vigilancia de los despertares.** Artur vigila cualquier señal de despertar del **poder puro**, porque no se distingue de un Clasical. Los Clasical son raros, pero se sabe que existen, y ante la mínima sospecha se envía a la persona a la E.N.M.A.
- **Para el público, Artur es solo el rector actual**, uno más en la lista. Nadie sabe que fundó la E.N.M.A.
- **La primera guerra de la E.N.M.A.** La academia se fundó en 1615, al margen de gobiernos y religiones, y los imperios de entonces la asediaron como "herejía del poder absoluto". Fue el asedio del Distrito Central y del Distrito 1, entre 1615 y 1650.

### El poder de Artur y Alextsu

- **Artur ve el destino de todos**, a través de sus ojos y como un pensamiento: lo que ve es lo que de verdad pasaría si se toman ciertas decisiones (por ejemplo, permitir que un estudiante resida en la E.N.M.A.). Tener *libre albedrío* significa que Artur no puede manipularte, aunque siga viendo tu destino.
- **Alextsu es el único inmune por completo.** Además, mientras él está cerca, **Artur no ve el destino de nadie**. Desde que Alextsu llega a la E.N.M.A., Artur juega a ciegas por primera vez. Por eso la llave de Itzela es Alextsu: ella recupera la memoria justo al lado de la única persona junto a la que Artur está ciego.
- **Las corrupciones** son el arma de Artur para acabar con Alextsu sin intervenir. Una corrupción es alguien cuya **alma quedó sellada en un libro**: está muerta, pero su alma no descansa.
- **Las corrupciones de los clubes.** Los 18 personajes de las viñetas opcionales del Capítulo 1 (uno por club) serán las **futuras corrupciones de su club**, ya en esta entrega. Además, cada uno es el lazo de un principal en el listario. Sus nombres son una excepción a la regla del listario y se quedan como en la historia.
- **Los Defensores** son la excusa de Artur para tener cerca a los Clasicals.
- **Los guías.** Después de cada despertar, la E.N.M.A. asigna al nuevo Clasical un "guía", que es una excusa para vigilarlo de cerca. Los guías **informan directamente a Artur**, pertenecen a la HP envolvente y en el final abandonan a Artur. El reparto coincide con los lazos de octubre de `PERSONAJES LISTARIO.xlsx`:

  | Principal | Guía |
  |---|---|
  | Itzela | Graves Sochist. No coopera: por sus diferencias con Artur y la E.N.M.A., y por haber perdido a su esposa Ilset en la Tarde Oscura, sus informes son vagos. Solo quiere estar solo, y se abre muy poco a poco. |
  | Ren | Magdalena Rosso |
  | Kali | Charles Leoroux (ejecutó el atentado de 2012; su misión era eliminar a Alextsu) |
  | Dorokan | Vint Wind |
  | Amy | Yumiko Katakuchi |
  | Richard | Rupert Engel |
  | Ares | Clementine Anuar |
  | Haruka | Roman Farfox |

  **Alextsu no tiene guía.** No tiene poderes hasta el final, y además, como portador de la Oscuridad, **repele y anula el poder de Artur**: Artur aumenta su poder fragmentándolo, y Alextsu niega esos fragmentos. Artur lo sabe de primera mano.
- **Mike** fue colaborador de Artur durante siglos. Está arrepentido y apuesta por que la última generación de Clasicals, junto al portador de la Oscuridad, lo detenga. Como lo vigilan, su posición es pasiva: el cierre de los recuerdos de Itzela es lo único que ha hecho de verdad.
- **Lord Rodrerick** (Cap. 7) fue el único superviviente de un asedio que provocó Artur, y cree que Artur lo salvó. En nombre de la FDPH masacró pueblos y religiones. Es amable con quien cede («Le Finale») y despiadado con quien se niega (la Sacrificadora). Su espada lleva parte del alma de Artur y captura las almas de los vivos para convertirlas en sus bestias.

### Cómo llega Alextsu y otros datos de poder

- **La carta que encuentra Alextsu al despertar** se la dejó Mike: son las instrucciones de lo que tiene que hacer si quiere recordar su pasado. Alextsu hace el **examen de la FDPH** y entra directo en la E.N.M.A. por su brillantez al piano, con un expediente de **"promesa de Clasical"**. La FDPH hizo todo el papeleo.
- **Su amnesia es física**, por el atentado: el poder de Artur no le afecta.
- **La voz** que oye el 25 de enero ("Pronto la E.N.M.A. te necesitará") es un recuerdo de su línea 3. En la línea 3 la Oscuridad ya vivía en él, así que entre sus recuerdos habla también la Oscuridad, su subconsciente. Ver "Las voces".
- **El ser de hielo** del concierto del 28 de enero lo mandó Artur.
- **Las bestias de Rodrerick** las purifica el collar de Ares.
- **El juramento de Artur:** los guías ceden su libre albedrío a cambio de poder y de la protección de la E.N.M.A.
- **Graves no hizo el juramento.** Le da igual lo que le pase a la E.N.M.A.: vive como muerto en vida, deprimido por dentro. A medida que se abre, se inclina hacia el grupo, y es él quien le da la información para unir las piezas que va descubriendo sobre la E.N.M.A.
- **Los artefactos de Artur:** objetos con parte de su alma, como la pluma de Leopold, la espada de Rodrerick o el que conservaba Mike. Con el poco poder que le quedaba en el suyo, Mike cerró los recuerdos de Itzela. Cada artefacto guarda **fragmentos del pasado**: tenerlo revela qué fue y quién lo usó, pero **no toda la verdad**.
- **Amy:** Artur sabe que es Clasical, y lo usa a su favor para manipularla.
- **Leopold** no descubre que es la semilla de Webell hasta la tercera entrega de la saga.

### El final y el alma de Itzela

- **El Capítulo Final ocurre el 24 de diciembre de 2017**, el día que nació Itzela y el día que muere. Es la misma fecha que el Preludio de 2012: la historia empieza y termina el mismo día.
- **Itzela le dio a Alextsu la mitad de su alma** mientras estaba en coma. Eso fue lo que lo salvó, y por eso ella ya tenía el Clasical despierto. Según las reglas del Clasical (§1 del lore del docx): solo quien guarda la fracción del alma puede ver y oír al portador cuando muere; ese Clasical no pasa a nadie mientras exista la fracción; y es posible renacer si alguien entrega su vida.
- **El Año Rojo y la Tarde Oscura** se cuentan, pero fuera de la HP Prota-Prota.
- **Graves intuye la naturaleza de Alextsu** por lo que va pasando: corrupciones, despertares y las investigaciones de ambos lados.
- **La Ruta Secreta** reúne todos los fragmentos de Artur. *Por definir.*

### Los capítulos que faltan y lo que viene

- **Elementos de los principales**, anunciados en los títulos de los capítulos: Alextsu Oscuridad, Itzela Hielo, Ren Fuego, Kali Naturaleza, Dorokan Viento, Amy Luz, Richard Electricidad, Ares Alma y Haruka Cristal.
- **Capítulo 8, "Cristal y tela" (Haruka).** Haruka evita llamar la atención por su apellido prestigioso. Se dedica a la textilería y hace muchos trabajos para los clubes y para la E.N.M.A., y no habla de su padre ni del piano. Se pone nerviosa si alguien la oye tocar. Su guía es Roman Farfox.
- **Capítulo 9, "Al filo de la verdad".** Artur deja de despertar corrupciones mayores, deja de atacar y va aislando al grupo. Es fin de ciclo y los residentes se van. El grupo sabe que Artur irá a por uno de ellos, pero no a por quién.
- **Capítulo Final:** el 24 de diciembre (ver arriba).
- **Leopold viaja a 2013** para darle la carta a Itzela. En esta entrega solo se menciona, hasta el final.
- **Después del final** vuelve el libre albedrío, aunque casi no se nota porque la E.N.M.A. está de vacaciones de invierno. Artur, sin apoyo, queda en coma. **La siguiente entrega** empieza un año después, con una E.N.M.A. que flaquea por el nuevo libre albedrío.

### El calendario de cada arco

Desde el primer contacto con el personaje del arco, los días van **de 5 en 5 hasta el clímax del día 26**. Después vienen el hospital (27) y el nombramiento (28), y 5 días más tarde empieza el arco siguiente. Los días intermedios quedan libres para los lazos. Los días de la semana son los reales de 2017.

### Otros datos fijados

- **Leopold** es hijo de Artur, en sentido literal. En público se llama **Leopold Wether** (líder del Consejo Estudiantil, lazo de Alextsu en abril); **Hosenfield** es su apellido privado.
- **El Capítulo 8** es el de Haruka.
- **Graves**, exdefensor Clasical, cuida de las hermanas de Itzela en una historia paralela.
- **La bofetada de 2012** se verá en un recuerdo futuro.
- **Shiro Durán y Mark Norst** son secundarios por desarrollar.
- **"AZERION"** es un estilo de pelea.
- **La sangre AB+** recuerda que Alextsu es vulnerable.

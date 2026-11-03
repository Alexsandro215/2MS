# Ventana de combate

El núcleo de combate (`src/EnmaCore`) con una cara: la misma partida que el modo `jugar` de la consola, contra el mismo muñeco, pero con ratón.

**La ventana no sabe ninguna regla.** Pinta el estado, convierte cada botón en una acción y se la pasa al motor. Si el motor la rechaza, el registro dice por qué en rojo: también es una forma de probar las reglas.

## Cómo se abre

Doble clic en **`LANZAR.bat`**. Compila el proyecto y abre la ventana. Con una semilla fija salen los mismos dados que en la consola:

```
LANZAR.bat 7
```

Necesita Godot 4.7.2 con C# (el que instala winget, igual que el grabador de temas) y .NET 8. El SDK de C# de Godot no está en nuget.org: viene con el propio Godot, y `nuget.config` apunta a él.

## Qué hay en pantalla

| Zona | Qué enseña |
|---|---|
| **Arriba** | Ciclo, barra de Carga Musical (con ✦ CLASICAL), objetos gratis y orden del turno |
| **Tu equipo** | Cada personaje con su línea, su casilla (DEF · NEU · OFE), su vida y si ya actuó |
| **Rival** | El muñeco: un millón de vida, no ataca nunca |
| **Acciones** | Lo que puede hacer quien tiene el turno. Al empezar el ciclo, eliges quién abre |
| **Registro** | Cada golpe con su daño, su crítico y su CM; y cada rechazo del motor con su motivo |

**Resumen** escribe en el registro, por personaje, golpes, acierto, daño, daño por ciclo y la CM que metió en la barra. **Otra partida** empieza de nuevo con otra semilla.

## Lo que aún no hay

- De los 20 kits hay **dos** montados: **Kronwell** en la *media* (Q y W acumulan cargas, E y R las gastan) y **Halltus** en la *pesada* (ninguna tecla pega: sube stats a los compañeros, les limpia debuffs y da inmunidad). Los demas personajes gastan CM y no hacen nada, y el registro lo dice.
- **El ritmo del básico no está conectado**: todos los golpes cuentan como fuera de tempo, y la sección de la canción de la Carga Musical se elige con el botón.
- El muñeco **no ataca**, así que no se ven el K.O., el rescate ni el remate.

## Para pruebas automáticas

Detrás de `--`, la ventana acepta tres opciones. Sirven para comprobar la interfaz sin tocar el ratón:

```
Godot.exe --path . -- --semilla=7 --guion=abrir:ligera,mover:3,basicos,terminar --captura=pantalla.png
```

| Opción | Qué hace |
|---|---|
| `--semilla=N` | Los mismos dados que con esa semilla en la consola |
| `--guion=...` | Acciones seguidas, como si se pulsaran: `abrir:ID`, `basicos`, `mover:1-3`, `tecla:q`, `carga:1-3`, `objeto:1-3`, `defender`, `huir`, `esconder`, `salir`, `swap:ID`, `terminar` |
| `--captura=ruta.png` | Guarda una imagen de la ventana y se cierra |

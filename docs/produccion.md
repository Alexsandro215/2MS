# E.N.M.A — Notas para la fase de creación

> Decisiones y advertencias que no son de diseño de juego, sino de **producción**. Se guardan aquí para retomarlas cuando se empiece a construir.

---

## 1. Animación de estilos — retargeting

**Duda (nov 2026):** como cualquier personaje puede usar cualquier estilo, ¿basta con pasar las animaciones de un modelo a otro?

**Respuesta corta:** en buena parte sí, si todos comparten esqueleto. No es automático del todo.

### Cuándo funciona

Si **todos los personajes jugables comparten el mismo esqueleto** (mismos huesos, nombres y jerarquía), el motor transfiere las animaciones casi solo:

| Motor / herramienta | Sistema |
|---|---|
| Unity | *Humanoid* (Mecanim) |
| Unreal | *IK Retargeter* |
| Godot | Retargeting con *BoneMap* |
| Blender | Rokoko, Auto-Rig Pro |

### Dónde se rompe

| Problema | Causa | Solución |
|---|---|---|
| Proporciones distintas | Manos que atraviesan el cuerpo, pies que resbalan | **2–3 tipos de cuerpo**, no uno por personaje; IK de pies y manos |
| El arma | No forma parte del esqueleto | **Socket** en la mano + IK para la segunda mano |
| Pérdida de personalidad | La misma R se ve igual en todos | Personalidad en *idles*, entradas y victorias, no en las skills |

### El volumen: separar en capas

Estilo y arma son independientes, así que la cuenta ingenua es:

```
20 estilos × 3 armas × (Q, W, E, R, EVOL)  ≈  300 animaciones
```

Para reducirla:

- **El arma decide el cuerpo.** Básicos y postura van por arma (ligera · media · pesada). Pocas animaciones.
- **El estilo decide el gesto.** La skill es un gesto corto, a menudo solo del torso, mezclado encima (máscaras / capas aditivas).
- **El VFX carga la identidad.** Se tiñe según el **elemento del personaje**: el mismo estilo se ve distinto en Fuego y en Hielo, igual que en mecánicas ([elementos.md](elementos.md)).

### Sincronía con la música

Si las skills declaran su duración en **tiempos musicales** (propuesta en [musica.md](musica.md) §2.1), cada animación tiene un largo en beats. El retargeting no cambia el timing, y el motor puede escalar la reproducción según el **BPM** para que la skill caiga siempre en el compás.

✅ **Motor: Godot** (los prototipos de `proto/` ya están en Godot, con el núcleo en C#). El retargeting se hace con *BoneMap*: conviene fijar el esqueleto base desde el primer modelo.

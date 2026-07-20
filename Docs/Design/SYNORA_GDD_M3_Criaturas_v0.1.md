# SYNORA — GDD delta M3: Criaturas (v0.1)

> Delta funcional propuesto para M3. Documenta **únicamente comportamiento funcional aprobado**. No es canon cerrado: el retrato creativo de Verak (origen, biología, historia, simbolismo, relación con SYNORA) requiere **revisión creativa separada** (autoridad de ChatGPT y del Director). Este documento no inventa lore.
>
> Autoridad: la Biblia manda sobre canon; el GDD sobre gameplay y alcance. Este delta describe solo la capa mecánica de M3.

## 1. Propósito de M3 (funcional)

Que el mundo deje de sentirse vacío mediante la primera criatura ambiental viva: **Verak**. M3 es la demostración de "ecosistema vivo": criaturas que existen, patrullan, descansan, perciben al Player y reaccionan de forma no agresiva.

## 2. Verak — comportamiento funcional

- Criatura **ambiental tranquila**, cautelosa y consciente de la presencia del Player.
- **Patrulla lentamente** por una ruta definida.
- Realiza **pausas** ("respiración") entre tramos.
- **Detecta** al Player dentro de su radio de percepción.
- Al detectarlo: **se detiene**, **lo observa** y **permanece alerta**.
- Cuando el Player **se aleja** lo suficiente (y por un tiempo), **vuelve a su ruta**.

No hace nada más: **no persigue, no ataca, no huye, no captura, no combate, no vincula**.

## 3. Integración concreta en M3

- **Escena:** `ClaroExterior`.
- **Cantidad:** **2 Verak**.
- **Estado narrativo:** ambos **libres**; no atrapados; no forman parte de una cinemática; no representan aún una misión o evento narrativo mayor.
- **Rutas:** cada Verak tiene una **ruta independiente** (PingPong); las rutas se colocan en espacio libre y no deben cruzarse de forma que provoquen bloqueos constantes.
- **Prefab/identidad:** ambos usan el mismo prefab y el mismo perfil de especie; sus rutas son datos de instancia de escena.
- **Observación:** **no obligatoria** en M3. La arquitectura queda compatible con el sistema de observación de M2 (`ExaminableInteractable` + `ExaminableData`), pero **no se añaden** textos, prompts, paneles ni interacción obligatoria con los Verak. Cualquier texto de observación futuro requiere aprobación del GDD.

## 4. Fuera de alcance funcional (M3)

Persecución, ataque, huida, captura, vínculo, restauración, evolución, misiones, diálogos, cinemáticas, eventos narrativos mayores, IA de grupo.

## 5. No inventado (requiere revisión creativa separada)

Origen, biología, alimentación, historia, diálogo, relación con SYNORA, simbolismo, poderes y evolución de Verak **no** se definen aquí. Este delta solo congela el comportamiento mecánico aprobado.

## 6. Criterio de aceptación funcional

El jugador entra a `ClaroExterior`, ve dos Verak patrullando con pausas; al acercarse, cada Verak se detiene y lo observa (alerta); al alejarse el jugador, cada Verak retoma su ruta. Sin comportamientos agresivos ni narrativos.

---

**Especificación técnica correspondiente:** [`../Technical/SYNORA_SPEC_M3_Criaturas_v0.1.md`](../Technical/SYNORA_SPEC_M3_Criaturas_v0.1.md).

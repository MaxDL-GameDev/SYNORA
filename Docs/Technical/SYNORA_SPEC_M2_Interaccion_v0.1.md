# SYNORA — Especificación Técnica M2: Interacción y Observación Básica

| Campo | Valor |
|---|---|
| Documento | `SYNORA_SPEC_M2_Interaccion_v0.1.md` |
| Versión | 0.1 |
| Documento padre | `SYNORA_GDD_M2_Interaccion_v0.1.md` (Docs/Design) |
| Editor verificado | Unity 6000.5.3f1 |
| URP 2D | 17.6.0 |
| Input System | 1.19.0 |
| HEAD verificado | `13b97ac` (tag `m1-complete`) |
| Fecha real de redacción | 2026-07-17 |
| Estado | **Propuesta para aprobación del Director — no se ha escrito código** |

---

## 1. Objetivo y alcance

### 1.1 Objetivo técnico

Implementar, sin romper M0/M1, un sistema pequeño y desacoplado que permita:
detectar un único interactuable frontal dentro de 1.25 unidades, mostrar un prompt,
y abrir/cerrar un panel de observación mediante la acción `Interact`, bloqueando
movimiento y orientación mientras el panel está abierto.

### 1.2 Dependencias respecto de M1

- Reutiliza `PlayerOrientation.Facing` (Vector2Int cardinal) como fuente de dirección frontal — **sin modificarlo**.
- Reutiliza `PlayerInputReader` como único punto de lectura de Input System — se añade una segunda acción (`Interact`) leída por un lector nuevo, no por `PlayerInputReader` (ver §5.4).
- Reutiliza el `Player` prefab (Rigidbody2D + CapsuleCollider2D en layer `Player`=8) como referencia de posición/orientación del jugador.
- No depende de `SceneLoader`/`AreaTransition`/`SceneTransitionContext`; M2 no toca transiciones.

### 1.3 Elementos incluidos

- Acción `Interact` en `Controls.inputactions`.
- Modelo de datos de examinable (ScriptableObject + componente, ver §4).
- Detección frontal por Physics2D (buffer preasignado, sin GC).
- Selector determinista de candidato único.
- Gate de bloqueo de control reutilizable.
- Máquina de estados de interacción explícita (3 estados).
- UI graybox (prompt + panel) vía prefab reutilizado por escena.
- 3 instancias de examinable graybox (una por escena de área).
- Pruebas EditMode nuevas, coexistiendo con las 4 de M1.

### 1.4 Elementos fuera de alcance

Los listados en el GDD §5 (NPCs, diálogo, inventario, guardado, cinemáticas, audio definitivo, sprites definitivos, mouse, interacción sostenida, localización completa, rebinding completo), más: Pause/Cancel/Submit, cualquier map de Input adicional, Cinemachine, Addressables, y cualquier paquete nuevo.

### 1.5 Definition of Done técnica

Ver §19.

---

## 2. Arquitectura propuesta

Ningún `InteractionManager` monolítico. Ocho responsabilidades separadas:

```
InteractionInputReader   → lee la acción Interact (Input System), expone un evento "pulsación"
InteractionDetector      → Physics2D frontal, produce la lista de candidatos válidos (0..N, buffer fijo)
InteractionSelector      → función estática pura: candidatos → objetivo único (o ninguno)
InteractionController    → orquesta: detector + selector + input + gate + presentadores; máquina de estados
PlayerControlGate        → bitmask de bloqueo, consultado por PlayerMotor/PlayerOrientation
InteractionPromptPresenter   → muestra/oculta [E] Examinar
ObservationPanelPresenter    → muestra/oculta panel (Título, Texto, [E] Cerrar)
ExaminableInteractable + ExaminableData → datos editables del examinable (IInteractable)
```

Todas las referencias de escena se cablean en el Inspector (SerializeField). Prohibido:
`Singleton`, `Service Locator` estático, eventos estáticos, `DontDestroyOnLoad`,
`GameObject.Find`, `FindObjectOfType`/`FindFirstObjectByType`/`FindAnyObjectByType`,
y cualquier búsqueda global en runtime.

---

## 3. Contratos y responsabilidades

Namespace raíz: `Synora` (igual que M0/M1). Carpetas dentro de `Assets/Scripts/`, que ya
contiene `Core`, `Data`, `Gameplay/Player`, `Systems`. Se añade `Gameplay/Interaction`
para todo lo específico de interacción, salvo el gate de control (que vive en `Systems`
porque es un mecanismo genérico reutilizable, no exclusivo de interacción) y el propio
`PlayerControlGate` es consumido por `Gameplay/Player`.

### 3.1 `IInteractable` (interfaz)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/IInteractable.cs`
- Responsabilidad: contrato mínimo que el detector/selector necesitan, sin acoplarse a `ExaminableInteractable` concreto.
- Miembros:
  - `string InteractionId { get; }`
  - `float Priority { get; }`
  - `bool IsEnabled { get; }`
  - `Transform Anchor { get; }` (punto usado para distancia; ver §7)
  - `void Interact();` (ejecuta la interacción; para M2, abre el panel)

### 3.2 `ExaminableInteractable` (componente)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/ExaminableInteractable.cs`
- Responsabilidad única: adaptar un `ExaminableData` (SO) + su Collider2D en escena al contrato `IInteractable`.
- Dependencias: `ExaminableData` (serializado), `ObservationPanelPresenter` (serializado, cableado por escena).
- Campos serializados: `data` (`ExaminableData`), `priority` (float, default 0), `enabledOnStart` (bool, default true).
- Métodos públicos: implementa `IInteractable`. `Interact()` llama a `panelPresenter.Open(data)`.
- No conoce al `InteractionController` ni al `InteractionDetector`; estos lo conocen a él mediante el contrato `IInteractable`, obtenido por `GetComponent<IInteractable>()` cacheado una vez en preparación de escena (no por frame).
- Ciclo de vida: `Awake` valida `data != null` (log de error si no).

### 3.3 `InteractionInputReader`

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionInputReader.cs`
- Responsabilidad única: leer la acción `Gameplay/Interact` y exponer un evento de pulsación consumido solo por `InteractionController`.
- Campos serializados: `interactAction` (`InputActionReference`).
- Métodos/eventos públicos: `event Action InteractPressed;`
- Ciclo de vida: `OnEnable`/`OnDisable` (igual patrón que `PlayerInputReader`: suscribe/desuscribe y hace `Enable()`/`Disable()` de la acción él mismo).
- Usa la fase `performed` del callback (botón discreto; ver §5).

### 3.4 `InteractionDetector`

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionDetector.cs`
- Responsabilidad única: consulta física frontal por `FixedUpdate`, produce un buffer reutilizable de candidatos `IInteractable` válidos (rango + zona frontal + habilitado).
- Dependencias: `PlayerOrientation` (facing), `Transform` del jugador (origen), `LayerMask` de interactuables (serializado).
- Campos serializados: `playerOrientation`, `originPoint` (Transform), `interactableLayer` (LayerMask), `detectionRange` (float, default 1.25), `frontalHalfWidth` (float, ver §7).
- Métodos públicos: `IReadOnlyList<IInteractable> Candidates { get; }` (vista de solo lectura sobre el buffer interno; no expone el array mutable).
- No selecciona objetivo — eso es responsabilidad de `InteractionSelector`.

### 3.5 `InteractionSelector` (clase estática, pura)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionSelector.cs`
- Responsabilidad única: dado (candidatos, objetivo actual, posición del jugador), devolver el objetivo que corresponde, aplicando la regla de estabilidad y el orden de desempate.
- Sin `MonoBehaviour`. Sin estado. Testeable con GameObjects temporales o con implementaciones falsas de `IInteractable`.
- Firma propuesta: `static IInteractable SelectTarget(IReadOnlyList<IInteractable> candidates, IInteractable currentTarget, Vector2 playerPosition)`.

### 3.6 `InteractionController`

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionController.cs`
- Responsabilidad única: máquina de estados (§9). Conecta `InteractionDetector`, `InteractionSelector`, `InteractionInputReader`, `PlayerControlGate`, `InteractionPromptPresenter`, `ObservationPanelPresenter`.
- Dependencias serializadas: las cinco piezas anteriores.
- Métodos públicos: ninguno necesario fuera de Unity (todo reactivo a input/estado); expone `enum State { ExploringWithoutTarget, ExploringWithTarget, ObservationOpen }` y una propiedad de solo lectura `CurrentState` para tests/depuración.
- Ciclo de vida: se suscribe a `InteractionInputReader.InteractPressed` en `OnEnable`, se desuscribe en `OnDisable`; lee `detector.Candidates` en `Update` (después de que `FixedUpdate` del detector haya corrido — ver §7.7 sobre frecuencia).
- Puede comunicarse con: detector, selector (estático), input reader, gate, presentadores, y con el `IInteractable` objetivo (para llamar `Interact()`).
- No debe conocer: `SceneLoader`, `AreaTransition`, `PlayerMotor` ni `PlayerInputReader` directamente (el bloqueo de movimiento pasa por el gate, no por una referencia directa).

### 3.7 `InteractionPromptPresenter`

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionPromptPresenter.cs`
- Responsabilidad única: mostrar/ocultar el GameObject del prompt y fijar su texto solo cuando cambia.
- Campos serializados: `promptRoot` (GameObject), `label` (TMP_Text o equivalente, ver §10).
- Métodos públicos: `void Show(string text)`, `void Hide()`. Ambos son no-op si ya está en ese estado (evita `SetActive`/asignaciones de texto redundantes).

### 3.8 `ObservationPanelPresenter`

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/ObservationPanelPresenter.cs`
- Responsabilidad única: mostrar/ocultar el panel y volcar `ExaminableData` en Título/Texto.
- Campos serializados: `panelRoot`, `titleLabel`, `bodyLabel`, `closeHintLabel` (opcional, puede ser texto estático `[E] Cerrar`).
- Métodos públicos: `void Open(ExaminableData data)`, `void Close()`, `bool IsOpen { get; }`.
- No conoce `InteractionController` ni el gate; solo presenta datos. `InteractionController` decide *cuándo* llamar `Open`/`Close` y *cuándo* pedir el bloqueo al gate.

### 3.9 `PlayerControlGate`

- Namespace: `Synora.Systems`
- Carpeta: `Assets/Scripts/Systems/PlayerControlGate.cs`
- Responsabilidad única: mecanismo genérico de bloqueo de control, con razones (bitmask), consultado por `PlayerMotor`/`PlayerOrientation`.
- Ver diseño completo en §6.

### 3.10 `ExaminableData` (ScriptableObject)

- Namespace: `Synora.Data`
- Carpeta: `Assets/Scripts/Data/ExaminableData.cs`
- Ver modelo completo en §4.

### Qué clases pueden comunicarse

`InteractionController` ↔ todo lo demás de M2 (es el único orquestador).
`ExaminableInteractable` → `ObservationPanelPresenter` (solo para abrir con sus propios datos, vía `IInteractable.Interact()` que el controller invoca).
`PlayerMotor`/`PlayerOrientation` → `PlayerControlGate` (solo lectura, `IsBlocked`).

### Qué clases no deben conocerse

- `InteractionDetector`/`InteractionSelector` no conocen la UI.
- `ObservationPanelPresenter`/`InteractionPromptPresenter` no conocen Physics2D, Input System ni el gate.
- `PlayerControlGate` no conoce Interaction; es genérico.
- `PlayerMotor`/`PlayerOrientation` no conocen `InteractionController` ni ningún tipo de `Gameplay.Interaction`.

---

## 4. Modelo de datos

**Decisión: combinación de ScriptableObject + componente.**

- `ExaminableData` (ScriptableObject): contenido reutilizable, editable sin tocar código, apto para localización futura.
- `ExaminableInteractable` (componente): ancla en escena, prioridad de escena, referencia al presentador de panel de esa escena.

Justificación: los tres textos de M2 ya sugieren que el contenido irá creciendo (M3 con sprites/canon). Poner `InteractionId`/textos directamente en el componente acoplaría el contenido a cada instancia de escena y dificultaría reutilizar el mismo dato en variantes. Un SO puro sin componente obligaría a un `MonoBehaviour` "puente" igualmente para el Collider2D/Transform, así que la combinación no añade complejidad real.

### 4.1 Campos de `ExaminableData`

```
[CreateAssetMenu(fileName = "ExaminableData", menuName = "SYNORA/Examinable Data")]
public sealed class ExaminableData : ScriptableObject
{
    [SerializeField] private string interactionId;
    [SerializeField] private string displayName;
    [SerializeField] private string observationTitle;
    [SerializeField, TextArea] private string observationBody;
}
```

- `InteractionId`: único y estable, **no** deriva del nombre del asset ni del GameObject.
- `DisplayName`: solo para depuración/inspector (no se muestra al jugador en M2).
- `ObservationTitle` / `ObservationBody`: lo que ve el jugador.
- Prompt/tipo de acción: en M2 es siempre "Examinar"; se representa como una constante en `InteractionPromptPresenter`/`ExaminableInteractable`, no como campo del SO (no hay variación todavía — añadir el campo ahora sería sobrearquitectura; se añade en M3 si aparece un segundo tipo).
- `Priority` y `Enabled`: **no** viven en el SO (son de instancia de escena, ver `ExaminableInteractable.priority`/`enabledOnStart`), porque el mismo contenido podría reutilizarse con distinta prioridad según la escena.

### 4.2 Validaciones `OnValidate` (en `ExaminableData`)

- `interactionId` vacío → `Debug.LogWarning`.
- `interactionId` con espacios al borde → recortar automáticamente (mismo patrón que `SpawnPoint.id`).
- `observationTitle`/`observationBody` vacíos → `Debug.LogWarning` (dato incompleto, no bloquea).

### 4.3 Reglas de IDs duplicados

- Duplicados **entre asset y asset** no son detectables por `OnValidate` de un asset individual. Se detectan en preparación de escena: `InteractionController` (o un validador de editor dedicado, a decidir en Fase 6) recorre los `IInteractable` de la escena al entrar en Play Mode / build y reporta duplicados por escena mediante `Debug.LogError`. Duplicados entre escenas distintas no son un problema porque cada escena es autónoma y solo un examinable está activo a la vez.

### 4.4 Ubicación y convención de nombres de assets

- Ruta: `Assets/Data/Examinables/` (carpeta nueva; el proyecto no tiene aún una carpeta `Data` de assets, solo `Scripts/Data`).
- Nombre de archivo = `InteractionId` en PascalCase, ej. `TerminalDiagnostico.asset`, `PanelMantenimiento.asset`, `NodoInactivo.asset`.

---

## 5. Input System

### 5.1 Cambio exacto en `Controls.inputactions`

Nueva acción dentro del mismo action map `Gameplay` (el único existente):

```
Gameplay/Interact
  Type: Button
  Bindings:
    - Keyboard/e
    - Gamepad/buttonSouth
```

No se crean nuevos Action Maps ni Control Schemes (el asset actual no define ninguno explícito).

### 5.2 Fase del callback

Se usa `performed`. Para un binding `Button` sin interacciones (`Press` por defecto), `performed` dispara una vez al presionar — exactamente lo que necesita un botón discreto de abrir/cerrar. No se usa `started` (dispararía antes de completarse el umbral) ni se necesita `canceled` (no hay estado sostenido que limpiar, a diferencia de `Move`).

### 5.3 Quién se suscribe

Únicamente `InteractionController`, y solo al evento `InteractionInputReader.InteractPressed` — nunca directamente al `InputAction`. Esto evita acoplar la UI o la lógica de estados al `InputActionAsset` (requisito de §5 del brief).

### 5.4 Por qué un lector separado de `PlayerInputReader`

`PlayerInputReader` tiene una responsabilidad única y ya cerrada: `Move`. Añadirle `Interact` violaría esa responsabilidad única y obligaría a tocar un script de M1 ya congelado. `InteractionInputReader` es un componente nuevo, mínimo, con el mismo patrón (`OnEnable`/`OnDisable`, `InputActionReference`).

### 5.5 Cómo se evita abrir y cerrar en la misma pulsación

La regla no depende del Input System sino de la máquina de estados (§9): cada pulsación de `Interact` se procesa **una vez** por evento `performed` (no hay lectura por frame ni polling), y el `InteractionController` decide la transición de estado *antes* de que pueda llegar una segunda pulsación en el mismo frame. Como el Input System entrega el evento una sola vez por flanco de presión, no existe un escenario de doble disparo por la misma pulsación física.

### 5.6 Cómo sigue funcionando Interact mientras el movimiento está bloqueado

`InteractionInputReader` es independiente de `PlayerControlGate`: nunca se deshabilita por el bloqueo de control. Solo `PlayerMotor`/`PlayerOrientation` consultan el gate.

### 5.7 Limpieza de suscripción

`OnDisable` de `InteractionInputReader` desuscribe y deshabilita la acción (mismo patrón que `PlayerInputReader.OnDisable`). `InteractionController.OnDisable` desuscribe su propio handler de `InteractPressed`.

---

## 6. Bloqueo de control

### 6.1 Decisión: gate explícito con bitmask de razones

Se descarta un booleano simple controlado por la UI (no sería extensible: un segundo sistema de bloqueo futuro pisaría al primero al desbloquear). Se adopta:

```
namespace Synora.Systems
{
    [Flags]
    public enum ControlBlockReason
    {
        None = 0,
        Observation = 1 << 0,
        // futuros bloqueos (cinemática, pausa, etc.) se añaden aquí cuando se aprueben
    }

    public sealed class PlayerControlGate : MonoBehaviour
    {
        private ControlBlockReason activeReasons;

        public bool IsBlocked => activeReasons != ControlBlockReason.None;

        public void Block(ControlBlockReason reason) => activeReasons |= reason;
        public void Unblock(ControlBlockReason reason) => activeReasons &= ~reason;
    }
}
```

### 6.2 Consumo por `PlayerMotor` y `PlayerOrientation`

- Se añade una referencia serializada `PlayerControlGate gate` a ambos componentes (cambio mínimo sobre M1, ver §18).
- `PlayerMotor.FixedUpdate`: si `gate.IsBlocked`, usa `Vector2.zero` en vez de `inputReader.MoveInput` al calcular la velocidad, y además fuerza `body.linearVelocity = Vector2.zero` explícitamente (doble garantía: ni siquiera un input residual puede filtrarse).
- `PlayerOrientation.Update`: si `gate.IsBlocked`, no llama a `Resolve` — `facing` se congela en su último valor.

### 6.3 Al desbloquear

- `PlayerInputReader.MoveInput` sigue reflejando el estado real de las teclas (no se limpia artificialmente), pero como `PlayerMotor`/`PlayerOrientation` solo empiezan a leerlo de nuevo tras `Unblock`, no hay "input pegado": si el jugador mantenía una tecla presionada durante la observación, el movimiento se reanuda inmediatamente en esa dirección — comportamiento esperado para un juego 2D cenital, y no un input obsoleto porque proviene de un evento `performed`/`canceled` real, no de un valor cacheado del momento del bloqueo.

### 6.4 Restricciones respetadas

- No usa `Time.timeScale = 0`.
- No desactiva el Player GameObject (todos sus componentes siguen `Update`/`FixedUpdate`; simplemente no producen efecto).
- Sin asignaciones por frame (`Block`/`Unblock` son operaciones de bits, y solo se llaman en las transiciones de la máquina de estados, no por frame).

### 6.5 Extensibilidad

Un futuro sistema (cinemática, pausa) añade su propio valor a `ControlBlockReason` y llama `Block`/`Unblock` con su propia razón, sin coordinarse con Observation. `IsBlocked` es verdadero mientras exista **cualquier** razón activa. Esto es suficiente para M2 sin construir un framework de "locks" con conteo de referencias, colas de prioridad, etc., que no tiene ningún caso de uso real todavía.

---

## 7. Detección frontal

### 7.1 Forma de consulta y parámetros

- **Forma:** `Physics2D.OverlapBoxNonAlloc` (o `Physics2D.OverlapBox` con `ContactFilter2D` + buffer, según preferencia del `NonAlloc` explícito) — una caja alineada a ejes, no un `Circle`, para poder controlar ancho frontal independientemente del alcance.
- **Centro de la consulta:** `originPoint.position + facingDirection * (detectionRange / 2)`, donde `facingDirection` es el vector cardinal de `PlayerOrientation.Facing` convertido a `Vector2`.
- **Tamaño de la caja:** `size = (frontalHalfWidth * 2, detectionRange)` cuando la orientación es horizontal (este/oeste), y `(detectionRange, frontalHalfWidth * 2)` cuando es vertical (norte/sur) — la caja se alinea siempre a los ejes del mundo porque las orientaciones son cardinales puras, sin necesidad de rotar la consulta.
- **Ancho de la zona frontal (`frontalHalfWidth`):** propuesto **0.4 unidades** (ancho total 0.8, ligeramente menor que la distancia máxima de 1.25, para que la zona sea claramente "frontal" y no capture objetos muy laterales). **Marcado como decisión abierta en §20** — depende de sensación de juego con los tres graybox reales.
- **LayerMask:** nueva layer `Interactables` = índice **11** (verificado como primer índice libre tras `Player`=8, `Environment`=9, `Transitions`=10; no se modifica `ProjectSettings` en esta fase — se deja como cambio de Fase 6).
- **`ContactFilter2D`:** `useLayerMask = true` con la máscara anterior; `useTriggers` según cómo se configure el collider del examinable (ver §11 — se recomienda **trigger** para no interferir con colisiones físicas de M1, así que `useTriggers = true`).
- **Tamaño máximo de candidatos:** buffer de **8** elementos (`Collider2D[8]` preasignado en `Awake`); es un margen amplio para 1-2 examinables realmente próximos, sin nunca crear el array por frame.
- **Frecuencia de evaluación:** `FixedUpdate`, igual cadencia que `PlayerMotor` — la detección depende de la posición física del jugador, que solo cambia de forma determinista en el paso de física; evaluarla en `Update` la haría depender del framerate sin beneficio (el jugador no puede reaccionar más rápido que un paso físico de todos modos).
- **Collider2D múltiples en un mismo objeto:** el detector deduplica por `IInteractable` (obtenido vía `GetComponentInParent<IInteractable>` cacheado una vez, no por `Collider2D`), de forma que un examinable con varios colliders cuenta como un solo candidato (requisito también de §8).
- **Punto usado para calcular distancia:** `IInteractable.Anchor.position` (normalmente el propio Transform del examinable), no el punto de contacto del collider — evita que la forma del collider afecte el desempate por distancia.
- **Condición exacta para "enfrente":** el candidato debe caer dentro de la caja de overlap descrita arriba; no se hace ningún cálculo adicional de ángulo/producto punto porque la caja alineada a ejes ya expresa geométricamente "dentro del cono frontal cardinal".
- **Gizmos de Editor:** `OnDrawGizmosSelected` en `InteractionDetector` dibuja la caja de detección actual (color distinto si hay candidatos), reutilizando el mismo estilo de gizmo que `CameraBounds2D`/`SpawnPoint`.

### 7.2 Preferencia técnica respetada

- `OverlapBoxNonAlloc` con buffer preasignado — cero asignaciones por frame.
- No se usan variantes de Physics2D que devuelvan arrays nuevos (`OverlapBoxAll`).
- No hay búsquedas de escena ni triggers globales; el detector solo conoce lo que cae en su propia consulta.

---

## 8. Selección determinista

### 8.1 Orden obligatorio

1. `Priority` descendente.
2. Distancia al cuadrado (`sqrMagnitude`) ascendente.
3. `InteractionId` mediante `string.CompareOrdinal` ascendente.

### 8.2 Reglas

- El objetivo actual (`currentTarget`) se conserva si sigue apareciendo en `candidates` (por referencia/identidad, no por recalcular su rango — el detector ya solo entrega candidatos válidos).
- Si `currentTarget` no está en `candidates`, se recalcula un reemplazo con el orden de §8.1 sobre toda la lista.
- Nunca se usa `GetInstanceID()` como parte del desempate (prohibido explícitamente).
- La selección no ordena la lista de candidatos in-place con `List.Sort`/LINQ; recorre el buffer una vez llevando el "mejor visto hasta ahora" (patrón de reducción, sin asignaciones).

### 8.3 Pseudocódigo

```
static IInteractable SelectTarget(candidates, currentTarget, playerPosition)
{
    if currentTarget != null and candidates.Contains(currentTarget) by reference:
        return currentTarget

    best = null
    bestSqrDist = +infinity

    for each c in candidates:
        if not c.IsEnabled: continue

        if best == null:
            best = c; bestSqrDist = SqrDist(c, playerPosition); continue

        if c.Priority > best.Priority:
            best = c; bestSqrDist = SqrDist(c, playerPosition); continue
        if c.Priority < best.Priority:
            continue

        d = SqrDist(c, playerPosition)
        if d < bestSqrDist:
            best = c; bestSqrDist = d; continue
        if d > bestSqrDist:
            continue

        if string.CompareOrdinal(c.InteractionId, best.InteractionId) < 0:
            best = c; bestSqrDist = d

    return best   // null si no hay candidatos válidos
}
```

`candidates.Contains(currentTarget) by reference` se implementa con un bucle simple (no `List<T>.Contains` con comparador por defecto si eso implicara boxing; se compara referencia directamente ya que `IInteractable` es un tipo referencia).

---

## 9. Estados de interacción

| Estado | Prompt | Movimiento | Orientación | Target activo | Respuesta a Interact |
|---|---|---|---|---|---|
| `ExploringWithoutTarget` | oculto | libre | libre | ninguno | sin efecto |
| `ExploringWithTarget` | visible (`[E] Examinar`) | libre | libre | el seleccionado | abre panel → `ObservationOpen` |
| `ObservationOpen` | oculto | bloqueado (gate) | bloqueada (gate) | congelado (no se re-evalúa) | cierra panel → vuelve a `ExploringWith(out)Target` según validez actual |

### Transiciones

- **Entrada a `ExploringWithTarget`:** el selector produce un target no nulo estando en `ExploringWithoutTarget`. Muestra el prompt.
- **Salida de `ExploringWithTarget`:** el selector produce `null` (vuelve a `ExploringWithoutTarget`, oculta prompt) o Interact (va a `ObservationOpen`).
- **Entrada a `ObservationOpen`:** `gate.Block(Observation)`, `panelPresenter.Open(target.Data)`, `promptPresenter.Hide()`. El detector puede seguir corriendo en `FixedUpdate` (no cuesta nada relevante y simplifica el código), pero el controller **ignora** sus resultados mientras está en este estado.
- **Salida de `ObservationOpen`:** Interact → `panelPresenter.Close()`, `gate.Unblock(Observation)`. Se reevalúa inmediatamente contra `detector.Candidates` para decidir si el nuevo estado es `ExploringWithTarget` (si el mismo u otro objetivo sigue válido) o `ExploringWithoutTarget`.
- **Cambio de escena:** cada escena tiene su propia instancia de `InteractionController` y UI (no sobreviven); no hay transición explícita que gestionar entre escenas, cada una arranca en `ExploringWithoutTarget`.
- **Desactivación del objetivo actual estando en `ObservationOpen`:** no afecta el panel abierto (el panel ya mostró los datos que copió al abrir); al cerrar, el objetivo desactivado simplemente no aparecerá entre los candidatos re-evaluados.

No se implementa una máquina de estados genérica (no hay biblioteca ni framework externo): el estado es un `enum` privado de `InteractionController` con un `switch` en el manejador de `InteractPressed` y en el `Update` que consume `detector.Candidates`.

---

## 10. UI

### 10.1 Jerarquía del prefab

```
InteractionCanvas (Canvas, prefab reutilizado por escena)
├── InteractionPrompt (inactivo por defecto)
│   └── Label (TMP)
└── ObservationPanel (inactivo por defecto)
    ├── Title (TMP)
    ├── Body (TMP)
    └── CloseHint (TMP, texto estático "[E] Cerrar")
```

- **Render mode:** Screen Space – Overlay (no hay justificación para otra cosa en M2: no hay elementos diegéticos en mundo).
- **Posición:** `InteractionPrompt` anclado a la parte inferior central; `ObservationPanel` centrado o inferior (graybox — posición exacta es decisión visual menor, no bloqueante, se resuelve al construir el prefab en Fase 5).
- Cada escena de área instancia su propia copia del prefab (no `DontDestroyOnLoad`); `InteractionController` de esa escena referencia esa instancia.
- Panel y prompt **inactivos** al cargar la escena.
- Sin animaciones, sin pausas, sin arte definitivo, sin paquetes nuevos.

### 10.2 TextMeshPro vs. alternativa

El manifiesto de paquetes (`Packages/manifest.json`) no declara `com.unity.textmeshpro` como dependencia explícita, pero el proyecto usa `com.unity.ugui` 2.5.0 (Unity 6), versión en la que TextMeshPro se distribuye integrado con UGUI y no requiere instalar un paquete adicional — solo importar, la primera vez que se usa, los "TMP Essential Resources" (un recurso del proyecto, no un paquete de Package Manager). **Esto debe confirmarse en el Editor real antes de la Fase 5** (abrir cualquier ventana que use TMP y comprobar si pide importar los recursos esenciales); se deja registrado como decisión abierta en §20 más que como hecho verificado, tal como pide el brief. Si por algún motivo no estuviera disponible sin instalar nada, la alternativa es `UnityEngine.UI.Text` (siempre presente vía `com.unity.ugui`), con la misma jerarquía.

### 10.3 Resolución y `CanvasScaler`

- `CanvasScaler` en modo **Scale With Screen Size**, referencia `1920×1080` (la mayor de las dos resoluciones objetivo), `Match` = 0.5 para un compromiso razonable entre ancho y alto al probar en `1280×720`.
- **Safe padding:** margen mínimo de 24 px (a la resolución de referencia) desde cualquier borde para el prompt y el panel.
- **Tamaño mínimo legible:** texto del panel no menor a 18 px (referencia 1080p) — graybox, sujeto a ajuste con arte real.
- **Orden de render respecto de la cámara pixel-perfect:** el Canvas Overlay se dibuja siempre por encima de cualquier cámara (no usa Camera espacio, no compite con `Pixel Perfect Camera`); no requiere `Sorting Layer` especial.
- **Referencias serializadas:** cada presentador (`InteractionPromptPresenter`, `ObservationPanelPresenter`) referencia directamente sus propios `GameObject`/`TMP_Text` dentro del mismo prefab — no se buscan por nombre ni por `Find`.

---

## 11. Prefabs y escenas

### 11.1 Nuevos prefabs

- `Assets/Prefabs/InteractionCanvas.prefab` (jerarquía de §10.1 + los dos presentadores como componentes en sus raíces respectivas).
- `Assets/Prefabs/ExaminableGraybox.prefab` (GameObject base: `Transform`, `BoxCollider2D` (o `CircleCollider2D`, a decidir en Fase 6 según el graybox visual) en modo **trigger**, layer `Interactables`, componente `ExaminableInteractable`). Las 3 instancias por escena solo cambian el `ExaminableData` asignado y su posición/prioridad.

### 11.2 Nuevos ScriptableObjects/assets

- `Assets/Data/Examinables/TerminalDiagnostico.asset`
- `Assets/Data/Examinables/PanelMantenimiento.asset`
- `Assets/Data/Examinables/NodoInactivo.asset`

### 11.3 Nuevos GameObjects por escena

- Una instancia de `InteractionCanvas.prefab`.
- Una instancia de `InteractionController` (puede vivir en el mismo GameObject que `SceneSystems`, junto a `SceneLoader`, para no multiplicar raíces — a confirmar en Fase 4/6 sin romper la convención existente de esa jerarquía).
- Una instancia de `InteractionDetector` (puede vivir en el propio Player prefab, ya que necesita `PlayerOrientation` y el origen; ver §18 — esto lo convertiría en parte del prefab compartido entre escenas, coherente con `PlayerMotor`/`PlayerOrientation`).
- Una instancia de `ExaminableGraybox.prefab` bajo el grupo `Environment` existente de cada escena (mismo grupo donde ya viven otros elementos de escena, según la jerarquía inspeccionada: `CameraBounds`, `Grid`, `SceneSystems`, `SpawnPoints`, `Transitions`, `Environment`).

### 11.4 Posiciones propuestas de los objetos graybox

No se fija ninguna coordenada concreta en esta SPEC: depende de la geometría real de cada escena (paredes, spawn points, triggers de transición), que no se modifica en esta fase. **Se deja como decisión de Fase 6**, con el único requisito duro de que ningún examinable se superponga con un `SpawnPoint`, un `AreaTransition` (trigger) ni un collider de `Environment`/`Collision` — a verificar visualmente en el Editor al colocarlos.

### 11.5 Aislamiento de escenas

Cada escena sigue abriéndose de forma independiente (M1 ya lo garantiza): al abrir cualquier escena de área directamente, `PlayerSpawner` usa el spawn `Default` (sin contexto pendiente) y el `InteractionController`/`InteractionDetector` de esa escena arrancan en `ExploringWithoutTarget` sin ningún estado previo que importar.

---

## 12. Asmdefs y dependencias

- Todos los scripts nuevos entran en `Synora.Runtime` (mismo asmdef que M0/M1; no se crea un asmdef nuevo — no hay necesidad real de separar interacción en su propio assembly todavía).
- `Synora.Runtime.asmdef` ya referencia `Unity.InputSystem`; se necesita añadir referencia a **TMPro** solo si se confirma su disponibilidad (§10.2) y se usa TMP — de lo contrario, `UnityEngine.UI` ya está implícito vía módulos base y no requiere referencia de asmdef adicional para `UnityEngine.UI.Text`.
- `Synora.Tests.EditMode.asmdef` ya referencia `Synora.Runtime`; no requiere cambios.
- Sin dependencias circulares: `Gameplay.Interaction` depende de `Systems` (por `PlayerControlGate`) y de `Data` (por `ExaminableData`), nunca al revés.
- Tipos puros y no dependientes de `MonoBehaviour`: `InteractionSelector` (estático) y `IInteractable` (interfaz).

---

## 13. Rendimiento

- Cero asignaciones recurrentes: buffer de `Collider2D[8]` preasignado en `InteractionDetector.Awake`; `InteractionSelector.SelectTarget` no crea listas ni closures.
- Sin LINQ en detección/selección (bucles `for` explícitos, igual que el código M1 existente).
- Sin `GetComponent` repetido por frame: `IInteractable` se cachea una vez por candidato detectado (el propio `Collider2D` puede cachear su `IInteractable` en un diccionario armado en preparación de escena, o el componente `ExaminableInteractable` puede exponerse directamente si el collider vive en el mismo GameObject — a decidir en Fase 3 según cómo queden los colliders del graybox).
- Sin strings construidos por frame: el texto del prompt (`"[E] Examinar"`) es una constante; `InteractionPromptPresenter.Show`/`ObservationPanelPresenter.Open` solo tocan el texto cuando cambia el target/estado, no en cada `Update`.
- Sin logs por frame: los `Debug.LogError`/`LogWarning` solo ocurren en validación (`Awake`/`OnValidate`) o en condiciones de error real, nunca en el bucle normal.
- La consulta física usa `OverlapBoxNonAlloc` con buffer reutilizable (§7).
- Distancia usa `sqrMagnitude` (§8), nunca `Vector2.Distance` en el camino caliente.
- Validación con Profiler: mismo procedimiento que en el informe de M1 (`Docs/Technical/SYNORA_M1_Test_Report.md` §6) — ventana CPU Usage / GC Alloc, ≥10 s de exploración continua con un objetivo detectado y sin él, confirmando 0 B/frame atribuibles a `InteractionDetector`/`InteractionController`.

---

## 14. Validaciones y errores de configuración

| Condición | Mecanismo |
|---|---|
| `ExaminableData` ausente en `ExaminableInteractable` | `Debug.LogError` en `Awake` |
| `InteractionId` vacío | `Debug.LogWarning` en `OnValidate` del SO |
| `InteractionId` duplicado en la misma escena | `Debug.LogError` al preparar la escena (recorrido único en `InteractionController.Awake` o script de validación de editor) |
| Collider2D ausente en el examinable | `[RequireComponent(typeof(Collider2D))]` en `ExaminableInteractable` |
| Layer incorrecta (no `Interactables`) | `Debug.LogWarning` en `OnValidate` si el `gameObject.layer` no coincide con el esperado |
| UI sin referencias (`panelRoot`/`label` nulos) | `Debug.LogError` en `Awake` de los presentadores |
| `PlayerControlGate` ausente en `PlayerMotor`/`PlayerOrientation` | `Debug.LogError` en `Awake` (mismo patrón que referencias faltantes actuales) |
| `Rigidbody2D` ausente | ya cubierto por `[RequireComponent(typeof(Rigidbody2D))]` existente en `PlayerMotor` |
| Input no cableado (`interactAction` nulo) | `Debug.LogError` en `OnEnable` de `InteractionInputReader` (mismo patrón que `PlayerInputReader`) |
| Dos `InteractionController` en la misma escena | `Debug.LogError` si se detecta más de una instancia activa (chequeo simple en `Awake`, sin buscar globalmente — cada escena solo debería tener una por diseño de prefab) |
| Más de un panel abierto | estructuralmente imposible: `ObservationPanelPresenter.Open` es idempotente y el `InteractionController` solo llama `Open` desde `ExploringWithTarget`, nunca desde `ObservationOpen` |

Ninguna de estas validaciones debe generar warnings en un proyecto correctamente configurado (todas son condicionales a un estado erróneo).

---

## 15. Pruebas automáticas

Todas en `Synora.Tests.EditMode`, mismo assembly que las 4 pruebas de M1 (coexisten sin conflicto: distinto namespace de clases bajo prueba).

| # | Prueba | Tipo | Necesita GameObjects temporales |
|---|---|---|---|
| 1 | Un candidato detrás del jugador no es válido | Pura (test de `InteractionSelector`/geometría de detección aislada) | No, si se testea la condición geométrica como función pura; si se testea `InteractionDetector` completo, sí (GameObject + Collider2D temporales, patrón `TransitionSystemTests`) |
| 2 | Un candidato fuera de rango no es válido | Igual que arriba | Igual que arriba |
| 3 | Mayor prioridad gana | Pura (`InteractionSelector.SelectTarget` con `IInteractable` falsos) | No |
| 4 | Con igual prioridad, gana el más cercano | Pura | No |
| 5 | Con igual prioridad y distancia, gana `InteractionId` ordinal menor | Pura | No |
| 6 | El objetivo actual se conserva mientras siga siendo válido | Pura | No |
| 7 | Al invalidarse el objetivo actual se elige un reemplazo | Pura | No |
| 8 | Abrir una observación bloquea control y limpia `linearVelocity` | Con GameObjects temporales (`Rigidbody2D`, `PlayerControlGate`, `PlayerMotor`) | Sí |
| 9 | Una pulsación posterior cierra y recupera el control | Con GameObjects temporales o test directo de `InteractionController` invocando su manejador de evento por reflexión (mismo patrón `BindingFlags.NonPublic` usado en `TransitionSystemTests`) | Sí |
| 10 | La misma pulsación no abre y cierra inmediatamente | Se cubre por construcción: cada pulsación dispara una única transición; test que invoca el handler dos veces seguidas dentro del mismo estado y confirma que la segunda no revierte la primera en el mismo evento | Sí (mínimos) |

- Para las pruebas 1-7, se crea una implementación de prueba `FakeInteractable : IInteractable` con campos mutables, evitando `GameObjects`/`Collider2D` reales cuando la prueba es sobre el **selector**, no sobre la **consulta física**.
- Limpieza: mismo patrón `[TearDown]` + `DestroyImmediate` sobre una lista de objetos temporales, ya usado en `TransitionSystemTests`.
- Logs esperados: cualquier `LogAssert.Expect` necesario para warnings esperados de `OnValidate` al crear componentes vía `AddComponent` sin cablear referencias, igual que en las pruebas M1 existentes.
- Resultado esperado en Test Runner: **10 nuevas pruebas, 0 failed, 0 skipped**, coexistiendo con las 4 de M1 → **14/14** en total tras M2.
- No se usan escenas reales ni assets reales (`ExaminableData` de producción) en las pruebas puras del selector; se crean instancias `ScriptableObject.CreateInstance<ExaminableData>()` o `FakeInteractable` según el caso.
- Sobre pruebas PlayMode: **no se considera necesaria ninguna** para M2 — toda la lógica crítica (selección, bloqueo, apertura/cierre) es testeable en EditMode con GameObjects temporales, como ya demuestra `TransitionSystemTests` para M1. Se deja registrado como decisión abierta en §20 por si la revisión manual revela un caso que solo se manifieste con el ciclo de vida completo de Play Mode.

---

## 16. Pruebas manuales

| Caso | CamaraPreservacion | CorredorTecnico | ClaroExterior |
|---|---|---|---|
| Detección Norte | ☐ | ☐ | ☐ |
| Detección Sur | ☐ | ☐ | ☐ |
| Detección Este | ☐ | ☐ | ☐ |
| Detección Oeste | ☐ | ☐ | ☐ |
| Dentro de rango (1.25u) | ☐ | ☐ | ☐ |
| Justo fuera de rango | ☐ | ☐ | ☐ |
| Objeto detrás del jugador (no detecta) | ☐ | ☐ | ☐ |
| Abrir panel y verificar texto correcto | ☐ | ☐ | ☐ |
| Cerrar panel con pulsación posterior | ☐ | ☐ | ☐ |
| La pulsación que abre no cierra | ☐ | ☐ | ☐ |
| Movimiento bloqueado durante observación | ☐ | ☐ | ☐ |
| `linearVelocity` = 0 al abrir | ☐ | ☐ | ☐ |
| Recuperación de control al cerrar (sin input pegado) | ☐ | ☐ | ☐ |
| Pulsaciones rápidas repetidas (sin parpadeo ni error) | ☐ | ☐ | ☐ |
| Estabilidad del objetivo con 2 candidatos equivalentes | ☐ | ☐ | ☐ |
| Escena abierta en aislamiento (Play directo) | ☐ | ☐ | ☐ |
| Cambio de escena sin UI/estado residual | ☐ | ☐ | ☐ |
| Resolución 1280×720 | ☐ | ☐ | ☐ |
| Resolución 1920×1080 | ☐ | ☐ | ☐ |
| Build Windows (fuera del Editor) | ☐ | ☐ | ☐ |
| `Player.log` limpio (0 errores/warnings inesperados) | ☐ | ☐ | ☐ |
| Regresión M1 (movimiento, cámara, colisiones, transición) | ☐ | ☐ | ☐ |

---

## 17. Plan de implementación

| Fase | Contenido | Archivos creados (previstos) | Archivos modificados | Riesgos | Pruebas | Commit sugerido |
|---|---|---|---|---|---|---|
| 1 | Input, datos y contratos | `IInteractable.cs`, `ExaminableData.cs`, `Controls.inputactions` (acción nueva) | ninguno de código | Bajo | ninguna aún (compilación) | `feat: add M2 interaction data and input contract` |
| 2 | `PlayerControlGate` y adaptación controlada de movimiento/orientación | `PlayerControlGate.cs` | `PlayerMotor.cs`, `PlayerOrientation.cs`, `Player.prefab` (nueva referencia serializada) | Medio (toca M1) | prueba 8 | `feat: add player control gate` |
| 3 | Detección y selector determinista | `InteractionDetector.cs`, `InteractionSelector.cs` | ninguno | Medio (Physics2D/layers) | pruebas 1-7 | `feat: add interaction detection and selection` |
| 4 | `InteractionController` y flujo de estados | `InteractionController.cs`, `InteractionInputReader.cs` | ninguno | Medio | pruebas 9-10 | `feat: add interaction controller and state flow` |
| 5 | UI graybox y prefab de escena | `InteractionPromptPresenter.cs`, `ObservationPanelPresenter.cs`, `InteractionCanvas.prefab` | ninguno | Bajo-medio (TMP a confirmar) | manuales de UI | `feat: add M2 UI graybox` |
| 6 | Tres interactuables y cableado de escenas | `ExaminableInteractable.cs`, `ExaminableGraybox.prefab`, 3 `.asset` de datos | `CamaraPreservacion.unity`, `CorredorTecnico.unity`, `ClaroExterior.unity` (instancias nuevas), `ProjectSettings/TagManager.asset` (layer `Interactables`=11) | Medio (primera vez que se tocan escenas/ProjectSettings) | manuales de detección por escena | `feat: wire M2 examinables into area scenes` |
| 7 | Pruebas automáticas y QA | pruebas EditMode restantes | ninguno | Bajo | 14/14 | `test: add M2 automated tests and QA report` |
| 8 | Build, recorrido final, informe, commit y tag | `SYNORA_M2_Test_Report.md` | ninguno de código | Bajo | matriz manual completa | `chore: finalize M2 validation and build` + tag `m2-complete` |

No se avanza de fase sin aprobación del Director, tal como exige el brief.

---

## 18. Migración y regresión

### Scripts de M1 modificados

- **`PlayerMotor.cs`:** añade campo serializado `PlayerControlGate gate`; en `FixedUpdate`, si `gate.IsBlocked`, calcula velocidad con `Vector2.zero` en vez de `inputReader.MoveInput` (o, equivalentemente, salta el cálculo y fuerza `body.linearVelocity = Vector2.zero`). Cambio mínimo, aditivo, no toca `CalculateVelocity` (que sigue siendo la función pura ya testeada).
- **`PlayerOrientation.cs`:** añade el mismo campo `gate`; en `Update`, si `gate.IsBlocked`, no llama a `Resolve` (mantiene `facing`).
- **`Player.prefab`:** gana un componente `PlayerControlGate` y las dos referencias serializadas nuevas en `PlayerMotor`/`PlayerOrientation` (y, si se decide en Fase 6, el `InteractionDetector` también vive aquí).

### Scripts NO modificados

- `PlayerInputReader.cs` — sin cambios (M2 usa un lector propio).
- `Controls.inputactions` — solo **añade** la acción `Interact`; no toca `Move` ni sus bindings.
- `SceneLoader.cs`, `AreaTransition.cs`, `SceneTransitionContext.cs`, `PlayerSpawner.cs`, `CameraFollow.cs`, `CameraBounds2D.cs`, `GameBootstrap.cs` — sin cambios.
- Las 4 escenas — solo **ganan** GameObjects nuevos (Canvas de interacción, examinable, y posiblemente `InteractionController` en `SceneSystems`); no se elimina ni reestructura nada existente.

### Por qué no se rompe M1

- **Movimiento/diagonales:** `PlayerMotor.CalculateVelocity` no cambia; el gate solo decide *qué input* se le pasa, no cómo se calcula la velocidad.
- **Colisiones:** el examinable usa un collider en modo **trigger**, en una layer nueva (`Interactables`=11) que no colisiona físicamente con `Player` a menos que se configure explícitamente en la Collision Matrix — configuración a revisar en Fase 6, con el objetivo explícito de que `Interactables` no bloquee físicamente al jugador (solo se detecta por `Physics2D.OverlapBox`, no por colisión de `Rigidbody2D`).
- **Cámara:** `CameraFollow` no se toca.
- **Spawn:** `PlayerSpawner` no se toca; los examinables no son spawn points.
- **Transiciones:** `SceneLoader`/`AreaTransition`/`SceneTransitionContext` no se tocan.
- **Contexto de escena:** M2 no persiste nada entre escenas, igual que M1.
- **Build:** ningún paquete nuevo declarado (TMP, si se usa, ya viene con `com.unity.ugui`); el Build Profile de Windows no cambia.
- **Las 4 pruebas actuales:** ninguna de ellas testea código modificado de forma incompatible — `PlayerMotorTests` sigue probando la función estática pura `CalculateVelocity`, que no cambia de firma ni de comportamiento.

---

## 19. Definition of Done

M2 está terminado cuando:

- Los tres objetos graybox son examinables correctamente en sus tres escenas, con el texto correcto cada uno.
- La selección de objetivo es estable y determinista (pruebas 3-7 en verde, más verificación manual con candidatos equivalentes).
- El control queda bloqueado sin movimiento residual al abrir observación, y se recupera limpiamente al cerrar (pruebas 8-9 en verde + matriz manual).
- La UI (prompt + panel) funciona en graybox, sin arte definitivo, en ambas resoluciones objetivo.
- Las cuatro escenas siguen funcionando en aislamiento.
- Las transiciones de M1 permanecen intactas (matriz de regresión en verde).
- Las 4 pruebas de M1 + las 10 nuevas de M2 pasan (14/14).
- Consola con cero errores/warnings inesperados (los únicos warnings aceptables son los ya documentados como ruido de test, igual que en M1).
- Cero asignaciones recurrentes propias de M2 (verificado con Profiler, igual metodología que M1 §6 del informe).
- Build Windows funcional, fuera del Editor.
- Recorrido manual completo de la matriz de §16 aprobado por el Director.
- `Player.log` limpio tras el recorrido.
- Documentación final (este documento + informe de pruebas de M2) al día.
- Tag `m2-complete` creado **únicamente** después de la aprobación final del Director — no antes.

---

## 20. Decisiones abiertas

| Decisión | Alternativas | Recomendación de Claude | Impacto | Requiere aprobación del Director |
|---|---|---|---|---|
| Ancho exacto de la zona frontal (`frontalHalfWidth`) | 0.3 / 0.4 / 0.5 unidades | 0.4 (propuesto en §7.1) | Sensación de detección (demasiado angosto se siente injusto; demasiado ancho detecta objetos casi laterales) | Sí |
| ScriptableObject vs. datos inline | SO + componente (propuesto) / todo inline en el componente | SO + componente (§4) | Reutilización y localización futura vs. simplicidad inmediata | Sí |
| Diseño exacto del `PlayerControlGate` | Bitmask de razones (propuesto) / booleano simple | Bitmask (§6) | Extensibilidad para bloqueos futuros | Sí |
| Forma exacta de consulta Physics2D | `OverlapBox` (propuesto) / `OverlapCircle` + filtro angular | `OverlapBox` (§7) | Precisión y simplicidad de la zona frontal | Sí |
| Capacidad inicial del buffer de candidatos | 4 / 8 (propuesto) / 16 | 8 (§7.1) | Margen de seguridad vs. memoria reservada (mínima en cualquier caso) | No (detalle técnico de bajo impacto) |
| Frecuencia de detección | `FixedUpdate` (propuesto) / `Update` | `FixedUpdate` (§7.1) | Consistencia con el paso físico del jugador | Sí |
| TextMeshPro o alternativa ya instalada | TMP (probable, a confirmar) / `UnityEngine.UI.Text` | TMP si se confirma disponible sin instalar paquete (§10.2) | Calidad tipográfica vs. certeza de "sin paquetes nuevos" | Sí |
| Necesidad real de pruebas PlayMode | Ninguna (propuesto) / añadir 1-2 si el recorrido manual revela un caso no cubierto en EditMode | Ninguna por ahora (§15) | Cobertura vs. tiempo de CI | No, salvo que Fase 7 revele un caso real |
| Posición exacta de los objetos graybox en cada escena | Depende de la geometría real de cada escena | A definir visualmente en Fase 6, evitando spawn points/transiciones/colisión | Sensación de exploración, evitar softlocks visuales | Sí |
| Cantidad final de pruebas automatizadas | 10 propuestas (§15) / menos si se fusionan casos | 10, con posibilidad de fusionar 1-2 si en Fase 7 resultan redundantes en la práctica | Cobertura vs. mantenimiento de tests | No (ajuste técnico menor, se informará si cambia) |

Ninguna de las preferencias técnicas anteriores se trata como aprobada; todas quedan explícitamente pendientes de la decisión del Director antes de comenzar la Fase 1.

---

**FIN DE LA SPEC TÉCNICA M2 v0.1. No se ha escrito código. No se ha creado ningún commit.**

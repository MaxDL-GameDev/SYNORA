# SYNORA — Especificación Técnica M2: Interacción y Observación Básica

| Campo | Valor |
|---|---|
| Documento | `SYNORA_SPEC_M2_Interaccion_v0.2.md` |
| Versión | **0.2** (aplica las 12 correcciones de revisión sobre v0.1; v0.1 se conserva sin cambios) |
| Documento padre | `SYNORA_GDD_M2_Interaccion_v0.1.md` (Docs/Design) — **aprobado sin cambios funcionales** |
| Editor verificado | Unity 6000.5.3f1 |
| URP 2D | 17.6.0 |
| Input System | 1.19.0 |
| HEAD verificado | `13b97ac` (tag `m1-complete`) |
| Fecha real de redacción | 2026-07-17 |
| Estado | **Propuesta para aprobación del Director — no se ha escrito código** |

> **Nota de acceso.** No dispongo de MCP for Unity conectado a un Editor en vivo en este entorno: soy un agente en la nube sin sesión de Unity abierta. Donde la revisión pidió "inspeccionar mediante MCP" (§11), inspeccioné en su lugar los archivos `.unity`/`.prefab`/`ProjectSettings` reales del repositorio directamente (mismo repositorio clonado y verificado en v0.1), lo cual da datos igual de reales (coordenadas, tamaños, IDs) pero no permite renderizar visualmente la escena. Lo dejo explícito para no presentar como "verificado en Editor" algo que verifiqué por archivo.

---

## Registro de cambios v0.1 → v0.2

| # | Corrección aplicada | Seción afectada |
|---|---|---|
| 1 | Contrato de interacción rediseñado: `InteractionController` es el único que abre/cierra el panel y el prompt; `ExaminableInteractable` ya no referencia ningún presenter | §2, §3 |
| 2 | `Priority` → `int`; `IsEnabled` → `CanInteract`; `Anchor: Transform` → `InteractionPosition: Vector2`; IDs globales congelados | §3, §4 |
| 3 | Geometría frontal exacta + clase pura `InteractionGeometry` | §7 |
| 4 | API exacta de `Physics2D.OverlapBox` congelada, incluida regla de log si el buffer se llena | §7 |
| 5 | `InteractionDetector` con lista serializada explícita de examinables, validación en `Awake`, cache sin `GetComponent`/`Find` en `FixedUpdate` | §7, §14 |
| 6 | Prefab `InteractionSceneRoot` sustituye a `InteractionCanvas` aislado | §11 |
| 7 | Aclaraciones sobre input mantenido/soltado y limpieza en `OnDisable` del controller | §6 |
| 8 | Regla de una-transición-por-emisión explicitada para ambos sentidos | §9 |
| 9 | 10 pruebas con nombres exactos | §15 |
| 10 | TMP investigado contra el repositorio real; decisión congelada (ver abajo) | §10 |
| 11 | Coordenadas reales de spawns/transiciones/bounds extraídas de las escenas; posiciones propuestas para los 3 graybox | §16 (antes en el cuerpo de prefabs/escenas) |
| 12 | Plan de fases corregido: nuevos archivos añadidos, `Controls.inputactions` movido a "modificado", fase documental previa | §17 |

---

## 1. Objetivo y alcance

Sin cambios respecto a v0.1 salvo lo que se deriva de los contratos nuevos:

### 1.1 Objetivo técnico

Detectar un único interactuable frontal dentro de 1.25 unidades, mostrar un prompt, y abrir/cerrar un panel de observación mediante `Interact`, bloqueando movimiento y orientación mientras el panel está abierto — todo ello orquestado **exclusivamente** por `InteractionController`.

### 1.2 Dependencias respecto de M1

Igual que v0.1: reutiliza `PlayerOrientation.Facing`, `PlayerInputReader` (sin modificarlo — se añade `InteractionInputReader` aparte), y el `Player` prefab. No toca `SceneLoader`/`AreaTransition`/`SceneTransitionContext`.

### 1.3 Elementos incluidos / 1.4 Fuera de alcance / 1.5 DoD

Sin cambios de alcance respecto a v0.1 (ver GDD §5 y SPEC v0.1 §1.4). La Definition of Done técnica se mantiene y se detalla en §19.

---

## 2. Arquitectura propuesta (corregida)

Se mantienen ocho piezas separadas, pero el **flujo de control cambia**: ya no existen dos rutas para abrir el panel. Solo `InteractionController` decide estado, abre/cierra panel, muestra/oculta prompt, y bloquea/desbloquea control.

```
InteractionInputReader   → lee Interact, expone evento de pulsación
InteractionDetector      → Physics2D broad-phase + InteractionGeometry (filtro exacto) → candidatos
InteractionGeometry       → función pura: ¿está el candidato en la zona frontal?
InteractionSelector      → función estática pura: candidatos → objetivo único
InteractionController    → ÚNICO orquestador: estados, panel, prompt, gate (implementa IInteractionReceiver)
PlayerControlGate        → bitmask de bloqueo, consultado por PlayerMotor/PlayerOrientation
InteractionPromptPresenter    → muestra/oculta el prompt (solo lo que el controller le pida)
ObservationPanelPresenter     → muestra/oculta el panel (solo lo que el controller le pida)
IInteractable + IInteractionReceiver + ExaminableInteractable + ExaminableData → contrato y datos
```

`ExaminableInteractable` **no** conoce a `ObservationPanelPresenter` ni a ningún presenter: solo conoce el contrato `IInteractionReceiver` que recibe como parámetro en `Execute`. Esto elimina la contradicción señalada: antes existía una ruta "el interactuable abre su propio panel" y otra "el controller cambia de estado", que podían desincronizarse. Ahora solo hay una ruta:

```
InteractionController detecta InteractPressed en ExploringWithTarget
  → target.Execute(this)                     // this = InteractionController, como IInteractionReceiver
    → dentro de Execute: receiver.ShowObservation(data)
  → InteractionController.ShowObservation(data):
        panelPresenter.Open(data)
        promptPresenter.Hide()
        gate.Block(Observation)
        state = ObservationOpen
```

Prohibiciones sin cambios: sin `Singleton`, sin Service Locator estático, sin eventos estáticos, sin `DontDestroyOnLoad`, sin `GameObject.Find`/`FindObjectOfType`/variantes, sin referencias globales en runtime.

---

## 3. Contratos y responsabilidades (corregido)

### 3.1 `IInteractable` (interfaz) — contrato final

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/IInteractable.cs`

```csharp
public interface IInteractable
{
    string InteractionId { get; }
    int Priority { get; }
    bool CanInteract { get; }
    Vector2 InteractionPosition { get; }
    string PromptText { get; }
    void Execute(IInteractionReceiver receiver);
}
```

- `Priority` es **`int`** (antes `float`): las prioridades de M2 son categóricas (0 = normal; no hay necesidad de fracciones), y `int` hace el desempate y las pruebas más simples y exactas, sin comparaciones de punto flotante.
- `CanInteract` sustituye a `IsEnabled` para no chocar semánticamente con `Component.enabled`/`GameObject.activeInHierarchy` de Unity. Ver regla exacta en §3.2.
- `InteractionPosition: Vector2` sustituye a `Anchor: Transform`. Esto permite que una prueba cree un `FakeInteractable` **sin ningún GameObject**, con `InteractionPosition` como una propiedad de solo lectura respaldada por un campo simple — requisito explícito de la corrección 2.
- `PromptText`: en M2 siempre `"Examinar"`; vive en el contrato para que `InteractionPromptPresenter` no necesite conocer tipos concretos de interactuable, y para no cerrar la puerta a un segundo tipo de prompt en M3 sin volver a tocar el contrato.

### 3.2 `CanInteract` — regla exacta

`ExaminableInteractable.CanInteract` combina tres condiciones (todas deben cumplirse):

```csharp
public bool CanInteract =>
    isActiveAndEnabled &&   // componente activo (Unity)
    interactionEnabled &&   // estado de interacción habilitado (campo propio, ver 3.3)
    data != null;           // data válida
```

- `isActiveAndEnabled`: cubre GameObject inactivo o componente deshabilitado sin necesidad de un flag propio para eso.
- `interactionEnabled`: campo serializado (`bool`, default `true`) para casos futuros donde un examinable deba desactivarse lógicamente sin desactivar el GameObject (fuera de alcance usarlo en M2 más allá de exponerlo, pero se declara ahora porque el GDD ya prevé "Enabled state" en el modelo de datos).
- `data != null`: evita `NullReferenceException` si el asset no fue asignado.

### 3.3 `IInteractionReceiver` (interfaz nueva)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/IInteractionReceiver.cs`

```csharp
public interface IInteractionReceiver
{
    void ShowObservation(ExaminableData data);
}
```

- Responsabilidad única: desacoplar `ExaminableInteractable` de `InteractionController`. El interactuable no sabe qué hace el receptor con los datos; solo se los entrega.
- `InteractionController` es la única clase de producción que implementa esta interfaz en M2.

### 3.4 `ExaminableInteractable` (componente) — corregido

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/ExaminableInteractable.cs`
- Responsabilidad única: adaptar un `ExaminableData` + su Collider2D en escena al contrato `IInteractable`. **No referencia ningún presenter.**
- Campos serializados: `data` (`ExaminableData`), `priority` (`int`, default 0), `interactionEnabled` (`bool`, default `true`).
- `InteractionPosition => (Vector2)transform.position;` (no requiere un Transform "ancla" separado; el propio Transform del GameObject basta, ya que el collider está en el mismo objeto).
- `PromptText => "Examinar";` (constante en M2; ver §3.1).
- `Execute(IInteractionReceiver receiver) => receiver.ShowObservation(data);` — una sola línea, ninguna lógica de estado.
- `[RequireComponent(typeof(Collider2D))]`.
- Ciclo de vida: `Awake` valida `data != null` (log de error si no).

### 3.5 IDs exactos (congelados)

- `camara_preservacion.terminal_diagnostico`
- `corredor_tecnico.panel_mantenimiento`
- `claro_exterior.nodo_inactivo`

Convención: `snake_case`, `escena.objeto`, globalmente únicos y estables (no dependen del nombre del asset ni de la escena en la que se instancian, aunque por convención el prefijo coincida con la escena de origen del contenido).

### 3.6 `InteractionInputReader` — sin cambios respecto a v0.1

Ver v0.1 §3.3. Namespace/carpeta iguales.

### 3.7 `InteractionDetector` — corregido (ver detalle completo en §7 y §14)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionDetector.cs`
- Vive en `InteractionSceneRoot`, **no** en `Player.prefab` (corrección 5/6).
- Campos serializados: `playerOrientation` (`PlayerOrientation`), `originPoint` (`Transform`, referencia al Player de la escena), `interactableLayer` (`LayerMask`), `sceneExaminables` (`List<ExaminableInteractable>`, cableada explícitamente por escena — **no se descubre en runtime**).
- Ver §7 para la geometría y §14 para las validaciones de `Awake`.

### 3.8 `InteractionGeometry` (clase nueva, estática, pura)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionGeometry.cs`
- Responsabilidad única: decidir si un punto candidato cae dentro de la zona frontal exacta. Ver fórmula y pseudocódigo en §7.3.

### 3.9 `InteractionSelector` — mismo rol que v0.1, firma ajustada a los tipos nuevos

- Firma: `static IInteractable SelectTarget(IReadOnlyList<IInteractable> candidates, IInteractable currentTarget, Vector2 playerPosition)`.
- Usa `Priority` (`int`) y `InteractionPosition` (`Vector2`) del contrato corregido. Lógica de orden sin cambios (§8).

### 3.10 `InteractionController` — corregido

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionController.cs`
- **Implementa `IInteractionReceiver`.**
- Responsabilidad única: máquina de estados + único punto que llama `Open`/`Close` del panel, `Show`/`Hide` del prompt, y `Block`/`Unblock` del gate.
- Dependencias serializadas: `detector` (`InteractionDetector`), `inputReader` (`InteractionInputReader`), `gate` (`PlayerControlGate`), `promptPresenter` (`InteractionPromptPresenter`), `panelPresenter` (`ObservationPanelPresenter`).
- `ShowObservation(ExaminableData data)` (implementación de `IInteractionReceiver`): abre el panel, oculta el prompt, bloquea el gate, cambia a `ObservationOpen`. Es el **único** lugar del código donde ocurre esta secuencia.
- `OnDisable` (ver corrección 7): cierra el panel, oculta el prompt, `Unblock(Observation)`, limpia el target interno, y restaura `state = ExploringWithoutTarget`. Nunca usa `ClearAll` ni retira razones de bloqueo que no sean las suyas.
- No conoce `SceneLoader`, `AreaTransition`, `PlayerMotor` ni `PlayerInputReader`.

### 3.11 `InteractionPromptPresenter` / `ObservationPanelPresenter` — sin cambios de responsabilidad

Idénticos a v0.1 (§3.7/§3.8 de v0.1): solo muestran/ocultan y fijan texto cuando el `InteractionController` se lo pide. No conocen Physics2D, Input System, ni el gate.

### 3.12 `PlayerControlGate` — sin cambios de diseño (ver §6)

### 3.13 `ExaminableData` (ScriptableObject) — ver §4

### Qué clases pueden comunicarse (actualizado)

- `InteractionController` ↔ `detector`, `inputReader`, `gate`, `promptPresenter`, `panelPresenter`, y el `IInteractable` objetivo (a través de `Execute(this)`).
- `ExaminableInteractable.Execute` ↔ el `IInteractionReceiver` recibido como parámetro (no una referencia guardada; se le pasa en cada llamada).
- `PlayerMotor`/`PlayerOrientation` ↔ `PlayerControlGate` (solo lectura de `IsBlocked`).

### Qué clases no deben conocerse (actualizado)

- `ExaminableInteractable` no conoce `ObservationPanelPresenter`, `InteractionPromptPresenter`, `InteractionController` ni `PlayerControlGate`. Solo conoce `IInteractionReceiver` y `ExaminableData`.
- `InteractionDetector`/`InteractionGeometry`/`InteractionSelector` no conocen la UI.
- `PlayerControlGate` no conoce `Interaction`.

---

## 4. Modelo de datos (ajustado)

Sin cambios de decisión (SO + componente, ver v0.1 §4), con dos ajustes de tipo:

```csharp
[CreateAssetMenu(fileName = "ExaminableData", menuName = "SYNORA/Examinable Data")]
public sealed class ExaminableData : ScriptableObject
{
    [SerializeField] private string interactionId;
    [SerializeField] private string displayName;
    [SerializeField] private string observationTitle;
    [SerializeField, TextArea] private string observationBody;
}
```

- `Priority` y `CanInteract` (antes `Enabled`) **no** viven en el SO — son de instancia de escena, en `ExaminableInteractable` (`priority: int`, `interactionEnabled: bool`), igual justificación que en v0.1.
- Validaciones `OnValidate`, reglas de ID vacío/recorte y ubicación de assets (`Assets/Data/Examinables/`): sin cambios respecto a v0.1 §4.2/§4.4.
- **Detección de IDs duplicados** (corrección 5): ya no se plantea como un recorrido genérico de escena; es responsabilidad explícita de `InteractionDetector.Awake`, que valida `sceneExaminables` (ver §14).
- Nombres de archivo actualizados a los IDs congelados de §3.5: `Assets/Data/Examinables/camara_preservacion.terminal_diagnostico.asset`, `Assets/Data/Examinables/corredor_tecnico.panel_mantenimiento.asset`, `Assets/Data/Examinables/claro_exterior.nodo_inactivo.asset`.

---

## 5. Input System

Sin cambios respecto a v0.1 (§5 completo): acción `Gameplay/Interact` tipo Button, bindings `Keyboard/e` y `Gamepad/buttonSouth`, fase `performed`, lector separado (`InteractionInputReader`), sin acoplar la UI al `InputActionAsset`.

**Corrección de inventario (12):** `Controls.inputactions` es un **archivo existente que se modifica** (se le añade una acción dentro del mismo map `Gameplay`), no un archivo nuevo. Se corrige en la tabla de fases (§17).

---

## 6. Bloqueo de control (aclarado)

Diseño sin cambios respecto a v0.1 (bitmask `ControlBlockReason { None, Observation }`, consumido por `PlayerMotor`/`PlayerOrientation`). Aclaraciones de la corrección 7:

- **Input mantenido no es "obsoleto".** Si el jugador mantiene una tecla de movimiento físicamente presionada durante toda la observación, `PlayerInputReader.MoveInput` sigue reflejando ese valor real (el Input System no dejó de generar el evento `performed` correspondiente). Al desbloquear, `PlayerMotor` vuelve a leer ese valor y el movimiento se reanuda de inmediato — esto es comportamiento correcto, no un input "pegado".
- **Si la tecla se soltó durante la observación**, `PlayerInputReader` ya contiene `Vector2.zero` (su callback `canceled` corrió igual, con independencia del gate, porque `PlayerInputReader` no se deshabilita nunca por el bloqueo). Al desbloquear no hay nada que reanudar.
- **`InteractionController.OnDisable`** (evento de vida del propio componente, p. ej. al desactivar la escena o el objeto): cierra el panel, oculta el prompt, `Unblock(Observation)`, limpia el target interno y vuelve a `ExploringWithoutTarget`. Nunca llama a un `ClearAll` que quitara razones de bloqueo de otros sistemas — solo retira su propia razón (`Observation`).

---

## 7. Detección frontal (reescrita — congelada)

### 7.1 Valores congelados

```
detectionRange   = 1.25
frontalHalfWidth = 0.4     // ancho total = 0.8
```

### 7.2 Broad phase — API exacta congelada

```csharp
private static readonly Collider2D[] overlapBuffer = new Collider2D[8]; // preasignado en Awake, no por frame

int count = Physics2D.OverlapBox(
    point: origin + facing * (detectionRange / 2f),
    size: (facing.x != 0)
        ? new Vector2(detectionRange, frontalHalfWidth * 2f)   // Este/Oeste
        : new Vector2(frontalHalfWidth * 2f, detectionRange),  // Norte/Sur
    angle: 0f,
    contactFilter: interactableFilter,   // ContactFilter2D con LayerMask = Interactables, useTriggers = true
    results: overlapBuffer);
```

- `ContactFilter2D.useLayerMask = true`, máscara = layer `Interactables` (índice **11**, primer índice libre confirmado tras `Player`=8/`Environment`=9/`Transitions`=10 en `ProjectSettings/TagManager.asset`; el cambio de capa en sí se aplica en Fase 6, no ahora).
- `useTriggers = true` (los examinables usan Collider2D en modo trigger).
- **Sin `OverlapBoxAll`** (crea un array nuevo cada llamada — prohibido).
- **Sin arrays creados por frame**: `overlapBuffer` es un campo de instancia, preasignado una vez en `Awake`.
- **Si el buffer se llena** (`count == overlapBuffer.Length`): se registra **como máximo un** `Debug.LogWarning` por sesión/instancia de `InteractionDetector` (un flag `bool hasLoggedBufferFull` evita repetirlo), nunca uno por frame.

### 7.3 Filtro exacto — `InteractionGeometry` (clase pura nueva)

El broad phase de Physics2D es una primera criba barata; el filtro **exacto** de "zona frontal" es esta función pura, aplicada sobre `InteractionPosition` de cada candidato devuelto por el overlap:

```csharp
namespace Synora.Gameplay.Interaction
{
    public static class InteractionGeometry
    {
        public static bool IsInsideFrontZone(
            Vector2 origin,
            Vector2 facing,
            Vector2 candidatePosition,
            float range,
            float halfWidth)
        {
            Vector2 delta = candidatePosition - origin;
            float forwardDistance = Vector2.Dot(delta, facing);
            Vector2 perpendicular = new Vector2(-facing.y, facing.x);
            float lateralDistance = Mathf.Abs(Vector2.Dot(delta, perpendicular));

            return forwardDistance > 0f
                && forwardDistance <= range
                && lateralDistance <= halfWidth;
        }
    }
}
```

- `facing` es siempre uno de los 4 vectores cardinales unitarios (`PlayerOrientation.Facing` convertido a `Vector2`), así que `perpendicular` es también unitario y ortogonal — no requiere normalización adicional.
- Sin asignaciones (todo son `struct Vector2`/`float`).
- Testeable sin ningún `GameObject` (ver pruebas 1-2 en §15).

### 7.4 Orden de aplicación

1. `Physics2D.OverlapBox` (broad phase, layer + trigger) → hasta 8 `Collider2D`.
2. Para cada `Collider2D`, resolver su `IInteractable` vía el lookup construido en `Awake` (§14) — **sin** `GetComponent` en este paso.
3. Deduplicar por `IInteractable` (un examinable con varios colliders cuenta una sola vez).
4. Aplicar `InteractionGeometry.IsInsideFrontZone` sobre `InteractionPosition` de cada `IInteractable` único.
5. Filtrar además por `CanInteract`.
6. El resultado (0..8, típicamente 0-1) es `Candidates`, expuesto como `IReadOnlyList<IInteractable>`.

### 7.5 Punto usado para distancia

`IInteractable.InteractionPosition` (ya no depende de un `Transform` "ancla" separado — ver corrección 2).

### 7.6 Gizmos de Editor

`OnDrawGizmosSelected` en `InteractionDetector` dibuja la caja de detección actual (tamaño/posición según la fórmula de §7.2), mismo estilo que `CameraBounds2D`/`SpawnPoint`.

### 7.7 Frecuencia

`FixedUpdate`, igual cadencia que `PlayerMotor` (sin cambios respecto a v0.1; justificación idéntica: la posición física del jugador solo cambia de forma determinista en el paso de física).

---

## 8. Selección determinista

Sin cambios de algoritmo respecto a v0.1 (orden: `Priority` descendente → distancia al cuadrado ascendente → `InteractionId` ordinal ascendente; estabilidad del objetivo; sin `GetInstanceID`; sin `List.Sort`/LINQ). Ajuste de tipos: `Priority` ahora es `int` (comparación exacta, sin tolerancia de punto flotante) y la distancia se calcula sobre `InteractionPosition` (`Vector2`), no sobre `Transform.position`.

Pseudocódigo (idéntico en estructura a v0.1 §8.3, con los tipos corregidos):

```
static IInteractable SelectTarget(candidates, currentTarget, playerPosition)
{
    if currentTarget != null and candidates contains currentTarget by reference:
        return currentTarget

    best = null
    bestSqrDist = +infinity

    for each c in candidates:
        if not c.CanInteract: continue

        if best == null:
            best = c; bestSqrDist = SqrDist(c.InteractionPosition, playerPosition); continue

        if c.Priority > best.Priority:
            best = c; bestSqrDist = SqrDist(c.InteractionPosition, playerPosition); continue
        if c.Priority < best.Priority:
            continue

        d = SqrDist(c.InteractionPosition, playerPosition)
        if d < bestSqrDist:
            best = c; bestSqrDist = d; continue
        if d > bestSqrDist:
            continue

        if string.CompareOrdinal(c.InteractionId, best.InteractionId) < 0:
            best = c; bestSqrDist = d

    return best
}
```

---

## 9. Estados de interacción (aclarado)

Misma tabla de v0.1 §9 (`ExploringWithoutTarget`, `ExploringWithTarget`, `ObservationOpen`), con la regla de transición explicitada por la corrección 8:

- **Una emisión de `InteractPressed` produce como máximo una transición de estado.** El manejador de evento en `InteractionController` no se invoca dos veces por una sola pulsación (el Input System entrega `performed` una vez por flanco, y el controller procesa el evento sincrónicamente antes de que pueda llegar otro).
- **Desde `ExploringWithTarget`:** una emisión → `target.Execute(this)` → `ShowObservation` → `ObservationOpen`. Esa misma emisión no puede, en la misma pasada, además cerrar el panel — el cierre solo ocurre en una emisión **posterior**, procesada en un evento distinto.
- **Desde `ObservationOpen`:** una emisión posterior → `panelPresenter.Close()`, `gate.Unblock(Observation)`, reevaluación inmediata contra `detector.Candidates` para decidir si el nuevo estado es `ExploringWithTarget` o `ExploringWithoutTarget`.
- El target puede quedar **congelado** durante `ObservationOpen` (no se reevalúa mientras el panel está abierto), pero la UI ya copió los datos (`ExaminableData`) al abrir — el panel no depende de que el GameObject del target siga activo después de eso.
- Cambio de escena y desactivación del objetivo: sin cambios respecto a v0.1.

---

## 10. UI (TMP verificado — decisión congelada)

### 10.1 Verificación realizada

Sobre el repositorio real (no sobre un Editor en vivo, ver nota de acceso al inicio):

- `Packages/manifest.json` **no** declara `com.unity.textmeshpro` como dependencia.
- `Packages/packages-lock.json` muestra `com.unity.ugui` en versión `2.5.0`, `source: builtin` (módulo integrado del Editor, no un paquete que se instale aparte).
- Búsqueda exhaustiva en el repositorio (`grep -r "TMPro\|TextMeshPro\|TMP_"`) no encontró **ningún** uso, referencia de asmdef, ni carpeta `Assets/TextMesh Pro/` (la carpeta que Unity crea al importar los "TMP Essential Resources" la primera vez que se usa TMP). Esto indica que, **hasta el HEAD verificado**, TMP no ha sido usado ni sus recursos esenciales importados en este proyecto.
- No puedo confirmar desde archivos de texto si `com.unity.ugui` 2.5.0 en Unity 6000.5 permite crear `TMP_Text` sin un paso adicional de importación de recursos esenciales (ese paso, si es necesario, ocurre dentro del Editor y no deja rastro en el repositorio hasta que se ejecuta). No tengo un Editor en vivo ni MCP for Unity conectado para comprobarlo de forma definitiva ahora mismo.

### 10.2 Decisión congelada

**Opción B: `UnityEngine.UI.Text` (uGUI clásico).**

Justificación: es la única opción de la que tengo **certeza total** de disponibilidad sin instalar ni importar nada — `com.unity.ugui` ya está presente como módulo builtin y el proyecto ya usa uGUI (Canvas/CanvasScaler son parte del mismo paquete). Adoptar TMP ahora mismo sería una afirmación no verificada, algo que la propia corrección 10 pide evitar explícitamente. Si en Fase 5, al abrir el Editor real, se confirma que TMP está disponible sin fricción (import de recursos esenciales con un clic, sin paquete nuevo), se puede sustituir `UnityEngine.UI.Text` por `TMP_Text` como un cambio de implementación menor y localizado a los tres componentes de UI — no afecta a ningún contrato de `Gameplay.Interaction` (los presentadores exponen `Show(string)`/`Open(ExaminableData)`, no un tipo de texto concreto).

### 10.3 Resto de decisiones de UI — sin cambios respecto a v0.1

- Screen Space – Overlay.
- `CanvasScaler`: Scale With Screen Size, Reference Resolution **1920×1080**, Match = **0.5**.
- Safe padding: **24 px** (referencia 1080p) desde cualquier borde.
- Sin animaciones, sin pausas, sin arte definitivo, sin paquetes nuevos.
- Instancia propia por escena (no `DontDestroyOnLoad`), panel y prompt inactivos al cargar.
- Jerarquía (ahora dentro de `InteractionSceneRoot`, ver §11):

```
InteractionSceneRoot
├── InteractionController
├── InteractionInputReader
├── InteractionDetector
└── InteractionCanvas (Canvas + CanvasScaler)
    ├── InteractionPrompt (inactivo)
    │   └── Label (Text)
    └── ObservationPanel (inactivo)
        ├── Title (Text)
        ├── Body (Text)
        └── CloseHint (Text, contenido estático "[E] Cerrar")
```

---

## 11. Prefabs y escenas (corregido)

### 11.1 Prefab raíz — `InteractionSceneRoot` (sustituye a `InteractionCanvas` aislado)

```
Assets/Prefabs/InteractionSceneRoot.prefab
InteractionSceneRoot
├── InteractionController   (implementa IInteractionReceiver)
├── InteractionInputReader
├── InteractionDetector      (sceneExaminables se cablea por escena, NO en el prefab)
└── InteractionCanvas
    ├── InteractionPrompt
    │   └── Label
    └── ObservationPanel
        ├── Title
        ├── Body
        └── CloseHint
```

Cada escena de área contiene **una** instancia de este prefab. `InteractionDetector.sceneExaminables` es una lista serializada que **cada escena** completa manualmente con sus propios `ExaminableInteractable` (no se auto-descubre en runtime).

### 11.2 Unicidad — sin búsquedas globales

No se valida "solo un `InteractionController` por escena" con `FindObjectsOfType` ni estado estático (prohibido). Se valida por:

- Jerarquía del prefab (una sola raíz `InteractionSceneRoot` por escena, por construcción).
- Inspección manual de la escena antes de Fase 6.
- Checklist manual de Fase 6 (§16 pasa a incluir esta verificación).
- Auditoría final de Fase 8.

### 11.3 Nuevo prefab de examinable

`Assets/Prefabs/ExaminableGraybox.prefab`: `Transform` + `BoxCollider2D` (trigger) en layer `Interactables` + `ExaminableInteractable`. Tamaño de collider y orientaciones válidas por instancia: ver §16 (coordenadas reales).

### 11.4 Nuevos ScriptableObjects

Los 3 `.asset` de `ExaminableData` listados en §4, con nombre de archivo = ID congelado.

### 11.5 `InteractionDetector` no vive en `Player.prefab`

Corrección 5/6 aplicada: el detector vive en `InteractionSceneRoot` (por escena) y recibe `originPoint`/`playerOrientation` como referencias serializadas al `Player` de esa escena — el `Player.prefab` en sí **no** gana ningún componente de interacción (solo gana `PlayerControlGate` y las referencias del gate en `PlayerMotor`/`PlayerOrientation`, ver §18, sin cambios respecto a v0.1 en ese punto).

---

## 12. Asmdefs y dependencias

Sin cambios respecto a v0.1, salvo que ya no hace falta evaluar una referencia a TMPro (§10.2 congela `UnityEngine.UI`, ya cubierto por los módulos base sin referencia de asmdef adicional). Todo entra en `Synora.Runtime`; sin assemblies nuevos; sin dependencias circulares (`Gameplay.Interaction` depende de `Systems` y `Data`, nunca al revés).

---

## 13. Rendimiento

Sin cambios de objetivo respecto a v0.1. Ajuste puntual: el lookup `Collider2D → IInteractable` se construye **una vez** en `InteractionDetector.Awake` (§14) a partir de `sceneExaminables`, así que en `FixedUpdate` no hay `GetComponent`, no hay `Find`, no se crean listas ni diccionarios, no se generan strings — todo el trabajo por frame es: un `OverlapBox` sobre buffer preasignado, una deduplicación sobre ese mismo buffer, y llamadas a `InteractionGeometry.IsInsideFrontZone` (todas `struct`, sin asignaciones).

---

## 14. Validaciones y errores de configuración (corregido — registro y cache)

`InteractionDetector.Awake` realiza, en orden:

1. **Validar la lista `sceneExaminables`:** ningún elemento nulo (log de error por cada nulo encontrado, sin detener la carga del resto).
2. **Validar IDs vacíos o duplicados dentro de la lista:** recorrido único sobre `sceneExaminables`, usando un `HashSet<string>` de IDs vistos (esto ocurre una sola vez en `Awake`, no por frame — no viola la regla de "sin colecciones nuevas" de `FixedUpdate`). `Debug.LogError` por cada ID vacío o duplicado.
3. **Validar colliders:** cada `ExaminableInteractable` debe tener un `Collider2D` (ya garantizado por `[RequireComponent]`, pero se verifica también que esté en modo `isTrigger = true` — `Debug.LogWarning` si no).
4. **Validar layer:** cada examinable debe estar en la layer `Interactables` — `Debug.LogWarning` si no coincide.
5. **Construir el lookup `Collider2D → IInteractable`:** un `Dictionary<Collider2D, IInteractable>` armado una sola vez, iterando `sceneExaminables` y sus `GetComponent<Collider2D>()` (el único `GetComponent` de todo el sistema, y ocurre en `Awake`, no en `FixedUpdate`).
6. **Preasignar `Collider2D[8]`** (buffer de overlap) y la colección de candidatos con capacidad 8 (`List<IInteractable>` con `Capacity = 8` fijada una vez, reutilizada por frame con `Clear()` en vez de recrearse).

Resto de la tabla de validaciones — igual que v0.1 §14, con dos filas ajustadas:

| Condición | Mecanismo |
|---|---|
| `ExaminableData` ausente en `ExaminableInteractable` | `Debug.LogError` en `Awake` del propio `ExaminableInteractable` |
| `InteractionId` vacío o duplicado **en la escena** | `Debug.LogError` en `InteractionDetector.Awake` (ya no en un validador genérico separado) |
| Collider2D ausente | `[RequireComponent(typeof(Collider2D))]` |
| Collider2D no está en modo trigger | `Debug.LogWarning` en `InteractionDetector.Awake` |
| Layer incorrecta | `Debug.LogWarning` en `InteractionDetector.Awake` |
| UI sin referencias | `Debug.LogError` en `Awake` de los presentadores |
| `PlayerControlGate` ausente | `Debug.LogError` en `Awake` de `PlayerMotor`/`PlayerOrientation` |
| Input no cableado | `Debug.LogError` en `OnEnable` de `InteractionInputReader` |
| Dos `InteractionSceneRoot` en la misma escena | No se detecta en runtime (prohibido buscar globalmente); se previene por jerarquía de prefab + checklist manual (§11.2) |
| Buffer de overlap lleno | `Debug.LogWarning` **una sola vez** por instancia (§7.2), nunca por frame |
| Más de un panel abierto | Estructuralmente imposible: `ShowObservation` es el único punto de apertura y solo se llama desde `ExploringWithTarget` |

---

## 15. Pruebas automáticas (nombres exactos — 10 pruebas)

Todas en `Synora.Tests.EditMode`. Total tras M2: **14** (4 de M1 + 10 de M2).

| # | Nombre exacto | Tipo | GameObjects |
|---|---|---|---|
| 1 | `InteractionGeometry_CandidateBehind_ReturnsFalse` | Pura | No |
| 2 | `InteractionGeometry_CandidateBeyondRange_ReturnsFalse` | Pura | No |
| 3 | `InteractionSelector_HigherPriorityWins` | Pura | No |
| 4 | `InteractionSelector_EqualPriorityNearestWins` | Pura | No |
| 5 | `InteractionSelector_EqualPriorityAndDistanceOrdinalIdWins` | Pura | No |
| 6 | `InteractionSelector_CurrentValidTargetIsPreserved` | Pura | No |
| 7 | `InteractionSelector_InvalidCurrentTargetSelectsReplacement` | Pura | No |
| 8 | `PlayerControlGate_ObservationBlockStopsMotorAndClearsVelocity` | Con GameObjects temporales | Sí |
| 9 | `InteractionController_SecondSeparatePressClosesAndRestoresControl` | Con GameObjects temporales | Sí |
| 10 | `InteractionController_SinglePressOpensWithoutImmediateClose` | Con GameObjects temporales | Sí |

Detalle de la prueba 10 (la más sensible a la corrección 8):

- Parte de `ExploringWithTarget` (target válido ya seleccionado).
- Emite `InteractPressed` **una sola vez** (una sola invocación del handler — dos invocaciones representarían dos pulsaciones físicas distintas, lo cual es un escenario distinto ya cubierto por la prueba 9).
- Asserts: estado resultante es `ObservationOpen`; `panelPresenter.IsOpen == true`; `gate.IsBlocked == true`.

Pruebas 1-7: usan `FakeInteractable : IInteractable` (clase de prueba con campos mutables para `Priority`, `CanInteract`, `InteractionPosition`, `InteractionId`), **sin ningún GameObject**, posible gracias a que `InteractionPosition` es `Vector2` y no `Transform` (corrección 2). Pruebas 8-10: GameObjects temporales + `[TearDown]`/`DestroyImmediate`, mismo patrón que `TransitionSystemTests` de M1 (incluyendo `LogAssert.Expect` para warnings esperados de `OnValidate`/validación al usar `AddComponent` sin cablear referencias). Sin escenas ni assets reales. Sin pruebas PlayMode.

---

## 16. Coordenadas propuestas de los tres graybox (verificadas contra la geometría real)

Extraídas directamente de `Assets/Scenes/*.unity` (posiciones de `CameraBounds2D`, `SpawnPoint` y `AreaTransition` reales, no estimadas):

### 16.1 CamaraPreservacion

| Referencia real | Posición |
|---|---|
| `CameraBounds2D` (centro, tamaño) | (0, 0) · 12 × 10 → habitación x:[-6, 6], y:[-5, 5] |
| Spawn `Default` | (-3.5, 0.5) |
| Spawn `FromCorredorTecnico` | (3.0, 1.0) |
| Transition `ToCorredorTecnico` | (4.5, 1.0) |

**Propuesta — Terminal de diagnóstico:** `(0.0, -3.0)`, contra el muro sur de la habitación (coherente con "terminal" montada en pared). Distancia a `Default`: ≈4.6 u. Distancia a `FromCorredorTecnico`: ≈4.6 u. Distancia a `ToCorredorTecnico`: ≈5.7 u. Orientación válida para examinar: **Sur únicamente** (el jugador se aproxima desde el lado norte del objeto y mira hacia el muro).

### 16.2 CorredorTecnico

| Referencia real | Posición |
|---|---|
| `CameraBounds2D` | (0, 0) · 18 × 8 → habitación x:[-9, 9], y:[-4, 4] |
| Spawn `Default` | (-6.5, 0.5) |
| Spawn `FromCamaraPreservacion` | (-7.0, 0.0) |
| Transition `ToCamaraPreservacion` | (-8.5, 0.0) |
| Spawn `FromClaroExterior` | (6.0, -1.0) |
| Transition `ToClaroExterior` | (7.5, -1.0) |

**Propuesta — Panel de mantenimiento:** `(0.0, 3.0)`, contra el muro norte del corredor. Distancia mínima a cualquier spawn/transición real ≈7.0 u (a `Default`). Orientación válida: **Norte únicamente**.

### 16.3 ClaroExterior

| Referencia real | Posición |
|---|---|
| `CameraBounds2D` | (0, 0) · 24 × 20 → claro x:[-12, 12], y:[-10, 10] |
| Spawn `Default` | (-8.5, -6.5) |
| Spawn `FromCorredorTecnico` | (-10.0, 0.0) |
| Transition `ToCorredorTecnico` | (-11.5, 0.0) |

**Propuesta — Nodo inactivo:** `(3.0, 3.0)`, en zona abierta del claro (no hay pared cercana en los datos disponibles; el nodo es una estructura libre, coherente con su descripción). Distancia mínima a cualquier spawn/transición real ≈13.3 u (a `FromCorredorTecnico`). Orientación válida: **las 4 cardinales** (objeto exento, examinable desde cualquier lado), a diferencia de los dos anteriores que son de pared.

### 16.4 Tamaño de collider graybox

`BoxCollider2D`, tamaño **0.6 × 0.6** unidades (comparable al tamaño del `CapsuleCollider2D` del jugador, 0.55 × 0.35 — un objeto pequeño pero claramente detectable dentro del rango de 1.25 u), `isTrigger = true`.

### 16.5 Límite de la verificación

Verifiqué contra datos **reales y exactos** del repositorio: límites de cámara (`CameraBounds2D`), y posiciones exactas de todos los `SpawnPoint`/`AreaTransition` de las tres escenas (extraídas de los archivos `.unity`, no estimadas). **No pude verificar la geometría exacta de los tiles de colisión** (`Tilemap`/`CompositeCollider2D`): los datos de tiles se almacenan comprimidos en el `.unity` y reconstruir la forma real de los muros desde ahí sin renderizar tendría un margen de error inaceptable para decisiones de posicionamiento. Por eso estas tres coordenadas son una **propuesta fundamentada en la geometría conocida**, no una posición "a prueba de colisión garantizada" — antes de instanciar los prefabs en Fase 6, hay que confirmarlas visualmente en el Editor contra el `Tilemap`/`Collision` real de cada escena, tal como ya exige el propio flujo de Fase 6.

---

## 17. Plan de implementación (corregido)

| Fase | Contenido | Archivos creados (previstos) | Archivos modificados | Riesgos | Pruebas | Commit sugerido |
|---|---|---|---|---|---|---|
| 0 | **Documental** (esta fase) | `SYNORA_SPEC_M2_Interaccion_v0.2.md` | ninguno | Ninguno | ninguna | `docs: define M2 contextual interaction` (**solo después de aprobar v0.2**) |
| 1 | Input, datos y contratos | `IInteractable.cs`, `IInteractionReceiver.cs`, `ExaminableData.cs` | `Controls.inputactions` (**modificado**, no creado — se añade la acción `Interact` dentro del map `Gameplay` existente) | Bajo | ninguna aún (compilación) | `feat: add M2 interaction data and input contract` |
| 2 | `PlayerControlGate` y adaptación controlada de movimiento/orientación | `PlayerControlGate.cs` | `PlayerMotor.cs`, `PlayerOrientation.cs`, `Player.prefab` | Medio (toca M1) | prueba 8 | `feat: add player control gate` |
| 3 | Detección, geometría y selector determinista | `InteractionDetector.cs`, `InteractionGeometry.cs`, `InteractionSelector.cs` | ninguno | Medio (Physics2D/layers) | pruebas 1-7 | `feat: add interaction detection and selection` |
| 4 | `InteractionController` y flujo de estados | `InteractionController.cs`, `InteractionInputReader.cs` | ninguno | Medio | pruebas 9-10 | `feat: add interaction controller and state flow` |
| 5 | UI graybox y prefab de escena | `InteractionPromptPresenter.cs`, `ObservationPanelPresenter.cs`, `InteractionSceneRoot.prefab` | ninguno | Bajo (TMP ya no es riesgo — decisión B congelada) | manuales de UI | `feat: add M2 UI graybox` |
| 6 | Tres interactuables y cableado de escenas | `ExaminableInteractable.cs`, `ExaminableGraybox.prefab`, 3 `.asset` de datos | `CamaraPreservacion.unity`, `CorredorTecnico.unity`, `ClaroExterior.unity`, `ProjectSettings/TagManager.asset` (layer `Interactables`=11) | Medio (primera vez que se tocan escenas/ProjectSettings; verificar coordenadas de §16 contra el Tilemap real) | manuales de detección por escena | `feat: wire M2 examinables into area scenes` |
| 7 | Pruebas automáticas y QA | pruebas EditMode restantes | ninguno | Bajo | 14/14 | `test: add M2 automated tests and QA report` |
| 8 | Build, recorrido final, informe, commit y tag | `SYNORA_M2_Test_Report.md` | ninguno de código | Bajo | matriz manual completa | `chore: finalize M2 validation and build` + tag `m2-complete` |

No se avanza de fase sin aprobación del Director. La Fase 0 (este documento) no produce ningún commit todavía — el commit `docs: define M2 contextual interaction` queda **propuesto para después** de que apruebes esta v0.2, no creado ahora.

---

## 18. Migración y regresión

Sin cambios respecto a v0.1 §18, con una corrección: `InteractionDetector` **no** se añade a `Player.prefab` (vivía ahí en el planteamiento original de v0.1; ahora vive en `InteractionSceneRoot`, por escena). El `Player.prefab` solo gana `PlayerControlGate` y las dos referencias serializadas en `PlayerMotor`/`PlayerOrientation`, exactamente como en v0.1.

Confirmación de no ruptura de M1: idéntica a v0.1 §18 (movimiento/diagonales, colisiones, cámara, spawn, transiciones, contexto de escena, build, las 4 pruebas actuales) — ninguno de estos puntos cambia con la corrección de dónde vive el detector.

---

## 19. Definition of Done

Sin cambios de fondo respecto a v0.1 §19. Se añade un criterio explícito derivado de las correcciones:

- El contrato `IInteractable`/`IInteractionReceiver` tiene una **única** ruta de apertura del panel (verificable por inspección de código: `ObservationPanelPresenter.Open` solo se llama desde `InteractionController.ShowObservation`).
- `ExaminableInteractable` no importa ni referencia ningún tipo de `Gameplay.Interaction.*Presenter` (verificable por `using`/referencias del archivo).

---

## 20. Decisiones abiertas (restantes tras v0.2)

| Decisión | Alternativas | Recomendación de Claude | Impacto | Requiere aprobación del Director |
|---|---|---|---|---|
| Confirmación final de TMP en el Editor real | Mantener `UnityEngine.UI.Text` (congelado en v0.2) / migrar a TMP si se confirma sin fricción en Fase 5 | Mantener Opción B hasta confirmar en Editor | Calidad tipográfica vs. certeza | Sí (si se decide migrar) |
| Verificación visual de las 3 coordenadas de §16 contra el Tilemap real | Coordenadas propuestas (§16) / ajustar en Editor | Verificar visualmente antes de instanciar en Fase 6 | Evitar solape con paredes reales | Sí |
| Modo trigger vs. no-trigger para el examinable de pared | Trigger (propuesto, congelado en §4/§7) | Trigger | Coherencia con "detección, no colisión física" | No (ya congelado, listado por completitud) |
| Capacidad del buffer (8) | 4 / 8 (congelado) / 16 | 8 | Margen vs. memoria | No |
| Necesidad real de pruebas PlayMode | Ninguna (congelado) | Ninguna por ahora | Cobertura vs. tiempo de CI | No, salvo que Fase 7 revele un caso real |
| Cantidad final de pruebas automatizadas | 10 (congeladas con nombres exactos) | 10 | Cobertura vs. mantenimiento | No |

Las decisiones marcadas como "congeladas" en esta versión ya no están abiertas; se listan por trazabilidad respecto a v0.1. Solo las dos primeras filas requieren una acción del Director antes o durante la Fase 5/6.

---

**FIN DE LA SPEC TÉCNICA M2 v0.2. No se ha escrito código. No se ha creado ningún commit. `SYNORA_SPEC_M2_Interaccion_v0.1.md` se conserva sin modificar.**

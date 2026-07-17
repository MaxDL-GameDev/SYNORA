# SYNORA — Especificación Técnica M2: Interacción y Observación Básica

| Campo | Valor |
|---|---|
| Documento | `SYNORA_SPEC_M2_Interaccion_v0.3.md` |
| Versión | **0.3** (incorpora todas las decisiones de v0.2 + doce precisiones finales; v0.1 y v0.2 se conservan sin cambios) |
| Documento padre | `SYNORA_GDD_M2_Interaccion_v0.1.md` (Docs/Design) — **aprobado sin cambios funcionales** |
| Editor verificado | Unity 6000.5.3f1 |
| URP 2D | 17.6.0 |
| Input System | 1.19.0 |
| HEAD verificado | `13b97ac` (tag `m1-complete`) |
| Fecha real de redacción | 2026-07-17 |
| Estado | **APROBADA Y CONGELADA POR EL DIRECTOR** |

> **Nota de vigencia.** `SYNORA_SPEC_M2_Interaccion_v0.1.md` y `SYNORA_SPEC_M2_Interaccion_v0.2.md` quedan como **historial documental** del proceso de revisión: no se modifican, no se eliminan, y no deben usarse como referencia para implementar. **Esta v0.3 es la única SPEC técnica vigente para implementar M2.** Cualquier detalle presente en v0.1/v0.2 que haya sido corregido en v0.2 o v0.3 queda sin efecto; en caso de duda, esta v0.3 prevalece.

> **Nota de acceso (heredada de v0.2).** No tengo MCP for Unity conectado a un Editor en vivo en este entorno. Toda verificación de geometría real (§16) se hizo inspeccionando directamente los archivos `.unity`/`ProjectSettings` del repositorio, no renderizando la escena.

---

## Parche de congelamiento (post-aprobación arquitectónica)

| # | Parche | Sección afectada |
|---|---|---|
| 1 | Estado del documento → **APROBADA Y CONGELADA POR EL DIRECTOR**; nota de vigencia sobre v0.1/v0.2 como historial | Encabezado |
| 2 | Utilidad `InteractionTargetUtility.IsAlive` para no acceder a propiedades de un `IInteractable` cuyo `MonoBehaviour` ya fue destruido por Unity; regla sticky actualizada para comprobarla primero | §3.7 (nueva), §8 |
| 3 | Guards de `ShowObservation` ampliados con el orden exacto de siete comprobaciones, incluida `IsAlive` | §3.10 |
| 4 | `OnValidate` de `ExaminableData` confirmado con las cuatro validaciones heredadas (trim, warning ID vacío, warning título vacío, warning cuerpo vacío); `DisplayName` vacío permitido | §4.1 |
| 5 | `InteractionTargetUtility.cs` añadido a los archivos previstos de Fase 3 | §17 |

## Registro de cambios v0.2 → v0.3

| # | Precisión aplicada | Sección afectada |
|---|---|---|
| 1 | Buffer de overlap y colección de candidatos son de **instancia** (inicializador de campo), no `static`; ya no se describen como "asignados en `Awake`" | §7.2, §7.7 |
| 2 | `ExaminableData` expone propiedades públicas explícitas; nada de acceso directo a campos privados | §4.1 |
| 3 | `CanInteract` incluye `data.HasValidInteractionId` | §3.2 |
| 4 | Regla exacta de "objetivo sticky" en `InteractionSelector` (3 condiciones) | §8 |
| 5 | Cache de colliders vía `GetComponents<Collider2D>()` en `Awake`, deduplicación por recorrido lineal en `FixedUpdate`, sin `GetComponentInChildren` | §7.4, §14 |
| 6 | Guards explícitos en `Execute` y `ShowObservation`; ninguna llamada inválida cambia estado ni genera excepción; sin logs por teardown previsible | §3.4, §3.10, §9 |
| 7 | Semántica final del prompt: contrato devuelve `"Examinar"`; el presentador antepone `"[E] "` solo al cambiar de objetivo; hint de teclado fijo, sin adaptación por dispositivo | §3.1, §10 |
| 8 | `InteractionController.OnDisable` idempotente y null-safe, detallado paso a paso | §3.10, §6 |
| 9 | Aclaración de "Sur/Norte únicamente": son resultado esperado de la colocación física, no una máscara de código; Nodo inactivo accesible en las 4 direcciones físicamente | §16 |
| 10 | Plan de pruebas repartido por fase (2, 3, 4, 7) en vez de "todas en Fase 7" | §17 |
| 11 | TMP retirado de las decisiones abiertas; congelado `UnityEngine.UI.Text` para todo M2; evaluación de TMP diferida formalmente a M3 | §10, §20 |
| 12 | Única decisión abierta restante: ajuste visual de las 3 coordenadas en Fase 6 | §20 |

---

## 1. Objetivo y alcance

Sin cambios respecto a v0.2 §1.

## 2. Arquitectura propuesta

Sin cambios respecto a v0.2 §2: `InteractionController` sigue siendo el único orquestador; `ExaminableInteractable` no conoce ningún presenter; el flujo pasa por `IInteractionReceiver`. Ver guards exactos en §9 (antes descritos solo conceptualmente, ahora con condiciones explícitas).

---

## 3. Contratos y responsabilidades

### 3.1 `IInteractable` — sin cambios de forma, semántica de `PromptText` precisada

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

`PromptText` devuelve únicamente la palabra **`"Examinar"`** (sin prefijo). El prefijo visual `[E] ` es responsabilidad exclusiva de `InteractionPromptPresenter`, nunca del contrato ni del examinable — ver §10.

### 3.2 `CanInteract` — regla exacta final

```csharp
public bool CanInteract =>
    isActiveAndEnabled
    && interactionEnabled
    && data != null
    && data.HasValidInteractionId;
```

Cuatro condiciones, todas necesarias:

1. `isActiveAndEnabled` — componente/GameObject activo (Unity).
2. `interactionEnabled` — campo serializado propio (`bool`, default `true`).
3. `data != null` — asset asignado.
4. `data.HasValidInteractionId` — el ID del asset no está vacío ni es solo espacios.

Un `InteractionId` vacío produce **dos efectos simultáneos**, ambos intencionales: (a) genera una advertencia de configuración en `ExaminableData`/`InteractionDetector` (§14, sin cambios respecto a v0.2), y (b) hace que `CanInteract` sea `false`, excluyendo automáticamente al examinable del flujo normal de detección/selección — no basta con advertir, el objeto mal configurado tampoco debe ser interactuable.

### 3.3 `IInteractionReceiver` — sin cambios respecto a v0.2 §3.3

### 3.4 `ExaminableInteractable` — cache de collider y guards de `Execute`

```csharp
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ExaminableInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ExaminableData data;
    [SerializeField] private int priority;
    [SerializeField] private bool interactionEnabled = true;

    public string InteractionId => data != null ? data.InteractionId : string.Empty;
    public int Priority => priority;
    public bool CanInteract =>
        isActiveAndEnabled && interactionEnabled && data != null && data.HasValidInteractionId;
    public Vector2 InteractionPosition => transform.position;
    public string PromptText => "Examinar";

    public void Execute(IInteractionReceiver receiver)
    {
        if (receiver == null) return;
        if (!CanInteract) return;
        receiver.ShowObservation(data);
    }
}
```

- `[RequireComponent(typeof(BoxCollider2D))]` (antes `Collider2D` genérico en v0.2 — precisado a `BoxCollider2D`, el tipo concreto que usa el graybox de M2, ver §7.4).
- `Execute` tiene dos guards explícitos, en este orden: `receiver == null` → no hace nada; `!CanInteract` → no hace nada; de lo contrario, llama **exactamente una vez** a `receiver.ShowObservation(data)`. No hay ninguna otra rama.
- Sigue sin referenciar ningún presenter, sin cambios respecto a v0.2.

### 3.5 IDs exactos — sin cambios respecto a v0.2 §3.5

### 3.6 `InteractionInputReader` — sin cambios respecto a v0.2

### 3.7 `InteractionDetector` — instancia, no estático (ver §7 completo)

Campos de instancia (inicializadores de campo, **no** `static`, **no** asignación en `Awake`):

```csharp
private readonly Collider2D[] overlapBuffer = new Collider2D[8];
private readonly List<IInteractable> candidateBuffer = new List<IInteractable>(8);
```

`Awake` ya no "crea" estos buffers — ya existen desde la construcción del componente por el inicializador de campo. Lo que `Awake` sí hace es la validación de configuración y la construcción del **lookup** `Collider2D → IInteractable` (ver §14), que sí depende de datos serializados (`sceneExaminables`) y por tanto no puede resolverse con un inicializador de campo simple.

### 3.7.1 `InteractionTargetUtility` (utilidad nueva — Parche de congelamiento)

- Namespace: `Synora.Gameplay.Interaction`
- Carpeta: `Assets/Scripts/Gameplay/Interaction/InteractionTargetUtility.cs`
- **Motivo:** una referencia `IInteractable` guardada (p. ej. `currentTarget` en `InteractionSelector`/`InteractionController`) puede apuntar a un `MonoBehaviour` que Unity ya destruyó (cambio de escena, destrucción del GameObject, etc.). Acceder a `CanInteract` o a cualquier otra propiedad de una referencia así no lanza necesariamente una excepción de inmediato, pero puede leer un estado sin sentido o interactuar mal con la sobrecarga de igualdad de `UnityEngine.Object` — por eso ninguna clase debe leer propiedades de un `IInteractable` guardado sin comprobar antes que sigue "vivo".

```csharp
public static class InteractionTargetUtility
{
    public static bool IsAlive(IInteractable target)
    {
        if (ReferenceEquals(target, null))
        {
            return false;
        }

        return target is not UnityEngine.Object unityObject
            || unityObject != null;
    }
}
```

- Funciona con `ExaminableInteractable` (que **es** `UnityEngine.Object`, y por tanto se beneficia de la sobrecarga de igualdad de Unity que detecta objetos destruidos) y también con cualquier `FakeInteractable` puro de las pruebas (que **no** es `UnityEngine.Object`, así que la comparación `unityObject != null` ni siquiera se evalúa para ese caso — la segunda condición del `||` ya es `true` por el patrón `is not`).
- No genera asignaciones (todo es comprobación de tipo y comparación de referencia).
- No genera logs — es una comprobación pura, no un punto de validación de configuración.
- **Debe usarse antes de acceder a cualquier propiedad de un `IInteractable` guardado entre frames** (`CanInteract`, `Priority`, `InteractionPosition`, `InteractionId`, etc.). No hace falta usarla sobre un `IInteractable` recién obtenido de `detector.Candidates` en el mismo frame en que se detectó (ese ya se sabe vivo porque `Physics2D.OverlapBox` no puede devolver un `Collider2D` de un objeto destruido); el caso que cubre es específicamente una referencia **retenida** de un frame a otro.

### 3.8 `InteractionGeometry` — sin cambios respecto a v0.2 §3.8

### 3.9 `InteractionSelector` — regla de "sticky target" explícita (ver §8)

### 3.10 `InteractionController` — guards de `ShowObservation` y `OnDisable` idempotente

```csharp
public void ShowObservation(ExaminableData data)
{
    if (state != State.ExploringWithTarget) return;
    if (!InteractionTargetUtility.IsAlive(currentTarget)) return;
    if (!currentTarget.CanInteract) return;
    if (data == null) return;
    if (panelPresenter == null) return;
    if (promptPresenter == null) return;
    if (gate == null) return;

    panelPresenter.Open(data);
    promptPresenter.Hide();
    gate.Block(ControlBlockReason.Observation);
    state = State.ObservationOpen;
}
```

**Siete guards, en este orden exacto** (Parche de congelamiento): estado correcto → `currentTarget` vivo (comprobado con `InteractionTargetUtility.IsAlive` **antes** de leer cualquier propiedad suya) → `currentTarget.CanInteract` → `data` no nulo → `panelPresenter` no nulo → `promptPresenter` no nulo → `gate` no nulo. Si **cualquiera** falla, el método retorna sin abrir panel, sin bloquear control, sin cambiar estado, y **sin lanzar ninguna excepción** (todas son comprobaciones de precondición, no aserciones, y el orden garantiza que nunca se intenta usar una referencia antes de confirmarla). No se registran logs por estas condiciones cuando ocurren de forma previsible durante el apagado de la escena (ver `OnDisable` abajo) — solo se loguean errores de **configuración** (referencias nulas detectadas en `Awake`), no rechazos válidos de una llamada tardía.

```csharp
private void OnDisable()
{
    if (inputReader != null) inputReader.InteractPressed -= HandleInteractPressed;

    if (panelPresenter != null) panelPresenter.Close();
    if (promptPresenter != null) promptPresenter.Hide();
    if (gate != null) gate.Unblock(ControlBlockReason.Observation);

    currentTarget = null;
    state = State.ExploringWithoutTarget;
}
```

- Cada paso comprueba su propia referencia antes de usarla (`null`-safe): si `panelPresenter` ya fue destruido (orden de destrucción de escena), no se lanza `NullReferenceException`.
- **Idempotente:** llamarlo dos veces seguidas no produce ningún efecto distinto la segunda vez ni genera error — cerrar un panel ya cerrado, ocultar un prompt ya oculto, y desbloquear una razón ya desbloqueada son operaciones seguras por diseño de `ObservationPanelPresenter`/`InteractionPromptPresenter` (no-op si ya están en ese estado) y de `PlayerControlGate.Unblock` (operación de bits, `&= ~reason`, segura de repetir).
- **Nunca usa `ClearAll`** ni ninguna operación que retire razones de bloqueo que no sean la propia (`Observation`).
- No genera warnings ni errores durante la descarga de escena: todas las ramas son condicionales a que la referencia exista.

### 3.11 `InteractionPromptPresenter` / `ObservationPanelPresenter` — ver §10 para el prompt

### 3.12 `PlayerControlGate` — sin cambios respecto a v0.2

### 3.13 `ExaminableData` — ver §4 completo

---

## 4. Modelo de datos

### 4.1 `ExaminableData` — código conceptual final

```csharp
[CreateAssetMenu(fileName = "ExaminableData", menuName = "SYNORA/Examinable Data")]
public sealed class ExaminableData : ScriptableObject
{
    [SerializeField] private string interactionId;
    [SerializeField] private string displayName;
    [SerializeField] private string observationTitle;
    [SerializeField, TextArea] private string observationBody;

    public string InteractionId => interactionId;
    public string DisplayName => displayName;
    public string ObservationTitle => observationTitle;
    public string ObservationBody => observationBody;

    public bool HasValidInteractionId => !string.IsNullOrWhiteSpace(interactionId);

    private void OnValidate()
    {
        if (interactionId != null)
        {
            interactionId = interactionId.Trim();
        }

        if (!HasValidInteractionId)
        {
            Debug.LogWarning("ExaminableData: interactionId is empty.", this);
        }

        if (string.IsNullOrWhiteSpace(observationTitle))
        {
            Debug.LogWarning("ExaminableData: observationTitle is empty.", this);
        }

        if (string.IsNullOrWhiteSpace(observationBody))
        {
            Debug.LogWarning("ExaminableData: observationBody is empty.", this);
        }

        // displayName vacío se permite: solo es un dato auxiliar de Inspector,
        // no se muestra al jugador ni participa en CanInteract.
    }
}
```

- Las cinco propiedades públicas (`InteractionId`, `DisplayName`, `ObservationTitle`, `ObservationBody`, `HasValidInteractionId`) son la **única** superficie que cualquier otra clase puede usar. Ninguna clase consumidora (`ExaminableInteractable`, presentadores, pruebas) accede a los campos serializados privados directamente.
- `HasValidInteractionId` es la fuente única de verdad tanto para la advertencia de configuración en `OnValidate` como para el cuarto término de `CanInteract` (§3.2) — no hay dos implementaciones distintas de "¿el ID es válido?" que puedan desincronizarse.
- **`OnValidate` conserva las cuatro validaciones heredadas** (Parche de congelamiento, confirmando lo ya decidido en versiones previas): recorte (`Trim`) de `interactionId`; advertencia si `interactionId` está vacío; advertencia si `observationTitle` está vacío; advertencia si `observationBody` está vacío.
- **`displayName` vacío se permite** en M2: es únicamente un dato auxiliar de Inspector (no se muestra al jugador, no participa en `CanInteract`, no se usa en ningún contrato), así que no genera advertencia.
- Resto del modelo (Priority/CanInteract en `ExaminableInteractable`, no en el SO; ubicación de assets; convención de nombres; IDs congelados) sin cambios respecto a v0.2 §4.

---

## 5. Input System

Sin cambios respecto a v0.2 §5 (acción `Interact`, bindings, fase `performed`, `Controls.inputactions` como archivo **modificado**, no creado).

---

## 6. Bloqueo de control

Sin cambios de diseño respecto a v0.2 §6 (bitmask `ControlBlockReason`, aclaraciones sobre input mantenido/soltado). Precisión añadida por la corrección 8: la limpieza completa de `InteractionController.OnDisable` (secuencia exacta ya mostrada en §3.10) es la que garantiza que `Unblock(Observation)` ocurre siempre que el componente se desactive, incluso si la escena se descarga con el panel abierto — sin depender de que el jugador haya pulsado Interact para cerrar.

---

## 7. Detección frontal

### 7.1 Valores congelados — sin cambios

`detectionRange = 1.25`, `frontalHalfWidth = 0.4` (ancho total 0.8).

### 7.2 Buffers — de instancia, no estáticos (corrección 1)

```csharp
public sealed class InteractionDetector : MonoBehaviour
{
    [SerializeField] private PlayerOrientation playerOrientation;
    [SerializeField] private Transform originPoint;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private List<ExaminableInteractable> sceneExaminables = new List<ExaminableInteractable>();

    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private readonly List<IInteractable> candidateBuffer = new List<IInteractable>(8);
    private readonly Dictionary<Collider2D, IInteractable> colliderLookup = new Dictionary<Collider2D, IInteractable>();

    private ContactFilter2D interactableFilter;
    private bool hasLoggedBufferFull;

    public IReadOnlyList<IInteractable> Candidates => candidateBuffer;
    // ...
}
```

- `overlapBuffer`, `candidateBuffer` y `colliderLookup` se declaran con **inicializador de campo de instancia** (`= new ...`), no `static`, y no se afirma en ningún punto de esta SPEC que se "asignen en `Awake`" — existen desde que el componente se construye. Cada instancia de `InteractionDetector` (una por escena, dentro de `InteractionSceneRoot`) tiene su propio buffer independiente.
- `Awake` solo **llena** `colliderLookup` (a partir de `sceneExaminables`) y configura `interactableFilter`; no crea ningún array ni lista nueva en ese momento — esos ya existen por el inicializador de campo.

### 7.3 Filtro exacto — `InteractionGeometry` — sin cambios respecto a v0.2 §7.3

### 7.4 Cache de colliders y orden de aplicación (corrección 5)

El graybox de M2 usa un único `BoxCollider2D` principal por examinable (0.6 × 0.6, `isTrigger = true` — ver §16.4, sin cambios), pero el diseño contempla que un examinable **pueda** tener más de un `Collider2D` en el mismo GameObject (p. ej. un collider adicional invisible para ampliar el área de trigger en M3); por eso el cache se construye con `GetComponents<Collider2D>()`, no asumiendo uno solo:

```csharp
private void Awake()
{
    // ... validaciones de sceneExaminables (ver §14) ...

    foreach (ExaminableInteractable examinable in sceneExaminables)
    {
        if (examinable == null) continue;

        Collider2D[] colliders = examinable.GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            colliderLookup[collider] = examinable; // todos apuntan al mismo IInteractable
        }
    }

    interactableFilter = new ContactFilter2D();
    interactableFilter.useLayerMask = true;
    interactableFilter.SetLayerMask(interactableLayer);
    interactableFilter.useTriggers = true;
}
```

- `GetComponents<Collider2D>()` se llama **una sola vez por examinable, en `Awake`** — nunca en `FixedUpdate`.
- **No se usa `GetComponentInChildren`/`GetComponentsInChildren`**: en M2, todos los colliders registrados deben estar en el **mismo GameObject** que `ExaminableInteractable` (requisito explícito de la corrección 5). Un collider hijo en un GameObject distinto no se recogería — no es un caso soportado en M2.

### 7.5 `FixedUpdate` — sin `GetComponent`, sin colecciones nuevas, deduplicación lineal

```csharp
private void FixedUpdate()
{
    Vector2 origin = originPoint.position;
    Vector2 facing = ToVector2(playerOrientation.Facing);
    Vector2 point = origin + facing * (detectionRange / 2f);
    Vector2 size = (facing.x != 0f)
        ? new Vector2(detectionRange, frontalHalfWidth * 2f)
        : new Vector2(frontalHalfWidth * 2f, detectionRange);

    int count = Physics2D.OverlapBox(point, size, 0f, interactableFilter, overlapBuffer);

    if (count == overlapBuffer.Length && !hasLoggedBufferFull)
    {
        Debug.LogWarning("InteractionDetector: overlap buffer full; some candidates may be ignored.", this);
        hasLoggedBufferFull = true;
    }

    candidateBuffer.Clear(); // reutiliza la capacidad existente, no crea una lista nueva

    for (int i = 0; i < count; i++)
    {
        if (!colliderLookup.TryGetValue(overlapBuffer[i], out IInteractable candidate))
        {
            continue; // collider fuera de sceneExaminables (no debería ocurrir con la layer correcta)
        }

        bool alreadyAdded = false;
        for (int j = 0; j < candidateBuffer.Count; j++)
        {
            if (ReferenceEquals(candidateBuffer[j], candidate))
            {
                alreadyAdded = true;
                break;
            }
        }
        if (alreadyAdded) continue; // deduplicación: un examinable con varios colliders cuenta una vez

        if (!candidate.CanInteract) continue;

        if (InteractionGeometry.IsInsideFrontZone(origin, facing, candidate.InteractionPosition, detectionRange, frontalHalfWidth))
        {
            candidateBuffer.Add(candidate);
        }
    }
}
```

Cumplimiento exacto de la corrección 5 durante `FixedUpdate`:

- **Sin `GetComponent`.**
- **Sin `GetComponents`.**
- **Sin `HashSet`** (ni ninguna colección creada en el método — `candidateBuffer` se reutiliza con `Clear()`).
- **Sin colecciones nuevas** de ningún tipo.
- **Deduplicación por recorrido lineal** (`for` anidado comparando `ReferenceEquals`), no por diccionario ni conjunto — con como máximo 8 candidatos por frame, un recorrido O(n²) sobre n≤8 es trivial y no requiere estructuras auxiliares.
- Un interactuable con varios colliders solo aparece una vez en `candidateBuffer`, verificado antes de aplicar `InteractionGeometry`.

### 7.6 Gizmos de Editor — sin cambios respecto a v0.2

### 7.7 Frecuencia — `FixedUpdate`, sin cambios respecto a v0.2

---

## 8. Selección determinista — regla de "objetivo sticky" (corrección 4)

`InteractionSelector.SelectTarget` conserva `currentTarget` **únicamente** cuando las tres condiciones siguientes se cumplen simultáneamente, comprobadas **en este orden** (Parche de congelamiento):

1. `InteractionTargetUtility.IsAlive(currentTarget)` — comprobar primero que la referencia sigue viva, **antes** de tocar ninguna propiedad suya.
2. `currentTarget.CanInteract == true` — solo se evalúa si el paso 1 fue verdadero.
3. `currentTarget` sigue presente en `candidates` **por comparación de referencia** (`ReferenceEquals`, no por `InteractionId` ni ningún otro campo).

Si **cualquiera** de las tres falla, se descarta `currentTarget` **sin acceder a ninguna otra propiedad suya** y se selecciona un reemplazo con el orden completo de §8 (prioridad descendente → distancia al cuadrado ascendente → `InteractionId` ordinal ascendente). El mismo principio aplica al recorrer `candidates` en busca de un reemplazo: `InteractionSelector` debe ignorar cualquier candidato para el que `InteractionTargetUtility.IsAlive` sea `false`, **antes** de leer su `CanInteract`, `Priority`, `InteractionPosition` o `InteractionId`.

```
static IInteractable SelectTarget(candidates, currentTarget, playerPosition)
{
    if InteractionTargetUtility.IsAlive(currentTarget)
        and currentTarget.CanInteract
        and candidates contains currentTarget by reference:
        return currentTarget

    best = null
    bestSqrDist = +infinity

    for each c in candidates:
        if not InteractionTargetUtility.IsAlive(c): continue
        if not c.CanInteract: continue
        // ... resto del algoritmo sin cambios respecto a v0.2 §8 ...

    return best
}
```

En la práctica, los elementos de `candidates` provienen siempre de `detector.Candidates` del mismo frame (nunca de una referencia retenida entre frames dentro del propio detector — ver §3.7.1), así que la comprobación `IsAlive` dentro del bucle es una salvaguarda defensiva de bajo costo, no una operación que se espere que falle en el camino normal; la comprobación sobre `currentTarget` sí es la que importa en la práctica, porque **esa** referencia sí se retiene de un frame a otro.

El resto del algoritmo (orden de desempate, tipos `int`/`Vector2`, sin `GetInstanceID`, sin `List.Sort`/LINQ) permanece exactamente como en v0.2 §8.

---

## 9. Estados de interacción

Misma tabla y transiciones que v0.2 §9, con el flujo de apertura ahora gobernado por los guards explícitos de `ShowObservation` (§3.10): una emisión de `InteractPressed` en `ExploringWithTarget` invoca `target.Execute(this)`, que a su vez llama `ShowObservation` solo si `Execute` pasó sus propios guards (`receiver != null`, `CanInteract`) — si el examinable dejó de poder interactuar entre la detección y la pulsación (caso límite, poco probable dado que ambos ocurren en el mismo frame lógico), `Execute` no llama a `ShowObservation` y el estado permanece en `ExploringWithTarget` sin efecto, sin error.

---

## 10. UI — prompt y TMP congelados

### 10.1 Semántica final del prompt (corrección 7)

- El contrato (`IInteractable.PromptText`) devuelve únicamente **`"Examinar"`**.
- `InteractionPromptPresenter` es responsable de anteponer el prefijo visual: al mostrar, construye y muestra **`"[E] Examinar"`**.
- El string con el prefijo se construye **solo cuando cambia el objetivo activo** (no en cada `Update`/frame): `InteractionPromptPresenter.Show(string promptText)` compara internamente si el texto a mostrar ya es el actual y, si lo es, no vuelve a asignar ni a concatenar nada — mismo principio de "no-op si ya está en ese estado" que rige `Show`/`Hide` desde v0.1.
- El hint de teclado (`E`) se muestra siempre, **incluso si el binding activo en ese momento es el de gamepad** (`buttonSouth`). M2 no detecta el dispositivo activo ni adapta el hint dinámicamente — eso queda explícitamente fuera de alcance (ya lo indicaba el GDD respecto a "reasignación completa de controles"; aquí se precisa que tampoco hay detección de dispositivo para el hint visual). Un hint por dispositivo es una mejora candidata para un hito posterior, no para M2.

### 10.2 TMP — congelado para todo M2, evaluación diferida a M3 (corrección 11)

- **`UnityEngine.UI.Text` es la única opción para todo M2.** No se migra a TMP durante la Fase 5 ni en ninguna fase posterior de M2.
- No se importan los "TMP Essential Resources" durante M2.
- No se instala ningún paquete relacionado con TMP durante M2.
- La evaluación de TextMeshPro se **difiere formalmente a M3**, para hacerse junto con la guía visual y los sprites reales (momento en el que de todas formas se revisará tipografía y presentación). Por esto, TMP **ya no aparece** en la tabla de decisiones abiertas de M2 (§20) — no es una decisión pendiente de este hito, sino un tema explícitamente pospuesto a otro hito.

### 10.3 Resto de UI — sin cambios respecto a v0.2 §10.3

Screen Space – Overlay; `CanvasScaler` Scale With Screen Size, referencia 1920×1080, Match 0.5; safe padding 24 px; sin animaciones; instancia propia por escena; jerarquía `InteractionSceneRoot` (sin cambios respecto a v0.2 §11).

---

## 11. Prefabs y escenas

Sin cambios respecto a v0.2 §11, con una precisión: `ExaminableGraybox.prefab` usa **`BoxCollider2D`** como collider principal (0.6 × 0.6, `isTrigger = true`), coherente con el `[RequireComponent(typeof(BoxCollider2D))]` de §3.4.

---

## 12. Asmdefs y dependencias

Sin cambios respecto a v0.2 §12. Con TMP formalmente fuera de M2 (§10.2), no hay ninguna referencia de asmdef pendiente de evaluar para este hito.

---

## 13. Rendimiento

Sin cambios de objetivo respecto a v0.2 §13, con el detalle de instancia (no estático) de los buffers ya incorporado en §7.2, y la deduplicación lineal de §7.5 confirmando que no hay asignaciones ni estructuras auxiliares creadas en el camino caliente.

---

## 14. Validaciones y errores de configuración

Sin cambios de fondo respecto a v0.2 §14, con dos precisiones:

- La construcción del lookup en `Awake` ahora usa `GetComponents<Collider2D>()` por examinable (§7.4), en vez de un único `GetComponent<Collider2D>()` — cubre el caso (no usado en M2, pero soportado por el diseño) de un examinable con más de un collider propio.
- Los guards de `ShowObservation`/`Execute` (§3.4/§3.10) **no generan log** cuando la condición fallida es una llamada tardía o fuera de secuencia previsible durante teardown (p. ej. `OnDisable` corriendo mientras algo más intenta interactuar) — solo se loguean errores de **configuración real** detectados en `Awake`/`OnValidate` (referencias nulas, IDs vacíos/duplicados, layer incorrecta, etc.), tal como ya establecía v0.2.

---

## 15. Pruebas automáticas

Mismos 10 nombres exactos que v0.2 §15, sin cambios:

1. `InteractionGeometry_CandidateBehind_ReturnsFalse`
2. `InteractionGeometry_CandidateBeyondRange_ReturnsFalse`
3. `InteractionSelector_HigherPriorityWins`
4. `InteractionSelector_EqualPriorityNearestWins`
5. `InteractionSelector_EqualPriorityAndDistanceOrdinalIdWins`
6. `InteractionSelector_CurrentValidTargetIsPreserved`
7. `InteractionSelector_InvalidCurrentTargetSelectsReplacement`
8. `PlayerControlGate_ObservationBlockStopsMotorAndClearsVelocity`
9. `InteractionController_SecondSeparatePressClosesAndRestoresControl`
10. `InteractionController_SinglePressOpensWithoutImmediateClose`

Precisión derivada de la corrección 4: la prueba 6 (`InteractionSelector_CurrentValidTargetIsPreserved`) debe cubrir las tres condiciones de la regla sticky (§8) — un `FakeInteractable` con `CanInteract = true` presente en `candidates` por referencia se conserva; y la prueba 7 (`InteractionSelector_InvalidCurrentTargetSelectsReplacement`) debe cubrir al menos el caso en que `currentTarget.CanInteract` se vuelve `false` (no solo el caso de ausencia en `candidates`), ya que ambos casos están cubiertos por la misma regla de tres condiciones.

**No se agrega una prueba número 11 por `InteractionTargetUtility`** (Parche de congelamiento): las pruebas siguen siendo exactamente 10 nuevas de M2 y 14 en total junto con M1. La prueba 7 (`InteractionSelector_InvalidCurrentTargetSelectsReplacement`) puede validar, con `FakeInteractable` puro, el caso general de "un target no interactuable se reemplaza" (incluyendo `CanInteract == false`); la seguridad **específica** ante un `UnityEngine.Object` ya destruido por Unity (el caso que motiva `InteractionTargetUtility.IsAlive`) no es reproducible de forma limpia con un `FakeInteractable` puro — se valida por **inspección de código** (revisar que `SelectTarget`/`ShowObservation` llaman a `IsAlive` antes de tocar cualquier propiedad) y por **QA manual** en Fase 7 (por ejemplo, destruir un examinable mientras es el target activo y confirmar que no hay excepción). Esto no sacrifica la pureza de las pruebas 1-7: ninguna de ellas necesita un `GameObject` real ni un ciclo de destrucción de Unity para seguir siendo válida.

Distribución por fase: ver §17. Total tras M2: **14** (4 de M1 + 10 de M2) — sin cambios respecto a v0.2.

---

## 16. Coordenadas propuestas de los tres graybox

Mismas coordenadas verificadas en v0.2 §16, con la aclaración de la corrección 9 incorporada:

### 16.1 Aclaración sobre "Norte/Sur únicamente" (corrección 9)

- **"Sur únicamente" (Terminal de diagnóstico) y "Norte únicamente" (Panel de mantenimiento) son el resultado esperado de colocar el objeto contra una pared real**, no una restricción de código. M2 **no implementa** ningún `AllowedApproachDirections` ni mecanismo equivalente que filtre direcciones válidas por examinable — el único filtro direccional que existe es `InteractionGeometry.IsInsideFrontZone` (§7.3), que depende exclusivamente de la orientación actual del jugador, igual para los tres examinables.
- Que en la práctica solo se pueda examinar la Terminal desde el sur (o el Panel desde el norte) depende de que **el Tilemap de colisión real impida físicamente** que el jugador rodee el objeto y se coloque, por ejemplo, al norte de la Terminal (dentro de la pared). Esto se verifica visualmente en Fase 6, junto con la posición exacta (§16.5, sin cambios respecto a v0.2).
- **Si la pared no garantiza la aproximación esperada** (por ejemplo, si hay espacio suficiente para rodear el objeto), **la corrección es mover el objeto**, no agregar un sistema nuevo de direcciones permitidas — esto mantendría la arquitectura de M2 sin ampliar su alcance.
- **El Nodo inactivo** (ClaroExterior), al ser una estructura exenta en el claro exterior sin pared adyacente conocida en los datos verificados, **debe quedar físicamente accesible desde las cuatro direcciones cardinales** — esto también se confirma en Fase 6 contra la geometría real (Tilemap/Collision), no se fuerza por código.

### 16.2 Resto de §16 — sin cambios respecto a v0.2

Coordenadas (Terminal de diagnóstico `(0, -3)`, Panel de mantenimiento `(0, 3)`, Nodo inactivo `(3, 3)`), tamaño de collider (`BoxCollider2D` 0.6×0.6, trigger), y la salvedad sobre no haber podido reconstruir la geometría exacta del Tilemap desde archivo: idénticos a v0.2 §16.1-§16.5.

---

## 17. Plan de implementación — pruebas repartidas por fase (corrección 10)

| Fase | Contenido | Pruebas implementadas en esta fase | Resultado acumulado esperado |
|---|---|---|---|
| 0 | Documental (esta fase) | ninguna | — |
| 1 | Input, datos y contratos | ninguna | compilación limpia |
| 2 | `PlayerControlGate` y adaptación de movimiento/orientación | **Prueba 8** (`PlayerControlGate_ObservationBlockStopsMotorAndClearsVelocity`) | 4 (M1) + 1 = **5/5** |
| 3 | Detección, geometría, selector determinista y utilidad de vida de referencias (`InteractionDetector.cs`, `InteractionGeometry.cs`, `InteractionSelector.cs`, **`InteractionTargetUtility.cs`**) | **Pruebas 1-7** | 4 (M1) + 8 = **12/12** |
| 4 | `InteractionController` y flujo de estados | **Pruebas 9-10** | 4 (M1) + 10 = **14/14** |
| 5 | UI graybox y prefab de escena | ninguna nueva | 14/14 (sin regresión) |
| 6 | Tres interactuables y cableado de escenas | ninguna nueva | 14/14 (sin regresión) |
| 7 | QA final | **ninguna prueba nueva** — reejecuta las 14 existentes | **14/14**, más QA manual, GC Alloc, regresión de M1, y creación de `SYNORA_M2_Test_Report.md` |
| 8 | Build, recorrido final, informe, commit y tag | — | build + tag `m2-complete` |

Archivos previstos por fase, riesgos y commits sugeridos: sin cambios respecto a v0.2 §17 (incluida la corrección de `Controls.inputactions` como archivo modificado, no creado, y el commit documental `docs: define M2 contextual interaction` propuesto para después de aprobar esta v0.3, no creado ahora).

---

## 18. Migración y regresión

Sin cambios respecto a v0.2 §18.

---

## 19. Definition of Done

Sin cambios de fondo respecto a v0.2 §19. Se precisa un criterio adicional derivado de las correcciones de esta versión:

- Ninguna llamada inválida a `ShowObservation`/`Execute` (guards de §3.4/§3.10) produce excepción, cambio de estado, ni log durante el recorrido de QA de Fase 7 — solo las condiciones de configuración real deben loguear.
- `InteractionController.OnDisable` verificado como idempotente (llamarlo repetidamente, o durante descarga de escena con referencias parcialmente destruidas, no genera error).

---

## 20. Decisiones abiertas (única restante)

TMP se retira de esta tabla (congelado como `UnityEngine.UI.Text` para todo M2, diferido a M3 — corrección 11). Tras v0.3, la única decisión que queda pendiente **durante la implementación** es:

| Decisión | Alternativas | Recomendación de Claude | Impacto | Requiere aprobación del Director |
|---|---|---|---|---|
| Ajuste visual final de las tres coordenadas (§16) contra el Tilemap/Collision real en Fase 6 | Mantener `(0,-3)`/`(0,3)`/`(3,3)` si la geometría real lo permite / desplazar cualquiera de las tres si no | Verificar visualmente antes de instanciar; mover el objeto si la pared no aísla la aproximación esperada (§16.1), sin tocar arquitectura, rango, collider, IDs ni contratos | Solo posicional — no afecta ningún contrato, algoritmo ni prueba | Sí (confirmación visual en Fase 6) |

Ninguna otra decisión arquitectónica, de tipos, de contratos, de geometría, de UI o de plan de pruebas queda abierta: **v0.3 es implementable sin decisiones arquitectónicas pendientes**, salvo este único ajuste posicional que, por su propia naturaleza, solo puede confirmarse dentro del Editor en Fase 6.

---

**FIN DE LA SPEC TÉCNICA M2 v0.3 — APROBADA Y CONGELADA POR EL DIRECTOR.**

Esta es la **única SPEC técnica vigente para implementar M2**. `SYNORA_SPEC_M2_Interaccion_v0.1.md` y `SYNORA_SPEC_M2_Interaccion_v0.2.md` se conservan sin modificar, como historial documental del proceso de revisión — no deben usarse como referencia de implementación. El GDD `SYNORA_GDD_M2_Interaccion_v0.1.md` se mantiene como documento funcional aprobado, sin cambios. No se ha escrito código todavía; el commit documental que registra estos cuatro documentos se crea por separado, después de este parche de congelamiento.

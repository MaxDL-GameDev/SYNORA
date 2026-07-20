# SYNORA — SPEC M3: Criaturas y Ecosistema Vivo (v0.1)

> Especificación técnica del hito M3. Documento de diseño aprobado (Fase 0C). No contiene código de producción; solo firmas conceptuales, contratos, pseudocódigo, tablas y diagramas.
>
> Estado canónico al redactar: M2 cerrado y publicado — commit `32fbcfb96b1bf39bffad5c48a00058e4471d5ddd`, tag `m2-complete`. M3 sin implementación.

## Documentos relacionados

- Diseño creativo/funcional: [`../Design/SYNORA_GDD_M3_Criaturas_v0.1.md`](../Design/SYNORA_GDD_M3_Criaturas_v0.1.md)
- Estándar de arte: [`../Art/SYNORA_Creature_Art_Standard_v0.1.md`](../Art/SYNORA_Creature_Art_Standard_v0.1.md)
- ADRs: [`./ADR/ADR-M3-001-state-pattern.md`](./ADR/ADR-M3-001-state-pattern.md) · [`./ADR/ADR-M3-002-creatures-layer-collision.md`](./ADR/ADR-M3-002-creatures-layer-collision.md) · [`./ADR/ADR-M3-003-dual-radius-sensor.md`](./ADR/ADR-M3-003-dual-radius-sensor.md) · [`./ADR/ADR-M3-004-per-scene-creature-placement.md`](./ADR/ADR-M3-004-per-scene-creature-placement.md) · [`./ADR/ADR-M3-005-animator-as-presenter.md`](./ADR/ADR-M3-005-animator-as-presenter.md)

---

## 1. Objetivo

Que el mundo deje de sentirse vacío. Al finalizar M3, el jugador podrá encontrar criaturas que **existen** en el mundo, **patrullan**, permanecen en **Idle**, **detectan** al Player, entran en **Alert** y **vuelven a patrullar**.

## 2. No objetivos

M3 **no** implementa: perseguir, atacar, huir, combatir, restaurar, vincular, capturar, dormir, evolucionar; ni misiones, diálogos, cinemáticas, IA de grupo, pathfinding, NavMesh o audio de criaturas. No se añade lore, biología ni relación narrativa (eso es autoridad creativa separada).

## 3. Alcance

Incluye: `CreatureIdentity` (SO), contratos de estado (`ICreatureState`), `CreatureContext`, estados `IdleState`/`PatrolState`/`AlertState`, `CreatureBrain`, `CreatureSensor` (dos radios), `CreatureMovement` (Rigidbody2D 2D simple), `CreatureAnimator` (presenter), lógica pura (`CreaturePatrolMath`, `CreatureSensing`), prefab de criatura autocontenido, integración concreta de **Verak ×2 en `ClaroExterior`**, estándar de arte, QA, build y tag `m3-complete`. Primera criatura: **Verak**. Diseñado para soportar especies futuras sin rediseño.

## 4. Arquitectura y responsabilidades

| Componente | Tipo | Responsabilidad única |
|---|---|---|
| `CreatureIdentity` | ScriptableObject | Tuning estable por especie. |
| `ICreatureState` | interfaz | Contrato de estado (`Enter`/`Tick`/`Exit`). |
| `IdleState` / `PatrolState` / `AlertState` | clases | Un comportamiento cada una; independientes. |
| `CreatureBrain` | MonoBehaviour | Host de la máquina de estados; construye/valida el contexto; resuelve transiciones. |
| `CreatureContext` | clase | Contenedor local de dependencias + estado mutable encapsulado. |
| `CreatureSensor` | MonoBehaviour | Detección física por radio (OverlapCircle). |
| `CreatureMovement` | MonoBehaviour | Movimiento 2D vía `Rigidbody2D.linearVelocity`. |
| `CreatureAnimator` | MonoBehaviour | Presenter: traduce estado visual + facing a clips/parámetros/`flipX`. |
| `CreaturePatrolMath` | static | Lógica pura de arribo y avance PingPong. |
| `CreatureSensing` | static | Lógica pura del veredicto de percepción (dos radios + linger). |

Convenciones heredadas de M1/M2: `sealed`, dependencias por `[SerializeField] private` validadas en `Awake`/`OnValidate`, sin `Find`/`FindObjectsOfType`/singletons/`DontDestroyOnLoad`, física solo por `Rigidbody2D` (nunca escribir `Transform`), lógica de decisión en clases `static` testeables. Todo en el asmdef `Synora.Runtime`; tests en `Synora.Tests.EditMode`.

### 4.1 Diagrama de componentes

```
CreatureBrain (MonoBehaviour)
 ├─ construye/valida una vez ─▶ CreatureContext
 │      refs permanentes: Identity, Movement, Sensor, Animator, Root, PatrolPoints
 │      estado mutable (encapsulado): PatrolIndex, PatrolDirection, StateTimer,
 │                                    Facing, DetectedPlayer, IsMoving
 ├─ estados (instanciados una vez):  IdleState · PatrolState · AlertState  (ICreatureState)
 └─ FixedUpdate: current.Tick(ctx, dt) → CreatureStateId? → Brain resuelve transición

Lógica pura (static, testeable):  CreaturePatrolMath · CreatureSensing
Física:  CreatureSensor (OverlapCircle vs Player layer) · CreatureMovement (Rigidbody2D)
Presentación:  CreatureAnimator → Animator Controller + SpriteRenderer.flipX
```

## 5. Contrato `ICreatureState`

```csharp
namespace Synora.Gameplay.Creatures
{
    public enum CreatureStateId { Idle, Patrol, Alert }   // token neutral de transición

    public interface ICreatureState
    {
        void Enter(CreatureContext context);
        // null = permanecer; un id = solicitar transición. El estado nunca instancia
        // ni referencia otra clase de estado.
        CreatureStateId? Tick(CreatureContext context, float deltaTime);
        void Exit(CreatureContext context);
    }
}
```

Garantías (ver [ADR-M3-001](./ADR/ADR-M3-001-state-pattern.md)): estados polimórficos independientes; sin herencia; sin conocerse entre sí; instancias creadas una sola vez; retorno value-type → cero alloc por transición; `CreatureBrain` resuelve `id → instancia` mediante un mapa construido en `Awake`. El `switch` de resolución vive **solo** en el Brain, no como arquitectura de comportamiento.

## 6. `CreatureContext` (estado mutable encapsulado)

```csharp
public sealed class CreatureContext
{
    // Referencias permanentes (solo lectura tras construcción por CreatureBrain)
    public CreatureIdentity Identity { get; }
    public CreatureMovement Movement { get; }
    public CreatureSensor   Sensor   { get; }
    public CreatureAnimator Animator { get; }
    public Transform        Root     { get; }
    public IReadOnlyList<Transform> PatrolPoints { get; }

    // Estado mutable: lectura pública, ESCRITURA CONTROLADA (setters privados)
    public int        PatrolIndex     { get; private set; }
    public int        PatrolDirection { get; private set; }  // +1 / -1 (PingPong)
    public float      StateTimer      { get; private set; }
    public Vector2Int Facing          { get; private set; }
    public Transform  DetectedPlayer  { get; private set; }  // null salvo detección vigente
    public bool       IsMoving        { get; private set; }

    // API de mutación pequeña y controlada (no es una bolsa de campos públicos)
    public void SetDetectedPlayer(Transform player);
    public void ClearDetectedPlayer();
    public void SetFacing(Vector2Int facing);
    public void AdvancePatrolPoint();     // aplica CreaturePatrolMath.NextIndex (PingPong)
    public void ResetStateTimer();
    public void AdvanceStateTimer(float deltaTime);
    public void SetMoving(bool moving);
}
```

- **Referencias permanentes vs estado mutable** están explícitamente separados. El estado mutable **no** se expone como campos públicos libremente modificables; solo se altera por la API anterior.
- **Local a una criatura**, construido y validado por `CreatureBrain`. Sin `static` mutable, sin singleton, sin `Find`, sin `DontDestroyOnLoad`, sin service locator.
- Objetivo: estados pequeños, dependencias centralizadas y validadas, testeabilidad.

## 7. `CreatureIdentity` (schema)

`sealed : ScriptableObject`, `[CreateAssetMenu(menuName = "SYNORA/Creature Identity")]`, campos `[SerializeField] private` + getters de solo lectura.

| Campo | Tipo | Uso M3 |
|---|---|---|
| `creatureId` | string | identidad estable |
| `displayName` | string | nombre visible |
| `description` | string `[TextArea]` | descripción de especie |
| `species` | string | clasificación |
| `biome` | string | bioma |
| `moveSpeed` | float | velocidad de patrulla |
| `idleDuration` | float | duración de Idle antes de patrullar |
| `patrolPauseDuration` | float | pausa ("respiración") al llegar a un punto |
| `detectionRadius` | float | radio de entrada a Alert |
| `loseRadius` | float | radio de permanencia en Alert (≥ detection) |
| `alertLingerDuration` | float | histéresis temporal al perder al Player |
| `arrivalThreshold` | float | distancia de arribo a punto |
| `isSymmetric` | bool | habilita Right por `flipX` |
| `spriteScale` | float | escala visual del sprite |

**Campos pospuestos (no se incluyen — evitar God ScriptableObject):** `shadowOffset`, `footstepType`, `ambientSound`, `canBeCaptured`, `canAttack`, `canFollow`, `canSleep`, `rotationSpeed`, estadísticas de combate, vínculo, afinidad, rareza, evolución.

**`OnValidate` (nunca lanza excepción):** `Trim` de `creatureId`/`displayName`/`species`/`biome`; `LogWarning` en ids/campos requeridos vacíos; `detectionRadius <= 0` → warning + clamp positivo; `loseRadius < detectionRadius` → warning + `loseRadius = detectionRadius`; `moveSpeed`/`idleDuration`/`patrolPauseDuration`/`alertLingerDuration < 0` → warning + clamp 0; `arrivalThreshold <= 0` y `spriteScale <= 0` → warning + clamp a mínimo positivo.

## 8. Estados y transiciones

```
Idle ──(timer ≥ idleDuration)──▶ Patrol ──(Player ≤ detectionRadius)──▶ Alert
  ▲                                 │  ▲                                   │
  └───────(pausa al llegar)─────────┘  └──(Player fuera de loseRadius + linger)┘
```

| Estado | Enter | Tick (retorno) | Exit |
|---|---|---|---|
| **IdleState** | `ResetStateTimer`; `Animator.PlayIdle`; `Movement.Stop`; `SetMoving(false)` | `AdvanceStateTimer`; si sensor detecta (`≤ detectionRadius`) → `Alert`; si `PatrolPoints` válidos y `StateTimer ≥ idleDuration` → `Patrol`; si no `null` | — |
| **PatrolState** | `Animator.PlayWalk`; `SetMoving(true)` | si sensor detecta → `Alert`; mueve hacia `PatrolPoints[PatrolIndex]`; si `CreaturePatrolMath.HasArrived` → `AdvancePatrolPoint` → `Idle` (pausa "respirar"); si no `null` | — |
| **AlertState** | `Movement.Stop`; `SetMoving(false)`; `Animator.PlayAlert`; encara al Player | evalúa `CreatureSensing`: dentro de `loseRadius` → resetea linger, `null`; fuera → `AdvanceStateTimer`; `StateTimer ≥ alertLingerDuration` → `Patrol`; mantiene encare | `ResetStateTimer` |

Transiciones permitidas exactamente: `Idle→Patrol`, `Idle→Alert`, `Patrol→Alert`, `Patrol→Idle`, `Alert→Patrol`. No existen clases ni transiciones para Follow/Attack/Flee/etc.

## 9. Movimiento

`CreatureMovement`: `[RequireComponent(typeof(Rigidbody2D))]`, `sealed`. Rigidbody2D **Dynamic**, `GravityScale = 0`, `FreezeRotationZ`. En `FixedUpdate` aplica `body.linearVelocity` hacia el objetivo indicado por el estado (`MoveTowards(target)` / `Stop()`); nunca escribe `Transform`; `OnDisable` pone velocidad a cero. Matemática determinista en método `static` testeable (dirección normalizada × `moveSpeed`, cero al llegar). Sin NavMesh, sin pathfinding, sin A*.

## 10. Patrulla PingPong

Lista serializada `Transform[] patrolPoints` (mín. recomendado 2, soporta N). Recorrido: `0→1→…→N-1→N-2→…→1→0`. Determinista; pausa Idle al llegar; sin destinos aleatorios; sin generación en runtime. Ver [ADR-M3-004](./ADR/ADR-M3-004-per-scene-creature-placement.md).

```csharp
public static class CreaturePatrolMath
{
    public static bool HasArrived(Vector2 current, Vector2 target, float arrivalThreshold);
    public static int  NextIndex(int index, int count, ref int direction); // PingPong: invierte direction en extremos
}
```

**Casos límite** (sin stuck detection compleja): 0 puntos → `LogError` + permanece en Idle; 1 punto → va y queda en Idle allí; referencia destruida → guard `IsAlive`, se salta y avanza, si no queda válida → Idle; dos puntos coincidentes → arribo inmediato, doble pausa breve, índice avanza (benigno); bloqueo contra Environment → mitigación por autoría (puntos en espacio libre, validado en preflight de Fase 6).

## 11. Sensor dual

`CreatureSensor`: una `Physics2D.OverlapCircle(origin, loseRadius, playerFilter, buffer)` por `FixedUpdate` (buffer reutilizado + `ContactFilter2D` con `useLayerMask`/`SetLayerMask(Player)`/`useTriggers`, patrón `InteractionDetector`). Expone presencia y distancia². La decisión vive en lógica pura. Ver [ADR-M3-003](./ADR/ADR-M3-003-dual-radius-sensor.md).

```csharp
public enum SensorVerdict { RemainCalm, BecomeAlert, RemainAlert, ReturnToPatrol }

public static class CreatureSensing
{
    // playerDistanceSqr < 0  ⇒  ningún Player dentro del loseRadius
    public static SensorVerdict Evaluate(
        bool currentlyAlert, float playerDistanceSqr,
        float detectionRadius, float loseRadius,
        float lingerElapsed, float alertLingerDuration);
}
```

Histéresis **espacial** (entra por `detectionRadius`, sale por `loseRadius ≥`) + **temporal** (`alertLingerDuration`; reingreso resetea). La física se consulta con el radio mayor (`loseRadius`).

## 12. `CreatureAnimator` (presenter)

API de comandos (no decide IA): `PlayIdle()`, `PlayWalk()`, `PlayAlert()`, `PlayReaction()`, `SetFacing(Vector2Int)`. Traduce a parámetros del Animator Controller (`FacingIndex`, `VisualState`) y `spriteRenderer.flipX` en código cuando `identity.isSymmetric`. Direcciones **Down / Up / Side(Left)**; **Right = flipX(Side)** si simétrico; sprites Right independientes para asimétricas futuras. El Controller no detecta al Player, no maneja timers, no cambia estados, no usa `StateMachineBehaviour` para IA. Ver [ADR-M3-005](./ADR/ADR-M3-005-animator-as-presenter.md).

## 13. Reaction

Pertenece al **estándar de animaciones**, no a la máquina lógica: **no** es estado, **no** existe `ReactionState`, **no** modifica Idle/Patrol/Alert, **no** es ataque/daño/huida. `CreatureAnimator` la soporta como **evento visual puntual** (one-shot). En M3 `CreatureBrain` **no** la invoca (no hay disparador aprobado). Distinción: *estado lógico* (`CreatureStateId`) ≠ *clip visual* (`.anim`) ≠ *evento visual puntual* (Reaction).

## 14. Integración concreta de Verak

- **Escena:** `ClaroExterior`. **Cantidad:** 2 Verak. **Condición:** ambos **libres**, no atrapados, sin cinemática, sin misión; primera demostración de ecosistema vivo.
- Comportamiento: patrulla lenta con pausas; al detectar al Player se detiene, lo observa y permanece Alert; vuelve a patrullar al alejarse.
- Ambos usan el **mismo prefab** y el **mismo `CreatureIdentity`**; cada uno con **ruta PingPong independiente** como datos de instancia de escena; rutas colocadas en espacio libre en Fase 6 y que no se crucen de forma que causen bloqueos constantes.
- Sin lore adicional, diálogos, vínculo, captura ni combate. Detalles funcionales en el [GDD delta](../Design/SYNORA_GDD_M3_Criaturas_v0.1.md).

## 15. Colocación por escena

Instancias del prefab autocontenido colocadas directamente en la escena; rutas de patrulla como Transforms/lista serializada por instancia; registro por lista serializada explícita, sin búsquedas en runtime. Sin spawner de runtime en M3. Ver [ADR-M3-004](./ADR/ADR-M3-004-per-scene-creature-placement.md).

## 16. Layers y colisiones (propuesta)

Nueva layer **`Creatures`**, índice tentativo **12** (verificado libre; sujeto a preflight read-only antes de tocar ProjectSettings en Fase 6). Matriz: Creature↔Environment **sólida**; Creature↔Player **sin colisión física**; sensado por trigger/layer mask. Ver [ADR-M3-002](./ADR/ADR-M3-002-creatures-layer-collision.md). **En Fase 0C no se modifica ProjectSettings.**

## 17. Reutilización de M2

Una criatura podrá tener opcionalmente `CreatureBrain` + `ExaminableInteractable` + `ExaminableData` (M2 sin cambios). **No** se crean `CreatureInteractable`, prompts, paneles ni textos nuevos. En M3 los Verak **no son observables obligatoriamente**; la arquitectura queda compatible para activarlo después sin código nuevo.

## 18. Arte y placeholders

El arte definitivo de Verak está **parcialmente disponible** (turnaround, Idle Down/Up/Side v1). Faltan o requieren refinamiento: Idle Right (si no se usa flipX), Walk, Alert, Reaction, limpieza de fondo/transparencia, eliminación de artefactos/marcas de generación, corte de frames, uniformidad de celda/pivot. **Los assets definitivos no bloquean la implementación**: placeholders técnicos aprobados; Fase 5 integra lo definitivo disponible + placeholders para lo faltante; sustituir placeholder por definitivo **no** requiere cambios en `CreatureBrain`/`CreatureMovement`/`CreatureSensor`. Categorías (ver estándar de arte): *arte conceptual/generado* → *sprite limpio listo para Unity* → *placeholder técnico* → *asset definitivo aprobado*.

## 19. Fases

| Fase | Contenido |
|---|---|
| 0A | Análisis read-only (hecho) |
| 0B | Revisión arquitectónica (hecho) |
| 0C | Documentación versionable (esta entrega) |
| 1 | Datos, contratos, `CreatureContext` y lógica pura + tests |
| 2 | `CreatureMovement` + tests |
| 3 | `CreatureSensor` + tests |
| 4 | `CreatureBrain` + estados + tests |
| 5 | Animación y pipeline de arte |
| 6 | Prefab + integración en escena (incl. preflight de layer/colisión) |
| 7 | QA y reporte |
| 8 | Build, commit final y tag `m3-complete` |

Cada fase se autoriza por separado; cada una termina con verificación e informe. No se inicia implementación sin autorización expresa.

## 20. Riesgos

1. Divergencia del patrón de estados vs M2 → mitigado: ADR-001 aprobado y acotado.
2. Arte parcial de Verak → placeholders aprobados; sustitución sin tocar lógica.
3. Parpadeo Alert↔Patrol → dos radios + linger.
4. Criatura atraviesa paredes → matriz de colisión (ADR-002) + preflight.
5. Límite de MCP para input real → QA de detección con Player real movido por el Director.
6. Introducir Animator (primer uso) → aislado en Fase 5.
7. Bloqueo contra Environment sin stuck detection → mitigación por autoría de rutas.
8. Alcance creep (Follow/Flee) → congelado por SPEC.

## 21. Supuestos

- Player en layer `Player` (8); el sensor detecta esa layer. (Confirmado en preflight.)
- Patrulla PingPong con puntos serializados por instancia (aprobado).
- Creature↔Player sin colisión física (aprobado).
- Verak simétrico → `flipX` (a confirmar contra arte definitivo).
- Placeholder graybox/parcial aceptable para cerrar M3 técnico (aprobado).

## 22. Lista de scripts esperados

Runtime (`Synora.Runtime`, `sealed`): `CreatureIdentity` (`Synora.Data`); en `Synora.Gameplay.Creatures`: `ICreatureState`, `CreatureStateId`, `IdleState`, `PatrolState`, `AlertState`, `CreatureBrain`, `CreatureContext`, `CreatureSensor`, `CreatureMovement`, `CreatureAnimator`, `CreaturePatrolMath` (static), `CreatureSensing` (static). Tests (`Synora.Tests`): `CreaturePatrolMathTests`, `CreatureSensingTests`, `CreatureBrainTests`, `CreatureMovementTests`.

## 23. Lista de assets

`Assets/Data/Creatures/Verak.asset` (`CreatureIdentity`); `Assets/Prefabs/Creatures/Verak.prefab`; `Assets/Animation/Creatures/Verak.controller` + clips `Verak_Idle_*`, `Verak_Walk_*`, `Verak_Alert_*` (+ `Verak_Reaction`); `Assets/Art/Creatures/Verak/…` (definitivos + placeholders, import con `PixelArtTexture.preset`); cambio en `TagManager.asset` (layer `Creatures`) y matriz 2D — **solo en Fase 6**; 2 instancias de `Verak.prefab` en `ClaroExterior.unity`.

## 24. Tests

Ver §22. EditMode NUnit puro para lógica (`CreaturePatrolMath`, `CreatureSensing`); reflexión estilo M2 para `CreatureBrain`/`CreatureMovement` donde haya que invocar ciclo de vida. Cubrir: arribo/PingPong; histéresis dual; transiciones permitidas y **ausencia** de transiciones prohibidas; instancias creadas una sola vez; velocidad `static`. Los 14 tests M1/M2 deben permanecer verdes.

## 25. QA

Verak patrulla autónomo (2 instancias independientes); Idle→Patrol; pausa por punto; Idle/Patrol→Alert con Player real; Alert detiene y encara; Alert→Patrol tras linger sin parpadeo; no persigue/ataca/huye/combate; no atraviesa paredes; no empuja al Player; animaciones correctas; sin persistencia/duplicados; GC por método sin alloc recurrente (Profiler, Director); consola 0/0.

## 26. Build

Tests verdes; Build Profile Windows sin cambios de plataforma; `ClaroExterior` en build settings; Build Succeeded 0/0; ejecución fuera del Editor con criaturas patrullando y Alert; Player.log limpio; sin crash dumps; `Builds/` ignorado.

## 27. Auditoría

Solo archivos del plan; sin `Find`/singletons/`DontDestroyOnLoad`; sin números mágicos (todo en `CreatureIdentity`); dependencias `[SerializeField] private` validadas; sin escritura de `Transform` para física; layer/matriz exactamente lo aprobado; M1/M2 intactos; commits convencionales sin atribución IA; repo limpio.

## 28. Definition of Done

Consola 0/0; Verak ×2 en `ClaroExterior` patrulla/Idle/Alert/retorno; sin comportamientos prohibidos; movimiento 2D simple sin NavMesh/pathfinding; animaciones Idle/Walk/Alert (+ Reaction soportada); sensor dual con histéresis verificada; `CreatureContext` validado en `Awake` con estado mutable encapsulado; estados polimórficos sin referencias cruzadas; cero GC recurrente; regresión M1+M2 intacta; tests nuevos verdes + 14 previos; build corre fuera del Editor; ADRs registrados; layer `Creatures` documentada; repo limpio tras commit final; tag `m3-complete` sobre el commit de validación.

## 29. Plan de commits

Uno por fase, conventional commits, identidad repo-local `MaxDL-GameDev`, sin atribución IA; el Director pushea manualmente; verificación contra el remoto real tras cada push. Fase 0C: `docs: define M3 creature architecture and design`.

## 30. Plan de tag

Tag anotado único de cierre **`m3-complete`** sobre el commit `chore: finalize M3 validation and build`, mensaje estilo previo: `M3 complete: creatures and living ecosystem`. Creado local, verificado, **pusheado por el Director** tras revisión. Sin firma GPG (consistente con `m0`/`m1`/`m2-complete`).

---

**Fin de SPEC M3 v0.1.** Documento de diseño; sin implementación autorizada.

# SYNORA — Informe de pruebas M3

> Documento de resultados de QA del hito M3 (Criaturas y Ecosistema Vivo — Verak). No modifica la SPEC M3 v0.1 ni el GDD M3 v0.1; solo registra resultados.

## 1. Entorno de ejecución

| Elemento | Valor |
|---|---|
| Editor | Unity 6000.5.3f1 |
| Render Pipeline | Universal RP 2D |
| Input System | com.unity.inputsystem |
| Test Framework | com.unity.test-framework (EditMode) |
| Plataforma | Windows (Editor, Mono) |
| Fecha de ejecución | 2026-07-22 |
| HEAD base | `e972aea` (feat: integrate Verak creatures into ClaroExterior) |
| SPEC de referencia | `SYNORA_SPEC_M3_Criaturas_v0.1.md` |
| GDD de referencia | `../Design/SYNORA_GDD_M3_Criaturas_v0.1.md` |
| ADRs | `ADR-M3-001..005` |

## 2. Alcance validado

Ecosistema vivo mínimo de M3: `CreatureIdentity`, contratos e implementación de estados polimórficos (`ICreatureState`, `IdleState`/`PatrolState`/`AlertState`), `CreatureContext`, lógica pura (`CreaturePatrolMath`, `CreatureSensing`), movimiento (`CreatureMovement`), sensor dual-radio (`CreatureSensor`), máquina de estados (`CreatureBrain`), presentación (`CreatureAnimationResolver`, `CreatureAnimator` + Animator Controller/clips), y la integración de **Verak ×2 en `ClaroExterior`** (layer `Creatures`, matriz de colisión, patrullas PingPong independientes). Regresión de M1/M2 y rendimiento (GC Alloc) incluidos. Fuera de alcance (no implementado): perseguir, atacar, huir, combatir, capturar, vincular.

## 3. Historial de commits M3

| Commit | Fase |
|---|---|
| `4c09c7b` | Fase 0C — documentación (SPEC/ADR/GDD/arte) |
| `00bdaec` | Fase 1 — datos, contratos, contexto y lógica pura |
| `ee1aac5` | Fase 2 — CreatureMovement |
| `7b9e540` | Fase 3 — CreatureSensor |
| `1cc684d` | Fase 4 — CreatureBrain + estados |
| `2e8b26f` | Fase 5 — presenter de animación + clips/controller |
| `e972aea` | Fase 6 — integración de Verak en ClaroExterior |

## 4. Pruebas automatizadas (EditMode)

Assembly `Synora.Tests.EditMode`. **139/139 · 0 failed · 0 skipped.** De ellas, **125 nuevas de M3** y 14 de regresión M1/M2.

| Suite | Tests | Cobertura |
|---|---|---|
| `CreaturePatrolMathTests` | 11 | Arribo (dentro/fuera/exacto/umbral inválido), PingPong (0/1/2/N puntos, rebote, dirección normalizada, índices en rango) |
| `CreatureSensingTests` | 12 | Histéresis dual-radio + linger; bordes exactos; reingreso; negativos; `lose<detection`; `ShouldResetLinger` |
| `CreatureContextTests` | 7 | Defaults, timer, detectedPlayer, facing, moving, PingPong index, clamp |
| `CreatureIdentityTests` | 5 | Valores válidos, clamps, `loseRadius≥detectionRadius`, trim, sin excepción en OnValidate |
| `CreatureMovementTests` | 14 | `ComputeVelocity` (velocidad, dt≤0, moveSpeed 0, no overshoot, direcciones), `ResolveFacing`, FixedTick (no escribe Transform) |
| `CreatureSensorTests` | 20 | OverlapCircle, dentro/fuera de radios, destruido/deshabilitado/layer, multi-collider→root estable, más cercano, desempate `EntityId`, OnDisable |
| `CreatureBrainTests` | 13 | Init idempotente, tick pre-init, sync, 3 transiciones, sin churn, desconocido seguro, OnDisable detiene Movement, sin FixedUpdate, 3 instancias reutilizadas |
| `IdleStateTests` | 6 | Stop+reset, timer, dt negativo, →Patrol, →Alert |
| `PatrolStateTests` | 7 | Destino actual, arribo+PingPong→Idle, 0/1/null puntos, →Alert, no mueve Transform |
| `AlertStateTests` | 8 | Stop+reset, remain en lose, linger→Patrol, reingreso resetea, no persigue, **conserva facing de entrada (4 direcciones), Player alrededor no cambia facing** |
| `CreatureAnimationResolverTests` | 8 | Idle/Walk/Alert por dirección, Alert ignora movimiento, fallbacks, FlipX false, determinista |
| `CreatureAnimatorViewTests` | 7 | Init idempotente, Refresh pre-init, aplica presentación, flipX false, animator/sprite null seguros, **no modifica contexto** |
| `VerakPrefabTests` | 7 | Prefab existe, componentes+layer Creatures, Rigidbody gravity0/rot congelada, controller+rootMotion off, refs de Brain/Movement/Sensor |

## 5. QA manual con Player real (aprobado por el Director)

Ejecutado por el Director en `ClaroExterior` (Play directo; Player en spawn Default). MCP no puede observar el render ni el tiempo real de forma fiable (throttle con Game View desenfocada), por lo que estos casos fueron validados manualmente.

| Caso | Descripción | Resultado |
|---|---|---|
| A — Arranque | Ambos Verak visibles, en Idle (respiración calmada ~1s), escala razonable (~1.4× Player), sin rectángulo de fondo, sin Missing Sprite, sin errores | ✅ |
| B — Patrulla | Tras el idle timer pasan a Walk (cadencia natural, sin patinaje excesivo), PingPong por sus puntos, rutas independientes, sin atravesar paredes, raíz suave (Interpolate) | ✅ |
| C — Alert | Al acercarse el Player se detienen, muestran Alert, **conservan la dirección de entrada** (sin snap frontal), **no persiguen**, el Player los **atraviesa sin empuje** | ✅ |
| D — Histéresis | Dentro de `loseRadius` sigue Alert; fuera + linger (~1.5s) vuelve a Patrol | ✅ |
| E — Independencia | Alertar a Verak_A no afecta a Verak_B | ✅ |
| F — Ciclo prolongado (~2 min) | Sin errores, sin `NullReferenceException`, sin drift ni estados trabados | ✅ |

## 6. Percepción, Alert e independencia (verificación runtime determinista, Fase 6)

Además del playthrough manual, en Play Mode se verificó de forma determinista (manipulando estado por reflexión): ambos Verak inicializan y arrancan en Idle; Idle→Patrol por timer; **Patrol→Alert** al entrar el Player en `detectionRadius`; **Alert conserva el facing de entrada** (Walk_Left→Alert_Left, sin salto a frontal) incluso con el Player rodeando; **Verak_B independiente** (sin detección) mientras Verak_A está en Alert (`distSqr`: A=4 detectado / B=−1). `CreatureContext.CurrentState`/`StateTimer`/`DetectedPlayer` como únicas fuentes de verdad.

## 7. Seguridad / robustez

| Validación | Resultado |
|---|---|
| Referencias null (Identity/Movement/Sensor/Animator) → no lanza, no inicializa, warning | ✅ |
| Patrulla: 0 puntos / 1 punto / punto null | ✅ Seguro (Idle / navega / se salta), sin excepción, sin índice fuera de rango |
| Player destruido / collider deshabilitado / layer incorrecta durante sensado | ✅ Detección limpiada de forma segura |
| `OnDisable` (Movement/Sensor/Brain) | ✅ Detiene movimiento / limpia detección, sin estado stale |
| Sin `Find`/`FindObjectsOfType`/singletons/`DontDestroyOnLoad`/`Time.timeScale` en Creatures | ✅ (inspección estructural) |
| Sin colisión física Creature↔Player; Creature↔Environment sí | ✅ (matriz verificada) |
| 0 Missing Scripts en escena y prefab | ✅ |

## 8. Regresión de M1 / M2

Los 14 tests originales de M1/M2 permanecen verdes (incluidos en los 139). En el playthrough, el movimiento del Player, cámara, colisiones, transiciones y la interacción/observación de M2 (Nodo en ClaroExterior) siguen operativos; la matriz de colisión solo añadió `Creatures` sin alterar combinaciones previas.

## 9. GC Alloc y rendimiento

**Medición por método realizada por el Director** con el Profiler de Unity (frames estables tras calentamiento, sin Deep Profile). Escenarios: patrulla con Player lejos, Alert, y ambos Verak activos.

| Método | GC Alloc / frame |
|---|---|
| `CreatureSensor.FixedUpdate` (Sense / OverlapCircle) | N/A¹ |
| `CreatureMovement.FixedUpdate` | N/A¹ |
| `CreatureBrain.Update` (Tick) | 0 B |
| `CreatureAnimator.LateUpdate` (Refresh) | 0 B |

¹ **N/A individual sample — sin GC.Alloc recurrente observado — PASS.** El Profiler no expuso una fila individual para esos métodos en el camino estable; no se observaron eventos `GC.Alloc` recurrentes atribuibles. **No se convierte "N/A" en un 0 B inventado ni se declara medición fallida.**

**Evidencia complementaria (inspección de código, no sustituye al Profiler):** `CreatureSensor` reutiliza `Collider2D[8]` + `ContactFilter2D` (una `OverlapCircle` non-alloc por FixedUpdate); `CreatureMovement.ComputeVelocity` y `ResolveFacing` son `static` sin asignaciones; `CreatureBrain` resuelve transiciones con un `Dictionary` construido una vez (token value-type, sin alloc por transición); `CreatureAnimator` cachea la última presentación y solo escribe parámetros al cambiar; sin LINQ ni colecciones por tick. **Resultado 9: PASS** — sin asignación positiva recurrente por frame.

## 10. Deuda visual aceptada (MVP, no bloqueante)

- **Clip Alert** = placeholder (frame estático); el arte definitivo de Alert queda pendiente. `CreatureAnimator`/máquina no dependen de él.
- **Baseline lateral** entre Idle-entrada y Walk: Left ~0.78u / Right ~0.41u; y un vaivén horizontal leve en `Walk_Right` (~0.19u, medido: centro de bbox por frame entre -0.66u y -0.84u respecto al centro de celda). Aceptado para el MVP; corrección definitiva en una futura pasada de arte. Down/Up estables.
- El teletransporte vertical del ciclo Walk quedó **resuelto** con los sprites re-alineados (baseline swing 0px).

### Incidencia resuelta post-QA

- **Sangrado de cola en `Walk_Right`**: se detectó que la punta de la cola de un frame invadía la celda contigua (píxeles cruzando los bordes x=512 / x=1024). Resuelto reemplazando **solo** el PNG `Verak_Walk_Right.png` por un re-export limpio (Gemini, bajo brief del Director) de 1536×1024 RGBA. Verificación objetiva: exactamente 3 bloques opacos por fila (uno por celda), **cero píxeles** en las columnas de borde x=511/512 y x=1023/1024, baseline por frame con variación ≤2px. Se conservó el `.meta` (mismos fileIDs, rects y pivots), por lo que el clip `Verak_Walk_Right.anim` mantiene sus 6 keyframes sin nulls y controller/prefab quedan intactos. El vaivén horizontal (~0.19u) **no** era parte de esta incidencia y persiste como deuda MVP.

## 11. Logs conocidos

- Logs transitorios del Test Framework (`IPrebuildSetup`/`IPostBuildCleanup`, `Saving results to TestResults.xml`).
- Warnings esperados por `LogAssert.Expect` de tests que pasan (id vacío de `CreatureIdentity`, layer vacío de `CreatureSensor`, estado desconocido de `CreatureBrain`, movement no asignado, y los 3 de `PlayerSpawner` de M1).
- **Consola estable final del proyecto: 0 errores / 0 warnings.**

## 12. Limitaciones reales de la herramienta

- **MCP no observa el render ni el tiempo real de forma fiable** (throttle de Play Mode con Game View desenfocada). El playthrough visual (casos A–F) y la cadencia fueron validados **manualmente por el Director**.
- **GC Alloc por método** medido con el Profiler por el Director (igual criterio que M1/M2). Ninguna validación quedó omitida: las dependientes de render/tiempo real o Profiler fueron ejecutadas por el Director.

## 13. Resultado final de Fase 7

- **Resultado: PASS.**
- **Incidencias abiertas: ninguna.**
- Pruebas automatizadas 139/139 · 0 failed · 0 skipped.
- QA manual (casos A–F), percepción/Alert/independencia, seguridad, regresión y GC Alloc: todos PASS.
- Consola estable 0 errores / 0 warnings; escena y prefab sin Missing Scripts.
- **Build final: aún no realizado.**
- **Tag `m3-complete`: aún no creado.**
- El cierre definitivo de M3 (build Windows, validación fuera del Editor, tag) pertenece a **Fase 8** y aún no se ha ejecutado.

---

**Fin del informe de pruebas M3.** Fase 7 (QA final, regresión, GC Alloc y reporte): **PASS**, sin incidencias abiertas. Build y tag pendientes de Fase 8.

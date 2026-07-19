# SYNORA — Informe de pruebas M2

> Documento de resultados de QA del hito M2 (Interacción contextual y observación básica). No modifica la SPEC M2 v0.3 ni el GDD M2 v0.1; solo registra resultados.

## 1. Entorno de ejecución

| Elemento | Valor |
|---|---|
| Editor | Unity 6000.5.3f1 |
| Render Pipeline | Universal RP 17.6.0 (URP 2D) |
| Input System | com.unity.inputsystem 1.19.0 |
| Test Framework | com.unity.test-framework |
| Plataforma | Windows (Editor, Mono) |
| Fecha de ejecución | 2026-07-19 |
| HEAD base | `0c1d030` (feat: integrate M2 examinables into scenes) |
| SPEC de referencia | `SYNORA_SPEC_M2_Interaccion_v0.3.md` |
| GDD de referencia | `SYNORA_GDD_M2_Interaccion_v0.1.md` |

## 2. Alcance validado

Interacción contextual y observación básica de M2: contratos e datos (`IInteractable`, `IInteractionReceiver`, `ExaminableData`), bloqueo de control (`PlayerControlGate`), detección frontal y selección determinista (`InteractionDetector`, `InteractionGeometry`, `InteractionSelector`, `InteractionTargetUtility`), flujo de estados (`InteractionController`, `InteractionInputReader`), UI graybox (`InteractionSceneRoot.prefab`, presentadores) y los tres examinables integrados en las tres áreas. Regresión de M1 y rendimiento (GC Alloc) incluidos.

## 3. Historial de commits M2

| Commit | Fase |
|---|---|
| `9aabcd5` | Fase 0 — documental |
| `30fe73a` | Fase 1 — input, datos y contratos |
| `848dd2e` | Fase 2 — PlayerControlGate |
| `105847d` | Fase 3 — detección y selección |
| `cc9f158` | Fase 4 — InteractionController y flujo de estados |
| `5b87e46` | Fase 5 — UI graybox (InteractionSceneRoot.prefab) |
| `0c1d030` | Fase 6 — integración de examinables en escenas |

## 4. Pruebas automatizadas (EditMode)

Assembly `Synora.Tests.EditMode`. **Exactamente 14 pruebas.** Resultado: **14/14 · 0 failed · 0 skipped** (~1.07 s).

| # | Prueba | Resultado |
|---|---|---|
| 1 | `InteractionGeometryTests.InteractionGeometry_CandidateBehind_ReturnsFalse` | ✅ Passed |
| 2 | `InteractionGeometryTests.InteractionGeometry_CandidateBeyondRange_ReturnsFalse` | ✅ Passed |
| 3 | `InteractionSelectorTests.InteractionSelector_HigherPriorityWins` | ✅ Passed |
| 4 | `InteractionSelectorTests.InteractionSelector_EqualPriorityNearestWins` | ✅ Passed |
| 5 | `InteractionSelectorTests.InteractionSelector_EqualPriorityAndDistanceOrdinalIdWins` | ✅ Passed |
| 6 | `InteractionSelectorTests.InteractionSelector_CurrentValidTargetIsPreserved` | ✅ Passed |
| 7 | `InteractionSelectorTests.InteractionSelector_InvalidCurrentTargetSelectsReplacement` | ✅ Passed |
| 8 | `PlayerControlGateTests.PlayerControlGate_ObservationBlockStopsMotorAndClearsVelocity` | ✅ Passed |
| 9 | `InteractionControllerTests.InteractionController_SinglePressOpensWithoutImmediateClose` | ✅ Passed |
| 10 | `InteractionControllerTests.InteractionController_SecondSeparatePressClosesAndRestoresControl` | ✅ Passed |
| 11 | `PlayerMotorTests.CalculateVelocity_DiagonalInputDoesNotIncreaseSpeed` | ✅ Passed (M1) |
| 12 | `PlayerMotorTests.CalculateVelocity_InputAtOrAboveUnitMagnitudeUsesMoveSpeed` | ✅ Passed (M1) |
| 13 | `TransitionSystemTests.PlayerSpawner_AfterSpawn_ConsumesContextAndClearsVelocity` | ✅ Passed (M1) |
| 14 | `TransitionSystemTests.TryLoad_WhenAlreadyLoading_RejectsWithoutChangingContext` | ✅ Passed (M1) |

## 5. QA manual con tecla E física (aprobado por el Director)

Las pulsaciones de **E** fueron **físicas del Director** en la Game View enfocada (no simuladas por reflexión). Claude preparó el estado y verificó cada transición.

| Escena | Coordenada | interactionId | Aproximación | Prompt | 1ª E (abrir) | Bloqueo movimiento | Bloqueo orientación | 2ª E (cerrar) | Restauración | Resultado |
|---|---|---|---|---|---|---|---|---|---|---|
| CamaraPreservacion | (0.5,-3.0) | `camara_preservacion.terminal_diagnostico` | Norte, mirando Sur | `[E] Examinar` | `ObservationOpen`, panel abierto, prompt oculto, gate bloqueado | Sin movimiento; velocidad 0 | Facing sin cambio | Panel cerrado, gate desbloqueado | `ExploringWithTarget`, prompt visible | ✅ |
| CorredorTecnico | (0,2.5) | `corredor_tecnico.panel_mantenimiento` | Sur, mirando Norte | `[E] Examinar` | `ObservationOpen`, panel abierto, prompt oculto, gate bloqueado | Sin movimiento; velocidad 0 | Facing sin cambio | Panel cerrado, gate desbloqueado | `ExploringWithTarget`, prompt visible | ✅ |
| ClaroExterior | (3,3) | `claro_exterior.nodo_inactivo` | Sur, mirando Norte | `[E] Examinar` | `ObservationOpen`, panel abierto, prompt oculto, gate bloqueado | Sin movimiento; velocidad 0 | Facing sin cambio | Panel cerrado, gate desbloqueado | `ExploringWithTarget`, prompt visible | ✅ |

`Time.timeScale == 1` en todos los estados. `CloseHint` estático `"[E] Cerrar"`. Al girar en dirección contraria el prompt se ocultó (`Candidates=0`, `ExploringWithoutTarget`) en las tres escenas.

**Textos exactos mostrados (verificados en runtime):**

- **CamaraPreservacion — TERMINAL DE DIAGNÓSTICO:**
  > La unidad reconoce actividad biológica, pero no puede validar la identidad del sujeto.
  >
  > Varias funciones permanecen suspendidas.

- **CorredorTecnico — PANEL DE MANTENIMIENTO:**
  > La red secundaria sigue recibiendo energía, pero la conexión principal está interrumpida.
  >
  > El daño no parece reciente.

- **ClaroExterior — NODO INACTIVO:**
  > La estructura conserva una carga mínima.
  >
  > Algo en el entorno parece responder a su presencia, aunque el nodo no emite ninguna señal reconocible.

## 6. Nodo inactivo — acceso multidireccional

Detección y prompt `[E] Examinar` confirmados desde las cuatro cardinales (movimiento/orientación reales del Director en el recorrido y verificación por aproximación):

| Aproximación | Candidato | Prompt |
|---|---|---|
| Sur mirando Norte | `nodo_inactivo` | ✅ |
| Norte mirando Sur | `nodo_inactivo` | ✅ |
| Oeste mirando Este | `nodo_inactivo` | ✅ |
| Este mirando Oeste | `nodo_inactivo` | ✅ |

## 7. Seguridad

| Validación | Resultado |
|---|---|
| Destrucción runtime del target activo (CamaraPreservacion/ClaroExterior) | ✅ Sin `MissingReferenceException` ni `NullReferenceException` |
| Descarte seguro del target destruido | ✅ `currentTarget` descartado; `ExploringWithoutTarget`; prompt oculto; `Candidates=0` |
| `InteractionTargetUtility.IsAlive` antes de leer propiedades | ✅ (protege ante `UnityEngine.Object` destruido) |
| Teardown con panel abierto (stop Play con `ObservationOpen`) | ✅ `OnDisable` sin excepciones ni warnings |
| `OnDisable` null-safe e idempotente; solo `Unblock(Observation)`; sin `ClearAll` | ✅ (inspección de código) |
| `Time.timeScale` | ✅ Siempre en 1 |

## 8. Recorrido real mediante teclado (opción B, aprobado por el Director)

Ruta caminada con movimiento real del Player, transiciones normales (sin `SceneManager`, sin teletransporte):

`CamaraPreservacion → CorredorTecnico → ClaroExterior → CorredorTecnico → CamaraPreservacion`

| Checkpoint | Escena cargada | SpawnPoint usado | Autonomía |
|---|---|---|---|
| A | CorredorTecnico | `FromCamaraPreservacion` | ✅ |
| B | ClaroExterior | `FromCorredorTecnico` | ✅ |
| C | CorredorTecnico | `FromClaroExterior` | ✅ |
| D | CamaraPreservacion | `FromCorredorTecnico` | ✅ |

En cada carga: exactamente 1 Player / 1 InteractionSceneRoot / 1 ExaminableInteractable; `controller.gate`, `detector.playerOrientation` y `detector.originPoint` apuntan al Player local; `detector.sceneExaminables` contiene únicamente el examinable local (`interactableLayer == 1<<11`); **sin referencias cruzadas entre escenas**; **sin persistencia** de panel, prompt, target ni gate; `Rigidbody2D.linearVelocity` limpia al aparecer; cámara y `CameraBounds2D` presentes; `Time.timeScale == 1`; **sin duplicados de InteractionSceneRoot**; consola 0/0. Ningún script de M2 usa `DontDestroyOnLoad`.

## 9. GC Alloc y rendimiento

**Medición por método realizada por el Director** con el Profiler de Unity (CPU Usage / GC Alloc), frames estables tras calentamiento, sin Deep Profile. Escenarios: A (exploración sin target), B (target y prompt estables), C (panel abierto estable), D (panel cerrado y target recuperado). Valores exactos entregados por el Director:

| Método | A | B | C | D |
|---|---|---|---|---|
| `InteractionDetector.FixedUpdate` | 0 B | 0 B | 0 B | 0 B |
| `InteractionController.Update` | 0 B | 0 B | N/A¹ | N/A¹ |
| `PlayerMotor.FixedUpdate` | 0 B | 0 B | N/A¹ | N/A¹ |
| `PlayerOrientation.Update` | 0 B | 0 B | 0 B | 0 B |
| `CameraFollow.LateUpdate` | 0 B | 0 B | 0 B | 0 B |

¹ **N/A individual sample — no recurring GC.Alloc observed — PASS.** Muestra individual no expuesta por Hierarchy en el camino estable de bajo costo/retorno temprano. No se observaron eventos GC.Alloc recurrentes atribuibles durante los frames analizados. PASS.

**Nota sobre las celdas N/A¹ — limitación de granularidad del Profiler (aceptada por el Director):** en el camino estable de bajo costo/retorno temprano, el Profiler Hierarchy no expuso una fila individual para esos métodos. **No se convierte "N/A" en un 0 B numérico inventado, no se clasifica como medición fallida y no se declara Call Count > 0** cuando el Profiler no mostró la fila. No se observaron eventos `GC.Alloc` recurrentes atribuibles durante los frames analizados.

- **Escenario C:** `InteractionController.Update` retorna inmediatamente en `ObservationOpen`. `PlayerMotor.FixedUpdate` pone `linearVelocity` en `Vector2.zero` y retorna debido al `PlayerControlGate`.
- **Escenario D:** `InteractionController.Update`/`RefreshTarget` retorna rápidamente porque el target no cambió. `PlayerMotor.FixedUpdate` no mostró una muestra individual, pero tampoco se observaron eventos `GC.Alloc` recurrentes atribuibles.

Los valores **0 B** conservados corresponden exclusivamente a los métodos/escenarios que sí aparecieron como muestras individuales en el Profiler. **Resultado 7G: PASS** — ninguna asignación positiva recurrente por frame en ningún método/escenario.

### 9.1 Inspección de código (evidencia complementaria)

Verificado sobre el código commiteado (`grep` + revisión); no sustituye la medición del Profiler:

- `InteractionDetector.FixedUpdate`: reutiliza `candidateBuffer` (`Clear()`) y `overlapBuffer` (`Physics2D.OverlapBox`, no `OverlapBoxAll`); dedup lineal con `ReferenceEquals`. Los `new List/Dictionary`, `HashSet` y `GetComponents<Collider2D>()` están en **inicializadores de campo o `Awake`**, nunca por frame.
- `InteractionSelector.SelectTarget`: sin colecciones nuevas, sin LINQ, sin `Sort`, sin closures, sin `GetInstanceID`.
- `InteractionController.Update`/`RefreshTarget`: sin asignaciones; en estado estable con el mismo target retorna sin tocar UI.
- `InteractionInputReader`: event-driven, sin `Update`. `InteractionPromptPresenter`: concatena `[E]` solo al cambiar el texto. `ObservationPanelPresenter`: escribe texto solo al abrir.
- Sin `Find`/`FindObjectsOfType`/`DontDestroyOnLoad`/`Time.timeScale` en los scripts de interacción.

## 10. Regresión de M1

| Área | Resultado |
|---|---|
| Movimiento (PlayerMotor) — ruta M1 con gate desbloqueado | ✅ Sin cambios; `CalculateVelocity` intacto |
| Orientación (PlayerOrientation) — `Resolve` intacto | ✅ |
| Cámara / CameraBounds2D | ✅ Funcionando en el recorrido |
| Transiciones (AreaTransition / SceneLoader / SceneTransitionContext) | ✅ Bidireccionales, contexto consumido |
| SpawnPoints | ✅ Spawn correcto en cada checkpoint |
| `PlayerControlGate` (M2) — no afecta el movimiento sin bloqueo | ✅ |
| Cuatro pruebas originales de M1 | ✅ Intactas (pruebas 11–14, 4/4) |

## 11. Logs conocidos

- Logs transitorios del Test Framework durante las corridas (`IPrebuildSetup`/`IPostBuildCleanup` de PerformanceTesting, `TestResults.xml` en LocalLow).
- Tres warnings esperados de `PlayerSpawner.OnValidate` en la prueba 13, **declarados con `LogAssert.Expect`** (salida interna de un test que pasa; no son bugs de producción).
- **Consola estable final del proyecto: 0 errores / 0 warnings.**

## 12. Limitaciones reales de la herramienta

- **MCP no pudo inyectar la tecla E ni movimiento sostenido real.** El Director ejecutó **físicamente** las pulsaciones de E (7C/7E) y el recorrido completo con teclado (7F). Estas pruebas **se completaron manualmente**, no fueron omitidas.
- **MCP no ofreció GC Alloc atribuible por método** (solo el contador global `GC Allocated In Frame`, que incluye overhead del Editor y no es de gameplay). La **medición por método se realizó con el Profiler de Unity por el Director** (§9), igual criterio que en M1.

Ninguna limitación implica una prueba omitida: todas las validaciones dependientes de input físico o del Profiler fueron ejecutadas por el Director.

## 13. Resultado final de Fase 7

- **Resultado: PASS.**
- **Incidencias abiertas: ninguna.**
- Pruebas automatizadas 14/14 · 0 failed · 0 skipped.
- QA manual con E física, bloqueo/restauración, seguridad, teardown, recorrido y GC Alloc: todos PASS.
- Consola estable 0 errores / 0 warnings; Bootstrap y las tres escenas no dirty.
- **Build final: aún no realizado.**
- **Tag `m2-complete`: aún no creado.**
- El cierre definitivo de M2 (build, recorrido fuera del Editor, tag) pertenece a **Fase 8** y aún no se ha ejecutado.

---

**Fin del informe de pruebas M2.** Fase 7 (QA final, regresión, GC Alloc y reporte): **PASS**, sin incidencias abiertas. Build y tag pendientes de Fase 8.

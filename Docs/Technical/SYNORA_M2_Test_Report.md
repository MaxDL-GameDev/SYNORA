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
- El build final Windows y la validación fuera del Editor se documentan en **§14 (Fase 8A)**.

## 14. Fase 8A — Build final Windows y validación fuera del Editor

Base del build: commit `f896bbb` (docs: add M2 QA report), `main`, working tree limpio. No se modificaron código, escenas, prefabs, assets, ProjectSettings ni Packages para el build.

### 14.A Build final Windows

| Elemento | Valor |
|---|---|
| Fecha/hora del artefacto (`SYNORA.exe`) | 2026-07-19 15:14:46 (-06:00) |
| Editor | Unity 6000.5.3f1 |
| Plataforma | Standalone Windows 64-bit / **x86_64** |
| Scripting Backend | **Mono** (Mono2x) |
| Development Build | Off |
| Script Debugging | Off |
| Deep Profiling | Off |
| Autoconnect Profiler | Off |
| Escenas y orden | 0 Bootstrap · 1 CamaraPreservacion · 2 CorredorTecnico · 3 ClaroExterior |
| Resultado | **Build Succeeded** · 0 errores · 0 warnings inesperados |
| Duración | 30.3 s |
| Tamaño total (filesystem, `Builds/Windows`) | **106 694 334 bytes = 101.75 MiB** |
| Tamaño total (Unity Build Report) | 101.51 MB (dato informado por el build report; el valor de disco es el de la fila anterior) |
| Nº de archivos (filesystem) | **201** (200 + 1 en `*_BurstDebugInformation_DoNotShip`, símbolos Burst no distribuibles) |
| `SYNORA.exe` | **667 648 bytes** |
| SHA-256 `SYNORA.exe` | `5ae0c84993c6bff7d0f03166cafd148cdec61f67ec85b5d97600e04b2abb26e4` |
| Backend Mono confirmado por | presencia de `MonoBleedingEdge/` (+ `SYNORA_Data/`, `UnityCrashHandler64.exe`, `UnityPlayer.dll`) |

Durante el build apareció el mensaje informativo conocido de URP (`"1 URP assets included in build"`), idéntico al de M1. **No quedó como warning estable:** la consola posterior al build quedó en **0 errores / 0 warnings**.

### 14.B Validación standalone 1280×720

Ejecución **fuera del Editor**, ventana 1280×720, recorrido completo confirmado por el Director:

`CamaraPreservacion → CorredorTecnico → ClaroExterior → CorredorTecnico → CamaraPreservacion`

| Comprobación | Resultado |
|---|---|
| Interacción con los tres examinables (terminal, panel, nodo) | ✅ |
| Prompt `[E] Examinar` visible y sin recorte | ✅ |
| Panel y textos legibles | ✅ |
| `[E] Cerrar` visible | ✅ |
| Ausencia de clipping | ✅ |
| Bloqueo y restauración de control | ✅ |
| Movimiento, orientación, cámara y colisiones | ✅ |
| Transiciones y spawns | ✅ |
| Ausencia de persistencia entre escenas | ✅ |
| Ausencia de duplicados | ✅ |
| Pixel art sin filtrado ni deformación inesperada | ✅ |
| **Resultado** | **PASS** |

### 14.C Player.log 1280×720

| Elemento | Valor |
|---|---|
| Ruta | `Builds/Logs/M2_1280x720_Player.log` |
| Nº de líneas | 56 |
| Cargas/descargas observadas | 6 ciclos de `Unloading` (= cargas del recorrido) |
| Excepciones / errores | ninguno |
| Referencias missing / NullReference | ninguna |
| Asserts / crash / failed / warning inesperado | ninguno |
| Cierre | normal, exit code 0 |
| **Resultado** | **PASS** |

### 14.D Validación standalone 1920×1080

Mismo recorrido, ventana 1920×1080, confirmado por el Director:

| Comprobación | Resultado |
|---|---|
| Prompt centrado | ✅ |
| Panel centrado | ✅ |
| Título y cuerpo legibles | ✅ |
| Hint `[E] Cerrar` visible | ✅ |
| Sin clipping | ✅ |
| Sin elementos fuera de pantalla | ✅ |
| Sin solapamientos incorrectos | ✅ |
| Apertura / cierre | ✅ |
| Bloqueo / restauración | ✅ |
| Transiciones y spawns | ✅ |
| Ausencia de persistencia y duplicados | ✅ |
| **Resultado** | **PASS** |

### 14.E Player.log 1920×1080

| Elemento | Valor |
|---|---|
| Ruta | `Builds/Logs/M2_1920x1080_Player.log` |
| Nº de líneas | 56 |
| Cargas/descargas observadas | 6 ciclos de `Unloading` |
| Excepciones / errores / missing / NullReference | ninguno |
| Asserts / crash / failed / warning inesperado | ninguno |
| Cierre | normal, exit code 0 |
| **Resultado** | **PASS** |

### 14.F Regresión de M1 en la build

Verificada en ambos recorridos fuera del Editor (confirmado por el Director):

| Área | Resultado |
|---|---|
| Movimiento | ✅ |
| Orientación | ✅ |
| Colisiones | ✅ |
| Cámara | ✅ |
| `CameraBounds2D` | ✅ |
| Transiciones bidireccionales | ✅ |
| `SceneTransitionContext` | ✅ |
| SpawnPoints correctos | ✅ |
| Limpieza de velocidad al aparecer | ✅ |
| **Resultado (1280×720 y 1920×1080)** | **PASS** |

### 14.G Artefactos y crashes

- Artefacto ubicado bajo `Builds/Windows/` (201 archivos, `SYNORA.exe` + `SYNORA_Data/` + `MonoBleedingEdge/` + `UnityCrashHandler64.exe` + `UnityPlayer.dll`).
- `Builds/` está **ignorado por Git**; el artefacto **no forma parte del repositorio**.
- Logs bajo `Builds/Logs/` (`M2_1280x720_Player.log`, `M2_1920x1080_Player.log`), también ignorados.
- **Sin crash dumps** asociados a SYNORA en ninguna de las dos ejecuciones.

### 14.H Resultado final de M2

| Ítem | Resultado |
|---|---|
| Pruebas automatizadas | **14/14 PASS** (0 failed, 0 skipped) |
| QA dentro del Editor | **PASS** |
| QA standalone 1280×720 | **PASS** |
| QA standalone 1920×1080 | **PASS** |
| Player.log (ambas resoluciones) | **PASS** |
| Regresión M1 | **PASS** |
| Build final | **completado** |
| Incidencias abiertas | **ninguna** |
| **Resultado de M2** | **PASS** |

**Tag de cierre previsto: `m2-complete`.** Se creará sobre el commit final de validación después de publicar y verificar dicho commit en el remoto.

---

**Fin del informe de pruebas M2.** Fase 7 (QA final, regresión, GC Alloc) y Fase 8A (build final Windows + validación fuera del Editor a 1280×720 y 1920×1080): **PASS**, sin incidencias abiertas. **Resultado de M2: PASS.** El tag `m2-complete` aún no se ha creado ni publicado.

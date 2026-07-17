# SYNORA — Informe de pruebas M1

> Documento de resultados de pruebas del hito M1 (Movimiento, cámara, colisiones y transición entre áreas). No modifica la SPEC v0.4 ni la enmienda TEC-001; solo registra resultados.

## 1. Entorno de ejecución

| Elemento | Valor |
|---|---|
| Editor | Unity 6000.5.3f1 |
| Render Pipeline | Universal RP 17.6.0 (URP 2D) |
| Input System | com.unity.inputsystem 1.19.0 |
| Test Framework | com.unity.test-framework 1.7.0 |
| Plataforma | Windows (Standalone, Mono) |
| Fecha de ejecución | 2026-07-17 |

## 2. Pruebas automáticas (SPEC D2)

Assembly de pruebas: `Synora.Tests.EditMode` (Edit Mode). **Exactamente 4 pruebas.**

| # | Prueba | Cubre (D2) | Resultado |
|---|---|---|---|
| 1 | `PlayerMotorTests.CalculateVelocity_DiagonalInputDoesNotIncreaseSpeed` | Normalización diagonal | ✅ Passed |
| 2 | `PlayerMotorTests.CalculateVelocity_InputAtOrAboveUnitMagnitudeUsesMoveSpeed` | Cálculo de velocidad | ✅ Passed |
| 3 | `TransitionSystemTests.TryLoad_WhenAlreadyLoading_RejectsWithoutChangingContext` | Bloqueo de cargas duplicadas | ✅ Passed |
| 4 | `TransitionSystemTests.PlayerSpawner_AfterSpawn_ConsumesContextAndClearsVelocity` | Consumo y limpieza del contexto | ✅ Passed |

**Ejecuciones:**
- Fase 7 — Ejecución 1: **4/4** · Ejecución 2: **4/4** (0 failed, 0 skipped) ~0.61 s c/u.
- Fase 8 — Reejecución final (tras eliminar el harness temporal): **4/4** (0 failed, 0 skipped) ~0.61 s.

**Notas de logs durante la corrida (no son bugs de producción):** la prueba 4 dispara `OnValidate` de PlayerSpawner al `AddComponent` (3 warnings esperados, declarados con `LogAssert.Expect`); el Test Framework emite logs de infraestructura (PerformanceTesting, `TestResults.xml` en LocalLow). **Consola estable del proyecto: 0 errores / 0 warnings.**

## 3. Pruebas manuales realizadas (aprobadas por el Director)

| Prueba | Resultado |
|---|---|
| Movimiento WASD y flechas | ✅ |
| Normalización diagonal (no más rápida) | ✅ |
| Detención inmediata al soltar | ✅ |
| Alt+Tab sin input pegado | ✅ |
| Orientación cardinal (4 direcciones) | ✅ |
| Colisiones contra muros, esquinas (L) y obstáculos | ✅ |
| Deslizamiento limpio contra paredes | ✅ |
| Cámara a 1280×720 | ✅ |
| Cámara a 1920×1080 | ✅ |
| Habitación menor que el viewport (centrado) | ✅ |
| Recorrido bidireccional de las tres áreas (en Editor) | ✅ |
| Escenas abiertas en aislamiento (usan Default) | ✅ |
| Solicitud duplicada de transición (una sola carga) | ✅ |
| Consola 0 errores / 0 warnings (edición/play) | ✅ |

## 4. Decisiones y observaciones aceptadas

- **Reference Resolution del Pixel Perfect Camera: 320×180 (provisional para M1).** La resolución artística definitiva queda pendiente de pruebas con arte real.
- **Residual visual superior ~0.125 unidades** aceptado para el graybox de M1; se resolverá con arte real vía foreground/occlusion/render sorting, sin alterar física.
- **Campos `m_SpriteSheet` vacíos** en `PixelArtTexture.preset`: inofensivos.
- **Mensaje de preprocesamiento de URP** (`"N URP assets included in build"`): informativo, no warning persistente.
- **Transición entre áreas por corte directo (sin fundido)** conforme a SPEC B1; fundido diferido a hito posterior con UI.

## 5. Frame rate — SPEC D1/D3 — APROBADO

Medición con harness temporal (`FrameRateValidationProbe`, ya eliminado del repo). `vSyncCount=0` durante la medición (restaurado al finalizar). Cada medición: 2 s de calentamiento + 500 pasos de FixedUpdate (Fixed Timestep 0.02 → 10 s simulados), aplicando `PlayerMotor.CalculateVelocity(Vector2.right, 4.5)`.

| Target FPS | FPS promedio real | Frames | Pasos físicos | Tiempo real | Distancia |
|---|---|---|---|---|---|
| 30 | 29.1 | 292 | 500 | 10.02 s | 45.0001 |
| 60 | 58.3 | 583 | 500 | 10.00 s | 45.0001 |
| 120 | 104.1 | 1040 | 500 | 9.99 s | 45.0001 |

- Distancia esperada ≈ **45.00** unidades.
- `differencePercent = (max − min) / mean × 100 = (45.0001 − 45.0001) / 45.0001 × 100 =` **0.0000 %**.
- Criterio ≤ 1 % → **PASS**. El movimiento es consistente y desacoplado de la tasa de cuadros.
- Nota honesta: el target de 120 FPS rindió **104.1 FPS reales** (límite del entorno de prueba). Como el movimiento es por física a paso fijo, la distancia es idéntica (45.0001) igualmente → el criterio de desacople se cumple con independencia del render alcanzado.

## 6. GC Alloc del movimiento — SPEC D1 — APROBADO

Profiler (CPU Usage / GC Alloc) en CamaraPreservacion, tras calentamiento, con movimiento continuo ≥ 10 s (medición del Director):

| Componente | GC Alloc |
|---|---|
| `PlayerInputReader` | Funciona mediante callbacks; no presenta ejecución recurrente por frame ni asignaciones periódicas observadas. |
| `PlayerMotor.FixedUpdate` | 0 B/frame |
| `PlayerOrientation.Update` | 0 B/frame |
| `CameraFollow.LateUpdate` | 0 B/frame |

Resultado: **sin asignaciones por frame** atribuibles al movimiento y seguimiento. (Coherente con la auditoría de código: solo *value types*, sin asignaciones ni logs por frame.)

## 7. Build final de Windows

| Campo | Valor |
|---|---|
| Resultado | **Build Succeeded** |
| Errores (BuildReport) | 0 |
| Warnings (BuildReport) | 0 |
| Duración | 41.1 s |
| Tamaño total | 101.47 MB |
| Scripting backend | Mono |
| Escenas | 0 Bootstrap · 1 CamaraPreservacion · 2 CorredorTecnico · 3 ClaroExterior |
| Ruta de salida | `Builds/Windows/SYNORA.exe` (ignorada por Git) |

El mensaje informativo de preprocesamiento de URP se registra aparte; no es un warning persistente.

## 8. Ejecución fuera del Editor

- `SYNORA.exe` arranca fuera del Editor, vivo sin crash, cerrado limpio; sin crash dumps.
- Arranque verificado: Bootstrap carga CamaraPreservacion (swap de escena confirmado en `Player.log`).
- **Player.log** (`%USERPROFILE%\AppData\LocalLow\SynoraDev\SYNORA Prototipo\Player.log`): **sin excepciones propias de SYNORA, sin errores de carga, sin referencias faltantes, sin crashes**. PhysX e Input inicializados normalmente.
- **Recorrido WASD completo en la build — APROBADO por el Director.** Ruta: CamaraPreservacion → CorredorTecnico → ClaroExterior → CorredorTecnico → CamaraPreservacion, a 1280×720 y 1920×1080. Verificado: movimiento correcto, diagonales sin aceleración, colisiones correctas, cámara y pixel-perfect correctos, spawns correctos, sin transición inmediata de regreso, sin cargas duplicadas al mantener movimiento contra los triggers, sin freeze ni crash, cierre limpio del ejecutable.

## 9. Definition of Done de M1 (SPEC B10 / D4)

| Criterio | Estado |
|---|---|
| Movimiento 4 direcciones (WASD y flechas); diagonal no más rápida | ✅ (test + manual) |
| Velocidad constante e independiente del frame rate | ✅ (frame rate 0.0000 %) |
| No atraviesa muros/mobiliario/obstáculos; desliza limpio | ✅ (manual) |
| Cámara con seguimiento (suavizado 0) sin jitter, sin salir de la sala, centrada si sala < viewport | ✅ (manual) |
| Imagen pixel-perfect a 1080p sin suavizado | ✅ (manual) |
| Recorrido Cámara→Corredor→Claro y regreso, aparición correcta en ambos sentidos | ✅ (Editor + build, aprobado por el Director) |
| Cada escena de área jugable en aislamiento | ✅ (manual) |
| Build de Windows compila y arranca fuera del Editor | ✅ (0/0, arranca sin crash) |
| Build recorre las tres áreas fuera del Editor | ✅ (recorrido completo aprobado por el Director) |
| Sin errores/warnings en consola | ✅ |
| Sin `GameObject.Find`/`FindObjectOfType`/búsquedas globales | ✅ (auditoría) |
| Sin números mágicos de balance fuera de config | ✅ (MoveSpeed en `PlayerMovement.asset`) |
| Sin responsabilidades mezcladas / código no usado / TODO | ✅ (auditoría) |
| GC Alloc del movimiento 0 B/frame | ✅ |
| 4 pruebas automáticas 4/4 | ✅ |

**Definition of Done de M1: COMPLETA Y APROBADA.** Todos los criterios verificados.

---

**Fin del informe de pruebas M1.** Pruebas automáticas 4/4 (×3 corridas). Frame rate 0.0000 % (PASS). GC Alloc sin asignaciones por frame. Build 0/0. Recorrido completo fuera del Editor aprobado. Definition of Done de M1 completa.

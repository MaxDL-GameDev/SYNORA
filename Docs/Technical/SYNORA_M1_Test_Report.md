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

**Dos ejecuciones consecutivas:**
- Ejecución 1: **4/4** (4 passed, 0 failed, 0 skipped) — ~0.61 s.
- Ejecución 2: **4/4** (4 passed, 0 failed, 0 skipped) — ~0.61 s.

**Notas de logs durante la corrida (no son bugs de producción):**
- La prueba 4 crea un `PlayerSpawner` con `AddComponent`, lo que dispara `OnValidate` con los campos aún sin cablear y emite 3 warnings de validación esperados. Se declaran como esperados con `LogAssert.Expect` (no hacen fallar la prueba). Se limpian tras la corrida.
- El Test Framework emite logs de infraestructura propios en cada corrida (`IPrebuildSetup`/`IPostBuildCleanup` de PerformanceTesting y el guardado de `TestResults.xml` en `LocalLow`). No provienen de código de SYNORA.
- **Consola estable del proyecto (fuera de la corrida de tests): 0 errores / 0 warnings.**

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
| Recorrido bidireccional de las tres áreas | ✅ |
| Escenas abiertas en aislamiento (usan Default) | ✅ |
| Solicitud duplicada de transición (una sola carga) | ✅ |
| Consola 0 errores / 0 warnings (edición/play) | ✅ |

## 4. Decisiones y observaciones aceptadas

- **Reference Resolution del Pixel Perfect Camera: 320×180 (provisional para M1).** La resolución artística definitiva queda pendiente de pruebas con arte real.
- **Residual visual superior ~0.125 unidades** (la cabeza del sprite asoma sobre el muro superior al pegarse) **aceptado para el graybox de M1**; se resolverá con arte real vía foreground/occlusion/render sorting, sin alterar física.
- **Campos `m_SpriteSheet` vacíos** en `PixelArtTexture.preset` son inofensivos (particularidad de serialización del Preset); no contienen datos ni afectan la importación.
- **Mensaje de preprocesamiento de URP** (`"N URP assets included in build"`) es informativo de build y **no es un warning persistente** en consola estable.
- **Transición entre áreas por corte directo (sin fundido)** conforme a SPEC B1; el fundido queda diferido a un hito posterior con UI.

## 5. Prueba manual de frame rate (SPEC D1/D3) — PENDIENTE

**Estado: PENDIENTE DE VALIDACIÓN FINAL EN FASE 8. No medida todavía; no aprobada.**

Procedimiento previsto (a ejecutar antes de cerrar M1):
- Recorrer ~10 s en línea recta a **30, 60 y 120 FPS** fijando `Application.targetFrameRate`, con **VSync temporalmente desactivado**.
- Comparar la distancia recorrida entre las tres tasas.
- Umbral: diferencia máxima **≈ 1 %** (movimiento consistente y desacoplado de la tasa de cuadros).

Debe completarse y registrarse antes de declarar M1 terminado.

---

**Fin del informe de pruebas M1.** Pruebas automáticas 4/4 (×2). Pendiente: métrica de frame rate (Fase 8).

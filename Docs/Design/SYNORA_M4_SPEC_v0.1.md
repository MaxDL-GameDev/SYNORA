# SYNORA — SPEC M4: Observación de Criaturas (v0.1)

> Especificación técnica del hito **M4 — Observación de Criaturas**. Conecta el sistema de interacción contextual de M2 con las criaturas vivas de M3 (Verak). Documento de diseño; **no** modifica M1/M2/M3 ni sus documentos. Autoridad de implementación: Claude Code, bajo aprobación del Director.

## 0. Estado y contexto

- Hitos completos: **M0, M1, M2, M3**. Rama `main`. Último tag: `m3-complete` (`8258014`).
- M3 entregó **Verak** como criatura ambiental viva: Idle / Patrol (PingPong) / Alert; sensor `detectionRadius`/`loseRadius`/linger; facing preservado en Alert; movimiento y animación desacoplados; dos instancias independientes en `ClaroExterior`; **sin combate, persecución ni huida**.
- M2 entregó interacción contextual: detección frontal por caja, requisito de distancia y orientación, prompt, `ExaminableInteractable`, panel de examen, Player inmovilizado por `PlayerControlGate`, `Time.timeScale` intacto (=1), cierre y restauración de control.

### Contratos reales de M2 (fuente de verdad para esta SPEC)

| Tipo | Rol | Firma relevante |
|---|---|---|
| `IInteractable` | Contrato de objeto interactuable | `InteractionId`, `Priority`, `CanInteract`, `Vector2 InteractionPosition`, `PromptText`, `Execute(IInteractionReceiver)` |
| `IInteractionReceiver` | Receptor del examen | `ShowObservation(ExaminableData)` |
| `ExaminableInteractable` | Interactuable concreto (`sealed`) | implementa `IInteractable`; `Execute` llama `receiver.ShowObservation(data)` |
| `ExaminableData` (SO) | Contenido de examen | `InteractionId`, `DisplayName`, `ObservationTitle`, `ObservationBody`, `HasValidInteractionId` |
| `InteractionDetector` | Descubre candidatos | serializa `List<ExaminableInteractable> sceneExaminables`; `OverlapBox` frontal; publica `IReadOnlyList<IInteractable> Candidates` |
| `InteractionSelector` | Selecciona objetivo | `SelectTarget(candidates, currentTarget, origin)` con histéresis |
| `InteractionController` | Orquesta y es el `IInteractionReceiver` | estados `ExploringWithoutTarget`/`ExploringWithTarget`/`ObservationOpen`; `Execute` → `ShowObservation` → panel + `gate.Block(Observation)` |
| `ObservationPanelPresenter` | UI del panel | `Open(ExaminableData)` copia `ObservationTitle`/`ObservationBody`; `Close()` |
| `InteractionTargetUtility.IsAlive` | Robustez de target | valida target destruido/deshabilitado |
| `CreatureBrain` (M3) | Cerebro de criatura | **ya expone** `public CreatureStateId CurrentStateId` (solo lectura) |

**Hallazgo clave:** todo el pipeline aguas abajo del detector es genérico sobre `IInteractable` y `ExaminableData`. El **único** punto tipado concretamente a `ExaminableInteractable` es el **registro** en `InteractionDetector.sceneExaminables` (y su `colliderLookup`). Cualquier integración de criaturas debe resolver ese punto.

## 1. Alcance de M4

**Objetivo jugable:** el jugador se acerca a Verak, se orienta hacia él y lo **examina** con el sistema de M2, recibiendo una descripción breve y coherente de una criatura viva, **sin acoplar la UI a la lógica interna de la criatura**.

M4 une formalmente M2 (interacción) y M3 (criaturas). Solo **Verak** entra en alcance.

### Fuera de alcance (explícito)

Combate · domesticación · captura · alimentación · inventario · diálogo · misiones · códex persistente · guardado/progreso · afinidad · restauración · vinculación · nuevas especies · cualquier cambio de comportamiento de Verak provocado por el examen.

## 2. Decisiones de diseño (congeladas)

### A. Alcance de la observación → **Texto contextual por estado** (Opción 2)

El contenido refleja el estado observable en el instante del examen (Idle / Patrol / Alert). Se adopta **solo porque puede lograrse sin acoplar UI↔Brain**: la criatura expone su estado como dato público de solo lectura (`CreatureBrain.CurrentStateId`); el contenido por estado se preautora como assets de datos; la UI nunca consulta al Brain.

### B. Estado durante el examen → **Contenido capturado al abrir; panel estable**

- El texto se **captura al iniciar el examen** (en `Execute`) y **no** se actualiza continuamente.
- El panel permanece abierto **hasta que el jugador lo cierra**; no se cierra por cambios de estado ni por distancia.
- Si Verak cambia de estado, se aleja o queda fuera de rango con el panel abierto: **el texto mostrado no cambia** y Verak **sigue funcionando con normalidad**.
- Semántica de "snapshot" garantizada por construcción: `ObservationPanelPresenter.Open` copia el texto a los labels una sola vez y no vuelve a leer la fuente.

### C. Múltiples criaturas → **Selección de M2 sin cambios**

Se reutiliza exactamente `InteractionDetector` + `InteractionSelector` (prioridad + histéresis por `currentTarget` para evitar parpadeo). Dos Verak son simplemente dos `IInteractable`. **No** se crea selector especial para criaturas. Prioridad configurable por instancia (campo serializado del adaptador, igual que `ExaminableInteractable.priority`).

### D. Requisito de orientación → **Criterio de M2 sin excepciones**

Se reutiliza el criterio completo de M2: caja frontal (`detectionRange`/`frontalHalfWidth`), distancia y `InteractionGeometry.IsInsideFrontZone`. **Sin** raycast de línea de visión nuevo ni excepciones para criaturas (M2 hoy no exige LoS; M4 no lo introduce).

### E. Conducta de Verak durante el examen → **Cero efectos sobre la criatura**

Examinar **no**: fuerza Idle/Alert, pausa el Brain, pausa el Animator, bloquea el movimiento, cambia el facing, ni altera el sensor. El examen es una lectura pasiva de datos públicos.

## 3. Arquitectura

Principio rector: **la observación consulta información pública de la criatura, pero nunca controla su comportamiento.** Dirección de dependencia única:

```
InteractionController (M2)
   → CreatureExaminableInteractable  (adaptador IInteractable, M4)
      → ICreatureObservationSource   (vista pública de solo lectura, M4)
         → CreatureBrain.CurrentStateId  (dato público M3, solo lectura)
```

Nunca: `UI → Brain/estado concreto para controlarlo`.

### 3.1 Alternativas evaluadas

| # | Enfoque | Cambio a M2/M3 | Texto contextual | Veredicto |
|---|---|---|---|---|
| 1 | Reusar `ExaminableInteractable` tal cual en Verak (SO fijo) | **Cero** | **No** (estático) | Fallback si el Director rechaza tocar M2 |
| 2 | **Adaptador por composición `CreatureExaminableInteractable` + generalizar el registro del detector a `IInteractable`** | **Mínimo** (1 campo + 1 bucle en `InteractionDetector`) | **Sí** | **RECOMENDADO** |
| 3 | Hook de "data provider" opcional dentro de `ExaminableInteractable.Execute` | Bajo, pero mezcla la responsabilidad de criatura en el examinable genérico | Sí | Rechazado (peor cohesión) |

Herencia desde `ExaminableInteractable` es **imposible**: la clase es `sealed`. Por eso la única vía de "adaptador" es composición implementando `IInteractable` directamente (Opción 2), coherente con el estándar de M3 (composición, sin jerarquías).

### 3.2 Decisión recomendada — Opción 2

**Componentes nuevos (todos aditivos; ningún borrado):**

**`ICreatureObservationSource`** — vista pública de solo lectura. Expone datos, nunca comandos.
```
string DisplayName { get; }
CreatureObservationState CurrentObservationState { get; }
```

**`CreatureObservationState`** (enum de presentación): `Calm`, `Roaming`, `Watchful` (mapea 1:1 desde `CreatureStateId` Idle/Patrol/Alert). Enum propio para **no** acoplar el contenido de UI al enum de gameplay `CreatureStateId`.

**`CreatureObservationSource`** (MonoBehaviour, implementa `ICreatureObservationSource`) — lee `CreatureBrain.CurrentStateId` y `CreatureIdentity` (nombre); mapea a `CreatureObservationState`; **solo lectura**, sin `Update`, sin escritura al Brain.

**`CreatureExaminableInteractable`** (MonoBehaviour, implementa `IInteractable`) — adaptador:
- Serializa: referencia a `ICreatureObservationSource` (vía el `CreatureObservationSource`), un `priority`, un `interactionEnabled`, un `ExaminableData` **base** (fallback) y un mapa `{CreatureObservationState → ExaminableData}` (3 entradas).
- `InteractionId` / `CanInteract` / `InteractionPosition` / `PromptText` ("Examinar") replican el patrón de `ExaminableInteractable`.
- `Execute(receiver)`: si `CanInteract`, lee `source.CurrentObservationState`, resuelve el `ExaminableData` correspondiente (**fallback al base** si el estado no está mapeado), y llama `receiver.ShowObservation(chosen)`. Una sola lectura, sin polling.

**Cambio mínimo a M2 (requiere aprobación del Director):** generalizar el registro del detector para aceptar cualquier `IInteractable` en vez de solo `ExaminableInteractable`. Opción concreta preferida: cambiar `sceneExaminables` a una lista de componentes validados a `IInteractable` (p. ej. `List<MonoBehaviour>` + validación, o `List<Component>`), preservando 100% el comportamiento actual con los `ExaminableInteractable` existentes. Es retrocompatible y **corrige** una limitación de M2 (siempre debió indexar por `IInteractable`).

### 3.3 Por qué el "snapshot" struct queda diferido (YAGNI)

El pipeline entrega `ExaminableData` al panel. Con texto contextual = **N `ExaminableData` preautorados por estado**, el "snapshot" es la referencia al SO elegida en `Execute`; el panel copia el texto al abrir y no vuelve a leer. Introducir un `CreatureObservationSnapshot` (struct) obligaría a cambiar `IInteractionReceiver`/panel (M2 congelado) sin beneficio. Se documenta como alternativa futura si aparece contenido que un SO no pueda representar.

## 4. Límites arquitectónicos (prohibiciones)

Prohibido en M4: referencias de UI dentro de `CreatureBrain` o de los estados · lógica de examen dentro de `CreatureAnimator` · que la UI cambie el estado de la criatura · que el interactuable invoque transiciones · polling por frame desde la UI · `GameObject.Find`/`FindObjectOfType` · singleton nuevo · Service Locator · eventos globales · LINQ en runtime · coroutines innecesarias · pausar `Time.timeScale` · modificar la física de Verak durante el examen.

## 5. Contenido de Verak (congelado — detalle en el GDD)

- **Nombre:** `Verak`.
- **Título de panel (`ObservationTitle`):** `Verak`.
- **Descripción base + textos por estado (Calm/Roaming/Watchful):** ver GDD §5. El GDD congela **una** variante final por estado; la SPEC solo fija la estructura de datos (`ExaminableData` por estado + base de fallback).

## 6. Casos de uso

| # | Caso | Precondición | Acción | Resultado | Player | Verak | Contenido | Al cerrar |
|---|---|---|---|---|---|---|---|---|
| 1 | Examinar en Idle | Verak en Idle, en rango+frente | Pulsar E | Panel abre | Inmóvil (gate) | Sigue su Brain | Texto Calm | Recupera control |
| 2 | Examinar en Patrol | Verak en Patrol, en rango+frente | Pulsar E | Panel abre | Inmóvil | Sigue patrullando | Texto Roaming | Recupera control |
| 3 | Examinar en Alert | Verak en Alert, en rango+frente | Pulsar E | Panel abre | Inmóvil | Sigue en Alert | Texto Watchful | Recupera control |
| 4 | Cambio de estado con panel abierto | Panel abierto (texto X) | Verak cambia de estado | **Texto no cambia** | Inmóvil | Cambia libremente | El capturado en apertura | Recupera control |
| 5 | Verak se aleja con panel abierto | Panel abierto | Verak sale de rango | Panel **sigue abierto** | Inmóvil | Se mueve normal | El capturado | Recupera control |
| 6 | Dos Verak candidatos | Ambos en rango+frente | — | Selector de M2 elige uno (prioridad+histéresis) | — | Ambos independientes | Del seleccionado | — |
| 7 | Cerrar panel | Panel abierto | Pulsar E | Panel cierra | Recupera control | Intacto | — | `RefreshTarget(true)` |
| 8 | Referencia desaparece en teardown | Panel abierto / target vivo | Verak destruido / escena descargada | Cierre limpio, sin excepción | Control restaurado | N/A | — | `IsAlive` false → cierre |

## 7. Casos límite y fallbacks

| Situación | Fallback seguro |
|---|---|
| `ICreatureObservationSource` nulo en el adaptador | `CanInteract` = false; log de error en `Awake`; no aparece prompt |
| `ExaminableData` de un estado no asignado | Usar el **base**; nunca excepción |
| Estado observable no reconocido | Usar el **base** |
| `ObservationBody`/`Title` vacío | Se muestra vacío sin excepción (igual que M2); warning en `OnValidate` del SO |
| Criatura desactivada | `CanInteract` = false (vía `isActiveAndEnabled`) |
| Criatura destruida con panel abierto | `InteractionTargetUtility.IsAlive` → false; `InteractionController` cierra limpio y restaura gate |
| Panel ya abierto | `InteractionController` en `ObservationOpen` ignora nueva apertura; `ObservationPanelPresenter.Open` es idempotente |
| Interacción repetida / spam de tecla | Máquina de estados de M2 (una acción por pulsación); sin reentrada |
| Cambio de escena / teardown | `InteractionController.OnDisable` cierra panel, oculta prompt, desbloquea gate |
| Dos interactuables solapados | Selector de M2 resuelve uno; sin ambigüedad |
| Target perdido antes de confirmar | `Execute` revalida `CanInteract`/`IsAlive`; `ShowObservation` revalida estado |
| Estado cambia entre detección y apertura | Se captura el estado **en `Execute`**; consistente con lo mostrado |

**Regla:** ninguna ruta lanza excepción por contenido faltante; siempre se restaura el control del Player.

## 8. Criterios de aceptación

1. Verak es seleccionable con el sistema de interacción de M2.
2. El prompt aparece solo bajo los criterios existentes de M2 (rango + zona frontal + `CanInteract`).
3. El texto corresponde al estado **capturado al iniciar** el examen.
4. El Player queda inmóvil durante el panel (`PlayerControlGate` con `ControlBlockReason.Observation`).
5. `Time.timeScale` permanece en 1.
6. Verak continúa Brain, sensor, movimiento y animación durante y después del examen.
7. El panel no modifica el estado de Verak (verificable: `CurrentStateId` no cambia por abrir/cerrar).
8. Al cerrar, el Player recupera el control.
9. Dos Verak no comparten datos ni referencias (fuentes y adaptadores independientes por instancia).
10. Sin errores si Verak cambia de estado con panel abierto.
11. Sin errores si el objetivo desaparece.
12. Sin combate, sin persistencia, sin códex, sin nuevas especies.
13. Sin regresiones M1–M3 (139/139 EditMode siguen verdes; interacción de Nodo de M2 intacta).

## 9. Estrategia de pruebas

### EditMode (nuevas)
- Snapshot/selección correcta de `ExaminableData` para Calm / Roaming / Watchful.
- Fallback al base para estado desconocido / no mapeado.
- `ICreatureObservationSource` nulo → `CanInteract` false, sin excepción.
- `ObservationBody` vacío → sin excepción.
- El adaptador **no** modifica el Brain (mock de source; verificar que `Execute` no invoca transiciones ni escribe estado).
- `Execute` no toca `Time.timeScale`.
- `CreatureObservationSource` mapea correctamente `CreatureStateId` → `CreatureObservationState` (Idle→Calm, Patrol→Roaming, Alert→Watchful).
- Selección entre múltiples targets (reusa pruebas de `InteractionSelector`).
- Independencia entre dos criaturas (dos adaptadores → dos SO distintos).
- Regresión: suite de interacción de M2 intacta.

### Runtime / manual (fuera del Editor)
A. Prompt por distancia y facing · B. Examen en Idle · C. Examen en Patrol · D. Examen en Alert · E. Cambio de estado con panel abierto · F. Verak alejándose con panel abierto · G. Dos Verak cerca · H. Cierre y recuperación de movimiento · I. Regresión del Nodo `ExaminableInteractable` de M2 · J. Estabilidad repetida (spam de examen).

**PASS/FAIL:** cada caso PASS solo si cumple su fila de §6/§8; cualquier excepción, estado trabado, control no restaurado o cambio de comportamiento de Verak por la UI es FAIL.

## 10. Plan de fases de M4

| Fase | Contenido | Toca código |
|---|---|---|
| **1** | **SPEC + GDD** (esta fase) | No |
| 2 | Contratos de observación: `ICreatureObservationSource`, `CreatureObservationState`, `CreatureObservationSource`; mapeo estado→observable; tests unitarios | Sí (aditivo M4) |
| 3 | Adaptador + integración M2: `CreatureExaminableInteractable`; **generalización mínima del registro del detector** (requiere aprobación); tests | Sí (aditivo M4 + 1 cambio mínimo M2) |
| 4 | Contenido contextual de Verak: `ExaminableData` por estado + base; fallbacks; presentación | Assets de datos |
| 5 | Integración de prefab y escena: componentes en prefab Verak, dos instancias, referencias, alta en el detector | Prefab + escena |
| 6 | QA automatizado y manual: regresión completa + casos A–J + reporte | No (validación) |
| 7 | Build final y cierre: build Windows, validación fuera del Editor, Player.log, commit, tag `m4-complete` | Build + git |

Alcance pequeño; el número de fases se mantiene salvo razón arquitectónica clara.

## 11. Fuera de alcance / no hacer

No modificar documentos anteriores. No implementar código, escenas, prefabs ni assets en esta fase. No commit/push/tag sin autorización. No introducir LoS, códex, persistencia ni nuevas criaturas. El cambio al `InteractionDetector` (Fase 3) es el **único** toque previsto a M2 y requiere aprobación explícita antes de ejecutarse.

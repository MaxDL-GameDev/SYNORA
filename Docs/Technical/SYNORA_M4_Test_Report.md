# SYNORA — Informe de pruebas M4

> Documento de resultados de QA del hito M4 (Observación de Criaturas — Verak). No modifica la SPEC M4 v0.1 ni el GDD M4 v0.1; solo registra resultados.

## 1. Resumen ejecutivo

M4 conecta el sistema de interacción contextual de M2 con las criaturas vivas de M3: el jugador puede **examinar a Verak** y recibir una descripción coherente con su estado observable, sin acoplar la UI a la lógica de la criatura. QA completo: **220/220 pruebas EditMode PASS**, playthrough manual del Director **PASS**, consola estable 0/0, sin regresiones M1–M3. **Resultado: PASS.** Build y tag pertenecen a Fase 7.

## 2. Entorno de ejecución

| Elemento | Valor |
|---|---|
| Editor | Unity 6000.5.3f1 |
| Render Pipeline | Universal RP 2D |
| Input System | com.unity.inputsystem |
| Test Framework | com.unity.test-framework (EditMode) |
| Plataforma | Windows (Editor, Mono) |
| Fecha de ejecución | 2026-07-22 |
| HEAD base | `7fd3416` (feat: integrate Verak observation into ClaroExterior) |
| SPEC de referencia | `../Design/SYNORA_M4_SPEC_v0.1.md` |
| GDD de referencia | `../Design/SYNORA_M4_GDD_v0.1.md` |

## 3. Estado Git

| Elemento | Valor |
|---|---|
| Rama | `main` |
| HEAD == origin/main == remoto | `7fd3416` |
| ahead/behind | 0 / 0 |
| Working tree | limpio (antes de este reporte) |
| Tag `m4-complete` | no existe (Fase 7) |

### Historial de commits M4

| Commit | Fase |
|---|---|
| `f4f7142` | Fase 1 — SPEC + GDD |
| `8bf2f10` | Fase 2 — contratos de observación |
| `3680597` | Fase 3 — adaptador + generalización del detector |
| `32aeadf` | Fase 4 — contenido `ExaminableData` de Verak |
| `60bda5f` | Fase 5B — dedup del detector por referencia |
| `d5ef68a` | Fase 5C — integración en el prefab de Verak |
| `7fd3416` | Fase 5D — integración en ClaroExterior |

## 4. Pruebas automatizadas (EditMode)

Assembly `Synora.Tests.EditMode`. **220/220 · 0 failed · 0 skipped · 2.70 s.** De ellas, **31 nuevas de M4** (contratos + adaptador + contenido + detector generalizado + wiring de prefab) sobre las 189 previas (M1/M2/M3 + refinamientos).

| Suite (M4 y afectadas) | Tests | Cobertura |
|---|---|---|
| `CreatureObservationStateTests` | 5 | Mapeo puro `Resolve`: Idle→Calm, Patrol→Roaming, Alert→Watchful, valor fuera de enum→Unknown, todo estado definido mapea |
| `CreatureObservationSourceTests` | 11 | DisplayName desde identity / fallback; brain nulo→Unknown+warning; Idle→Calm; leer no muta el Brain; dos fuentes independientes; sin Update; sin UI; timeScale intacto; contrato |
| `CreatureExaminableInteractableTests` | 26 | Contrato `IInteractable`; selección Calm/Roaming/Watchful/Unknown→base; fallbacks; CanInteract (source/base/enabled/disabled/id inválido); Execute revalida; receiver nulo seguro; 1 sola llamada; estado leído 1 vez; no muta fuente/Brain; sin timeScale; dos adaptadores independientes; **InteractionId solo de baseData** (invariante entre estados, id contextual no cambia identidad, CanInteract independiente de ids contextuales); **sin setter público** |
| `InteractionDetectorTests` | 16 | Registro genérico `IInteractable`; ExaminableInteractable y CreatureExaminableInteractable coexisten; no-IInteractable/null reportados; **id compartido entre instancias distintas permitido**; misma referencia 2×→1 error+registro único; Nodo+2Verak; múltiples colliders→misma ref; id vacío reportado; **escala N=2/5/10** |
| `VerakObservationContentTests` | 13 | Existencia+tipo de los 4 assets; id canónico; título/cuerpo exactos vs GDD ×4; sin whitespace/saltos/mojibake; 4 GUIDs distintos; id compartido; base como fallback; wiring en el adaptador resolviendo por estado |
| `VerakPrefabTests` | 17 | Prefab (M3) + **M4 Fase 5C**: hijo `Interaction`/layer/trigger; adapter+trigger mismo GO; source cruzada al root; 4 data; obs wiring; capsule físico intacto; 0 Missing; instancia CanInteract+id; dos instancias distintas con capsule físico excluido |

## 5. Playthrough manual (aprobado por el Director)

Ejecutado por el Director en `ClaroExterior` (Play directo). MCP no observa el render ni el tiempo real de forma fiable (throttle con Game View desenfocada), por lo que estos casos fueron validados manualmente.

**Nota canónica sobre el estado alcanzable:** el rango de interacción (`detectionRange = 1.25`) está dentro del `detectionRadius = 3` del sensor de Verak. Al acercarse lo suficiente para examinar, Verak ya detectó al jugador y está en **Alert**. Por tanto el único estado observable alcanzable en la escena real es **Watchful**. Calm (Idle), Roaming (Patrol) y Unknown→Base quedan cubiertos por las pruebas automatizadas de contratos/adaptador/contenido; no se exige reproducirlos a mano (ni se altera arquitectura/sensores/radios para forzarlos).

| Caso | Verak_A | Verak_B |
|---|---|---|
| Aparece la interacción (prompt) | ✅ | ✅ |
| Al acercarse entra en Alert | ✅ | ✅ |
| El panel muestra **Watchful** | ✅ | ✅ |
| El panel abre correctamente | ✅ | ✅ |
| El jugador queda bloqueado | ✅ | ✅ |
| `Time.timeScale` permanece en 1 | ✅ | ✅ |
| La IA continúa funcionando | ✅ | ✅ |
| El panel cierra correctamente | ✅ | ✅ |
| El control del jugador se restaura | ✅ | ✅ |
| Independencia entre Verak_A y Verak_B | ✅ (ambos `creature.verak`, sin interferencia) |
| Selección estable (target correcto, sin parpadeo) | ✅ |

**Resultado global del playthrough: PASS.**

## 6. Regresión M2

| Validación | Resultado |
|---|---|
| `NodoInactivo`: prompt, selección, panel abre/cierra, control restaurado | ✅ (manual) |
| Suite de interacción M2 (`InteractionController`, `InteractionSelector`, `InteractionGeometry`) verde | ✅ |
| `sceneExaminables` conserva NodoInactivo en `[0]` | ✅ |
| Sin cambios en `InteractionSelector`/`InteractionController`/`IInteractable`/`ExaminableInteractable`/`ObservationPanelPresenter` | ✅ |

## 7. Validación M4

| Elemento | Resultado |
|---|---|
| Ambos Verak observables con el sistema de M2 | ✅ |
| Texto = estado capturado al iniciar el examen (Watchful in-scene) | ✅ |
| El panel no modifica el estado de Verak | ✅ (Execute solo lee `CurrentObservationState`) |
| Dos Verak no comparten datos ni referencias (adaptadores/fuentes independientes) | ✅ |
| `creature.verak` compartido sin error de dedup (validación por referencia) | ✅ |
| Sin combate / persistencia / códex / nuevas especies | ✅ |

## 8. Validación de IA (panel abierto)

Con el panel de observación abierto, verificado por el Director:

| Sistema | Continúa activo |
|---|---|
| Patrol (PingPong) | ✅ |
| Alert (histéresis/linger) | ✅ |
| Animator | ✅ |
| Sensor (percepción) | ✅ |
| CreatureBrain (tick lógico) | ✅ |
| `Time.timeScale` | ✅ = 1 |

El examen es una **lectura pasiva**: no fuerza estado, no pausa el Brain, no bloquea el movimiento ni cambia el facing.

## 9. Consola

- Consola estable del proyecto (edit mode, sin tests): **0 errores / 0 warnings**.
- Durante la ejecución de pruebas aparecen warnings/errores **esperados** capturados por `LogAssert.Expect` (id vacío / null / no-IInteractable / entrada duplicada del detector; brain no asignado de `CreatureBrain`/`CreatureObservationSource`; layer/movement de M1). Todos consumidos por tests que pasan.
- 0 Excepciones · 0 Missing Scripts · 0 Missing References · **0 errores por `InteractionId` compartido**.

## 10. Rendimiento (inspección práctica, sin profiling profundo)

Inspección de código y comportamiento (el profiling por método detallado corresponde al Director si lo requiere, como en M3):

- El **adaptador** no tiene `Update` ni polling: `Execute` se invoca una vez por interacción y lee `CurrentObservationState` una sola vez.
- `CreatureObservationSource` es lectura on-demand, sin `Update` ni caché mutable.
- El **detector** conserva su `OverlapBox` non-alloc preexistente (buffer reutilizado); la generalización del registro (validación por referencia) ocurre una vez en `Awake`, no por frame.
- Candidatos: dos Verak producen dos candidatos lógicos estables; sin crecimiento observable.
- Sin spam de logs en Play. Comportamiento estable en el playthrough.

## 11. Inspección estructural

| Elemento | Resultado |
|---|---|
| Verak registrados en `ClaroExterior` | **2** (Verak_A, Verak_B) |
| `sceneExaminables` | `[0]` NodoInactivo · `[1]` Verak_A/Interaction · `[2]` Verak_B/Interaction |
| `interactableLayer` | Interactables (bits 2048) |
| Prefab de Verak | root Creatures + hijo `Interaction` (Interactables) con trigger + adaptador; obs en root; capsule físico intacto |
| Jerarquía / overrides | correctos; sin overrides inesperados (solo el de `sceneExaminables`) |
| Missing Scripts (escena y prefab) | 0 / 0 |

## 12. Riesgos / deuda aceptada (no bloqueante)

1. **Estados Calm/Roaming no alcanzables en escena** por diseño (interacción dentro del radio de Alert). Cubiertos por pruebas automatizadas. No es un defecto.
2. **Desempate del selector por `InteractionId`**: con dos instancias del mismo id en un empate exacto de prioridad+distancia, el resultado depende del orden de candidatos (inalcanzable en la práctica; ambos entregan el mismo contenido). Deuda aceptada; no modificar en M4.
3. La independencia M2↔criaturas se mantiene por disciplina + inspección (comparten el asmdef `Synora.Runtime`); verificado: cero referencias a criaturas en `Assets/Scripts/Gameplay/Interaction/`.

## 13. Conclusión

- Pruebas automatizadas: **220/220 · 0 failed · 0 skipped**.
- Playthrough manual (Watchful, panel, bloqueo, IA, cierre, independencia, selección): **PASS**.
- Regresión M2 (NodoInactivo) y M1/M3: **PASS**.
- Consola estable 0/0; 0 Missing; 0 errores por id compartido.
- Estructura de escena y prefab correcta.
- **Build final y tag `m4-complete`: pertenecen a Fase 7 (aún no ejecutados).**

## 14. Recomendación

**PASS.** M4 — Observación de Criaturas queda validado funcional y automáticamente. Listo para avanzar a Fase 7 (build Windows, validación fuera del Editor, tag) bajo autorización del Director.

---

**Fin del informe de pruebas M4.** Fase 6 (QA final, regresión y reporte): **PASS**, sin incidencias abiertas. Build y tag pendientes de Fase 7.

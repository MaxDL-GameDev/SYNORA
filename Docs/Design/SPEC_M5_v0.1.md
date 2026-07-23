# SYNORA — SPEC M5: Combate no letal (v0.1)

> Especificación técnica del hito **M5 — Combate no letal**. Introduce el combate por primera vez: el jugador contiene temporalmente una amenaza (**Verak Alterado**) sin matar, capturar, restaurar ni vincular. Documento de diseño técnico; **no** modifica M0–M4 ni sus documentos. Autoridad de implementación: Claude Code, bajo aprobación del Director. Autoridad de canon/gameplay: Director (Biblia/GDD).

## 0. Estado y contexto

- Hitos completos: **M0, M1, M2, M3, M4**. Rama `main`. Último tag: `m4-complete` (`3bcc87a`). Suite EditMode: 220/220.
- **Sin código de combate preexistente** (partida limpia).
- Arquitectura congelada relevante: State Pattern con `ICreatureState` + `CreatureStateId` (sin `BaseState`); `CreatureBrain` único punto de transición; `CreatureContext` contenedor de dependencias; **Animator = solo presentación (ADR-M3-005)**; percepción `CreatureSensor` (OverlapCircle non-alloc, doble radio); ticks de física de dueño único; **Player(8)↔Creatures(12) no colisionan (ADR-M3-002)**; interacción por `IInteractable`/detector/selector/controller con `PlayerControlGate`; observación M4 de solo lectura con `Time.timeScale`=1.
- Base de roadmap: `SPEC_M0-M1:449` ("combate → M4–M7"); `ADR-M3-001` (estados futuros Attack/Flee previstos); `SPEC_M0-M1:102` (carpeta `Gameplay/Combat` prevista).

## 1. Decisiones de canon aprobadas (congeladas)

### Naturaleza
M5 introduce **combate no letal**. **No** existen: muerte · loot · experiencia · captura · restauración · vinculación · persistencia. **Objetivo:** contener temporalmente una amenaza.

### Participantes
- **Jugador:** puede atacar y recibir daño.
- **Verak normal:** permanece **no hostil**; conserva M3 (Idle/Patrol/Alert) y M4 (observable); **no** participa en combate.
- **Verak Alterado:** entidad nueva; participa en combate.

### Resultado
- **Verak Alterado → `Subdued`** (contenido). No muerto, no eliminado, no restaurado.
- **Jugador → derrota temporal.** Sin Game Over definitivo.

## 2. Alcance (MVP — corte vertical)

**Incluye:** ataque melee del jugador · modelo de daño · salud · persecución (Chase) del Verak Alterado · ataque del Verak Alterado · `Subdued` · derrota temporal del jugador.

**Excluye explícitamente:** restauración · vinculación · inventario · crafting · loot · experiencia · progresión · múltiples armas · habilidades · guardado/persistencia · combos · proyectiles · stamina · críticos/resistencias/elementos/armadura · nuevas especies (además de Verak Alterado) · IA de grupo · pathfinding/NavMesh.

## 3. Contratos de daño y salud

Aditivos, en `Assets/Scripts/Gameplay/Combat/` (carpeta prevista por `SPEC_M0-M1:102`). Estilo: contratos mínimos + lógica pura testeable en EditMode (espejo de `CreatureSensing`/`CreatureObservationSource`). Sin eventos globales, singletons, Service Locator ni God ScriptableObject.

| Tipo | Rol | Notas |
|---|---|---|
| `DamageInfo` (struct **inmutable**) | Carga de un impacto | Campos mínimos: `Amount`, `SourceKind`/origen (p. ej. posición o id del atacante) y opcional `HitDirection`. **Sin** crítico, resistencia, elemento ni armadura. |
| `IDamageable` (interfaz) | Receptor de daño | `void ApplyDamage(in DamageInfo damage);` — solo recibe; no expone comandos de control. |
| `Health` (MonoBehaviour, implementa `IDamageable`) | Salud por instancia | Serializa `maxHealth`; expone `Current`, `Max`, `Normalized`, `IsZero`; `ApplyDamage(in DamageInfo)` clampa a `[0,max]`; `ResetHealth()`. **Salud independiente por instancia** (sin estado compartido). Señala "llegó a cero" de forma consultable/desacoplada (flag + callback controlado, no evento global). |

- La **lógica de clamp/aplicación** se aísla en un método puro estático (`ComputeDamaged(current, amount, max)`), testeable sin MonoBehaviour.
- `Health` es reutilizable por jugador y Verak Alterado; la **interpretación** del cero (Subdued vs derrota temporal) la deciden sus dueños, no `Health`.

## 4. Detección de impactos

- **OverlapBox/OverlapCircle non-alloc** con `ContactFilter2D` (buffer reutilizado), igual patrón que `CreatureSensor`/`InteractionDetector`.
- **Sin `AnimationEvent`s. Sin dependencia del Animator.** La ventana de golpe es **lógica** (dirigida por el componente de ataque/Brain), no un evento de clip. El Animator reproduce el clip de ataque en paralelo como **presentación** (ADR-M3-005).
- **Máximo un impacto por objetivo por ventana**: dedupe por instancia dentro de la ventana activa (buffer/HashSet local), evitando daño múltiple por frame.
- Resolución `Collider2D → IDamageable` mediante `GetComponent`/lookup cacheado (sin `GetComponent` por frame repetido).

## 5. Ataque del jugador

`PlayerAttack` (MonoBehaviour nuevo). **Responsabilidad única:** ejecutar un ataque melee frontal y aplicar daño; **nunca controla el movimiento**.

- **Input:** nueva acción **`Attack`** en `Controls.inputactions` (mapa `Gameplay`), distinta de `Move` e `Interact`. Leída vía el patrón de `PlayerInputReader` (referencias serializadas de `InputActionReference`).
- **Dirección:** `PlayerOrientation.Facing` (cuatro direcciones). Sin apuntado libre.
- **Ventana:** una sola ventana de golpe por ataque; **un impacto máximo por objetivo**; sin combos.
- **Cooldown:** temporizador propio; no re-ataca hasta expirar.
- **Gate:** respeta `PlayerControlGate`; **no ataca** si `IsBlocked` (p. ej. panel de observación abierto). Reutiliza el gate; opción de añadir una razón `Combat` (ver §9).
- **Relaciones:** lee `PlayerOrientation`; consulta `PlayerControlGate`; recibe input de `PlayerInputReader`; **no** escribe en `PlayerMotor` (desacople total del movimiento).

## 6. Verak Alterado (IA)

Entidad **nueva** (prefab propio; el prefab de Verak normal **no se toca**). Reutiliza la infraestructura de criaturas (`CreatureBrain`, `CreatureContext`, `CreatureMovement`, `CreatureSensor`, `CreatureAnimator`) **sin** `BaseState` y con el mismo `ICreatureState` polimórfico.

### Estados (exactamente cuatro; no introducir otros)
| Estado | Rol |
|---|---|
| `Chase` | Persigue al jugador detectado (mueve hacia `DetectedPlayer` vía `CreatureMovement`). |
| `Attack` | En rango, ejecuta ataque (ventana lógica + overlap; aplica daño al jugador). |
| `Hurt` | Reacción al recibir daño (breve; sin control de movimiento por el Animator). |
| `Subdued` | **Terminal** al llegar su `Health` a cero: inerte/pasivo, permanece en escena, no muerto/eliminado/restaurado. |

### Transiciones (a congelar en la SPEC)
`Chase→Attack` (en rango) · `Attack→Chase` (fuera de rango / tras ventana) · `(Chase|Attack)→Hurt` (recibe daño) · `Hurt→Chase` (recuperación) · `(Chase|Attack|Hurt)→Subdued` (health cero). **No** hay retorno desde `Subdued`. `CreatureBrain` sigue siendo el **único** punto de transición.

### Extensión de `CreatureStateId`
Los cuatro estados se añaden a `CreatureStateId` de forma **aditiva** (Idle/Patrol/Alert intactos), coherente con `ADR-M3-001` (que ya anticipó Attack/Flee como tokens futuros). El `CreatureBrain` de Verak normal registra solo {Idle,Patrol,Alert}; el de Verak Alterado registra su propio conjunto. *(Alternativa: enum propio de estados alterados; se prefiere la extensión aditiva por consistencia con el patrón, a confirmar en F6.)*

### Salud y contención
Verak Alterado lleva `Health` + una **hurtbox** (recibe daño del jugador) y una **hitbox** (su `Attack` daña al jugador). A `Health`=0 → `Subdued`. `Subdued` deja al Verak Alterado como gancho para M6 (restauración), **fuera de M5**.

## 7. Salud del jugador y derrota temporal

- El jugador lleva `Health` (misma clase reutilizable).
- A `Health`=0 → **derrota temporal**: bloqueo breve de control (vía `PlayerControlGate`) + recuperación (reinicio a un estado seguro / spawn). **Sin Game Over definitivo, sin pantalla de fin, sin pérdida de progreso** (no hay progreso persistente en M5). El mecanismo exacto de recuperación es **mínimo** y se detalla en el GDD.

## 8. Física, layers y canal de daño

- **Canal de daño independiente de la colisión física.** Las hitbox/hurtbox son **triggers** detectados por overlap lógico; **no** deben reintroducir colisión física Player↔Creatures (ADR-M3-002) ni cruzarse con el trigger de interacción (layer Interactables 11).
- **Layers:** 8 Player · 9 Environment · 10 Transitions · 11 Interactables · 12 Creatures · **13–31 libres**.
- **Análisis (implementación NO decidida aún):**
  - *Opción A:* nuevas layers dedicadas (p. ej. `PlayerHitbox`, `CreatureHurtbox` en 13/14) + máscaras en `ContactFilter2D`. Ventaja: separación limpia, sin colisión física. Coste: 2 layers.
  - *Opción B:* sin nuevas layers; filtrado lógico por componente (`GetComponent<IDamageable>`) sobre overlaps en layers existentes marcadas como trigger. Ventaja: cero cambios de ProjectSettings. Riesgo: menos explícito.
  - La decisión (A/B) y cualquier cambio de `ProjectSettings`/matriz se posponen a **F5** con autorización explícita. **En esta fase no se modifica ProjectSettings.**

## 9. Compatibilidad con M2–M4

- **Observación M4 intacta.** Verak normal sigue examinable (Watchful in-scene). **Verak Alterado NO es examinable** en M5 (no lleva `CreatureExaminableInteractable`). **No** se amplía `CreatureObservationState`.
- **`Interact` y `Attack` son acciones distintas.** Durante la observación (panel abierto): **no atacar, no mover**, `Time.timeScale`=1. Se reutiliza `PlayerControlGate` (opción de añadir razón `Combat` de forma aditiva a `ControlBlockReason`, sin tocar `Observation`).
- **Interacción y combate en el mismo rango:** el prompt de examen (rango 1.25) y el ataque no deben consumir el mismo input; prioridad: con panel abierto no hay ataque ni movimiento.
- **Independencia por instancia:** `Health` y estado por instancia; sin estado compartido entre Verak Alterado.
- **Sin cambios** en `InteractionDetector`/`Selector`/`Controller`/`ObservationPanelPresenter`/`CreatureBrain` de Verak normal/`ExaminableData`.

## 10. Plan de fases

| Fase | Objetivo | Áreas | Contratos | Tests | Commit |
|---|---|---|---|---|---|
| **F2** (esta) | SPEC + GDD | Docs | — | — | `docs: define M5 combat architecture` |
| **F3** | Contratos + lógica pura de salud/daño | `Gameplay/Combat/` | `DamageInfo`, `IDamageable`, `Health` | EditMode: clamp, cero, reset, anti-doble-daño, independencia por instancia | sí |
| **F4** | Ataque del jugador | `Gameplay/Player/PlayerAttack`, `Controls.inputactions` | acción `Attack` | EditMode: dirección, cooldown, gate bloquea, no toca Motor, 1 impacto/objetivo | sí |
| **F5** | Canal de impactos | overlap + ventana lógica + layers/máscara (decisión A/B) | — | EditMode/estructural: overlap non-alloc, dedupe, sin colisión física | sí |
| **F6** | IA de Verak Alterado | `Gameplay/Creatures` (+`CreatureStateId` aditivo), prefab nuevo | estados Chase/Attack/Hurt/Subdued | EditMode: transiciones, Subdued terminal, Hurt, sin BaseState | sí |
| **F7** | Integración prefab/escena + presentación | prefab VerakAltered, escena, Animator | — | estructural + manual | sí |
| **F8** | QA completo + regresión M2–M4 | — | — | 220+→ verde; playthrough | reporte |
| **F9** | Build + validación externa + tag | build | — | build Windows + Player.log | `m5-complete` |

## 11. Riesgos

| Sev | Riesgo | Mitigación |
|---|---|---|
| Alto | AnimationEvents para golpes → viola ADR-M3-005 | Ventana lógica; Animator solo presenta |
| Alto | Daño múltiple por frame | Ventana + un impacto por objetivo + (opcional) invulnerabilidad breve |
| Alto | Hitboxes reintroducen colisión física (ADR-M3-002) | Canal de daño por trigger + layer/máscara separada |
| Medio | Jugador bloqueado permanentemente tras derrota | Recuperación garantizada; gate liberado siempre |
| Medio | Transiciones circulares en FSM de Verak Alterado | Grafo de transiciones cerrado; Subdued terminal |
| Medio | Extender `CreatureStateId` afecta a Verak normal | Extensión aditiva; brains registran conjuntos disjuntos |
| Medio | Expansión de alcance (armas, stamina, loot) | Fuera de alcance explícito |
| Bajo | Duplicación de salud jugador/criatura | `Health` reutilizable único |

## 12. Criterios de aceptación

1. El jugador ataca en melee frontal según `PlayerOrientation` (4 direcciones), con cooldown y **un impacto máximo por objetivo**.
2. El ataque **no** mueve al jugador y respeta `PlayerControlGate` (no ataca con panel abierto).
3. `Health`/`IDamageable`/`DamageInfo` aplican daño clampado, por instancia, sin críticos/resistencias.
4. La detección de impactos usa overlap non-alloc, **sin AnimationEvents**; el Animator no participa en la lógica.
5. Verak Alterado persigue (Chase), ataca (Attack), reacciona (Hurt) y a salud cero queda **Subdued** (terminal, en escena, no muerto).
6. El jugador a salud cero sufre **derrota temporal** (sin Game Over definitivo) y recupera el control.
7. Verak **normal** permanece no hostil, observable (M4 intacto) y **no** combate.
8. Verak Alterado **no** es examinable; `CreatureObservationState` **no** se amplía.
9. `Time.timeScale`=1 siempre; sin persistencia, loot, XP, restauración ni vinculación.
10. Sin regresiones M1–M4 (suite verde; interacción/observación/patrulla intactas).
11. Sin colisión física Player↔Creatures reintroducida por el combate.

## 13. Fuera de alcance / no hacer
No implementar código en esta fase. No modificar documentos anteriores, prefabs, escenas, assets ni ProjectSettings. No introducir restauración/vinculación/persistencia/loot/XP/armas múltiples/stamina/combos/proyectiles. No volver hostil al Verak normal. No decidir la implementación de layers de daño (F5).

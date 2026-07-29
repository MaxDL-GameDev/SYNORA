# SYNORA — SPEC M7: Vinculación (v1.0)

> Especificación **funcional** de M7. Describe QUÉ debe hacer el sistema en términos de
> comportamiento **observable** y de **arquitectura de estados/eventos**, no CÓMO se escribe el
> código. **No contiene implementación.** Se apoya en la Biblia v3.0 (§24, §37) y el GDD del
> Prototipo v0.1 (Segmento F), corregidos por la **Enmienda de canon CANON-001** (criatura única).
> M7 **reutiliza** los patrones ya probados en M3–M6 (máquina de estados, proceso con temporizador,
> interactuable, gate de control, presentador visual, fuente de observación). No introduce
> patrones nuevos salvo el **seguimiento de compañero**.

---

## 1. Introducción
M7 cierra la trayectoria del prototipo: `Encuentro → Observación → Contención (M5) → Restauración
(M6) → Vinculación (M7) → Compañero`. Sobre la **misma** criatura que quedó **restaurada** al final
de M6 (estado terminal `Restored`), el jugador establece un **vínculo voluntario**. Tras el vínculo,
la criatura deja de ser una presencia inerte y pasa a ser un **compañero activo visible** que
**sigue** al jugador a distancia controlada dentro de la escena. El resultado **no persiste**.

## Diagrama general del flujo
Vista rápida del flujo completo del milestone:

```text
Restored
    │
Interactuar
    │
    ▼
Bonding
    │
Aproximación voluntaria
    │
Temporizador
    │
Consentimiento
    ▼
Bonded
    │
Seguimiento
    ▼
Compañero
```

## 2. Objetivos
- Permitir al jugador **iniciar** la vinculación, de forma deliberada, sólo sobre una criatura
  **restaurada**, en condiciones normales de actuar y suficientemente cerca.
- Ejecutar una **secuencia breve, no interrumpible y sin posibilidad de fallo**: aproximación
  voluntaria de la criatura + registro de consentimiento.
- Convertir a la criatura en **compañero activo visible** que **sigue** al jugador a distancia controlada.
- Comunicar el resultado con feedback inequívoco: etiqueta **"Vínculo establecido"**, **ficha mínima**
  (nombre + afinidad provisional), **señal breve de ECO** y **señal visual provisional** de "vinculada".
- Garantizar **una sola** vinculación, **idempotente**, **gratuita**, **sin persistencia** y **sin
  regresiones** en M1–M6.

## 3. Arquitectura (descripción, sin código)
M7 extiende la máquina de estados de criatura ya existente (`CreatureBrain` como único orquestador
de transiciones; los estados devuelven un token `CreatureStateId` y nunca se referencian entre sí).
La extensión es **aditiva**, igual que M5 y M6: las criaturas ambientales (Verak normal) conservan
`{Idle, Patrol, Alert}` y **no** registran los estados de M7.

Piezas arquitectónicas de M7 (todas espejo de patrones existentes salvo el seguimiento):

| Pieza | Patrón que refleja | Rol en M7 |
|---|---|---|
| Estados `Bonding` y `Bonded` (nuevos tokens en `CreatureStateId`) | `Restoring`/`Restored` (M6) | Proceso de vínculo y estado de compañero. |
| Estado de proceso `Bonding` | `CreatureRestoringState` (M6) | Aproximación + consentimiento, con temporizador propio; completa a `Bonded`. |
| Temporizador de vínculo | `CreatureRestoreTimer` (M6) | Acumulador puro, avanzado por Tick, sin `Time`/coroutine; no interrumpible. |
| Estado `Bonded` | **nuevo** (no inerte, a diferencia de `Restored`) | Compañero: conduce el **seguimiento** vía `CreatureMovement`. |
| Interactuable de vínculo | `CreatureRestorationInteractable` (M6) | Único origen de `Restored → Bonding`; implementa `IInteractable`; disponible sólo si `Restored` + gate no bloqueado + en rango. |
| Razón de gate `Bonding` | `ControlBlockReason.Observation`/`Defeat` (bandera aditiva) | Mantiene al jugador inmóvil durante la secuencia de vínculo. |
| Presentador de vínculo | `CreatureRestorationPresentation` (M6) | Señal visual provisional (brillo de vínculo) leyendo el estado; sólo presentación. |
| Ficha + etiqueta + ECO | Presentadores/UI provisionales (M4/M6) | "Vínculo establecido", nombre + afinidad provisional, señal breve de ECO. |
| Mapeo de observación | `CreatureObservationSource.Resolve` (M4) | `Bonding`/`Bonded` → categoría observable (compañero en calma). |
| Bandera local `verak_vinculado` | estado de sesión no persistente | Marca el vínculo activo durante la escena; se pierde al recargar. |

Regla de dependencia (invariante del proyecto): la lógica **no** mueve `Transform` fuera de
`CreatureMovement`, **no** usa singletons/`Find`/`FindObjectOfType`, **no** consulta `Physics2D`
desde el `Brain`, y los presentadores son **solo visuales** (nunca cambian estado).

## 4. Estados involucrados
Estado inicial de M7 = estado final de M6: la criatura en **`Restored`**.

Nuevos estados (aditivos):
- **`Bonding`** — proceso temporal, no interrumpible. Al entrar: detiene el movimiento, inicia un
  temporizador propio (duración breve cuyo valor será parametrizable durante la implementación) y,
  durante la secuencia, la criatura **se aproxima** al jugador de forma dirigida (no controlada por
  el jugador). Al completar el temporizador, solicita `Bonded`. Su propia lógica **no puede
  cancelarlo**. Reentrada limpia (temporizador recreado en Enter).
- **`Bonded`** — estado de **compañero**. **No es inerte** (a diferencia de `Restored`): en cada
  Tick evalúa la posición del jugador y conduce el **seguimiento** a distancia controlada mediante
  `CreatureMovement`. No ataca, no patrulla, no vuelve a estados previos, no re-vincula.

Transición canónica de M7: **`Restored → Bonding → Bonded`**. `Bonding` sólo es alcanzable por un
`RequestTransition` externo emitido por el interactuable de vínculo (mismo esquema que
`Subdued → Restoring` en M6). `Bonded` no transiciona a ningún otro estado en M7.

Estados de M3–M6 (`Idle`, `Patrol`, `Alert`, `Chase`, `Attack`, `Subdued`, `Restoring`, `Restored`)
permanecen sin cambios de comportamiento.

## 5. Interacciones
- **Iniciar vínculo (jugador → criatura restaurada):** vía el interactuable de vínculo, integrado en
  el pipeline de interacción de M2 (detector/selector/controlador/proximidad). Prompt provisional:
  **"Vincular"**. Disponible **solo** si: la criatura está en `Restored`, el gate de control del
  jugador **no** está bloqueado (ni observando ni en derrota) y el jugador está en rango. Se
  **revalida** en el momento de confirmar: un intento inválido **no cambia nada**.
- **Secuencia de vínculo (criatura → jugador):** durante `Bonding`, la criatura **se aproxima por
  decisión propia** y el juego **registra el consentimiento**. El jugador permanece **inmóvil**
  (gate bloqueado por la razón `Bonding`).
- **Compañía (criatura ↔ jugador):** durante `Bonded`, la criatura **sigue** al jugador a distancia
  controlada. No hay órdenes, ni habilidades, ni combate cooperativo.
- **Observación del compañero (jugador → criatura):** la criatura vinculada permanece **observable**;
  su ficha/observación refleja que es un compañero en calma.

### Parámetros funcionales del seguimiento
Comportamiento esperado del compañero en `Bonded`, descrito **funcionalmente** (sin implementación ni
constantes numéricas impuestas):
- **Distancia mínima:** distancia por debajo de la cual el compañero **deja de acercarse** y se
  detiene; nunca se superpone al jugador.
- **Distancia máxima:** distancia por encima de la cual el compañero **comienza a acercarse** al jugador.
- **Banda de histéresis:** entre la distancia mínima y la máxima existe una **zona muerta**; dentro de
  ella el compañero **no** alterna nerviosamente entre acercarse y detenerse (evita el zumbido/jitter).
- **Velocidad configurable:** la velocidad de seguimiento es un **parámetro ajustable**, no una
  constante impuesta por este SPEC.
- **Orientación:** el compañero **encara la dirección de su movimiento** mientras se desplaza.
- **Cuando el jugador se detiene:** el compañero se aproxima hasta la banda y **se queda quieto**; no
  orbita, no empuja ni acosa al jugador.
- **Cuando el jugador cambia bruscamente de dirección:** el compañero **recalcula su objetivo de forma
  continua** y ajusta el rumbo de manera suave; no da saltos ni correcciones instantáneas.
- **Prohibido el teletransporte:** el compañero **nunca** se reposiciona de forma instantánea; siempre
  se desplaza de manera continua.
- **Prohibido el pathfinding:** el seguimiento **no** calcula rutas.
- **Prohibido NavMesh:** el seguimiento **no** usa navegación basada en malla.
- **Prohibida la navegación compleja:** sin evitación de obstáculos ni planificación; **seguimiento
  directo** con banda de distancia.

> **Propiedad del movimiento:** `CreatureMovement` continúa siendo el **único** responsable del
> desplazamiento físico de la criatura. El estado `Bonded` **únicamente decide el comportamiento de
> seguimiento**; **no** podrá modificar directamente `Transform.position`, `Rigidbody2D`, la velocidad
> (`Velocity`) ni ningún desplazamiento físico. **Toda locomoción deberá pasar por
> `CreatureMovement`.** Este principio es el mismo que rige `SpriteFlash` sobre `SpriteRenderer.color`.

## 6. Responsabilidades (por componente lógico)
- **Interactuable de vínculo:** único origen de `Restored → Bonding`. No guarda estado propio de
  vínculo, no gestiona el temporizador, no salta a `Bonded`, no emite presentación. Su única fuente
  de verdad es `CreatureBrain.CurrentStateId`.
- **Estado `Bonding`:** posee su temporizador; ordena la aproximación dirigida; completa a `Bonded`.
  No toca UI ni presentación; no lee `Time` directamente.
- **Temporizador de vínculo:** acumulador puro y determinista (Tick explícito); sin API de
  cancelar/pausar (el proceso no se interrumpe).
- **Estado `Bonded`:** conduce el seguimiento vía `CreatureMovement`; nada más.
- **Gate de control:** expone la razón `Bonding` (bandera aditiva) para inmovilizar al jugador
  durante la secuencia; se desbloquea al completar.
- **Bandera `verak_vinculado`:** estado de sesión no persistente; se activa al establecer el vínculo.
- **Presentadores (visual/UI/audio):** brillo de vínculo, "Vínculo establecido", ficha mínima y
  señal de ECO. Solo leen estado/eventos; **nunca** cambian estado.
- **Fuente de observación:** mapea `Bonding`/`Bonded` a la categoría observable adecuada.

## 7. Restricciones
- Se mantienen **sin cambios** el movimiento (M1), la interacción/observación (M2/M4), las criaturas
  ambientales (M3), el combate no letal (M5) y la restauración (M6).
- **No** se introduce persistencia, guardado, recursos, economía, inventario ni progresión.
- **Una sola** criatura vinculada activa; no hay colección, caja, equipo ni intercambio.
- El compañero **no** cruza transiciones de área (M7 es de escena única; sin persistencia entre escenas).
- El vínculo **no** se pierde ni se re-corrompe dentro de M7.
- No se define el origen de la alteración (canon abierto del Director).
- No se afirma lore mayor ni se inventan datos de canon; la señal de ECO es **provisional**.
- Extensión **aditiva**: las criaturas ambientales conservan exactamente `{Idle, Patrol, Alert}`.

## 8. Eventos
- **Vínculo posible** (derivado, no persistente): la criatura entra en `Restored` y el jugador queda
  en rango con capacidad de actuar → el prompt "Vincular" está disponible.
- **Vínculo iniciado:** el interactuable confirma → `RequestTransition(Bonding)`; el gate se bloquea
  con la razón `Bonding`; comienza la aproximación y el temporizador.
- **Consentimiento registrado / vínculo establecido:** el temporizador completa → `Bonding → Bonded`;
  se activa `verak_vinculado`; se dispara la etiqueta "Vínculo establecido", la ficha mínima y la
  señal de ECO; el gate se **desbloquea**.
- **Compañía activa:** en `Bonded`, cada Tick evalúa seguimiento.
- **Reinicio de escena:** al recargar, el estado vuelve a `Restored` sin vínculo (no persistencia).

Los eventos se comunican por los mecanismos ya usados en el proyecto (transiciones del `Brain`
observadas por presentadores que leen `CurrentStateId`, y/o eventos explícitos de C# análogos a
`Health.Depleted`). El SPEC **no** fija el mecanismo concreto; sí fija el comportamiento observable.

## 9. Dependencias
- **M6 (Restauración):** M7 arranca desde el estado `Restored`. Requiere que exista una criatura
  restaurada en escena.
- **M5 (Combate):** provee la contención previa (`Subdued`) que M6 restaura.
- **M2/M4 (Interacción/Observación):** el interactuable de vínculo reutiliza el pipeline de
  interacción; el compañero sigue siendo observable.
- **M1 (Movimiento/`PlayerControlGate`):** el gate inmoviliza al jugador durante la secuencia; el
  seguimiento del compañero usa `CreatureMovement`.
- **`CreatureIdentity`:** aporta el nombre para la ficha. La **afinidad provisional** de la ficha es
  dato **provisional** (no se expande `CreatureIdentity` a un ScriptableObject "Dios"; se resuelve en
  F5 como dato provisional acotado).

## 10. Flujo técnico (observable, sin implementación)
1. La criatura está en `Restored` (fin de M6). El interactuable de vínculo publica que la interacción
   "Vincular" es posible cuando el jugador está en rango y con capacidad de actuar.
2. El jugador confirma la interacción. Se **revalida** (`Restored` + gate libre + en rango). Si es
   válido: el `Brain` aplica `Bonding`; el gate se bloquea (`Bonding`).
3. En `Bonding`: la criatura se aproxima de forma dirigida; el temporizador avanza por Tick. La
   secuencia **no** se puede interrumpir ni fallar.
4. Al completar el temporizador, el `Brain` aplica `Bonded`. Se activa `verak_vinculado`; se disparan
   "Vínculo establecido", ficha mínima y señal de ECO; el gate se desbloquea.
5. En `Bonded`: la criatura **sigue** al jugador a distancia controlada; permanece observable.
6. Recargar la escena restablece `Restored` sin vínculo.

## 11. Casos límite
- Intentar vincular una criatura **no restaurada** (`Subdued`, combate, ambiental) → **no posible**.
- Intentar vincular con el **gate bloqueado** (observando o en derrota) → **no posible**.
- Intentar vincular **fuera de rango** → **no posible**.
- Intentar vincular una criatura **ya vinculada** (`Bonding`/`Bonded`) → **idempotente**, sin efecto.
- Soltar/entrar/salir del rango **durante** `Bonding` → **no** interrumpe (proceso no cancelable).
- `deltaTime` negativo o nulo, o Tick antes de Enter → sin regresión del temporizador (defensivo).
- Recarga o reinicio de escena → vuelve a `Restored` (sin persistencia).
- El jugador se mueve rápido en `Bonded` → el compañero mantiene distancia controlada, **no** se
  superpone al jugador y **no** teletransporta.
- Referencia de gate ausente en el interactuable → se trata como "no bloqueado" (el pipeline sigue
  filtrando por objetivo/rango), coherente con M6.
- Verak **ambiental** presente en escena → **nunca** ofrece "Vincular" (no registra los estados de M7).

## 12. QA esperado
Verificar **comportamiento observable**, parte automatizable (EditMode, patrón M3–M6) y parte manual:
- Sólo se vincula una criatura en `Restored`; no en otros estados ni fuera de rango/estado del jugador.
- La transición sigue `Restored → Bonding → Bonded`; `Bonding` sólo por origen externo.
- El proceso es de **duración breve** (parametrizable en implementación), **no** se interrumpe ni
  falla; el gate bloquea y luego desbloquea.
- Al completar: `verak_vinculado` activo; feedback "Vínculo establecido" + ficha + ECO percibidos.
- En `Bonded`, el compañero sigue al jugador a distancia controlada (banda min/max, sin solaparse).
- Vinculación **única** e **idempotente**; una sola criatura.
- **Sin persistencia**: recargar restablece `Restored`.
- **Sin regresiones** M1–M6 (suite verde; movimiento, interacción, observación, patrulla, combate,
  contención y restauración intactos).
- Pruebas de integración de escena/prefab (wiring), en la línea de `M5IntegrationAssetTests` y las
  pruebas de M6.

## 13. Riesgos
- **Percepción de captura/recompensa:** que el vínculo se lea como "obtener" o "premio por vencer";
  mitigar con aproximación voluntaria, etiqueta "Vínculo establecido" y feedback de consentimiento.
- **Alcance del seguimiento:** el seguimiento del compañero deberá resolverse mediante **movimiento
  directo con banda de distancia**; cualquier **navegación avanzada** (pathfinding, evitación de
  obstáculos, NavMesh) queda **explícitamente fuera del alcance de M7**. *Ceiling conocido: si en el
  futuro hiciera falta sortear obstáculos, se reevaluará como trabajo aparte.*
- **`Restored` era terminal:** en M6 `Restored` "nunca sale". En M7 `Restored` únicamente podrá salir
  hacia `Bonding` mediante una **transición solicitada externamente** (el interactuable de vínculo),
  sin que la criatura deambule por sí sola; hay riesgo de regresión de M6 si se abre de más.
- **Conflicto de gate:** `Bonding` es una **nueva razón aditiva** del `PlayerControlGate` y **nunca**
  reemplaza `Observation` ni `Defeat`; debe convivir con ellas sin interferir.
- **Expectativa de persistencia:** la ausencia de persistencia es una **limitación consciente del
  prototipo**; el jugador puede esperar que el compañero sobreviva a la recarga (mismo riesgo que M6).
  Debe **comunicarse mediante feedback** claro y documentarse como limitación.
- **Acompañamiento entre escenas:** el compañero **no** cruzará entre escenas; esta limitación **no**
  deberá resolverse mediante teletransportes automáticos ni soluciones temporales. Señalarla para no
  crear una regresión silenciosa.
- **Afinidad provisional:** la afinidad provisional **no** debe convertir `CreatureIdentity` en un
  objeto con responsabilidades excesivas (ScriptableObject "Dios"); mantener el dato provisional y acotado.
- **Señal de ECO:** narrativa; debe permanecer **provisional** y no afirmar lore mayor.
- **Propiedad del movimiento:** evitar que `Bonded` se convierta en un **segundo sistema de
  movimiento**. `CreatureMovement` debe seguir siendo el **único** propietario del desplazamiento
  físico; `Bonded` sólo decide el comportamiento de seguimiento.
- **Fuente única de verdad:** durante M7 la única fuente de verdad del vínculo en la sesión será la
  bandera local **`verak_vinculado`**. El vínculo **no** deberá inferirse desde el estado `Bonded`,
  la existencia del compañero ni la presentación visual.

## 14. Criterios de aceptación (milestone)
1. El jugador puede **vincular** una criatura **restaurada** mediante una acción deliberada; el
   proceso es de **duración breve**, no interrumpible y no puede fallar.
2. La transición observable es **`Restored → Bonding → Bonded`**, con `Bonding` alcanzable **solo**
   por el interactuable de vínculo.
3. Durante la secuencia el jugador queda **inmóvil** (gate `Bonding`) y la criatura **se aproxima
   por decisión propia**; al completar, el gate se **desbloquea**.
4. Al establecerse: se activa `verak_vinculado`; se muestran **"Vínculo establecido"**, **ficha
   mínima** (nombre + afinidad provisional) y una **señal breve de ECO**; hay una **señal visual
   provisional** de "vinculada".
5. La criatura vinculada queda como **compañero activo visible** y **sigue** al jugador a distancia
   controlada, sin superponerse ni teletransportarse, y permanece **observable**.
6. La vinculación es **única**, **idempotente**, **gratuita** y **segura**; una sola criatura.
7. El resultado **no persiste**: recargar la escena o reiniciar restablece `Restored` sin vínculo.
8. **Sin regresiones**: M1–M6 permanecen intactos (suite verde) y las criaturas ambientales conservan
   `{Idle, Patrol, Alert}`.

---

# Plan de fases

> Todas las fases de M7. Ninguna queda "por definir". El orden recomendado es **F1 → F2 → F3 → F4 →
> F5 → F6** (dependencia lineal). Cada fase entrega valor verificable y no invade milestones
> anteriores. Se sigue **TDD** y el patrón EditMode de M3–M6. **No** se implementa código en esta
> etapa: este plan describe qué deberá implementarse.

## M7 F1 — Arquitectura de Vinculación (estados y tokens)

**Objetivo**
Definir la extensión de la máquina de estados que soporta el vínculo, sin interacción ni seguimiento
todavía.

**Descripción completa**
Añadir los tokens `Bonding` y `Bonded` a `CreatureStateId` (aditivo). Crear el estado de proceso
`Bonding` (con temporizador propio, espejo de `CreatureRestoringState`/`CreatureRestoreTimer`,
duración breve parametrizable durante la implementación, no interrumpible, completa a `Bonded`) y el
estado `Bonded` (por ahora sin seguimiento: entra, detiene el movimiento y es estable). Extender el
proveedor de estados del Verak (`AlteredVerakSetup`) para registrar `Bonding` y `Bonded`, permitiendo
la transición `Restored → Bonding → Bonded` **solo** por `RequestTransition` externo. `Restored` deja
de ser "terminal por diseño" en el sentido estricto: puede salir **únicamente** hacia `Bonding` por
origen externo; nunca deambula por sí solo.

> **Nota de implementación:** aunque F1 entrega una sola feature arquitectónica, internamente podrá
> implementarse mediante **pequeños pasos incrementales**, siempre que **no** cambien el alcance ni
> los entregables aquí definidos.

**Incluye**
- Tokens `Bonding` y `Bonded` en `CreatureStateId`.
- Estado `Bonding` + temporizador de vínculo (acumulador puro, Tick explícito).
- Estado `Bonded` estable (sin seguimiento aún).
- Registro de ambos estados en el proveedor del Verak.
- Transición `Restored → Bonding → Bonded` disparable por `RequestTransition` externo.

**No incluye**
- Interactuable/origen de la interacción (F2).
- Aproximación, consentimiento ni bloqueo de gate (F3).
- Seguimiento del compañero (F4).
- Presentación, ficha, ECO (F5).
- Observación, bandera `verak_vinculado`, no-persistencia (F6).

**Entregables**
- Estados y tokens nuevos.
- Pruebas EditMode: `Restored` sólo sale a `Bonding` por origen externo; `Bonding` completa a
  `Bonded` por temporizador; `Bonded` es estable; criaturas ambientales conservan `{Idle,Patrol,Alert}`.

**Aceptación**
- La transición `Restored → Bonding → Bonded` ocurre solo por solicitud externa y por temporizador.
- Sin regresiones en M3–M6 (suite verde).

**Dependencias**
- M6 completado (estado `Restored`), máquina de estados de `CreatureBrain`.

**Riesgos**
- Regresión de M6 al abrir `Restored`. Mitigar con pruebas que confirmen que `Restored` no transiciona salvo a `Bonding` por origen externo.

---

## M7 F2 — Interacción de Vinculación (origen)

**Objetivo**
Dar al jugador la forma **deliberada** de iniciar el vínculo, integrada en el pipeline de interacción.

**Descripción completa**
Crear el interactuable de vínculo (espejo de `CreatureRestorationInteractable`) que implementa
`IInteractable` por composición y reutiliza el detector/selector/controlador/proximidad de M2.
`CanInteract` es verdadero **solo** si la criatura está en `Restored`, el `PlayerControlGate` no está
bloqueado y el jugador está en rango. `Execute` **revalida** y, si procede, emite
`RequestTransition(Bonding)`. Prompt provisional "Vincular". No guarda estado propio, no gestiona el
temporizador, no emite presentación.

**Incluye**
- Interactuable de vínculo (`IInteractable`) como único origen de `Restored → Bonding`.
- Condición de disponibilidad (`Restored` + gate libre + en rango) y revalidación en confirmación.
- Prompt "Vincular".

**No incluye**
- Bloqueo de gate durante la secuencia y aproximación (F3).
- Seguimiento (F4), presentación/ficha/ECO (F5), observación/bandera/no-persistencia (F6).

**Entregables**
- Componente interactuable de vínculo.
- Pruebas EditMode: disponible solo en `Restored`; no disponible con gate bloqueado, fuera de rango,
  o en otros estados; `Execute` inválido no cambia nada; `Execute` válido solicita `Bonding`.

**Aceptación**
- El único camino a `Bonding` es esta interacción, bajo sus condiciones.
- Intentos inválidos no producen efecto.

**Dependencias**
- F1 (estados). M2 (pipeline de interacción). M1 (`PlayerControlGate`).

**Riesgos**
- Divergencia con la fuente de verdad `CurrentStateId`. Mitigar reutilizando exactamente el patrón de M6.

---

## M7 F3 — Proceso de Vinculación y consentimiento

**Objetivo**
Convertir la solicitud en una **secuencia breve, dirigida y no interrumpible**: aproximación
voluntaria de la criatura + jugador inmóvil + registro de consentimiento.

**Descripción completa**
Durante `Bonding`, la criatura **se aproxima** al jugador de forma dirigida (movimiento conducido por
el estado vía `CreatureMovement`, no por el jugador) mientras el temporizador corre. Añadir la razón
`Bonding` al `ControlBlockReason` (bandera aditiva) y bloquear el `PlayerControlGate` al iniciar,
desbloqueándolo al completar. El proceso **no** puede interrumpirse ni fallar; al completar el
temporizador, `Bonding → Bonded` y se considera **consentimiento registrado**.

**Incluye**
- Aproximación dirigida durante `Bonding`.
- Razón de gate `Bonding` (aditiva) que inmoviliza al jugador; desbloqueo al completar.
- Confirmación de que el proceso es no interrumpible y sin fallo.

**No incluye**
- Seguimiento posterior (F4), presentación/ficha/ECO (F5), observación/bandera/no-persistencia (F6).

**Entregables**
- Comportamiento de aproximación en `Bonding` y gestión del gate.
- Pruebas EditMode/PlayMode: duración breve (parametrizable); no interrumpible (input/rango durante el
  proceso no lo cancelan); gate bloquea al iniciar y desbloquea al completar; `Observation`/`Defeat` no interfieren.

**Aceptación**
- El jugador queda inmóvil durante la secuencia y la criatura se aproxima por sí misma.
- El proceso completa siempre a `Bonded`; nunca se interrumpe ni falla.

**Dependencias**
- F1 (estado `Bonding`), F2 (origen). M1 (`PlayerControlGate`), `CreatureMovement`.

**Riesgos**
- Interferencia entre razones de gate. Mitigar con pruebas de banderas aditivas.
- La aproximación no debe leerse como movimiento inducido por el jugador.

---

## M7 F4 — Comportamiento de compañero (seguimiento)

**Objetivo**
Que la criatura vinculada **siga** al jugador a **distancia controlada** dentro de la escena.

**Descripción completa**
Dotar al estado `Bonded` de seguimiento: cada Tick evalúa la posición del jugador y conduce el
movimiento con `CreatureMovement` de modo que la criatura se acerque cuando supera una distancia
máxima y se detenga dentro de una banda muerta (distancia mínima), **sin superponerse** al jugador y
**sin teletransportarse**. Seguimiento **simple**: sin pathfinding, sin evitación de obstáculos, sin
NavMesh (canon). No hay órdenes, habilidades ni combate cooperativo. Los parámetros funcionales
detallados de este comportamiento están definidos en **§5 → Parámetros funcionales del seguimiento**.

**Incluye**
- Seguimiento con banda de distancia (min/max) en `Bonded`, orientado al movimiento.
- Detención dentro de la banda muerta.

**No incluye**
- Pathfinding/evitación de obstáculos, órdenes, habilidades o combate del compañero.
- Acompañamiento entre escenas (fuera de alcance).
- Presentación/ficha/ECO (F5), observación/bandera/no-persistencia (F6).

**Entregables**
- Lógica de seguimiento en `Bonded`.
- Pruebas EditMode (matemática de seguimiento determinista, en la línea de `CreaturePatrolMath`):
  se acerca al superar el máximo; se detiene en la banda; nunca se solapa; encara la dirección de avance.

**Aceptación**
- El compañero mantiene distancia controlada ante jugador quieto y en movimiento.
- Sin solapamiento ni saltos; sin pathfinding.

**Dependencias**
- F1 (estado `Bonded`), F3 (proceso que lleva a `Bonded`). `CreatureMovement`, M1 (posición del jugador).

**Riesgos**
- Deriva de alcance hacia navegación compleja. Mantener el seguimiento mínimo y acotado.

---

## M7 F5 — Presentación y feedback (ficha, brillo de vínculo, señal de ECO)

**Objetivo**
Comunicar de forma inequívoca que el vínculo comenzó, se estableció y que hay un compañero.

**Descripción completa**
Presentadores **solo visuales/UI/audio** que leen estado/eventos y **nunca** cambian estado (patrón
`CreatureRestorationPresentation`): (a) **brillo de vínculo** provisional durante `Bonding` y en
`Bonded`, compuesto por el mismo canal de color que M6 (`SpriteFlash`) para no crear un segundo
escritor de color; (b) etiqueta **"Vínculo establecido"** (nunca "Criatura obtenida") al completar;
(c) **ficha mínima** con **nombre** (de `CreatureIdentity`) y **afinidad provisional** (dato
provisional acotado, sin expandir `CreatureIdentity`); (d) **señal breve de ECO** provisional
(texto/audio placeholder) al establecerse el vínculo.

> **Propiedad del color:** `SpriteFlash` continúa siendo el **único** propietario de
> `SpriteRenderer.color`. Todo efecto visual del vínculo deberá **componerse utilizando
> `SpriteFlash`**. **No** podrán existir escritores adicionales del color del `SpriteRenderer`. Esta
> decisión proviene de M6 y se documenta aquí para evitar regresiones arquitectónicas.

**Incluye**
- Presentador visual del brillo de vínculo (provisional), coordinado con `SpriteFlash`.
- UI de "Vínculo establecido" + ficha mínima (nombre + afinidad provisional).
- Señal de ECO provisional.

**No incluye**
- Arte/animación/audio definitivos; lore mayor de ECO.
- Lógica de estado (no cambia estados).
- Observación/bandera/no-persistencia (F6).

**Entregables**
- Presentadores de M7 (visual + UI + señal de ECO), provisionales.
- Pruebas de presentación deterministas (patrón M6): el presentador refleja el estado y no lo altera;
  "Vínculo establecido" aparece sólo al completar; el brillo se limpia al salir de los estados de M7.

**Aceptación**
- El feedback de inicio/establecimiento/compañía se percibe con claridad.
- La etiqueta es "Vínculo establecido", nunca "Criatura obtenida".
- Ningún presentador modifica estado.

**Dependencias**
- F1 (estados/eventos), F3 (evento de establecimiento), F4 (compañero). M4/M6 (patrones de presentación), `CreatureIdentity`.

**Riesgos**
- Convertir `CreatureIdentity` en SO "Dios" por la afinidad. Mantener provisional y acotado.
- Segundo escritor de color descoordinado. Reutilizar `SpriteFlash` como único compositor.

---

## M7 F6 — Observación del compañero, no-persistencia e integración/regresión

**Objetivo**
Cerrar el milestone: compañero observable, bandera de vínculo, ausencia de persistencia, y ausencia
de regresiones, con verificación de integración.

**Descripción completa**
Mapear `Bonding`/`Bonded` a la categoría observable adecuada en `CreatureObservationSource.Resolve`
(compañero en calma; sin mislabel), manteniendo la criatura **observable**. Activar la bandera local
**`verak_vinculado`** al establecerse el vínculo (estado de sesión, **no** persistente). Confirmar
que recargar/reiniciar la escena restablece `Restored` **sin** vínculo. Verificar unicidad e
idempotencia (una sola criatura, sin re-vínculo) y **ausencia de regresiones** en M1–M6, con pruebas
de integración de escena/prefab (wiring) en la línea de las de M5/M6.

> **Fuente de verdad del vínculo:** durante M7 existirá una **única** fuente de verdad para saber si
> el vínculo fue establecido durante la sesión: la bandera local **`verak_vinculado`**. El vínculo
> **no** deberá inferirse desde el estado `Bonded`, la existencia del compañero, la presentación
> visual ni efectos temporales. **No deberán existir estados duplicados ni múltiples indicadores
> equivalentes.**

**Incluye**
- Mapeo de observación de `Bonding`/`Bonded`; compañero observable.
- Bandera `verak_vinculado` local no persistente.
- Verificación de no-persistencia (recarga restablece `Restored`).
- Unicidad/idempotencia y suite de regresión M1–M6.
- Pruebas de integración de escena/prefab.

**No incluye**
- Persistencia/guardado, santuarios, múltiples vínculos, acompañamiento entre escenas.
- Arte/audio definitivos.

**Entregables**
- Mapeo de observación y bandera de vínculo.
- Pruebas EditMode/integración: observación del compañero; recarga sin persistencia; unicidad e
  idempotencia; suite M1–M6 verde; wiring de escena/prefab válido.

**Aceptación**
- El compañero es observable y correctamente etiquetado.
- La bandera se activa al vincular y se pierde al recargar (sin persistencia).
- Vinculación única e idempotente; sin regresiones M1–M6; wiring verificado.

**Dependencias**
- F1–F5. M4 (observación), M1 (recarga de escena), suites de M1–M6.

**Riesgos**
- Mislabel de observación para estados nuevos. Usar `Unknown` como fallback honesto donde aplique.
- Expectativa de persistencia del jugador. Documentar la limitación y comunicarla por feedback.

# ADR-M3-005 — `CreatureAnimator` como presenter puro

- **Estado:** Aprobado (Director, Fase 0B).
- **SPEC:** [`../SYNORA_SPEC_M3_Criaturas_v0.1.md`](../SYNORA_SPEC_M3_Criaturas_v0.1.md)

## Contexto

M3 introduce el primer uso de animación del proyecto. Debe quedar claro que la inteligencia de la criatura vive en código y que la animación es solo presentación, para evitar lógica de gameplay dispersa en Animator Controllers.

## Decisión

`CreatureAnimator` es un **presenter puro**:

- Recibe **comandos** desde `CreatureBrain`/estados: `PlayIdle()`, `PlayWalk()`, `PlayAlert()`, `PlayReaction()`, `SetFacing(Vector2Int)`.
- **No** toma decisiones de gameplay, **no** detecta al Player, **no** administra timers, **no** cambia estados del Brain, **no** usa `StateMachineBehaviour` para IA.
- Traduce estado visual + facing a parámetros del Animator Controller (`FacingIndex`, `VisualState`) y a `SpriteRenderer.flipX`.

**Estrategia de dirección:**

- Sprites base: **Down**, **Up**, **Side (Left)**.
- **Right = `flipX(Side)`** cuando `isSymmetric == true`.
- Criaturas **asimétricas** futuras: sprites **Right independientes** (rama alternativa del presenter), soportado por diseño, no implementado en M3.

**Reaction:** soportada como **evento visual puntual** (one-shot); no es estado lógico; no existe `ReactionState`; no altera Idle/Patrol/Alert. En M3 el Brain no la invoca (sin disparador aprobado).

## Contrato del Animator Controller

Selección de clip **por parámetros** fijados desde código. Prohibido: detección, timers, cambios de estado del Brain, lógica de IA en `StateMachineBehaviour`.

## Consecuencias

Sustituir arte (placeholder → definitivo) no requiere cambios en `CreatureBrain`/`CreatureMovement`/`CreatureSensor`. La lógica permanece 100% testeable en código; la capa visual es reemplazable e independiente.

## Distinción documentada

- **Estado lógico** → `CreatureStateId` (Idle/Patrol/Alert), en código.
- **Clip visual** → un `.anim` (p. ej. `Verak_Walk_Side`), en arte.
- **Evento visual puntual** → `Reaction`, overlay one-shot disparable por `CreatureAnimator`.

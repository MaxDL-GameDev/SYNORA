# ADR-M3-002 — Layer `Creatures` y matriz de colisión

- **Estado:** Propuesta aprobada funcionalmente; **índice y aplicación en ProjectSettings pendientes de preflight read-only** (Fase 6).
- **SPEC:** [`../SYNORA_SPEC_M3_Criaturas_v0.1.md`](../SYNORA_SPEC_M3_Criaturas_v0.1.md)

## Contexto

Las criaturas necesitan colisionar con el entorno (no atravesar paredes) sin empujar ni bloquear al Player, y ser sensadas por su propia detección. Layers existentes: `Player=8`, `Environment=9`, `Transitions=10`, `Interactables=11`. El índice `12` fue **verificado libre** en `ProjectSettings/TagManager.asset` (lectura del 2026-07-20).

## Decisión (funcional, aprobada)

- Nueva layer **`Creatures`**.
- **Creature ↔ Environment:** colisión sólida.
- **Creature ↔ Player:** sin colisión física (evita bloqueos y empujones).
- Sensado e interacción mediante **layer masks, overlap y triggers**, no por colisión física.

## Pendiente de preflight (no congelado)

- El **índice tentativo 12** debe reverificarse con inspección read-only **justo antes** de modificar ProjectSettings en Fase 6 (por si cambió el estado del proyecto).
- La matriz de colisión 2D (`Creatures` vs cada layer) se aplicará solo en Fase 6.
- **Creature ↔ Creatures:** decisión menor a confirmar en preflight (propuesto: que no se solapen o que se ignoren; no crítico para M3).

## Restricciones

- **En Fase 0C no se modifica ProjectSettings ni layers.** Este ADR es documental.
- La aplicación real (layer + matriz) es un cambio de Project Settings dentro del alcance M3, sujeto a autorización explícita en su fase.

## Consecuencias

Las criaturas patrullan contenidas por el entorno sin interferir físicamente con el Player. La detección no depende de colisión física sino de `OverlapCircle` filtrado por layer (ver [ADR-M3-003](./ADR-M3-003-dual-radius-sensor.md)).

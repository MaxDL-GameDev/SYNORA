# ADR-M3-003 — Sensor de percepción con dos radios e histéresis

- **Estado:** Aprobado (Director, Fase 0B).
- **SPEC:** [`../SYNORA_SPEC_M3_Criaturas_v0.1.md`](../SYNORA_SPEC_M3_Criaturas_v0.1.md)

## Contexto

Un único radio de detección produce parpadeo Alert↔Patrol cuando el Player oscila en el borde. Se requiere una detección estable, testeable y sin alloc por frame.

## Decisión

Percepción con **dos radios** más **linger temporal**:

- `detectionRadius`: entrada a Alert.
- `loseRadius` (≥ `detectionRadius`): permanencia en Alert.
- `alertLingerDuration`: retardo antes de volver a Patrol tras salir de `loseRadius`.

Reglas:

- **Idle/Patrol → Alert** cuando el Player entra en `detectionRadius`.
- **Alert permanece** mientras el Player esté dentro de `loseRadius`.
- Al salir de `loseRadius`, comienza `alertLingerDuration`.
- Si el Player **reingresa** antes de expirar, se cancela/resetea la salida y permanece Alert.
- Si permanece fuera hasta **expirar** el linger, **Alert → Patrol**.

Esto aporta histéresis **espacial** (dos radios) y **temporal** (linger). No se usa únicamente un timer.

## Implementación de referencia

- `CreatureSensor` (MonoBehaviour): una `Physics2D.OverlapCircle(origin, loseRadius, playerFilter, buffer)` por `FixedUpdate`, con buffer reutilizado y `ContactFilter2D` (`useLayerMask` + `SetLayerMask(Player)` + `useTriggers`) construido en `Awake` — patrón `InteractionDetector`. La física se consulta con el **radio mayor** (`loseRadius`).
- Decisión en **lógica pura testeable**:

```csharp
public enum SensorVerdict { RemainCalm, BecomeAlert, RemainAlert, ReturnToPatrol }

public static class CreatureSensing
{
    public static SensorVerdict Evaluate(
        bool currentlyAlert, float playerDistanceSqr,
        float detectionRadius, float loseRadius,
        float lingerElapsed, float alertLingerDuration);
}
```

`OnValidate` de `CreatureIdentity` garantiza `loseRadius >= detectionRadius`.

## Consecuencias

Estabilidad de Alert sin parpadeo; comportamiento determinista y cubierto por `CreatureSensingTests`. Sin line-of-sight en M3 (fuera de alcance): la detección es por presencia dentro del radio.

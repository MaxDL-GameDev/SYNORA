# ADR-M3-004 — Colocación de criaturas por escena y patrulla PingPong

- **Estado:** Aprobado (Director, Fase 0B/0C).
- **SPEC:** [`../SYNORA_SPEC_M3_Criaturas_v0.1.md`](../SYNORA_SPEC_M3_Criaturas_v0.1.md)

## Contexto

Las criaturas deben existir en escenas concretas (M3: Verak ×2 en `ClaroExterior`) con rutas de patrulla propias. M1/M2 ya establecieron la convención de **listas serializadas explícitas por escena** (`sceneExaminables`, `spawnPoints`) sin búsquedas en runtime.

## Decisión

**Colocación:**

- Instancias del **prefab autocontenido** de criatura colocadas directamente en la escena.
- Cada instancia lleva su **ruta de patrulla como datos de instancia de escena** (Transforms / lista serializada `Transform[] patrolPoints`).
- Registro por lista/serialización explícita; **sin** `Find`/`FindObjectsOfType`, **sin** spawner de runtime en M3.
- Verak ×2 usan el **mismo prefab** y el **mismo `CreatureIdentity`**; se diferencian solo por sus rutas de instancia.

**Patrulla (modo definitivo M3): PingPong.**

- Recorrido `0→1→…→N-1→N-2→…→1→0`.
- Mínimo recomendado 2 puntos; soporte técnico para N.
- Determinista; pausa Idle al llegar; sin destinos aleatorios; sin NavMesh; sin pathfinding; sin stuck detection compleja.

```csharp
public static class CreaturePatrolMath
{
    public static bool HasArrived(Vector2 current, Vector2 target, float arrivalThreshold);
    public static int  NextIndex(int index, int count, ref int direction); // invierte direction en extremos
}
```

## Casos límite

| Caso | Comportamiento |
|---|---|
| 0 puntos | `LogError` en `Awake`; permanece en Idle, sin crash. |
| 1 punto | camina hacia él una vez y queda en Idle allí. |
| Referencia destruida | guard `IsAlive`; se salta y avanza; si no queda válida → Idle. |
| Dos puntos coincidentes | arribo inmediato; doble pausa breve; índice avanza (benigno). |
| Bloqueo contra Environment | sin recovery en M3; mitigación por autoría (puntos en espacio libre, validado en preflight de Fase 6). |

## Justificación de PingPong sobre Loop

Para un recorrido lineal de "unos metros" el vaivén es el movimiento natural y evita la pierna de retorno larga del último al primer punto que impone `Loop`. No se implementan ambos modos (no son necesarios en M3).

## Consecuencias

Rutas independientes por instancia sin lógica compartida frágil; colocación coherente con M1/M2; determinismo testeable en `CreaturePatrolMathTests`.

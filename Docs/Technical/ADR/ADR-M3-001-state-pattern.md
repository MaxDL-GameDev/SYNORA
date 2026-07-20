# ADR-M3-001 — Patrón de estados para el comportamiento de criaturas

- **Estado:** Aprobado (Director, Fase 0B).
- **Contexto de milestone:** M3 — Criaturas y Ecosistema Vivo.
- **SPEC:** [`../SYNORA_SPEC_M3_Criaturas_v0.1.md`](../SYNORA_SPEC_M3_Criaturas_v0.1.md)

## Contexto

M3 introduce criaturas con comportamiento por estados (Idle/Patrol/Alert) y una hoja de ruta futura de hasta ~9 estados (Follow, Attack, Flee, Bond, Sleep). M2 resolvió su única máquina de estados (`InteractionController`) con `enum + switch`, adecuado para 3 estados fijos. El Director requiere estados **independientes** y **SOLID**, y explícitamente rechaza "una clase gigante con muchos if".

Tensión evaluada: consistencia estricta con M2 (`enum+switch`) vs. escalabilidad e independencia de estados. M2 y M3 resuelven problemas de escala distintos, por lo que no hay conflicto real: M3 puede adoptar un patrón distinto por una necesidad justificada.

## Decisión

Se adopta un **patrón de estados polimórfico mínimo**:

- Interfaz delgada `ICreatureState` con `Enter`, `Tick`, `Exit`.
- Una clase por estado: `IdleState`, `PatrolState`, `AlertState`.
- Los estados **no se conocen entre sí**, **no instancian** otros estados y **no guardan referencias** a otros estados.
- `Tick` devuelve un **token neutral** de transición `CreatureStateId?` (`null` = permanecer).
- `CreatureBrain` resuelve `id → instancia` mediante un mapa construido **una sola vez** en `Awake`.
- **No** se usa `enum+switch` como arquitectura del comportamiento (el `switch` solo aparece, si acaso, en la resolución interna del Brain).
- **No** se crean jerarquías de herencia entre estados (`BaseCreatureState`, `MovingCreatureState`, etc.) salvo necesidad real aprobada en otro milestone.

Firma canónica:

```csharp
public enum CreatureStateId { Idle, Patrol, Alert }

public interface ICreatureState
{
    void Enter(CreatureContext context);
    CreatureStateId? Tick(CreatureContext context, float deltaTime);
    void Exit(CreatureContext context);
}
```

## Consecuencias

**Positivas:** cada estado es una unidad aislada y testeable; agregar estados futuros no toca a los existentes; retorno value-type ⇒ cero alloc por transición; instancias creadas una sola vez ⇒ sin creación por frame; el `CreatureBrain` es el único punto de resolución de transiciones.

**Negativas / costos:** primer sitio del proyecto con este patrón (leve divergencia estilística respecto a M2); una interfaz + un contexto adicionales frente al `enum+switch`.

## Alternativa descartada

`enum + switch` estilo M2: máxima consistencia, pero contradice el requisito de independencia de estados y escala mal hacia ~9 estados (el `switch` se vuelve la "clase gigante con ifs" que se busca evitar).

## Cumplimiento verificable

El estado no nombra clases concretas de otros estados; no hay `new IdleState()`/`new AlertState()` dentro de un estado; el mapa de estados se construye en `Awake`; los tests de `CreatureBrain` verifican transiciones permitidas, ausencia de transiciones prohibidas y ausencia de alloc por transición.

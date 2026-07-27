namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Neutral transition token for the creature state machine. States never
    /// reference each other's concrete classes; they return this id and
    /// CreatureBrain resolves it to the corresponding state instance.
    /// </summary>
    public enum CreatureStateId
    {
        Idle,
        Patrol,
        Alert,
        // Altered Verak combat states (M5 Fase 6). Additive: ambient creatures never
        // register these, so Verak normal (M3) keeps exactly {Idle, Patrol, Alert}.
        Chase,
        Attack,
        Subdued,
        // M6 restoration states (additive). Behavioral flow: Subdued → Restoring → Restored.
        // Ambient creatures never register these; Verak normal (M3) keeps {Idle, Patrol, Alert}.
        Restoring,
        Restored
    }
}

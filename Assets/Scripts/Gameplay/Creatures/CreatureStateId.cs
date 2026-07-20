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
        Alert
    }
}

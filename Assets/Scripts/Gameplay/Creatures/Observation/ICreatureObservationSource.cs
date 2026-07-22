namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Read-only, public view of a creature for observation purposes. It exposes data,
    /// never commands: there are no setters, no transition calls, no Brain exposure, and
    /// no dependency on UI or the interaction system. The interaction/adapter layer
    /// consults this contract; nothing here can control creature behavior.
    /// </summary>
    public interface ICreatureObservationSource
    {
        /// <summary>Human-readable name shown to the player.</summary>
        string DisplayName { get; }

        /// <summary>The observable category matching the creature's current internal state.</summary>
        CreatureObservationState CurrentObservationState { get; }
    }
}

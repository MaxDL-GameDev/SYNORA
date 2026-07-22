namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Public, presentation-facing category of what the player observes in a creature
    /// right now. Deliberately decoupled from the internal gameplay enum
    /// <see cref="CreatureStateId"/>: several internal states may map to one observable
    /// category, and observation content (UI, text) must never depend on Brain details.
    ///
    /// <see cref="Unknown"/> is the honest fallback for an internal state that has no
    /// defined observable meaning (e.g. a future gameplay state, or a missing Brain).
    /// It exists so the system never silently mislabels an unmapped state as Calm.
    /// </summary>
    public enum CreatureObservationState
    {
        Calm,
        Roaming,
        Watchful,
        Unknown
    }
}

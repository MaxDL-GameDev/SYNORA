namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Thin polymorphic state contract for creature behavior. Each state is an
    /// independent class; a state must not instantiate or hold references to
    /// other states. Tick returns null to remain, or a CreatureStateId to request
    /// a transition; CreatureBrain owns the resolution.
    /// </summary>
    public interface ICreatureState
    {
        void Enter(CreatureContext context);

        CreatureStateId? Tick(CreatureContext context, float deltaTime);

        void Exit(CreatureContext context);
    }
}

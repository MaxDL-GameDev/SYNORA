namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Terminal restored state (M6): calm, ambient, non-hostile. On entry it stops
    /// movement; thereafter it is inert and NEVER leaves — no patrol, chase, attack, or
    /// any transition request, and it does not start timers. Mirrors
    /// CreatureSubduedState's terminal shape. Stateless.
    /// </summary>
    public sealed class CreatureRestoredState : ICreatureState
    {
        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            return null; // terminal: never leaves Restored
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

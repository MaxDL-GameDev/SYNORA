namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Stable bonded state (M7 F1): its single responsibility is to REPRESENT that the
    /// creature became bonded (a companion). On entry it stops movement; thereafter it is
    /// inert and NEVER leaves — no patrol, chase, attack, or any transition request, and
    /// it does not start timers. Mirrors CreatureRestoredState's stable shape. Stateless.
    ///
    /// F1 scope: companion following, presentation, UI, audio, the verak_vinculado flag
    /// and any additional observation belong to later phases (F4–F6) and are intentionally
    /// not implemented here. This state is the extension point they will build on.
    /// </summary>
    public sealed class CreatureBondedState : ICreatureState
    {
        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            return null; // stable: never leaves Bonded (following arrives in F4)
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

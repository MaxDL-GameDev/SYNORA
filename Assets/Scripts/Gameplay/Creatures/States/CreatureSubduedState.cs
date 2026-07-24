namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Terminal contained state for the Altered Verak (M5 non-lethal outcome). On entry
    /// it stops movement and cancels any active attack; thereafter it is inert — no
    /// movement, no attacks, no automatic exit. It does NOT die, reset Health, destroy
    /// components, disable the GameObject, restore or bond (those belong to M6). The
    /// GameObject stays alive as a hook for future restoration. Stateless.
    /// </summary>
    public sealed class CreatureSubduedState : ICreatureState
    {
        private readonly CreatureAttackController attack;

        public CreatureSubduedState(CreatureAttackController attack)
        {
            this.attack = attack;
        }

        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            attack?.Cancel();
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            return null; // terminal: never leaves Subdued
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

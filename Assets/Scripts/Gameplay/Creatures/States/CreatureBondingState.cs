namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Temporary, non-interruptible bonding process (M7 F1): Restored → Bonding →
    /// Bonded. On entry it stops movement and starts a fresh CreatureBondingTimer — the
    /// state OWNS its timer; CreatureBrain stays the transition orchestrator only. Each
    /// Tick advances the timer and, once it completes, requests Bonded. It never moves,
    /// attacks, emits events, touches the Animator/UI, uses coroutines, or reads Time
    /// directly; its own logic cannot cancel the process. Not stateless: it holds a
    /// per-run timer, recreated on Enter so a re-entry restarts cleanly.
    ///
    /// F1 scope: this state only REPRESENTS the process. The voluntary approach movement
    /// and the player control gate belong to F3 and are intentionally not implemented here.
    /// </summary>
    public sealed class CreatureBondingState : ICreatureState
    {
        private readonly float duration;
        private CreatureBondingTimer timer;

        public CreatureBondingState(float duration)
        {
            this.duration = duration;
        }

        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            timer = new CreatureBondingTimer(duration); // fresh run (the timer normalizes the duration)
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            if (timer == null)
            {
                return null; // defensive: Tick before Enter
            }

            timer.Tick(deltaTime);
            return timer.IsComplete ? CreatureStateId.Bonded : (CreatureStateId?)null;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Temporary, non-interruptible restoration behavior (M6): Subdued → Restoring →
    /// Restored. On entry it stops movement and starts a fresh CreatureRestoreTimer —
    /// the state OWNS its timer; CreatureBrain stays the transition orchestrator only.
    /// Each Tick advances the timer and, once it completes, requests Restored. It never
    /// moves, attacks, emits events, touches the Animator/UI, uses coroutines, or reads
    /// Time directly; its own logic cannot cancel the process. Not stateless: it holds a
    /// per-run timer, recreated on Enter so a re-entry restarts cleanly.
    /// </summary>
    public sealed class CreatureRestoringState : ICreatureState
    {
        private readonly float duration;
        private CreatureRestoreTimer timer;

        public CreatureRestoringState(float duration)
        {
            this.duration = duration;
        }

        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            timer = new CreatureRestoreTimer(duration); // fresh run (the timer normalizes the duration)
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            if (timer == null)
            {
                return null; // defensive: Tick before Enter
            }

            timer.Tick(deltaTime);
            return timer.IsComplete ? CreatureStateId.Restored : (CreatureStateId?)null;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

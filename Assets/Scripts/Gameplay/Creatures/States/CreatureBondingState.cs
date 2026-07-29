using UnityEngine;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Temporary, non-interruptible bonding process (M7 F3): Restored → Bonding → Bonded.
    /// On entry it stops movement and starts a fresh CreatureBondingTimer — the state OWNS
    /// its timer; CreatureBrain stays the transition orchestrator only. Each Tick advances
    /// the timer and, while it is not complete, drives a controlled approach toward the
    /// detected player through CreatureMovement (F3). It never moves the Transform directly,
    /// attacks, emits events, touches the Animator/UI, uses coroutines, or reads Time
    /// directly; its own logic cannot cancel the process. Not stateless: it holds a per-run
    /// timer, recreated on Enter so a re-entry restarts cleanly.
    ///
    /// Bonding is non-interruptible: the ONLY exit is Bonded (on timer completion). Unlike
    /// CreatureChaseState, losing the player does NOT leave the state — the creature simply
    /// holds still while the timer runs. Player control blocking is owned by
    /// CreatureBondingControlBlock (F3), not by this state. Presentation, the
    /// verak_vinculado flag, permanent following, affinity, ECO, observation and persistence
    /// belong to later phases and are intentionally not implemented here.
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
            if (timer.IsComplete)
            {
                return CreatureStateId.Bonded; // Bonded.Enter stops movement
            }

            ApproachPlayer(context);
            return null; // non-interruptible: never any exit but Bonded
        }

        public void Exit(CreatureContext context)
        {
        }

        // Approaches the detected player via CreatureMovement — the single owner of
        // locomotion; this state never writes the Transform. CreatureMovement's own arrival
        // handling stops within the identity's arrival threshold, so an already-close player
        // yields no oscillation and no endless movement (no NavMesh, no pathfinding). A
        // missing player simply holds still: Bonding must not leave the state on a lost
        // target (it is non-interruptible), so this never requests a transition.
        private static void ApproachPlayer(CreatureContext context)
        {
            Transform player = context.DetectedPlayer;
            if (player == null || context.Root == null)
            {
                context.Movement?.Stop();
                return;
            }

            Vector2 toPlayer = (Vector2)player.position - (Vector2)context.Root.position;
            context.SetFacing(CreatureMovement.ResolveFacing(toPlayer, context.Facing));
            context.Movement?.SetDestination(player.position);
        }
    }
}

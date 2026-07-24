using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Hostile alert entry for the Altered Verak. Reached from Idle/Patrol when the
    /// sensor reports the player (those states return CreatureStateId.Alert, which the
    /// altered state set maps here instead of the ambient AlertState). Unlike the
    /// ambient Alert — which returns to Patrol — this one leads into Chase while the
    /// player is detected, or back to Idle when the player is lost. Preserves the
    /// facing it entered with (no snap). Stateless.
    /// </summary>
    public sealed class CreatureHostileAlertState : ICreatureState
    {
        private readonly Health health;
        private readonly float alertDuration;

        public CreatureHostileAlertState(Health health, float alertDuration)
        {
            this.health = health;
            this.alertDuration = alertDuration > 0f ? alertDuration : 0f;
        }

        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            context.ResetStateTimer(); // alert dwell starts now
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            if (health != null && health.IsZero)
            {
                return CreatureStateId.Subdued;
            }

            if (context.DetectedPlayer == null)
            {
                return CreatureStateId.Idle; // lost the player before pursuing
            }

            // Dwell briefly so the Alert visual is actually perceivable before Chase.
            // The Brain still owns the transition; this only delays the request, it does
            // not extend or decide the logical state elsewhere.
            context.AdvanceStateTimer(deltaTime);
            if (context.StateTimer >= alertDuration)
            {
                return CreatureStateId.Chase;
            }

            return null;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

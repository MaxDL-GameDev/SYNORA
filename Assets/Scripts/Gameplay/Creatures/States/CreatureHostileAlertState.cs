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

        public CreatureHostileAlertState(Health health)
        {
            this.health = health;
        }

        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            context.ResetStateTimer();
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

            return CreatureStateId.Chase;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

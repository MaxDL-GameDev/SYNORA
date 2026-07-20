using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Idle behavior: stop, wait for the identity's idle duration, then request
    /// Patrol. If perception reports the Player within detection range, request
    /// Alert immediately. Decides no physics and touches no Rigidbody2D directly.
    /// Stateless: all mutable data lives in CreatureContext.
    /// </summary>
    public sealed class IdleState : ICreatureState
    {
        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            context.ResetStateTimer();
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            CreatureIdentity identity = context.Identity;
            if (identity == null)
            {
                return null;
            }

            // Perception has priority: entering Alert never waits for the idle timer.
            float distanceSqr = context.Sensor != null ? context.Sensor.PlayerDistanceSqr : CreatureSensor.NoPlayer;
            SensorVerdict verdict = CreatureSensing.Evaluate(
                false, distanceSqr, identity.DetectionRadius, identity.LoseRadius, 0f, identity.AlertLingerDuration);
            if (verdict == SensorVerdict.BecomeAlert)
            {
                return CreatureStateId.Alert;
            }

            context.AdvanceStateTimer(deltaTime);
            if (context.StateTimer >= identity.IdleDuration && context.PatrolPointCount > 0)
            {
                return CreatureStateId.Patrol;
            }

            return null;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

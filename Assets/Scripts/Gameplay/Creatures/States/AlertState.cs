using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Alert behavior (MVP): stop, stay alert while the Player is within loseRadius,
    /// and return to Patrol when the dual-radius + linger hysteresis says so.
    /// PRESERVES the facing the creature had on entry — it does NOT turn toward the
    /// Player (that would snap the silhouette, e.g. Walk_Left -> Alert_Down). It never
    /// chases, flees, attacks, sets a destination toward the Player, or writes Facing;
    /// the only physical response is Stop(). The sensor keeps tracking the Player.
    /// Uses CreatureSensing as the pure decision authority; the linger timer is
    /// context.StateTimer. Stateless.
    /// </summary>
    public sealed class AlertState : ICreatureState
    {
        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            context.ResetStateTimer(); // linger starts now
            // Facing is intentionally left as-is (the direction the creature entered
            // Alert with), so the Alert clip matches the previous Idle/Walk direction.
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            CreatureIdentity identity = context.Identity;
            if (identity == null)
            {
                return null;
            }

            float distanceSqr = context.Sensor != null ? context.Sensor.PlayerDistanceSqr : CreatureSensor.NoPlayer;

            // Spatial hysteresis: the linger timer only accrues while the Player is
            // outside loseRadius; a re-entry resets it.
            if (CreatureSensing.ShouldResetLinger(distanceSqr, identity.LoseRadius))
            {
                context.ResetStateTimer();
            }
            else
            {
                context.AdvanceStateTimer(deltaTime);
            }

            SensorVerdict verdict = CreatureSensing.Evaluate(
                true, distanceSqr, identity.DetectionRadius, identity.LoseRadius,
                context.StateTimer, identity.AlertLingerDuration);

            if (verdict == SensorVerdict.ReturnToPatrol)
            {
                return CreatureStateId.Patrol;
            }

            // MVP: no facing update in Alert. The entry facing is kept so the Alert
            // clip stays on the same direction; the Player is still tracked by the
            // sensor. Direct facing-to-Player is deferred to a future turn/reaction anim.
            return null;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

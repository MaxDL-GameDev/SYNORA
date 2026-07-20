using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Alert behavior (MVP): stop, stay alert while the Player is within loseRadius,
    /// and return to Patrol when the dual-radius + linger hysteresis says so. Faces
    /// the Player while alert. It NEVER chases, flees, attacks, or sets a destination
    /// toward the Player — the only physical response is Stop(). Uses CreatureSensing
    /// as the pure decision authority; the linger timer is context.StateTimer.
    /// Stateless.
    /// </summary>
    public sealed class AlertState : ICreatureState
    {
        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);
            context.ResetStateTimer(); // linger starts now
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

            FacePlayer(context);
            return null;
        }

        public void Exit(CreatureContext context)
        {
        }

        private static void FacePlayer(CreatureContext context)
        {
            if (context.DetectedPlayer == null || context.Root == null)
            {
                return;
            }

            Vector2 direction = (Vector2)context.DetectedPlayer.position - (Vector2)context.Root.position;
            context.SetFacing(CreatureMovement.ResolveFacing(direction, context.Facing));
        }
    }
}

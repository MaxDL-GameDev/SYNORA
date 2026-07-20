using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Patrol behavior: order movement toward the current patrol point; on arrival,
    /// advance the PingPong index and request Idle (the pause "breath" between
    /// points). If perception reports the Player within detection range, request
    /// Alert. Reuses CreaturePatrolMath (via context.AdvancePatrolPoint) and never
    /// duplicates patrol math, moves the Transform, or queries Physics2D. Stateless.
    /// </summary>
    public sealed class PatrolState : ICreatureState
    {
        public void Enter(CreatureContext context)
        {
            OrderCurrentDestination(context);
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            CreatureIdentity identity = context.Identity;
            if (identity == null)
            {
                return null;
            }

            // Perception has priority.
            float distanceSqr = context.Sensor != null ? context.Sensor.PlayerDistanceSqr : CreatureSensor.NoPlayer;
            SensorVerdict verdict = CreatureSensing.Evaluate(
                false, distanceSqr, identity.DetectionRadius, identity.LoseRadius, 0f, identity.AlertLingerDuration);
            if (verdict == SensorVerdict.BecomeAlert)
            {
                return CreatureStateId.Alert;
            }

            // No route: stay put safely.
            if (context.PatrolPointCount == 0)
            {
                context.Movement?.Stop();
                return null;
            }

            // Arrival is signaled by the movement no longer having a destination.
            if (context.Movement != null && !context.Movement.HasDestination)
            {
                context.AdvancePatrolPoint();
                return CreatureStateId.Idle; // pause between points
            }

            return null;
        }

        public void Exit(CreatureContext context)
        {
        }

        private static void OrderCurrentDestination(CreatureContext context)
        {
            if (context.Movement == null)
            {
                return;
            }

            Transform point = CurrentPoint(context);
            if (point != null)
            {
                context.Movement.SetDestination(point.position);
            }
            else
            {
                context.Movement.Stop();
            }
        }

        private static Transform CurrentPoint(CreatureContext context)
        {
            var points = context.PatrolPoints;
            if (points == null || points.Count == 0)
            {
                return null;
            }

            int index = context.PatrolIndex;
            if (index < 0 || index >= points.Count)
            {
                return null;
            }

            return points[index]; // may be null/destroyed; caller null-checks
        }
    }
}

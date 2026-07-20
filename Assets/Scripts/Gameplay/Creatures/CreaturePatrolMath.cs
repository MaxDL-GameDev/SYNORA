using UnityEngine;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Pure, allocation-free patrol math. No collections, no MonoBehaviour, no
    /// Physics. Deterministic and unit-testable.
    /// </summary>
    public static class CreaturePatrolMath
    {
        /// <summary>
        /// True when <paramref name="current"/> is within <paramref name="arrivalThreshold"/>
        /// of <paramref name="target"/>, compared as squared distance. A non-positive
        /// threshold is treated as 0 (arrived only when exactly on the target).
        /// </summary>
        public static bool HasArrived(Vector2 current, Vector2 target, float arrivalThreshold)
        {
            float threshold = arrivalThreshold > 0f ? arrivalThreshold : 0f;
            return (current - target).sqrMagnitude <= threshold * threshold;
        }

        /// <summary>
        /// Next index for an N-point PingPong route (0..N-1..0). Normalizes
        /// <paramref name="direction"/> to +1 or -1, bounces at the ends, and never
        /// returns an out-of-range index. 0 points returns 0; 1 point returns 0.
        /// </summary>
        public static int NextPingPongIndex(int currentIndex, int pointCount, ref int direction)
        {
            if (pointCount <= 0)
            {
                direction = 1;
                return 0;
            }

            if (pointCount == 1)
            {
                direction = 1;
                return 0;
            }

            int dir = direction < 0 ? -1 : 1; // normalize (0 -> +1)

            int index = currentIndex;
            if (index < 0) index = 0;
            else if (index >= pointCount) index = pointCount - 1;

            int next = index + dir;
            if (next >= pointCount)
            {
                dir = -1;
                next = pointCount - 2;
            }
            else if (next < 0)
            {
                dir = 1;
                next = 1;
            }

            direction = dir;
            return next;
        }
    }
}

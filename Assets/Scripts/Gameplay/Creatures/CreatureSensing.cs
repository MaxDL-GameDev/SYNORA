namespace Synora.Gameplay.Creatures
{
    /// <summary>Result of a dual-radius perception evaluation.</summary>
    public enum SensorVerdict
    {
        RemainCalm,
        BecomeAlert,
        RemainAlert,
        ReturnToPatrol
    }

    /// <summary>
    /// Pure, allocation-free perception logic with dual-radius (spatial) and
    /// linger (temporal) hysteresis. No Physics2D, no MonoBehaviour. Distances are
    /// compared squared. Invalid parameters are handled safely.
    /// </summary>
    public static class CreatureSensing
    {
        /// <summary>
        /// Evaluates the perception verdict. <paramref name="playerDistanceSqr"/> &lt; 0
        /// means no Player is present. Enforces loseRadius &gt;= detectionRadius even if
        /// callers pass invalid values, and clamps negative radii/durations to 0.
        /// Border equality counts as "inside".
        /// </summary>
        public static SensorVerdict Evaluate(
            bool currentlyAlert,
            float playerDistanceSqr,
            float detectionRadius,
            float loseRadius,
            float lingerElapsed,
            float alertLingerDuration)
        {
            float detection = detectionRadius > 0f ? detectionRadius : 0f;
            float lose = loseRadius >= detection ? loseRadius : detection; // enforce lose >= detection
            bool playerPresent = playerDistanceSqr >= 0f;

            if (!currentlyAlert)
            {
                bool insideDetection = playerPresent && playerDistanceSqr <= detection * detection;
                return insideDetection ? SensorVerdict.BecomeAlert : SensorVerdict.RemainCalm;
            }

            if (playerPresent && playerDistanceSqr <= lose * lose)
            {
                return SensorVerdict.RemainAlert;
            }

            float linger = alertLingerDuration > 0f ? alertLingerDuration : 0f;
            return lingerElapsed >= linger ? SensorVerdict.ReturnToPatrol : SensorVerdict.RemainAlert;
        }

        /// <summary>
        /// True when the Player is within loseRadius, i.e. the alert linger timer
        /// should be reset. Absent Player (negative distance) never resets.
        /// </summary>
        public static bool ShouldResetLinger(float playerDistanceSqr, float loseRadius)
        {
            if (playerDistanceSqr < 0f)
            {
                return false;
            }

            float lose = loseRadius > 0f ? loseRadius : 0f;
            return playerDistanceSqr <= lose * lose;
        }
    }
}

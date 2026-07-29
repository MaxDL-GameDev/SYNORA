namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Pure, deterministic accumulator for the non-interruptible bonding duration
    /// (Restored → Bonding → Bonded, M7 F1). No Unity dependency, no Time, no
    /// coroutine/Invoke/Animator — advanced only by an explicit Tick, mirroring
    /// CreatureRestoreTimer. The target duration is injected and normalized (negative →
    /// 0); the concrete value is a serialized tuning parameter configured by the caller
    /// (AlteredVerakSetup), never baked in here. Bonding cannot be interrupted, so there
    /// is no cancel/reset/pause/percentage API.
    /// </summary>
    public sealed class CreatureBondingTimer
    {
        private readonly float duration;
        private float elapsed;

        public CreatureBondingTimer(float duration)
        {
            this.duration = Normalize(duration);
        }

        /// <summary>A non-positive duration is normalized to zero (deterministic), matching CreatureRestoreTimer.</summary>
        public static float Normalize(float value) => value > 0f ? value : 0f;

        /// <summary>
        /// True once accumulated time has reached the target duration. Monotonic: never
        /// returns to false. A zero (or normalized-to-zero) duration is complete
        /// immediately, before any Tick.
        /// </summary>
        public bool IsComplete => elapsed >= duration;

        /// <summary>
        /// Advances the accumulator. Negative deltaTime is treated as zero (never
        /// regresses), following CreatureRestoreTimer's convention. Idempotent once
        /// complete: further ticks keep it complete and never grow the accumulator past
        /// the duration.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || elapsed >= duration)
            {
                return;
            }

            elapsed += deltaTime;
            if (elapsed > duration)
            {
                elapsed = duration;
            }
        }
    }
}

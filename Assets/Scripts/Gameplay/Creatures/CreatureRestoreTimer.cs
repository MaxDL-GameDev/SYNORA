namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Pure, deterministic accumulator for the non-interruptible restoration duration
    /// (Subdued → Restoring → Restored, M6). No Unity dependency, no Time, no
    /// coroutine/Invoke/Animator — advanced only by an explicit Tick, mirroring
    /// CreatureAttackTimer. The target duration is injected and normalized (negative →
    /// 0); the canonical ~1.25s value is configured by the caller in a later phase, not
    /// baked in here. Restoration cannot be interrupted, so there is no
    /// cancel/reset/pause/percentage API.
    /// </summary>
    public sealed class CreatureRestoreTimer
    {
        private readonly float duration;
        private float elapsed;

        public CreatureRestoreTimer(float duration)
        {
            this.duration = Normalize(duration);
        }

        /// <summary>A non-positive duration is normalized to zero (deterministic), matching CreatureAttackTimer.</summary>
        public static float Normalize(float value) => value > 0f ? value : 0f;

        /// <summary>
        /// True once accumulated time has reached the target duration. Monotonic: never
        /// returns to false. A zero (or normalized-to-zero) duration is complete
        /// immediately, before any Tick.
        /// </summary>
        public bool IsComplete => elapsed >= duration;

        /// <summary>
        /// Advances the accumulator. Negative deltaTime is treated as zero (never
        /// regresses), following CreatureAttackTimer's convention. Idempotent once
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

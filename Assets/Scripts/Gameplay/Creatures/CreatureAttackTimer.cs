namespace Synora.Gameplay.Creatures
{
    /// <summary>Logical phase of a single creature melee attack.</summary>
    public enum CreatureAttackPhase
    {
        Ready,
        Windup,
        Active,
        Cooldown
    }

    /// <summary>
    /// Pure timing state machine for one creature attack: Ready → Windup → Active →
    /// Cooldown → Ready. No Unity dependency, no Time, no physics — driven by an
    /// explicit Tick so it is fully deterministic and unit-testable (mirrors the
    /// player's AttackTimer, plus a Windup anticipation phase). Durations are
    /// normalized (negatives → 0) at construction. A sequence starts only from Ready
    /// (no overlap); zero-length phases collapse within a Tick, carrying the remainder
    /// so large deltaTime steps stay deterministic.
    /// </summary>
    public sealed class CreatureAttackTimer
    {
        private readonly float windupDuration;
        private readonly float activeDuration;
        private readonly float cooldownDuration;

        private CreatureAttackPhase phase = CreatureAttackPhase.Ready;
        private float remaining;

        public CreatureAttackTimer(float windupDuration, float activeDuration, float cooldownDuration)
        {
            this.windupDuration = Normalize(windupDuration);
            this.activeDuration = Normalize(activeDuration);
            this.cooldownDuration = Normalize(cooldownDuration);
        }

        public CreatureAttackPhase Phase => phase;

        /// <summary>True only during the damaging window.</summary>
        public bool IsActiveWindow => phase == CreatureAttackPhase.Active;

        /// <summary>True while the attack is committed (windup or active) — the creature must not re-decide.</summary>
        public bool IsSequenceActive => phase == CreatureAttackPhase.Windup || phase == CreatureAttackPhase.Active;

        public bool CanStart => phase == CreatureAttackPhase.Ready;

        public static float Normalize(float duration) => duration > 0f ? duration : 0f;

        /// <summary>Begins a sequence if ready. Returns true only when a sequence actually started.</summary>
        public bool TryStart()
        {
            if (phase != CreatureAttackPhase.Ready)
            {
                return false;
            }

            phase = CreatureAttackPhase.Windup;
            remaining = windupDuration;
            return true;
        }

        /// <summary>
        /// Advances timing. Negative dt is treated as zero; the remainder carries across
        /// phases, EXCEPT that the tick stops the instant a positive active window opens
        /// so the window is always observable for at least one tick — a large deltaTime
        /// can never skip a positive active window. A zero-length active window is not
        /// observable (it collapses through) and deals no damage.
        /// </summary>
        public void Tick(float deltaTime)
        {
            float dt = deltaTime > 0f ? deltaTime : 0f;

            while (phase != CreatureAttackPhase.Ready)
            {
                if (remaining > 0f)
                {
                    if (dt <= 0f)
                    {
                        return; // no time left to consume this positive-duration phase
                    }

                    if (dt < remaining)
                    {
                        remaining -= dt;
                        return;
                    }

                    dt -= remaining;
                }

                // remaining == 0 (zero-length phase, or exactly consumed): collapse it,
                // even when dt is fully spent, so a trailing zero cooldown reaches Ready.
                AdvancePhase();

                // Stop as soon as a positive active window opens, discarding any leftover
                // dt this tick, so the controller always sees IsActiveWindow at least once.
                if (phase == CreatureAttackPhase.Active && remaining > 0f)
                {
                    return;
                }
            }
        }

        private void AdvancePhase()
        {
            switch (phase)
            {
                case CreatureAttackPhase.Windup:
                    phase = CreatureAttackPhase.Active;
                    remaining = activeDuration;
                    break;
                case CreatureAttackPhase.Active:
                    phase = CreatureAttackPhase.Cooldown;
                    remaining = cooldownDuration;
                    break;
                default: // Cooldown elapsed
                    phase = CreatureAttackPhase.Ready;
                    remaining = 0f;
                    break;
            }
        }

        /// <summary>Aborts any sequence/cooldown back to Ready with no stuck state.</summary>
        public void Cancel()
        {
            phase = CreatureAttackPhase.Ready;
            remaining = 0f;
        }
    }
}

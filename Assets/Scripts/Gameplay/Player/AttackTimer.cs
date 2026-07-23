namespace Synora.Gameplay.Player
{
    /// <summary>Logical phase of a single melee attack.</summary>
    public enum AttackPhase
    {
        Idle,
        ActiveWindow,
        Cooldown
    }

    /// <summary>
    /// Pure timing state machine for one melee attack: Idle → ActiveWindow → Cooldown →
    /// Idle. No Unity dependency, no Time, no physics — driven by an explicit Tick so it
    /// is fully deterministic and unit-testable. Durations are normalized (negatives → 0)
    /// at construction. A window opens only from Idle (no overlapping attacks); a
    /// zero-length window closes on the first Tick; a zero cooldown returns straight to
    /// Idle after the window.
    /// </summary>
    public sealed class AttackTimer
    {
        private readonly float windowDuration;
        private readonly float cooldownDuration;

        private AttackPhase phase = AttackPhase.Idle;
        private float remaining;

        public AttackTimer(float windowDuration, float cooldownDuration)
        {
            this.windowDuration = Normalize(windowDuration);
            this.cooldownDuration = Normalize(cooldownDuration);
        }

        public AttackPhase Phase => phase;
        public bool IsActiveWindow => phase == AttackPhase.ActiveWindow;
        public bool CanStart => phase == AttackPhase.Idle;

        public static float Normalize(float duration) => duration > 0f ? duration : 0f;

        /// <summary>Opens a window if idle. Returns true only when a window actually opened.</summary>
        public bool TryStart()
        {
            if (phase != AttackPhase.Idle)
            {
                return false;
            }

            phase = AttackPhase.ActiveWindow;
            remaining = windowDuration;
            return true;
        }

        /// <summary>Advances timing. Negative dt is treated as zero.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            if (phase == AttackPhase.Idle)
            {
                return;
            }

            remaining -= deltaTime;
            if (remaining > 0f)
            {
                return;
            }

            if (phase == AttackPhase.ActiveWindow)
            {
                if (cooldownDuration > 0f)
                {
                    phase = AttackPhase.Cooldown;
                    remaining = cooldownDuration;
                }
                else
                {
                    phase = AttackPhase.Idle;
                    remaining = 0f;
                }
            }
            else // Cooldown elapsed
            {
                phase = AttackPhase.Idle;
                remaining = 0f;
            }
        }

        /// <summary>Aborts any active window/cooldown back to Idle with no stuck state.</summary>
        public void Cancel()
        {
            phase = AttackPhase.Idle;
            remaining = 0f;
        }
    }
}

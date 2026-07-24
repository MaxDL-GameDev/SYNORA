using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Presentation-only adapter for the Altered Verak: attack flash from the controller,
    /// damage flash from Health decreases, and a persistent terminal tint when the Brain
    /// reaches Subdued. Chase/HostileAlert visuals are handled by the existing
    /// CreatureAnimator (reusing Walk/Alert clips); this component only adds sprite
    /// feedback. It never changes state, applies damage, modifies Health, or drives
    /// transitions. The Animator stays presentation-only.
    /// </summary>
    public sealed class AlteredVerakPresentation : MonoBehaviour
    {
        [SerializeField] private CreatureAttackController attackController;
        [SerializeField] private Health health;
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private SpriteFlash flash;

        private float lastNormalized = 1f;
        private bool subscribed;
        private bool subduedShown;

        private void OnEnable()
        {
            if (subscribed)
            {
                return;
            }

            if (attackController != null) attackController.AttackStarted += OnAttackStarted;
            if (health != null) { health.Changed += OnHealthChanged; lastNormalized = health.Normalized; }
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            if (attackController != null) attackController.AttackStarted -= OnAttackStarted;
            if (health != null) health.Changed -= OnHealthChanged;
            subscribed = false;
        }

        private void OnAttackStarted() => flash?.Flash();

        private void OnHealthChanged()
        {
            if (health == null) return;
            float n = health.Normalized;
            if (n < lastNormalized) flash?.Flash();
            lastNormalized = n;
        }

        private void LateUpdate() => RefreshTerminal();

        /// <summary>Applies the terminal Subdued tint once. Public for deterministic tests.</summary>
        public void RefreshTerminal()
        {
            if (brain == null || subduedShown)
            {
                return;
            }

            if (brain.CurrentStateId == CreatureStateId.Subdued)
            {
                flash?.SetTerminalTint(true);
                subduedShown = true;
            }
        }
    }
}

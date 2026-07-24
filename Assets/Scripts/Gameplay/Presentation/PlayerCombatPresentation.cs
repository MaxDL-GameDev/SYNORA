using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Player;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Presentation-only adapter for the player: turns combat facts into sprite feedback
    /// via <see cref="SpriteFlash"/>. Observes PlayerAttack.AttackStarted (attack flash),
    /// Health.Changed decreases (damage flash) and PlayerTemporaryDefeat (persistent
    /// defeat tint, cleared on recovery). It never applies damage, changes state,
    /// modifies Health, or writes the SpriteRenderer directly.
    /// </summary>
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        [SerializeField] private PlayerAttack attack;
        [SerializeField] private Health health;
        [SerializeField] private PlayerTemporaryDefeat defeat;
        [SerializeField] private SpriteFlash flash;

        private float lastNormalized = 1f;
        private bool subscribed;

        private void OnEnable()
        {
            if (subscribed)
            {
                return;
            }

            if (attack != null) attack.AttackStarted += OnAttackStarted;
            if (health != null) { health.Changed += OnHealthChanged; lastNormalized = health.Normalized; }
            if (defeat != null) { defeat.Defeated += OnDefeated; defeat.Recovered += OnRecovered; }
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            if (attack != null) attack.AttackStarted -= OnAttackStarted;
            if (health != null) health.Changed -= OnHealthChanged;
            if (defeat != null) { defeat.Defeated -= OnDefeated; defeat.Recovered -= OnRecovered; }
            subscribed = false;
        }

        private void OnAttackStarted() => flash?.Flash();

        private void OnHealthChanged()
        {
            if (health == null) return;
            float n = health.Normalized;
            if (n < lastNormalized) flash?.Flash(); // flash only on a decrease (damage)
            lastNormalized = n;
        }

        private void OnDefeated() => flash?.SetTerminalTint(true);
        private void OnRecovered() => flash?.SetTerminalTint(false);
    }
}

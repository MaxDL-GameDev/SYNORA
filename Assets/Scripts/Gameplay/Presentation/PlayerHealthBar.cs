using UnityEngine;
using UnityEngine.UI;
using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Minimal player health UI: a filled bar bound to Health.Normalized. It only
    /// observes Health (via the Changed event) — it never modifies it. Event-driven, no
    /// per-frame Update. <see cref="LastNormalized"/> mirrors the last applied value for
    /// testing without a UI dependency.
    /// </summary>
    public sealed class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Image fill;

        private bool subscribed;

        public float LastNormalized { get; private set; } = 1f;

        private void OnEnable()
        {
            if (health != null && !subscribed)
            {
                health.Changed += Refresh;
                subscribed = true;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (health != null && subscribed)
            {
                health.Changed -= Refresh;
                subscribed = false;
            }
        }

        private void Refresh()
        {
            if (health == null)
            {
                return;
            }

            LastNormalized = health.Normalized;
            if (fill != null)
            {
                fill.fillAmount = LastNormalized;
            }
        }
    }
}

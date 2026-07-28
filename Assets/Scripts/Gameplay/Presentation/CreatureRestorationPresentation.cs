using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Presentation-only projection of the restoration flow (M6 F6). It reads
    /// <see cref="CreatureBrain.CurrentStateId"/> — the single source of truth — and drives
    /// a restoration tint through <see cref="SpriteFlash"/>, the sole compositor of
    /// SpriteRenderer.color (so there is never a second, uncoordinated color writer):
    ///
    ///   • Restoring → a soft, deterministic pulse (a state tint whose intensity oscillates).
    ///   • Restored  → a stable, moderate tint held while the Brain stays in Restored.
    ///   • any other state → the restoration contribution is cleared, but ONLY if this
    ///     component owned it, so the Subdued terminal tint is left intact.
    ///
    /// Entering Restoring overwrites SpriteFlash's single persistent slot, which is how the
    /// Subdued terminal tint is replaced without touching AlteredVerakPresentation. It never
    /// changes state, requests a transition, or reads the restoration timer. The pulse uses
    /// an injected deltaTime (Update is only a Time.deltaTime adapter), so it is fully
    /// deterministic in tests. No Animator, AnimationEvents, coroutines, UI or materials.
    /// </summary>
    public sealed class CreatureRestorationPresentation : MonoBehaviour
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private SpriteFlash flash;

        [Header("Restoring pulse (provisional)")]
        [SerializeField] private Color restoringTint = new Color(0.6f, 0.85f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float pulseMinIntensity = 0.15f;
        [SerializeField, Range(0f, 1f)] private float pulseMaxIntensity = 0.5f;
        [SerializeField, Min(0f)] private float pulseSpeed = 6f; // radians per second

        [Header("Restored tint (provisional)")]
        [SerializeField] private Color restoredTint = new Color(0.7f, 0.9f, 0.75f, 1f);
        [SerializeField, Range(0f, 1f)] private float restoredIntensity = 0.35f;

        private float phase;
        private bool owns; // whether the restoration tint currently occupies SpriteFlash's slot

        private void OnEnable()
        {
            phase = 0f;
            Apply(0f); // resync to the current state without advancing the pulse
        }

        private void OnDisable()
        {
            // Restore safely: drop only our own contribution; leave any other tint untouched.
            if (owns)
            {
                flash?.ClearPersistentTint();
                owns = false;
            }
            phase = 0f;
        }

        private void Update() => Apply(Time.deltaTime);

        private void Apply(float deltaTime)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (brain == null)
            {
                if (owns)
                {
                    flash?.ClearPersistentTint();
                    owns = false;
                }
                return;
            }

            switch (brain.CurrentStateId)
            {
                case CreatureStateId.Restoring:
                    phase += deltaTime > 0f ? deltaTime * pulseSpeed : 0f;
                    float t = pulseMinIntensity
                              + (pulseMaxIntensity - pulseMinIntensity) * 0.5f * (Mathf.Sin(phase) + 1f);
                    flash?.SetPersistentTint(restoringTint, t);
                    owns = true;
                    break;

                case CreatureStateId.Restored:
                    flash?.SetPersistentTint(restoredTint, restoredIntensity);
                    owns = true;
                    break;

                default:
                    if (owns)
                    {
                        flash?.ClearPersistentTint();
                        owns = false;
                    }
                    phase = 0f;
                    break;
            }
        }
    }
}

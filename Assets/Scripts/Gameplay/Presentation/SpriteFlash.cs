using UnityEngine;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Reusable, presentation-only sprite feedback and the SINGLE compositor of
    /// SpriteRenderer.color (M5 F8 + M6 F6). Gameplay never touches the renderer.
    ///
    /// The composed color is layered deterministically:
    ///   base color (captured once) -> persistent state tint -> temporary flash.
    /// While a flash is active it dominates; when it ends the most recent persistent tint
    /// is shown; with no persistent tint the original base is restored. The base is
    /// captured exactly once (Awake), so a temporary or already-composed color is never
    /// mistaken for the base. The persistent tint is a single slot (no multi-layer stack):
    /// setting it again overwrites the previous one — which is how the restoration flow
    /// replaces the Subdued terminal tint without any other component clearing it.
    ///
    /// No per-frame allocations; no Animator; no AnimationEvents; no coroutines.
    /// </summary>
    public sealed class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color terminalTint = new Color(0.45f, 0.45f, 0.6f, 1f);
        [SerializeField, Min(0f)] private float flashDuration = 0.12f;

        private Color originalColor = Color.white;
        private bool captured;
        private float flashTimer;

        // Single persistent tint slot (state feedback: terminal tint, restoration tint…).
        private bool hasPersistentTint;
        private Color persistentTint;
        private float persistentIntensity;

        public bool IsFlashing => flashTimer > 0f;

        /// <summary>True while a persistent tint is held (terminal, restoration, …).</summary>
        public bool TerminalHeld => hasPersistentTint;

        private void Awake() => Capture();

        private void Capture()
        {
            if (!captured && spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                captured = true;
            }
        }

        // base -> persistent tint (flash is layered on top separately, in Flash/Apply).
        private Color ComposedBase
        {
            get
            {
                if (!hasPersistentTint)
                {
                    return originalColor;
                }

                Color c = Color.Lerp(originalColor, persistentTint, Mathf.Clamp01(persistentIntensity));
                c.a = originalColor.a; // preserve the original alpha; tint only affects RGB
                return c;
            }
        }

        /// <summary>Starts a brief flash. Successive calls simply re-arm the timer.</summary>
        public void Flash()
        {
            Capture();
            flashTimer = flashDuration;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = flashDuration > 0f ? flashColor : ComposedBase;
            }
        }

        /// <summary>
        /// Sets the single persistent tint layered over the base color. Does not disturb an
        /// active flash (the flash keeps dominating); the value is used the moment the flash
        /// ends. Intensity is clamped to [0,1].
        /// </summary>
        public void SetPersistentTint(Color tint, float intensity)
        {
            Capture();
            hasPersistentTint = true;
            persistentTint = tint;
            persistentIntensity = Mathf.Clamp01(intensity);
            if (flashTimer <= 0f)
            {
                Apply();
            }
        }

        /// <summary>Clears the persistent tint, returning to the captured base color.</summary>
        public void ClearPersistentTint()
        {
            Capture();
            hasPersistentTint = false;
            if (flashTimer <= 0f)
            {
                Apply();
            }
        }

        /// <summary>
        /// Backward-compatible terminal tint (Defeat / Subdued): a fixed-color persistent
        /// tint at full intensity. Kept so existing callers stay unchanged.
        /// </summary>
        public void SetTerminalTint(bool held)
        {
            if (held)
            {
                SetPersistentTint(terminalTint, 1f);
            }
            else
            {
                ClearPersistentTint();
            }
        }

        /// <summary>Advances the flash restore. Public for deterministic tests.</summary>
        public void Tick(float deltaTime)
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= deltaTime > 0f ? deltaTime : 0f;
            if (flashTimer <= 0f)
            {
                flashTimer = 0f;
                Apply();
            }
        }

        private void Update() => Tick(Time.deltaTime);

        private void Apply()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = ComposedBase;
            }
        }

        private void OnDisable()
        {
            flashTimer = 0f;
            Apply(); // never leave the renderer stuck on the flash color
        }
    }
}

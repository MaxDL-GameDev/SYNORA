using UnityEngine;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Reusable, presentation-only sprite feedback (M5 F8): a brief color flash for
    /// attacks/damage and a persistent terminal tint for Defeat/Subdued. It is the ONLY
    /// place that writes SpriteRenderer.color for combat feedback — gameplay never
    /// touches the renderer. Captures the original color once and always restores it (or
    /// the held terminal tint). No per-frame allocations; no Animator; no AnimationEvents.
    /// </summary>
    public sealed class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color terminalTint = new Color(0.45f, 0.45f, 0.6f, 1f);
        [SerializeField, Min(0f)] private float flashDuration = 0.12f;

        private Color originalColor = Color.white;
        private bool captured;
        private bool terminalHeld;
        private float flashTimer;

        public bool IsFlashing => flashTimer > 0f;
        public bool TerminalHeld => terminalHeld;

        private void Awake() => Capture();

        private void Capture()
        {
            if (!captured && spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                captured = true;
            }
        }

        private Color BaseColor => terminalHeld ? terminalTint : originalColor;

        /// <summary>Starts a brief flash. Successive calls simply re-arm the timer.</summary>
        public void Flash()
        {
            Capture();
            flashTimer = flashDuration;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = flashDuration > 0f ? flashColor : BaseColor;
            }
        }

        /// <summary>Holds or clears a persistent terminal tint (Defeat / Subdued).</summary>
        public void SetTerminalTint(bool held)
        {
            Capture();
            terminalHeld = held;
            if (flashTimer <= 0f)
            {
                Apply();
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
                spriteRenderer.color = BaseColor;
            }
        }

        private void OnDisable()
        {
            flashTimer = 0f;
            Apply(); // never leave the renderer stuck on the flash color
        }
    }
}

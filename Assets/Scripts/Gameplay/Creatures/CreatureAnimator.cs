using UnityEngine;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Presentation-only view of a creature. Reads the shared CreatureContext
    /// (CurrentState, Facing, IsMoving) and drives an Animator's parameters plus
    /// SpriteRenderer.flipX. It NEVER modifies gameplay: no transitions, no movement
    /// orders, no sensing, no Physics2D, no Transform writes. It is a consumer of
    /// state, never an authority. Refreshes in LateUpdate so it reflects the latest
    /// logical decision made by the Brain in Update.
    /// </summary>
    public sealed class CreatureAnimator : MonoBehaviour
    {
        // Animator parameters (kept minimal and non-redundant): VisualState + Direction.
        // Hashes are computed lazily on the main thread (never in a static initializer,
        // which would run off the main thread during test discovery and throw).
        private const string VisualStateParam = "VisualState";
        private const string DirectionParam = "Direction";

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int visualStateHash;
        private int directionHash;
        private bool hashesReady;

        private CreatureContext context;
        private bool isInitialized;
        private bool hasCache;
        private CreatureVisualState lastVisualState;
        private CreatureFacingDirection lastDirection;
        private bool lastFlipX;

        public bool IsInitialized => isInitialized;

        // Read-only view of the last presentation applied by Refresh (exposed for tests).
        public bool HasApplied => hasCache;
        public CreatureVisualState LastVisualState => lastVisualState;
        public CreatureFacingDirection LastDirection => lastDirection;
        public bool LastFlipX => lastFlipX;

        /// <summary>Binds the same context used by Brain/Movement/Sensor. Idempotent.</summary>
        public void Initialize(CreatureContext creatureContext)
        {
            if (isInitialized)
            {
                return;
            }

            context = creatureContext;
            isInitialized = true;
            hasCache = false;
        }

        private void LateUpdate()
        {
            Refresh();
        }

        /// <summary>
        /// Applies the current logical state to the visuals. Public for deterministic
        /// tests. No-op before Initialize; only writes Animator parameters when the
        /// resolved presentation actually changed.
        /// </summary>
        public void Refresh()
        {
            if (!isInitialized || context == null)
            {
                return;
            }

            CreaturePresentation p = CreatureAnimationResolver.Resolve(
                context.CurrentState, context.Facing, context.IsMoving);

            if (hasCache && p.VisualState == lastVisualState && p.Direction == lastDirection && p.FlipX == lastFlipX)
            {
                return; // nothing changed; avoid redundant parameter writes
            }

            // Only drive parameters when a controller is present; setting parameters
            // on a controller-less Animator logs "Animator is not playing an
            // AnimatorController".
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                EnsureHashes();
                animator.SetInteger(visualStateHash, (int)p.VisualState);
                animator.SetInteger(directionHash, (int)p.Direction);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = p.FlipX;
            }

            lastVisualState = p.VisualState;
            lastDirection = p.Direction;
            lastFlipX = p.FlipX;
            hasCache = true;
        }

        private void EnsureHashes()
        {
            if (hashesReady)
            {
                return;
            }

            visualStateHash = Animator.StringToHash(VisualStateParam);
            directionHash = Animator.StringToHash(DirectionParam);
            hashesReady = true;
        }
    }
}

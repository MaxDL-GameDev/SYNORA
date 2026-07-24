using UnityEngine;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Timing owner of the Altered Verak's attack (M5 Fase 6). Owns a pure
    /// <see cref="CreatureAttackTimer"/> and drives the dedicated
    /// <see cref="CreatureAttackHitResolver"/> in a single deterministic order
    /// (advance timer → resolve while the window is active), so no valid window is
    /// lost to script-execution order. It captures the attack direction when the
    /// sequence starts; the target position may change afterward but the direction is
    /// fixed. It never moves the creature, never touches Health, and never uses the
    /// Animator. The attack STATE decides WHEN to start; this component decides the
    /// timing and the physical hit.
    /// </summary>
    public sealed class CreatureAttackController : MonoBehaviour
    {
        [SerializeField] private CreatureAttackHitResolver resolver;
        [SerializeField] private float windupDuration = 0.25f;
        [SerializeField] private float activeDuration = 0.15f;
        [SerializeField] private float cooldownDuration = 0.8f;

        private CreatureAttackTimer timer;
        private Vector2Int capturedFacing;
        private bool wasActive;

        private CreatureAttackTimer Timer =>
            timer ?? (timer = new CreatureAttackTimer(windupDuration, activeDuration, cooldownDuration));

        /// <summary>Ready to begin a new attack (not in windup/active/cooldown).</summary>
        public bool CanStart => Timer.CanStart;

        /// <summary>Committed to the current attack (windup or active); the state must not re-decide.</summary>
        public bool IsSequenceActive => Timer.IsSequenceActive;

        /// <summary>Damaging window open.</summary>
        public bool IsHitWindowActive => Timer.IsActiveWindow;

        /// <summary>Cardinal direction captured when the current sequence started.</summary>
        public Vector2Int CapturedFacing => capturedFacing;

        private void Awake()
        {
            if (resolver == null)
            {
                Debug.LogError("CreatureAttackController: CreatureAttackHitResolver reference is not assigned.", this);
            }

            if (windupDuration < 0f || activeDuration < 0f || cooldownDuration < 0f)
            {
                Debug.LogWarning("CreatureAttackController: durations must not be negative (normalized to zero).", this);
            }

            if (activeDuration <= 0f)
            {
                Debug.LogWarning("CreatureAttackController: activeDuration should be greater than zero, or no damage lands.", this);
            }
        }

        /// <summary>
        /// Starts an attack in the given cardinal direction if ready. Returns true only
        /// when a new sequence actually started.
        /// </summary>
        public bool TryStartAttack(Vector2Int facing)
        {
            if (facing == Vector2Int.zero || !Timer.CanStart)
            {
                return false;
            }

            capturedFacing = facing;
            wasActive = false;
            return Timer.TryStart();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances timing and drives the resolver in order. Public so tests and the
        /// attack state can drive it deterministically. Cooldown advances here every
        /// frame regardless of the current behavior state.
        /// </summary>
        public void Tick(float deltaTime)
        {
            Timer.Tick(deltaTime);

            bool active = Timer.IsActiveWindow;
            if (active && !wasActive)
            {
                resolver?.BeginWindow();
            }

            if (active)
            {
                resolver?.ResolveHits(capturedFacing);
            }

            wasActive = active;
        }

        /// <summary>Aborts the current attack cleanly (used when subdued or disabled).</summary>
        public void Cancel()
        {
            Timer.Cancel();
            wasActive = false;
        }

        private void OnDisable()
        {
            Cancel();
        }
    }
}

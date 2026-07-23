using UnityEngine;
using Synora.Systems;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Control and timing of the player's frontal melee attack (M5 Fase 4). It decides
    /// WHEN an attack starts and captures its cardinal direction; it does NOT resolve
    /// hits, apply damage, or move the player. The physical hit channel (overlap →
    /// IDamageable) belongs to Fase 5; presentation (Animator) to Fase 7.
    ///
    /// Never references PlayerMotor, Physics2D, Collider2D, Animator, IDamageable or
    /// Health. Movement authority stays with PlayerMotor and the general block with
    /// PlayerControlGate; this component only reads the gate and the orientation.
    /// </summary>
    public sealed class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerOrientation orientation;
        [SerializeField] private PlayerControlGate gate;
        [SerializeField] private float windowDuration = 0.15f;
        [SerializeField] private float cooldownDuration = 0.5f;

        private AttackTimer timer;
        private Vector2Int capturedFacing;

        private AttackTimer Timer => timer ?? (timer = new AttackTimer(windowDuration, cooldownDuration));

        /// <summary>True while the logical attack (hit) window is open. Read by Fase 5.</summary>
        public bool IsAttackActive => Timer.IsActiveWindow;

        /// <summary>Alias for the hit window, exposed for the Fase 5 hit resolver.</summary>
        public bool IsHitWindowActive => Timer.IsActiveWindow;

        /// <summary>Cardinal facing captured when the current attack started.</summary>
        public Vector2Int CapturedFacing => capturedFacing;

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.AttackPressed += HandleAttackPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.AttackPressed -= HandleAttackPressed;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Logical tick. Public for deterministic tests. If the gate is blocked, an
        /// active window is cancelled cleanly (no stuck cooldown) and timing does not
        /// advance; otherwise the timer advances.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (gate != null && gate.IsBlocked)
            {
                Timer.Cancel();
                return;
            }

            Timer.Tick(deltaTime);
        }

        private void HandleAttackPressed()
        {
            TryAttack();
        }

        /// <summary>
        /// Attempts to start an attack. Returns true only when a new window opened.
        /// Fails (without side effects, without resetting cooldown) when the gate is
        /// blocked, orientation is missing/zero, or a window/cooldown is in progress.
        /// </summary>
        public bool TryAttack()
        {
            if (gate != null && gate.IsBlocked)
            {
                return false;
            }

            if (orientation == null)
            {
                return false;
            }

            Vector2Int facing = orientation.Facing;
            if (facing == Vector2Int.zero)
            {
                return false;
            }

            if (!Timer.TryStart())
            {
                return false;
            }

            capturedFacing = facing;
            return true;
        }
    }
}

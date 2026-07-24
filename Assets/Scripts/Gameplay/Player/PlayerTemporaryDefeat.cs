using System;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Systems;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Non-lethal temporary defeat + recovery of the player (M5 §7/F8). When Health
    /// reaches zero it blocks control through PlayerControlGate (Defeat reason) — which
    /// stops PlayerMotor and cancels PlayerAttack's window via existing infrastructure —
    /// then, after a configurable delay, recovers: repositions to a safe SpawnPoint,
    /// clears velocity, restores Health and removes ONLY the Defeat block (preserving any
    /// other active reason). It never destroys the player, reloads the scene, uses
    /// Time.timeScale, or touches PlayerMotor/PlayerAttack internals. Recovery timing is
    /// a deterministic Tick, mirroring the rest of the codebase.
    /// </summary>
    public sealed class PlayerTemporaryDefeat : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private PlayerControlGate gate;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpawnPoint recoveryPoint;
        [SerializeField, Min(0f)] private float recoveryDelay = 2f;

        private bool isDefeated;
        private bool subscribed;
        private float recoveryTimer;

        public bool IsDefeated => isDefeated;

        /// <summary>Seconds remaining before recovery while defeated (0 otherwise).</summary>
        public float RecoveryTimeRemaining => isDefeated ? Mathf.Max(0f, recoveryTimer) : 0f;

        /// <summary>Raised once when the player enters temporary defeat.</summary>
        public event Action Defeated;

        /// <summary>Raised once when the player recovers control.</summary>
        public event Action Recovered;

        private void Awake()
        {
            if (health == null)
            {
                Debug.LogError("PlayerTemporaryDefeat: Health reference is not assigned.", this);
            }

            if (gate == null)
            {
                Debug.LogError("PlayerTemporaryDefeat: PlayerControlGate reference is not assigned.", this);
            }

            if (body == null)
            {
                Debug.LogError("PlayerTemporaryDefeat: Rigidbody2D reference is not assigned.", this);
            }

            if (recoveryPoint == null)
            {
                Debug.LogWarning("PlayerTemporaryDefeat: recovery SpawnPoint is not assigned; recovery keeps the current position.", this);
            }
        }

        private void OnEnable()
        {
            if (health != null && !subscribed)
            {
                health.Depleted += HandleDepleted;
                subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (health != null && subscribed)
            {
                health.Depleted -= HandleDepleted;
                subscribed = false;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances the recovery countdown while defeated. Public for deterministic
        /// tests. No-op when not defeated; negative dt is treated as zero.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isDefeated)
            {
                return;
            }

            recoveryTimer -= deltaTime > 0f ? deltaTime : 0f;
            if (recoveryTimer <= 0f)
            {
                Recover();
            }
        }

        private void HandleDepleted()
        {
            if (isDefeated)
            {
                return; // enter defeat exactly once
            }

            isDefeated = true;
            recoveryTimer = recoveryDelay;
            gate?.Block(ControlBlockReason.Defeat);
            Defeated?.Invoke();
        }

        /// <summary>
        /// Restores the player to a safe playable state: reposition, clear velocity,
        /// refill Health, and release ONLY the Defeat block. The gate release cancels any
        /// pending attack cleanly (PlayerAttack cancels while blocked), so no attack
        /// carries over. Public so a future explicit trigger can force recovery.
        /// </summary>
        public void Recover()
        {
            if (!isDefeated)
            {
                return;
            }

            if (body != null)
            {
                if (recoveryPoint != null)
                {
                    body.position = recoveryPoint.transform.position;
                }

                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            health?.ResetHealth();      // refills and re-arms the Depleted signal
            gate?.Unblock(ControlBlockReason.Defeat); // release ONLY Defeat; keep others

            isDefeated = false;
            recoveryTimer = 0f;
            Recovered?.Invoke();
        }
    }
}

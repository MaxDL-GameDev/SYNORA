using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Systems;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Non-lethal temporary defeat of the player (M5 §7). Observes the player's Health
    /// and, the first time it reaches zero, blocks control through PlayerControlGate.
    /// Blocking the gate is the single lever: PlayerMotor zeroes velocity and
    /// PlayerAttack cancels its window while the gate is blocked, so movement stop and
    /// attack cancellation come from existing infrastructure — this component does NOT
    /// touch PlayerMotor, PlayerAttack, PlayerInputReader or Health internals.
    ///
    /// It never destroys the player, reloads the scene, resets Health, shows UI, plays
    /// animation or auto-recovers. Recovery (unblocking) is a later phase's explicit
    /// action via gate.Unblock(Defeat).
    /// </summary>
    public sealed class PlayerTemporaryDefeat : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private PlayerControlGate gate;

        private bool isDefeated;
        private bool subscribed;

        public bool IsDefeated => isDefeated;

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

        private void HandleDepleted()
        {
            if (isDefeated)
            {
                return; // enter defeat exactly once
            }

            isDefeated = true;
            gate?.Block(ControlBlockReason.Defeat);
        }
    }
}

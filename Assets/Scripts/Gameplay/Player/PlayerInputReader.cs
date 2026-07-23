using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Single source of player input. Reads the Gameplay/Move action (polling) and
    /// exposes the latest movement vector, plus the Gameplay/Attack action as a
    /// discrete event. No other component touches the Input System.
    /// Interact stays in the separate InteractionInputReader (M2), unchanged.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference moveAction;

        [SerializeField]
        private InputActionReference attackAction;

        private Vector2 moveInput;

        /// <summary>Latest movement input; zero when no input is active.</summary>
        public Vector2 MoveInput => moveInput;

        /// <summary>Discrete attack intent, raised once per Attack action "performed".</summary>
        public event Action AttackPressed;

        private void OnEnable()
        {
            if (moveAction == null || moveAction.action == null)
            {
                Debug.LogError("PlayerInputReader: Move action reference is not assigned.", this);
            }
            else
            {
                moveAction.action.performed += OnMovePerformed;
                moveAction.action.canceled += OnMoveCanceled;
                moveAction.action.Enable();
            }

            if (attackAction == null || attackAction.action == null)
            {
                Debug.LogError("PlayerInputReader: Attack action reference is not assigned.", this);
            }
            else
            {
                attackAction.action.performed += OnAttackPerformed;
                attackAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (moveAction != null && moveAction.action != null)
            {
                moveAction.action.Disable();
                moveAction.action.performed -= OnMovePerformed;
                moveAction.action.canceled -= OnMoveCanceled;
            }

            if (attackAction != null && attackAction.action != null)
            {
                attackAction.action.Disable();
                attackAction.action.performed -= OnAttackPerformed;
            }

            moveInput = Vector2.zero;
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            AttackPressed?.Invoke();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            moveInput = Vector2.zero;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                moveInput = Vector2.zero;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                moveInput = Vector2.zero;
            }
        }
    }
}

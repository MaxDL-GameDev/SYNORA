using UnityEngine;
using UnityEngine.InputSystem;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Single source of player input. Reads the Gameplay/Move action and exposes
    /// the latest movement vector. No other component touches the Input System.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference moveAction;

        private Vector2 moveInput;

        /// <summary>Latest movement input; zero when no input is active.</summary>
        public Vector2 MoveInput => moveInput;

        private void OnEnable()
        {
            if (moveAction == null || moveAction.action == null)
            {
                Debug.LogError("PlayerInputReader: Move action reference is not assigned.", this);
                return;
            }

            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
            moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null && moveAction.action != null)
            {
                moveAction.action.Disable();
                moveAction.action.performed -= OnMovePerformed;
                moveAction.action.canceled -= OnMoveCanceled;
            }

            moveInput = Vector2.zero;
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

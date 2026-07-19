using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Synora.Gameplay.Interaction
{
    public sealed class InteractionInputReader : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference interactAction;

        public event Action InteractPressed;

        private void OnEnable()
        {
            if (interactAction == null || interactAction.action == null)
            {
                Debug.LogError("InteractionInputReader: Interact action reference is not assigned.", this);
                return;
            }

            interactAction.action.performed += HandleInteractPerformed;
            interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (interactAction == null || interactAction.action == null)
            {
                return;
            }

            interactAction.action.performed -= HandleInteractPerformed;
            interactAction.action.Disable();
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            InteractPressed?.Invoke();
        }
    }
}

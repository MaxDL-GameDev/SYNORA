using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Interaction
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ExaminableInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private ExaminableData data;
        [SerializeField] private int priority;
        [SerializeField] private bool interactionEnabled = true;

        public string InteractionId =>
            data != null ? data.InteractionId : string.Empty;

        public int Priority => priority;

        public bool CanInteract =>
            isActiveAndEnabled
            && interactionEnabled
            && data != null
            && data.HasValidInteractionId;

        public Vector2 InteractionPosition =>
            (Vector2)transform.position;

        public string PromptText => "Examinar";

        private void Awake()
        {
            if (data == null)
            {
                Debug.LogError(
                    "ExaminableInteractable: ExaminableData reference is not assigned.",
                    this);
            }
        }

        public void Execute(IInteractionReceiver receiver)
        {
            if (receiver == null)
            {
                return;
            }

            if (!CanInteract)
            {
                return;
            }

            receiver.ShowObservation(data);
        }
    }
}

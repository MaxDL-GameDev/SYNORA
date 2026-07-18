using UnityEngine;

namespace Synora.Gameplay.Interaction
{
    public interface IInteractable
    {
        string InteractionId { get; }
        int Priority { get; }
        bool CanInteract { get; }
        Vector2 InteractionPosition { get; }
        string PromptText { get; }
        void Execute(IInteractionReceiver receiver);
    }
}

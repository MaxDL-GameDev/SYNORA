using UnityEngine;
using Synora.Gameplay.Interaction;
using Synora.Systems;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Interactive origin of restoration (M6 F4): the only way to start Subdued →
    /// Restoring. Implements M2's <see cref="IInteractable"/> by composition (reusing the
    /// normal detector/controller/proximity pipeline — it never computes distance itself)
    /// and, on execution, asks the Brain directly:
    /// <c>CreatureBrain.RequestTransition(CreatureStateId.Restoring)</c>.
    ///
    /// Single source of truth is <see cref="CreatureBrain.CurrentStateId"/>: it is
    /// available ONLY while the creature is Subdued, so it disappears the instant the
    /// Brain applies Restoring. It keeps no restoration state of its own (no
    /// canRestore/isRestored/started flags), does not manage the timer, never jumps to
    /// Restored, and emits no presentation. Player capability is honored by reusing the
    /// existing <see cref="PlayerControlGate"/> (blocked by Observation or Defeat), not a
    /// new definition.
    /// </summary>
    public sealed class CreatureRestorationInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private PlayerControlGate gate;
        [SerializeField] private string interactionId = "creature.restore";
        [SerializeField] private int priority;
        [SerializeField] private bool interactionEnabled = true;

        public string InteractionId => interactionId;

        public int Priority => priority;

        public bool CanInteract =>
            isActiveAndEnabled
            && interactionEnabled
            && brain != null
            && brain.CurrentStateId == CreatureStateId.Subdued
            && !GateBlocked();

        public Vector2 InteractionPosition => (Vector2)transform.position;

        public string PromptText => "Restaurar";

        public void Execute(IInteractionReceiver receiver)
        {
            // Re-validate at confirmation: only a Subdued creature, with the player able
            // to act, starts restoration. A rejected attempt changes nothing.
            if (!CanInteract)
            {
                return;
            }

            brain.RequestTransition(CreatureStateId.Restoring);
        }

        // Reuses the existing "can the player act" contract. A missing gate reference is
        // treated as "not blocked" (the interaction pipeline still gates by target/range).
        private bool GateBlocked() => gate != null && gate.IsBlocked;
    }
}

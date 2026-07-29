using UnityEngine;
using Synora.Gameplay.Interaction;
using Synora.Systems;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Interactive origin of bonding (M7 F2): the only way to start Restored → Bonding.
    /// Mirrors <see cref="CreatureRestorationInteractable"/> exactly. Implements M2's
    /// <see cref="IInteractable"/> by composition (reusing the normal
    /// detector/controller/proximity pipeline — it never computes distance itself) and, on
    /// execution, asks the Brain directly:
    /// <c>CreatureBrain.RequestTransition(CreatureStateId.Bonding)</c>.
    ///
    /// Single source of truth is <see cref="CreatureBrain.CurrentStateId"/>: it is
    /// available ONLY while the creature is Restored, so it disappears the instant the
    /// Brain applies Bonding. Its ONLY responsibility is to request that transition: it
    /// keeps no bonding state of its own, does not know Bonded, the timer, the duration,
    /// presentation, following, affinity, ECO or persistence, does not create timers,
    /// never jumps to Bonded, and emits no presentation. Player capability is honored by
    /// reusing the existing <see cref="PlayerControlGate"/> (blocked by Observation or
    /// Defeat), not a new definition.
    /// </summary>
    public sealed class CreatureBondingInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private PlayerControlGate gate;
        [SerializeField] private string interactionId = "creature.bond";
        [SerializeField] private int priority;
        [SerializeField] private bool interactionEnabled = true;

        public string InteractionId => interactionId;

        public int Priority => priority;

        public bool CanInteract =>
            isActiveAndEnabled
            && interactionEnabled
            && brain != null
            && brain.CurrentStateId == CreatureStateId.Restored
            && !GateBlocked();

        public Vector2 InteractionPosition => (Vector2)transform.position;

        public string PromptText => "Vincular";

        public void Execute(IInteractionReceiver receiver)
        {
            // Re-validate at confirmation: only a Restored creature, with the player able
            // to act, starts bonding. A rejected attempt changes nothing.
            if (!CanInteract)
            {
                return;
            }

            brain.RequestTransition(CreatureStateId.Bonding);
        }

        // Reuses the existing "can the player act" contract. A missing gate reference is
        // treated as "not blocked" (the interaction pipeline still gates by target/range).
        private bool GateBlocked() => gate != null && gate.IsBlocked;
    }
}

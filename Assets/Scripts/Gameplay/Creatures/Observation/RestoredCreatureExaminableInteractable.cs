using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Interaction;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Examination of a restored creature (M6 F5): the ONLY interaction available once the
    /// Altered Verak reaches Restored. Implements M2's <see cref="IInteractable"/> by
    /// composition (mirroring <see cref="CreatureExaminableInteractable"/>) and delivers a
    /// restored-specific <see cref="ExaminableData"/> through the normal observation
    /// pipeline: <c>IInteractionReceiver.ShowObservation(...)</c>.
    ///
    /// Single source of truth is <see cref="CreatureBrain.CurrentStateId"/>: it is available
    /// while the creature is Restored OR Bonded — the calm, post-combat states in which the
    /// creature (and later the companion) remains observable (M7 F6). That keeps it mutually
    /// exclusive with <see cref="CreatureRestorationInteractable"/> (Subdued only) and
    /// <see cref="CreatureBondingInteractable"/> (Restored only) — the exclusion comes from
    /// each component's own <c>CanInteract</c>, never a shared flag. It keeps no restoration
    /// state of its own, never requests a transition, never touches the timer, and emits no
    /// presentation. Examination is a passive observation, so — per the F4.1 decision — it
    /// does NOT consult <c>PlayerControlGate</c>. It shows restored (never Altered) content.
    /// </summary>
    public sealed class RestoredCreatureExaminableInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private ExaminableData restoredData;
        [SerializeField] private int priority;
        [SerializeField] private bool interactionEnabled = true;

        public string InteractionId =>
            restoredData != null ? restoredData.InteractionId : string.Empty;

        public int Priority => priority;

        public bool CanInteract =>
            isActiveAndEnabled
            && interactionEnabled
            && brain != null
            && (brain.CurrentStateId == CreatureStateId.Restored
                || brain.CurrentStateId == CreatureStateId.Bonded)
            && restoredData != null
            && restoredData.HasValidInteractionId;

        public Vector2 InteractionPosition => (Vector2)transform.position;

        public string PromptText => "Examinar";

        public void Execute(IInteractionReceiver receiver)
        {
            if (receiver == null)
            {
                return;
            }

            // Re-validate at confirmation: only a Restored/Bonded creature is examinable
            // here. A rejected attempt shows nothing and changes nothing.
            if (!CanInteract)
            {
                return;
            }

            receiver.ShowObservation(restoredData);
        }
    }
}

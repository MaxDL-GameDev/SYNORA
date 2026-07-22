using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Interaction;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Adapts a creature's read-only observation view (<see cref="ICreatureObservationSource"/>)
    /// to M2's interaction contract (<see cref="IInteractable"/>). It cannot inherit
    /// <c>ExaminableInteractable</c> (that class is sealed), so it implements the
    /// contract by composition and stays self-contained.
    ///
    /// On interaction it reads the observable state exactly once and hands the matching
    /// <see cref="ExaminableData"/> to the receiver. It NEVER controls the creature: no
    /// transitions, no Brain access (it only knows the observation interface), no
    /// Animator/Sensor/Movement, no Update, no polling, no Time.timeScale. The snapshot
    /// semantics come for free: one data asset is chosen at Execute and the panel copies
    /// its text on open; nothing updates the panel afterward.
    /// </summary>
    public sealed class CreatureExaminableInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private CreatureObservationSource source;
        [SerializeField] private ExaminableData baseData;   // identity + fallback content
        [SerializeField] private ExaminableData calmData;
        [SerializeField] private ExaminableData roamingData;
        [SerializeField] private ExaminableData watchfulData;
        [SerializeField] private int priority;
        [SerializeField] private bool interactionEnabled = true;

        // Test-only injection seam: lets EditMode tests drive observable states via a
        // fake ICreatureObservationSource without a live CreatureBrain. There is NO
        // public setter and no runtime consumer — production always resolves the
        // serialized 'source' reference, which is the sole production authority. Tests
        // assign this field by reflection (CreatureTestKit.SetPrivate), matching the
        // project's established test-injection pattern.
        private ICreatureObservationSource injectedSource;

        // baseData is the SOLE authority for the interactable's identity. The per-state
        // assets (calm/roaming/watchful) only supply presentation text; their own
        // InteractionId is never consulted and never affects selection, priority,
        // hysteresis, duplicate detection, target identity, or CanInteract.
        public string InteractionId =>
            baseData != null ? baseData.InteractionId : string.Empty;

        public int Priority => priority;

        public bool CanInteract =>
            isActiveAndEnabled
            && interactionEnabled
            && ResolveSource() != null
            && baseData != null
            && baseData.HasValidInteractionId;

        public Vector2 InteractionPosition => (Vector2)transform.position;

        public string PromptText => "Examinar";

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

            // Read the observable state exactly once, at confirmation time.
            CreatureObservationState state = ResolveSource().CurrentObservationState;
            ExaminableData data = SelectData(state);
            receiver.ShowObservation(data);
        }

        private ExaminableData SelectData(CreatureObservationState state)
        {
            switch (state)
            {
                case CreatureObservationState.Calm: return calmData != null ? calmData : baseData;
                case CreatureObservationState.Roaming: return roamingData != null ? roamingData : baseData;
                case CreatureObservationState.Watchful: return watchfulData != null ? watchfulData : baseData;
                default: return baseData; // Unknown or any unmapped value falls back to base.
            }
        }

        // Prefer the injected interface; otherwise the serialized concrete component.
        // The serialized field is compared with Unity's overloaded ==, so an unassigned
        // reference resolves to a real null.
        private ICreatureObservationSource ResolveSource()
        {
            if (injectedSource != null)
            {
                return injectedSource;
            }

            return source != null ? source : null;
        }
    }
}

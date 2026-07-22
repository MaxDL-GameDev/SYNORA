using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Adapts a creature's public, read-only data to <see cref="ICreatureObservationSource"/>.
    /// Single responsibility: translate the Brain's current internal state to a public
    /// observable category and surface a display name. It NEVER controls the creature:
    /// no transitions, no writes to the Brain, no control of Animator/Sensor/Movement,
    /// no UI or interaction references, no Update, no per-frame polling, no Find. State
    /// is read on demand (when queried), so there is no cached mutable state to drift.
    ///
    /// Dependency direction is one-way: Source -> CreatureBrain.CurrentStateId (read) and
    /// Source -> CreatureIdentity (name). The Brain does not know this component exists.
    /// </summary>
    public sealed class CreatureObservationSource : MonoBehaviour, ICreatureObservationSource
    {
        // Explicit serialized references (no GetComponent/Find at runtime). The identity
        // is referenced directly rather than through the Brain, which keeps M3's
        // CreatureBrain untouched (its identity field stays private).
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private CreatureIdentity identity;

        private bool hasWarnedMissingBrain;

        public string DisplayName
        {
            get
            {
                if (identity != null && !string.IsNullOrWhiteSpace(identity.DisplayName))
                {
                    return identity.DisplayName;
                }

                // Stable, lore-free fallback: the GameObject name. Never invents a
                // species or narrative name.
                return gameObject.name;
            }
        }

        public CreatureObservationState CurrentObservationState
        {
            get
            {
                if (brain == null)
                {
                    WarnMissingBrainOnce();
                    return CreatureObservationState.Unknown;
                }

                return Resolve(brain.CurrentStateId);
            }
        }

        private void Awake()
        {
            if (brain == null)
            {
                WarnMissingBrainOnce();
            }
        }

        private void WarnMissingBrainOnce()
        {
            if (hasWarnedMissingBrain)
            {
                return;
            }

            Debug.LogWarning(
                "CreatureObservationSource: CreatureBrain reference is not assigned; reporting Unknown state.",
                this);
            hasWarnedMissingBrain = true;
        }

        /// <summary>
        /// Pure mapping from internal gameplay state to public observable category.
        /// Static and side-effect free for direct, Brain-free unit testing. Any state
        /// without a defined observable meaning maps to <see cref="CreatureObservationState.Unknown"/>
        /// rather than being mislabeled.
        /// </summary>
        public static CreatureObservationState Resolve(CreatureStateId state)
        {
            switch (state)
            {
                case CreatureStateId.Idle: return CreatureObservationState.Calm;
                case CreatureStateId.Patrol: return CreatureObservationState.Roaming;
                case CreatureStateId.Alert: return CreatureObservationState.Watchful;
                default: return CreatureObservationState.Unknown;
            }
        }
    }
}

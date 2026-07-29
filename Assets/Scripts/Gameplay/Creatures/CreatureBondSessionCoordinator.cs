using UnityEngine;
using Synora.Systems;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Session coordinator (M7 F6): observes <see cref="CreatureBrain.CurrentStateId"/> and
    /// records the session-only verak_vinculado flag on <see cref="BondSessionState"/> when
    /// the creature enters Bonded. It is NOT presentation and is fully independent of the F5
    /// feedback — the flag must be set on reaching Bonded whether or not the ficha/ECO exist
    /// or are enabled.
    ///
    /// Single responsibility: mark the session on Bonded. It never presents, never changes
    /// the brain state, never requests a transition, never moves anything. It does not
    /// un-mark on leaving Bonded (the bond already happened this session) and performs no
    /// persistence. <see cref="BondSessionState.MarkBonded"/> is idempotent, so the call may
    /// safely repeat while Bonded. Mirrors the creature-side observer pattern
    /// (CreatureBondingControlBlock): explicit serialized references, no Find/tags/Service
    /// Locator/singletons/global events.
    /// </summary>
    public sealed class CreatureBondSessionCoordinator : MonoBehaviour
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private BondSessionState session;

        private void Update() => Sync();

        /// <summary>
        /// Marks the session while the creature is in Bonded. Public for deterministic tests.
        /// Idempotent (MarkBonded tolerates repeats) and never reverts.
        /// </summary>
        public void Sync()
        {
            if (brain != null && brain.CurrentStateId == CreatureStateId.Bonded)
            {
                session?.MarkBonded();
            }
        }
    }
}

using UnityEngine;

namespace Synora.Systems
{
    /// <summary>
    /// Session-only record that the player has bonded the companion this session — the
    /// <c>verak_vinculado</c> flag (M7 F6). It is deliberately NON-persistent: a plain
    /// runtime bool on a scene MonoBehaviour, with no serialization, no PlayerPrefs, no
    /// save/load and no global state — a new Play session starts a fresh instance at
    /// <see cref="IsBonded"/> == false, and the instance is discarded on exiting Play Mode.
    ///
    /// It does NOT belong to the creature state/brain/context/identity; it is updated by a
    /// coordination component (<c>CreatureBondedFeedback</c>) when the creature enters
    /// Bonded, never by the state itself. Once set it stays set for the session ("this
    /// session already obtained the companion"). Restoring/loading it is intentionally out
    /// of scope (belongs to a future persistence milestone, not M7).
    /// </summary>
    public sealed class BondSessionState : MonoBehaviour
    {
        /// <summary>True once the companion has been bonded this session. Runtime-only.</summary>
        public bool IsBonded { get; private set; }

        /// <summary>Records that the bond was established this session. Idempotent.</summary>
        public void MarkBonded()
        {
            IsBonded = true;
        }
    }
}

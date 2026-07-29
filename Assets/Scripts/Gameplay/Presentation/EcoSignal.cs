using UnityEngine;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Provisional ECO transmission signal (M7 F5). ECO has no runtime infrastructure yet;
    /// this is the minimal placeholder the SPEC calls for: on <see cref="Emit"/> it records
    /// the emission and, if a placeholder AudioSource + clip are wired, plays a one-shot cue.
    /// It holds no narrative state, no persistence, no affinity/progress, and no global
    /// events; a real ECO system replaces it later. Edge-triggering is the caller's
    /// responsibility (CreatureBondedFeedback) — this component just performs the emission
    /// when told.
    /// </summary>
    public sealed class EcoSignal : MonoBehaviour
    {
        [SerializeField] private AudioSource source; // provisional placeholder (optional)
        [SerializeField] private AudioClip clip;     // provisional placeholder (optional)

        /// <summary>How many times the signal has been emitted this session (observability/tests).</summary>
        public int EmitCount { get; private set; }

        public void Emit()
        {
            EmitCount++;
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }
}

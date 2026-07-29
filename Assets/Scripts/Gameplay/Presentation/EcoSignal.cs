using UnityEngine;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Provisional ECO transmission signal (M7 F5). ECO has no runtime infrastructure yet;
    /// this is the minimal placeholder the SPEC calls for. On <see cref="Emit"/> it produces
    /// a real, perceptible effect — a brief "ECO: vínculo confirmado" text through a reused
    /// transient presenter (the project has no audio assets, so the SPEC's text placeholder
    /// is used) — and, if an AudioSource + clip are ever wired, also plays a one-shot cue.
    /// <see cref="EmitCount"/> is kept for observability/tests but is NOT the only effect.
    ///
    /// It holds no narrative state, no persistence, no affinity/progress, no global events,
    /// and no Service Locator; a real ECO system replaces it later. Edge-triggering is the
    /// caller's responsibility (<see cref="CreatureBondedFeedback"/>).
    /// </summary>
    public sealed class EcoSignal : MonoBehaviour
    {
        [SerializeField] private BondEstablishedPresenter display; // reused transient-text surface
        [SerializeField] private string message = "ECO: vínculo confirmado";
        [SerializeField] private AudioSource source; // optional (no audio assets yet)
        [SerializeField] private AudioClip clip;     // optional

        /// <summary>How many times the signal has been emitted this session (observability/tests).</summary>
        public int EmitCount { get; private set; }

        public void Emit()
        {
            EmitCount++;

            if (display != null)
            {
                display.Show(message); // provisional, non-narrative confirmation text
            }

            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }
}

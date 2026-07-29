using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Fires the "bond established" feedback ONCE when the creature enters Bonded (M7 F5).
    /// Pure observer/coordinator: it watches <see cref="CreatureBrain.CurrentStateId"/> (the
    /// single source of truth) and, on the rising edge into Bonded, triggers the three
    /// provisional feedback channels — a one-shot <see cref="SpriteFlash.Flash"/> (SpriteFlash
    /// stays the ONLY writer of SpriteRenderer.color), the "Vínculo establecido" UI
    /// notification, and the ECO signal.
    ///
    /// It never changes state, requests a transition, moves the creature, or touches
    /// CreatureMovement / PlayerControlGate. Edge-detected (firedForBond), so nothing repeats
    /// every Update. Scene-level references (panel, eco) may be null in the prefab and are
    /// wired per scene; a null reference is a safe no-op, matching the rest of the creature's
    /// wiring. It does NOT write the verak_vinculado flag or any persistence (F6).
    /// </summary>
    public sealed class CreatureBondedFeedback : MonoBehaviour
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private SpriteFlash flash;
        [SerializeField] private BondEstablishedPresenter panel;
        [SerializeField] private EcoSignal eco;
        [SerializeField] private string message = "Vínculo establecido";

        private bool firedForBond;

        private void Update() => Sync();

        /// <summary>
        /// Fires the feedback once on entering Bonded; re-arms on leaving. Public for
        /// deterministic tests. Idempotent while bonded — no repeat every Update.
        /// </summary>
        public void Sync()
        {
            bool bonded = brain != null && brain.CurrentStateId == CreatureStateId.Bonded;

            if (bonded && !firedForBond)
            {
                flash?.Flash();
                panel?.Show(message);
                eco?.Emit();
                firedForBond = true;
            }
            else if (!bonded && firedForBond)
            {
                firedForBond = false; // re-arm (Bonded is terminal in play; this is safety)
            }
        }
    }
}

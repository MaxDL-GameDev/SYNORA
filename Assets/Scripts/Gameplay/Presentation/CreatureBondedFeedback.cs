using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Fires the one-shot bond-established feedback ONCE when the creature enters Bonded
    /// (M7 F5): the "Vínculo establecido" ficha and the ECO signal. The persistent bond
    /// glow is a SEPARATE responsibility (<see cref="CreatureBondPresentation"/>) with its
    /// own latch, so the two never interfere.
    ///
    /// Pure observer: it watches <see cref="CreatureBrain.CurrentStateId"/> (the single
    /// source of truth) and, on the rising edge into Bonded (NOT Bonding), shows the ficha
    /// and emits the ECO signal exactly once (firedForBond latch). It never changes state,
    /// requests a transition, moves the creature, touches CreatureMovement/PlayerControlGate,
    /// or writes SpriteRenderer.color. There is no OnDisable reset of the one-shot latch, so
    /// disabling and re-enabling the same instance while still Bonded does not replay the
    /// feedback. Scene references (panel, eco) may be null in the prefab and are wired per
    /// scene; a null reference is a safe no-op. No verak_vinculado / persistence (F6).
    ///
    /// The ficha is provisional presentation only: title + the creature's name (from
    /// CreatureIdentity) + a provisional affinity label — never real affinity/progress data.
    /// </summary>
    public sealed class CreatureBondedFeedback : MonoBehaviour
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private CreatureIdentity identity;
        [SerializeField] private BondEstablishedPresenter panel;
        [SerializeField] private EcoSignal eco;
        [SerializeField] private string title = "Vínculo establecido";
        [SerializeField] private string provisionalAffinity = "provisional";

        private bool firedForBond;

        private void Update() => Sync();

        /// <summary>
        /// Fires the ficha + ECO once on entering Bonded; re-arms on leaving. Public for
        /// deterministic tests. Idempotent while bonded — no repeat every Update.
        /// </summary>
        public void Sync()
        {
            bool bonded = brain != null && brain.CurrentStateId == CreatureStateId.Bonded;

            if (bonded && !firedForBond)
            {
                panel?.Show(BuildFicha());
                eco?.Emit();
                firedForBond = true;
            }
            else if (!bonded && firedForBond)
            {
                firedForBond = false; // re-arm (Bonded is terminal in play; this is safety)
            }
        }

        // Provisional ficha (SPEC M7 F5): "Vínculo establecido" + name + provisional affinity.
        // The name comes from CreatureIdentity (single source, "Verak"), not a duplicated
        // string; the affinity is a provisional presentation label only.
        private string BuildFicha()
        {
            string name = identity != null ? identity.DisplayName : string.Empty;
            return title + "\n" + name + "\nAfinidad: " + provisionalAffinity;
        }
    }
}

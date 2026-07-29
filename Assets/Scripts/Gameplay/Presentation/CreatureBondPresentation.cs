using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Persistent "bond glow" (M7 F5). Mirrors <see cref="CreatureRestorationPresentation"/>:
    /// it reads <see cref="CreatureBrain.CurrentStateId"/> — the single source of truth — and
    /// holds a stable bond tint through <see cref="SpriteFlash"/> (the sole compositor of
    /// SpriteRenderer.color) while the creature is in Bonding OR Bonded, clearing it on
    /// leaving both states and on disable. SPEC M7 F5: "brillo de vínculo provisional durante
    /// Bonding y en Bonded … el brillo se limpia al salir de los estados de M7."
    ///
    /// Ownership latch (<c>owns</c>): the tint is set ONCE on entering the bond states and
    /// cleared ONCE on leaving them — never re-applied every Update, and it never clears a
    /// tint it does not own (so the Subdued/restoration tints stay intact). SpriteFlash
    /// exposes a single shared persistent-tint slot, so this component runs AFTER
    /// <see cref="CreatureRestorationPresentation"/> (<see cref="DefaultExecutionOrder"/>) so
    /// the Restored→Bonding handoff (restoration clears, bond sets) never loses the bond tint.
    /// It never changes state, requests a transition, moves, or writes SpriteRenderer.color.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class CreatureBondPresentation : MonoBehaviour
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private SpriteFlash flash;

        [Header("Bond glow (provisional tuning — not canon)")]
        [SerializeField] private Color bondTint = new Color(1f, 0.85f, 0.4f, 1f);
        [SerializeField, Range(0f, 1f)] private float bondIntensity = 0.4f;

        private bool owns;

        private void OnEnable() => Apply();
        private void Update() => Apply();

        /// <summary>
        /// Aligns the bond tint with the current state. Public for deterministic tests.
        /// Sets the tint once on entering Bonding/Bonded and clears it once on leaving both;
        /// idempotent while inside the bond states.
        /// </summary>
        public void Apply()
        {
            bool inBond = brain != null
                && (brain.CurrentStateId == CreatureStateId.Bonding
                    || brain.CurrentStateId == CreatureStateId.Bonded);

            if (inBond)
            {
                if (!owns)
                {
                    flash?.SetPersistentTint(bondTint, bondIntensity);
                    owns = true;
                }
            }
            else if (owns)
            {
                flash?.ClearPersistentTint();
                owns = false;
            }
        }

        private void OnDisable()
        {
            if (owns)
            {
                flash?.ClearPersistentTint(); // release only the tint we own
                owns = false;
            }
        }
    }
}

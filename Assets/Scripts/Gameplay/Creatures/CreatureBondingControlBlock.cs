using UnityEngine;
using Synora.Systems;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Holds the player still while THIS creature is in Bonding (M7 F3), then releases
    /// control when Bonding ends. Mirrors how every other block reason is owned — a
    /// MonoBehaviour that holds the <see cref="PlayerControlGate"/> reference and toggles a
    /// single additive reason (PlayerTemporaryDefeat owns Defeat, InteractionController owns
    /// Observation). It observes the Brain's public state
    /// (<see cref="CreatureBrain.CurrentStateId"/> — the single source of truth) and never
    /// changes state, moves, presents, or writes any bonding data.
    ///
    /// It blocks on the rising edge into Bonding and releases on the falling edge out of it,
    /// releasing ONLY its own reason (leaving Observation/Defeat untouched) and also
    /// releasing on disable, so a creature disabled mid-bonding never leaves the player
    /// stuck. A missing gate reference is treated as "nothing to block" (per-scene wiring),
    /// matching the interactable's tolerance of a null gate.
    /// </summary>
    public sealed class CreatureBondingControlBlock : MonoBehaviour
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private PlayerControlGate gate;

        private bool owns;

        private void Update() => Sync();

        /// <summary>
        /// Aligns the Bonding block with the Brain's current state. Public for deterministic
        /// tests. Idempotent: blocks once on entering Bonding, releases once on leaving it.
        /// </summary>
        public void Sync()
        {
            bool bonding = brain != null && brain.CurrentStateId == CreatureStateId.Bonding;

            if (bonding && !owns)
            {
                gate?.Block(ControlBlockReason.Bonding);
                owns = true;
            }
            else if (!bonding && owns)
            {
                gate?.Unblock(ControlBlockReason.Bonding);
                owns = false;
            }
        }

        private void OnDisable()
        {
            if (owns)
            {
                gate?.Unblock(ControlBlockReason.Bonding); // release ONLY Bonding; keep others
                owns = false;
            }
        }
    }
}

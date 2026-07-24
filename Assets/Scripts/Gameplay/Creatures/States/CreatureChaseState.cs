using UnityEngine;
using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Pursuit behavior for the Altered Verak. Moves toward the detected player and
    /// faces it cardinally; stops on reaching attack range and requests Attack when the
    /// attack controller is ready; returns to Idle when the player is lost. Never
    /// applies damage, never touches Health, cooldown or the Animator (the controller
    /// owns cooldown; Health-zero routes to Subdued with top priority). Stateless.
    /// </summary>
    public sealed class CreatureChaseState : ICreatureState
    {
        private readonly Health health;
        private readonly CreatureAttackController attack;
        private readonly float attackRangeSqr;

        public CreatureChaseState(Health health, CreatureAttackController attack, float attackRange)
        {
            this.health = health;
            this.attack = attack;
            float range = attackRange > 0f ? attackRange : 0f;
            this.attackRangeSqr = range * range;
        }

        public void Enter(CreatureContext context)
        {
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            if (health != null && health.IsZero)
            {
                return CreatureStateId.Subdued;
            }

            Transform player = context.DetectedPlayer;
            if (player == null || context.Root == null)
            {
                context.Movement?.Stop();
                return CreatureStateId.Idle; // lost the target
            }

            Vector2 toPlayer = (Vector2)player.position - (Vector2)context.Root.position;
            context.SetFacing(CreatureMovement.ResolveFacing(toPlayer, context.Facing));

            if (toPlayer.sqrMagnitude <= attackRangeSqr)
            {
                context.Movement?.Stop();

                // A target whose Health is depleted is not attackable (M5 F8): hold in
                // range without attacking rather than looping Attack on a defeated player.
                if (TargetDefeated(context))
                {
                    return null;
                }

                if (attack != null && attack.CanStart)
                {
                    return CreatureStateId.Attack;
                }

                return null; // in range, waiting for the cooldown to clear
            }

            context.Movement?.SetDestination(player.position);
            return null;
        }

        // Reuses the shared Health contract (not PlayerTemporaryDefeat): a detected
        // target with Health at zero is treated as not attackable. A target without a
        // Health is considered attackable (unchanged behavior for other targets).
        internal static bool TargetDefeated(CreatureContext context)
        {
            Transform p = context.DetectedPlayer;
            if (p == null)
            {
                return false;
            }

            Health h = p.GetComponentInParent<Health>();
            return h != null && h.IsZero;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

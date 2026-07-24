using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Attack behavior for the Altered Verak. On entry it stops moving and asks the
    /// attack controller to start a sequence in the current facing (captured for the
    /// whole attack — the player may move, the direction does not). It does NOT chase
    /// while the sequence runs. It returns to Chase once the sequence (windup + active)
    /// ends; the controller keeps advancing its cooldown, which Chase honors before
    /// re-attacking. Health-zero routes to Subdued with top priority. The controller
    /// owns timing and the physical hit; this state never applies damage or touches the
    /// Animator. Stateless.
    /// </summary>
    public sealed class CreatureAttackState : ICreatureState
    {
        private readonly Health health;
        private readonly CreatureAttackController attack;

        public CreatureAttackState(Health health, CreatureAttackController attack)
        {
            this.health = health;
            this.attack = attack;
        }

        public void Enter(CreatureContext context)
        {
            context.Movement?.Stop();
            context.SetMoving(false);

            // Never open an attack while already depleted (Subdued has top priority).
            if (health != null && health.IsZero)
            {
                return;
            }

            attack?.TryStartAttack(context.Facing);
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            if (health != null && health.IsZero)
            {
                // Stop any in-flight window immediately so no impact resolves after
                // depletion, regardless of the controller's execution order this frame.
                attack?.Cancel();
                return CreatureStateId.Subdued;
            }

            // The sequence (windup + active) is over — or never started — so re-evaluate
            // from Chase. Cooldown, owned by the controller, gates the next attack there.
            if (attack == null || !attack.IsSequenceActive)
            {
                return CreatureStateId.Chase;
            }

            return null;
        }

        public void Exit(CreatureContext context)
        {
        }
    }
}

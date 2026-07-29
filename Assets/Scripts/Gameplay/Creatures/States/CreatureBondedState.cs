using UnityEngine;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Permanent companion state (M7 F4): the bonded creature follows the player, and
    /// nothing else. It never returns to hostile/ambient behavior (no patrol, chase or
    /// attack), never abandons the bond, and never requests a transition — Bonded is a
    /// stable terminal state.
    ///
    /// Following reuses CreatureMovement (the single owner of locomotion): the state only
    /// calls SetDestination/Stop and updates facing via ResolveFacing. It never writes the
    /// Transform or Rigidbody2D, never computes/writes velocities, and never uses
    /// Lerp/MoveTowards/NavMesh/pathfinding.
    ///
    /// Oscillation is avoided with distance hysteresis (SPEC M7 §5): a latched decision
    /// starts following only past followDistance (the max) and stops only within
    /// followStopDistance (the min); inside the dead band the previous decision is kept.
    /// The distances are injected by AlteredVerakSetup — never hardcoded here. Presentation,
    /// UI, audio, affinity, ECO, the verak_vinculado flag and persistence belong to F5/F6;
    /// it does not touch PlayerControlGate (the Bonding block is owned by
    /// CreatureBondingControlBlock).
    /// </summary>
    public sealed class CreatureBondedState : ICreatureState
    {
        private readonly float followDistanceSqr;      // max: start/resume following beyond this
        private readonly float followStopDistanceSqr;  // min: stop within this
        private bool isFollowing;

        public CreatureBondedState(float followDistance, float followStopDistance)
        {
            float follow = followDistance > 0f ? followDistance : 0f;
            float stop = followStopDistance > 0f ? followStopDistance : 0f;
            // Enforce the hysteresis invariant 0 <= stop <= follow deterministically, so a
            // misconfigured inversion (stop > follow) can never make consecutive ticks
            // alternate between SetDestination and Stop. Never throws on bad Inspector input.
            if (stop > follow)
            {
                stop = follow;
            }
            followDistanceSqr = follow * follow;
            followStopDistanceSqr = stop * stop;
        }

        public void Enter(CreatureContext context)
        {
            // Never inherit a destination from the Bonding approach; begin from a clean,
            // deterministic "not following" decision. Does not move or teleport the creature,
            // move the player, or touch the gate.
            context.Movement?.Stop();
            context.SetMoving(false);
            isFollowing = false;
        }

        public CreatureStateId? Tick(CreatureContext context, float deltaTime)
        {
            Follow(context);
            return null; // stable: Bonded never requests a transition
        }

        public void Exit(CreatureContext context)
        {
        }

        // Follows the detected player through CreatureMovement, with distance hysteresis to
        // avoid oscillation. Never writes the Transform. A missing player/root/movement
        // simply holds still (still no transition — Bonded is stable).
        private void Follow(CreatureContext context)
        {
            Transform player = context.DetectedPlayer;
            if (player == null || context.Root == null || context.Movement == null)
            {
                context.Movement?.Stop();
                isFollowing = false;
                return;
            }

            Vector2 toPlayer = (Vector2)player.position - (Vector2)context.Root.position;
            context.SetFacing(CreatureMovement.ResolveFacing(toPlayer, context.Facing));
            float distanceSqr = toPlayer.sqrMagnitude;

            if (isFollowing)
            {
                // Following: keep heading to the player until inside the stop distance.
                if (distanceSqr <= followStopDistanceSqr)
                {
                    context.Movement.Stop();
                    isFollowing = false;
                    return;
                }

                context.Movement.SetDestination(player.position);
                return;
            }

            // Stopped: resume only once the player is beyond the follow distance. Inside the
            // dead band [stop, follow] the decision stays "stopped" (no flip-flop).
            if (distanceSqr > followDistanceSqr)
            {
                isFollowing = true;
                context.Movement.SetDestination(player.position);
            }
        }
    }
}

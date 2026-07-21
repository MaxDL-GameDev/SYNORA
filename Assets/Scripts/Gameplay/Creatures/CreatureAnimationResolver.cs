using UnityEngine;

namespace Synora.Gameplay.Creatures
{
    /// <summary>Logical visual state a creature presents (independent of gameplay state).</summary>
    public enum CreatureVisualState
    {
        Idle = 0,
        Walk = 1,
        Alert = 2
    }

    /// <summary>Four-way facing used to pick a directional clip. Verak has dedicated art per direction (no flipX).</summary>
    public enum CreatureFacingDirection
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3
    }

    /// <summary>Immutable presentation result: what the visuals should show for a given logical state.</summary>
    public readonly struct CreaturePresentation
    {
        public readonly CreatureVisualState VisualState;
        public readonly CreatureFacingDirection Direction;
        public readonly bool IsMoving;
        public readonly bool FlipX;

        public CreaturePresentation(CreatureVisualState visualState, CreatureFacingDirection direction, bool isMoving, bool flipX)
        {
            VisualState = visualState;
            Direction = direction;
            IsMoving = isMoving;
            FlipX = flipX;
        }
    }

    /// <summary>
    /// Pure, allocation-free mapping from logical gameplay state to a visual
    /// presentation. No Animator, no MonoBehaviour, no strings. Deterministic and
    /// unit-testable. Verak uses dedicated per-direction art, so FlipX is always
    /// false (kept in the result so the presenter remains the single flip authority).
    /// </summary>
    public static class CreatureAnimationResolver
    {
        public static CreaturePresentation Resolve(CreatureStateId state, Vector2Int facing, bool isMoving)
        {
            CreatureFacingDirection direction = ResolveDirection(facing);

            CreatureVisualState visual;
            switch (state)
            {
                case CreatureStateId.Alert:
                    visual = CreatureVisualState.Alert; // Alert ignores movement for the visual
                    break;
                case CreatureStateId.Patrol:
                    visual = isMoving ? CreatureVisualState.Walk : CreatureVisualState.Idle;
                    break;
                case CreatureStateId.Idle:
                    visual = CreatureVisualState.Idle;
                    break;
                default:
                    visual = CreatureVisualState.Idle; // safe fallback for unknown state
                    break;
            }

            bool moving = visual == CreatureVisualState.Walk;
            const bool flipX = false; // dedicated Right art; no mirroring for Verak
            return new CreaturePresentation(visual, direction, moving, flipX);
        }

        private static CreatureFacingDirection ResolveDirection(Vector2Int facing)
        {
            if (facing == Vector2Int.up) return CreatureFacingDirection.Up;
            if (facing == Vector2Int.left) return CreatureFacingDirection.Left;
            if (facing == Vector2Int.right) return CreatureFacingDirection.Right;
            if (facing == Vector2Int.down) return CreatureFacingDirection.Down;
            return CreatureFacingDirection.Down; // safe fallback for zero/unknown facing
        }
    }
}

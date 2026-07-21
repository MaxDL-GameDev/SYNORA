using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureAnimationResolverTests
    {
        [Test]
        public void Idle_EachDirection_MapsToIdleWithCorrectDirection()
        {
            Assert.AreEqual(CreatureVisualState.Idle, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.down, false).VisualState);
            Assert.AreEqual(CreatureFacingDirection.Down, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.down, false).Direction);
            Assert.AreEqual(CreatureFacingDirection.Up, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.up, false).Direction);
            Assert.AreEqual(CreatureFacingDirection.Left, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.left, false).Direction);
            Assert.AreEqual(CreatureFacingDirection.Right, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.right, false).Direction);
        }

        [Test]
        public void Patrol_Moving_MapsToWalk()
        {
            CreaturePresentation p = CreatureAnimationResolver.Resolve(CreatureStateId.Patrol, Vector2Int.right, true);
            Assert.AreEqual(CreatureVisualState.Walk, p.VisualState);
            Assert.IsTrue(p.IsMoving);
        }

        [Test]
        public void Patrol_NotMoving_MapsToIdle()
        {
            CreaturePresentation p = CreatureAnimationResolver.Resolve(CreatureStateId.Patrol, Vector2Int.right, false);
            Assert.AreEqual(CreatureVisualState.Idle, p.VisualState);
            Assert.IsFalse(p.IsMoving);
        }

        [Test]
        public void Alert_MapsToAlert_IgnoringMovement()
        {
            CreaturePresentation moving = CreatureAnimationResolver.Resolve(CreatureStateId.Alert, Vector2Int.down, true);
            CreaturePresentation still = CreatureAnimationResolver.Resolve(CreatureStateId.Alert, Vector2Int.down, false);
            Assert.AreEqual(CreatureVisualState.Alert, moving.VisualState);
            Assert.AreEqual(CreatureVisualState.Alert, still.VisualState);
            Assert.IsFalse(moving.IsMoving); // Alert visual is never "moving"
        }

        [Test]
        public void UnknownState_FallsBackToIdle()
        {
            CreaturePresentation p = CreatureAnimationResolver.Resolve((CreatureStateId)999, Vector2Int.down, true);
            Assert.AreEqual(CreatureVisualState.Idle, p.VisualState);
        }

        [Test]
        public void UnknownOrZeroFacing_FallsBackToDown()
        {
            Assert.AreEqual(CreatureFacingDirection.Down, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.zero, false).Direction);
            Assert.AreEqual(CreatureFacingDirection.Down, CreatureAnimationResolver.Resolve(CreatureStateId.Idle, new Vector2Int(2, 2), false).Direction);
        }

        [Test]
        public void FlipX_IsAlwaysFalse_DedicatedPerDirectionArt()
        {
            Assert.IsFalse(CreatureAnimationResolver.Resolve(CreatureStateId.Idle, Vector2Int.left, false).FlipX);
            Assert.IsFalse(CreatureAnimationResolver.Resolve(CreatureStateId.Patrol, Vector2Int.right, true).FlipX);
            Assert.IsFalse(CreatureAnimationResolver.Resolve(CreatureStateId.Alert, Vector2Int.up, false).FlipX);
        }

        [Test]
        public void Resolve_IsDeterministic()
        {
            CreaturePresentation a = CreatureAnimationResolver.Resolve(CreatureStateId.Patrol, Vector2Int.left, true);
            CreaturePresentation b = CreatureAnimationResolver.Resolve(CreatureStateId.Patrol, Vector2Int.left, true);
            Assert.AreEqual(a.VisualState, b.VisualState);
            Assert.AreEqual(a.Direction, b.Direction);
            Assert.AreEqual(a.IsMoving, b.IsMoving);
            Assert.AreEqual(a.FlipX, b.FlipX);
        }
    }
}

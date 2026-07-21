using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    // Visual-only presenter tests for CreatureAnimator.
    public sealed class CreatureAnimatorViewTests
    {
        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i] != null) Object.DestroyImmediate(temp[i]);
            }
            temp.Clear();
        }

        private CreatureAnimator NewAnimator(out CreatureContext context, bool withAnimator = true, bool withSprite = true)
        {
            var id = CreatureTestKit.NewIdentity(temp);
            context = CreatureTestKit.BuildContext(temp, id, new List<Transform>(), out _, out _, out _);

            var go = new GameObject("VerakVisual");
            temp.Add(go);
            var presenter = go.AddComponent<CreatureAnimator>();
            if (withAnimator)
            {
                CreatureTestKit.SetPrivate(presenter, "animator", go.AddComponent<Animator>());
            }
            if (withSprite)
            {
                CreatureTestKit.SetPrivate(presenter, "spriteRenderer", go.AddComponent<SpriteRenderer>());
            }
            return presenter;
        }

        [Test]
        public void Initialize_ValidAndIdempotent()
        {
            var presenter = NewAnimator(out CreatureContext ctx);
            presenter.Initialize(ctx);
            Assert.IsTrue(presenter.IsInitialized);
            presenter.Initialize(ctx);
            Assert.IsTrue(presenter.IsInitialized);
        }

        [Test]
        public void Refresh_BeforeInitialize_NoOp()
        {
            var presenter = NewAnimator(out _);
            Assert.DoesNotThrow(() => presenter.Refresh());
            Assert.IsFalse(presenter.HasApplied);
        }

        [Test]
        public void Refresh_AppliesResolvedPresentation()
        {
            var presenter = NewAnimator(out CreatureContext ctx);
            presenter.Initialize(ctx);

            ctx.SetCurrentState(CreatureStateId.Patrol);
            ctx.SetFacing(Vector2Int.left);
            ctx.SetMoving(true);
            presenter.Refresh();

            Assert.IsTrue(presenter.HasApplied);
            Assert.AreEqual(CreatureVisualState.Walk, presenter.LastVisualState);
            Assert.AreEqual(CreatureFacingDirection.Left, presenter.LastDirection);
            Assert.IsFalse(presenter.LastFlipX);
        }

        [Test]
        public void Refresh_AppliesFlipXFalseToSpriteRenderer()
        {
            var presenter = NewAnimator(out CreatureContext ctx);
            var sr = (SpriteRenderer)CreatureTestKit.GetPrivate(presenter, "spriteRenderer");
            sr.flipX = true;
            presenter.Initialize(ctx);
            ctx.SetCurrentState(CreatureStateId.Idle);
            ctx.SetFacing(Vector2Int.right);
            presenter.Refresh();
            Assert.IsFalse(sr.flipX);
        }

        [Test]
        public void Refresh_AlertIgnoresMovement()
        {
            var presenter = NewAnimator(out CreatureContext ctx);
            presenter.Initialize(ctx);
            ctx.SetCurrentState(CreatureStateId.Alert);
            ctx.SetFacing(Vector2Int.up);
            ctx.SetMoving(true);
            presenter.Refresh();
            Assert.AreEqual(CreatureVisualState.Alert, presenter.LastVisualState);
        }

        [Test]
        public void AnimatorNull_IsSafe()
        {
            var presenter = NewAnimator(out CreatureContext ctx, withAnimator: false, withSprite: false);
            presenter.Initialize(ctx);
            ctx.SetCurrentState(CreatureStateId.Patrol);
            ctx.SetFacing(Vector2Int.down);
            ctx.SetMoving(true);
            Assert.DoesNotThrow(() => presenter.Refresh());
            Assert.AreEqual(CreatureVisualState.Walk, presenter.LastVisualState);
        }

        [Test]
        public void Refresh_DoesNotModifyContext()
        {
            var presenter = NewAnimator(out CreatureContext ctx);
            presenter.Initialize(ctx);
            ctx.SetCurrentState(CreatureStateId.Patrol);
            ctx.SetFacing(Vector2Int.left);
            ctx.SetMoving(true);
            int patrolIndexBefore = ctx.PatrolIndex;
            float timerBefore = ctx.StateTimer;

            presenter.Refresh();

            Assert.AreEqual(CreatureStateId.Patrol, ctx.CurrentState);
            Assert.AreEqual(Vector2Int.left, ctx.Facing);
            Assert.IsTrue(ctx.IsMoving);
            Assert.AreEqual(patrolIndexBefore, ctx.PatrolIndex);
            Assert.AreEqual(timerBefore, ctx.StateTimer);
        }
    }
}

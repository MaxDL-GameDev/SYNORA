using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureBondingStateTests
    {
        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i] != null) Object.DestroyImmediate(temp[i]);
            }
            temp.Clear();
        }

        private CreatureContext Build(out CreatureMovement movement)
        {
            var id = CreatureTestKit.NewIdentity(temp);
            return CreatureTestKit.BuildContext(temp, id, new List<Transform>(), out movement, out _, out _);
        }

        [Test]
        public void Enter_StopsMovement()
        {
            var ctx = Build(out CreatureMovement movement);
            movement.SetDestination(new Vector2(5f, 0f));
            var state = new CreatureBondingState(1.25f);
            state.Enter(ctx);
            Assert.IsFalse(movement.HasDestination);
            Assert.IsFalse(ctx.IsMoving);
        }

        [Test]
        public void DoesNotComplete_BeforeDuration()
        {
            var ctx = Build(out _);
            var state = new CreatureBondingState(1.25f);
            state.Enter(ctx);
            Assert.IsNull(state.Tick(ctx, 0.5f));
            Assert.IsNull(state.Tick(ctx, 0.5f)); // 1.0 < 1.25
        }

        [Test]
        public void RequestsBonded_WhenTimerCompletes()
        {
            var ctx = Build(out _);
            var state = new CreatureBondingState(1.0f);
            state.Enter(ctx);
            Assert.IsNull(state.Tick(ctx, 0.5f));
            Assert.AreEqual(CreatureStateId.Bonded, state.Tick(ctx, 0.5f)); // 1.0 >= 1.0
        }

        [Test]
        public void Completes_OnExactDuration()
        {
            var ctx = Build(out _);
            var state = new CreatureBondingState(1.0f);
            state.Enter(ctx);
            Assert.AreEqual(CreatureStateId.Bonded, state.Tick(ctx, 1.0f));
        }

        [Test]
        public void Enter_RestartsTimer()
        {
            var ctx = Build(out _);
            var state = new CreatureBondingState(1.0f);
            state.Enter(ctx);
            Assert.AreEqual(CreatureStateId.Bonded, state.Tick(ctx, 1.0f)); // complete
            state.Enter(ctx); // fresh run
            Assert.IsNull(state.Tick(ctx, 0.5f), "Re-entry must restart the timer.");
        }

        [Test]
        public void ZeroDuration_CompletesOnFirstTick()
        {
            var ctx = Build(out _);
            var state = new CreatureBondingState(0f);
            state.Enter(ctx);
            Assert.AreEqual(CreatureStateId.Bonded, state.Tick(ctx, 0.016f));
        }

        [Test]
        public void Tick_BeforeEnter_ReturnsNull()
        {
            var ctx = Build(out _);
            var state = new CreatureBondingState(1.0f);
            Assert.IsNull(state.Tick(ctx, 1.0f), "No timer before Enter; must be safe (cannot complete without its timer).");
        }

        // ── F3: approach via CreatureMovement (never moves the Transform directly) ──

        [Test]
        public void Tick_ApproachesPlayer_ViaMovement_WithoutMovingTransform()
        {
            var ctx = Build(out CreatureMovement movement);
            Vector3 rootBefore = ctx.Root.position;
            Transform player = CreatureTestKit.NewPoint(temp, new Vector2(5f, 0f));
            ctx.SetDetectedPlayer(player);

            var state = new CreatureBondingState(1.0f);
            state.Enter(ctx);
            Assert.IsNull(state.Tick(ctx, 0.2f), "not yet complete");

            Assert.IsTrue(movement.HasDestination, "Bonding drives the approach through CreatureMovement.");
            Assert.AreEqual((Vector2)player.position, movement.Destination, "destination is the player's position");
            Assert.AreEqual(rootBefore, ctx.Root.position, "the state must never move the Transform directly");
        }

        [Test]
        public void Tick_NoPlayer_HoldsStill()
        {
            var ctx = Build(out CreatureMovement movement);
            var state = new CreatureBondingState(1.0f);
            state.Enter(ctx);
            movement.SetDestination(new Vector2(9f, 9f)); // a leftover destination
            Assert.IsNull(state.Tick(ctx, 0.2f));
            Assert.IsFalse(movement.HasDestination, "with no player the approach holds still (no endless movement)");
        }

        [Test]
        public void NonInterruptible_NoPlayer_NeverLeavesUntilBonded()
        {
            var ctx = Build(out _); // no DetectedPlayer
            var state = new CreatureBondingState(0.5f);
            state.Enter(ctx);
            Assert.IsNull(state.Tick(ctx, 0.2f), "never Idle: Bonding is non-interruptible");
            Assert.IsNull(state.Tick(ctx, 0.2f), "never Idle: Bonding is non-interruptible");
            Assert.AreEqual(CreatureStateId.Bonded, state.Tick(ctx, 0.2f), "0.6 >= 0.5 → the only exit is Bonded");
        }
    }
}

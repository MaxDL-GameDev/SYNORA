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
    }
}

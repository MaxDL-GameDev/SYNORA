using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureBondedStateTests
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
            var state = new CreatureBondedState();
            state.Enter(ctx);
            Assert.IsFalse(movement.HasDestination);
            Assert.IsFalse(ctx.IsMoving);
        }

        [Test]
        public void Tick_AlwaysReturnsNull_Stable()
        {
            var ctx = Build(out _);
            var state = new CreatureBondedState();
            state.Enter(ctx);
            Assert.IsNull(state.Tick(ctx, 0.5f));
            Assert.IsNull(state.Tick(ctx, 100f));
            Assert.IsNull(state.Tick(ctx, 0f));
        }

        [Test]
        public void DoesNotRequestTransitions_OverManyTicks()
        {
            var ctx = Build(out _);
            var state = new CreatureBondedState();
            state.Enter(ctx);
            for (int i = 0; i < 50; i++)
            {
                Assert.IsNull(state.Tick(ctx, 0.1f), "Bonded is stable: never requests a transition (following arrives in F4).");
            }
        }
    }
}

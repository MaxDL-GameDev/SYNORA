using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class AlertStateTests
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

        private CreatureContext Build(out CreatureMovement movement, out CreatureSensor sensor, out Transform root,
            float lose = 4f, float linger = 1.5f)
        {
            var id = CreatureTestKit.NewIdentity(temp, lose: lose, linger: linger);
            return CreatureTestKit.BuildContext(temp, id, new List<Transform>(), out movement, out sensor, out root);
        }

        [Test]
        public void Enter_StopsMovementAndResetsLinger()
        {
            var ctx = Build(out CreatureMovement movement, out _, out _);
            movement.SetDestination(new Vector2(5f, 0f));
            var state = new AlertState();
            state.Enter(ctx);
            Assert.IsFalse(movement.HasDestination);
            Assert.IsFalse(ctx.IsMoving);
            Assert.AreEqual(0f, ctx.StateTimer);
        }

        [Test]
        public void Tick_WithinLoseRadius_RemainsAlert()
        {
            var ctx = Build(out _, out CreatureSensor sensor, out _);
            CreatureTestKit.InjectDistance(sensor, 4f); // within lose (16)
            var state = new AlertState();
            state.Enter(ctx);
            CreatureStateId? next = state.Tick(ctx, 0.5f);
            Assert.IsNull(next);
            Assert.AreEqual(0f, ctx.StateTimer); // linger reset while in range
        }

        [Test]
        public void Tick_OutsideLose_LingerElapses_RequestsPatrol()
        {
            var ctx = Build(out _, out CreatureSensor sensor, out _, linger: 0.5f);
            CreatureTestKit.InjectDistance(sensor, 100f); // beyond lose (16)
            var state = new AlertState();
            state.Enter(ctx);
            CreatureStateId? next = state.Tick(ctx, 1f); // exceeds linger
            Assert.AreEqual(CreatureStateId.Patrol, next);
        }

        [Test]
        public void Tick_Reentry_ResetsLinger()
        {
            var ctx = Build(out _, out CreatureSensor sensor, out _, linger: 1f);
            var state = new AlertState();
            state.Enter(ctx);

            CreatureTestKit.InjectDistance(sensor, 100f); // outside lose
            state.Tick(ctx, 0.3f);
            Assert.AreEqual(0.3f, ctx.StateTimer, 1e-5f);

            CreatureTestKit.InjectDistance(sensor, 4f); // re-enters lose
            CreatureStateId? next = state.Tick(ctx, 0.1f);
            Assert.IsNull(next);
            Assert.AreEqual(0f, ctx.StateTimer); // linger reset on reentry
        }

        [Test]
        public void Tick_NeverChasesPlayer()
        {
            var ctx = Build(out CreatureMovement movement, out CreatureSensor sensor, out _);
            CreatureTestKit.InjectDistance(sensor, 4f); // within lose
            var state = new AlertState();
            state.Enter(ctx);
            state.Tick(ctx, 0.1f);
            Assert.IsFalse(movement.HasDestination); // Alert never sets a destination
        }

        [Test]
        public void Tick_FacesPlayerWhileAlert()
        {
            var ctx = Build(out _, out CreatureSensor sensor, out Transform root);
            root.position = Vector3.zero;
            Transform player = CreatureTestKit.NewPoint(temp, new Vector2(2f, 0f)); // to the right
            ctx.SetDetectedPlayer(player);
            CreatureTestKit.InjectDistance(sensor, 4f); // within lose
            var state = new AlertState();
            state.Enter(ctx);
            state.Tick(ctx, 0.1f);
            Assert.AreEqual(Vector2Int.right, ctx.Facing);
        }
    }
}

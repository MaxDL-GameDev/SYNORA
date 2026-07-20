using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class PatrolStateTests
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

        private CreatureContext Build(out CreatureMovement movement, out CreatureSensor sensor,
            IReadOnlyList<Transform> points)
        {
            var id = CreatureTestKit.NewIdentity(temp);
            return CreatureTestKit.BuildContext(temp, id, points, out movement, out sensor, out _);
        }

        private static void ForceArrival(CreatureMovement movement)
        {
            // Place the body on its destination and tick once so it arrives (HasDestination -> false).
            Rigidbody2D body = (Rigidbody2D)CreatureTestKit.GetPrivate(movement, "body");
            body.position = movement.Destination;
            movement.FixedTick(0.02f);
        }

        [Test]
        public void Enter_OrdersCurrentDestination()
        {
            var p0 = CreatureTestKit.NewPoint(temp, new Vector2(2f, 0f));
            var p1 = CreatureTestKit.NewPoint(temp, new Vector2(-2f, 0f));
            var ctx = Build(out CreatureMovement movement, out _, new List<Transform> { p0, p1 });
            var state = new PatrolState();
            state.Enter(ctx);
            Assert.IsTrue(movement.HasDestination);
            Assert.AreEqual((Vector2)p0.position, movement.Destination);
        }

        [Test]
        public void Tick_OnArrival_AdvancesPingPongIndex_RequestsIdle()
        {
            var p0 = CreatureTestKit.NewPoint(temp, new Vector2(2f, 0f));
            var p1 = CreatureTestKit.NewPoint(temp, new Vector2(-2f, 0f));
            var ctx = Build(out CreatureMovement movement, out _, new List<Transform> { p0, p1 });
            var state = new PatrolState();
            state.Enter(ctx);
            ForceArrival(movement);

            CreatureStateId? next = state.Tick(ctx, 0.02f);
            Assert.AreEqual(CreatureStateId.Idle, next);
            Assert.AreEqual(1, ctx.PatrolIndex); // advanced 0 -> 1
        }

        [Test]
        public void Tick_ZeroPoints_IsSafe()
        {
            var ctx = Build(out CreatureMovement movement, out _, new List<Transform>());
            var state = new PatrolState();
            Assert.DoesNotThrow(() => state.Enter(ctx));
            CreatureStateId? next = null;
            Assert.DoesNotThrow(() => next = state.Tick(ctx, 0.02f));
            Assert.IsNull(next);
            Assert.AreEqual(0, ctx.PatrolIndex);
            Assert.IsFalse(movement.HasDestination);
        }

        [Test]
        public void Tick_OnePoint_IsSafe()
        {
            var p0 = CreatureTestKit.NewPoint(temp, new Vector2(2f, 0f));
            var ctx = Build(out CreatureMovement movement, out _, new List<Transform> { p0 });
            var state = new PatrolState();
            state.Enter(ctx);
            ForceArrival(movement);
            CreatureStateId? next = state.Tick(ctx, 0.02f);
            Assert.AreEqual(CreatureStateId.Idle, next);
            Assert.AreEqual(0, ctx.PatrolIndex); // 1-point PingPong stays at 0
        }

        [Test]
        public void Enter_NullPoint_IsSafe()
        {
            var ctx = Build(out CreatureMovement movement, out _, new List<Transform> { null });
            var state = new PatrolState();
            Assert.DoesNotThrow(() => state.Enter(ctx));
            Assert.IsFalse(movement.HasDestination); // no destination ordered for a null point
        }

        [Test]
        public void Tick_PlayerDetected_RequestsAlert()
        {
            var p0 = CreatureTestKit.NewPoint(temp, new Vector2(2f, 0f));
            var ctx = Build(out _, out CreatureSensor sensor, new List<Transform> { p0 });
            CreatureTestKit.InjectDistance(sensor, 1f); // within detection
            var state = new PatrolState();
            state.Enter(ctx);
            CreatureStateId? next = state.Tick(ctx, 0.02f);
            Assert.AreEqual(CreatureStateId.Alert, next);
        }

        [Test]
        public void Tick_DoesNotMoveTransform()
        {
            var p0 = CreatureTestKit.NewPoint(temp, new Vector2(5f, 0f));
            var ctx = Build(out CreatureMovement movement, out _, new List<Transform> { p0 });
            Vector3 before = movement.transform.position;
            var state = new PatrolState();
            state.Enter(ctx);
            state.Tick(ctx, 0.02f);
            Assert.AreEqual(before, movement.transform.position);
        }
    }
}

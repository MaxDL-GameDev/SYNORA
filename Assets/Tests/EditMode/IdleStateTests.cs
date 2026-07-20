using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class IdleStateTests
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
            float idle = 2f, int pointCount = 1)
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: idle);
            var points = new List<Transform>();
            for (int i = 0; i < pointCount; i++) points.Add(CreatureTestKit.NewPoint(temp, new Vector2(i + 1, 0f)));
            return CreatureTestKit.BuildContext(temp, id, points, out movement, out sensor, out _);
        }

        [Test]
        public void Enter_StopsMovementAndResetsTimer()
        {
            var ctx = Build(out CreatureMovement movement, out _);
            movement.SetDestination(new Vector2(5f, 0f));
            var state = new IdleState();
            state.Enter(ctx);
            Assert.IsFalse(movement.HasDestination);
            Assert.IsFalse(ctx.IsMoving);
            Assert.AreEqual(0f, ctx.StateTimer);
        }

        [Test]
        public void Tick_AdvancesTimer()
        {
            var ctx = Build(out _, out _, idle: 10f);
            var state = new IdleState();
            state.Enter(ctx);
            state.Tick(ctx, 0.5f);
            state.Tick(ctx, 0.25f);
            Assert.AreEqual(0.75f, ctx.StateTimer, 1e-5f);
        }

        [Test]
        public void Tick_NegativeDeltaTime_DoesNotReduceTimer()
        {
            var ctx = Build(out _, out _, idle: 10f);
            var state = new IdleState();
            state.Enter(ctx);
            state.Tick(ctx, 0.5f);
            state.Tick(ctx, -1f);
            Assert.AreEqual(0.5f, ctx.StateTimer, 1e-5f);
        }

        [Test]
        public void Tick_TimerElapsed_RequestsPatrol()
        {
            var ctx = Build(out _, out _, idle: 0.5f, pointCount: 1);
            var state = new IdleState();
            state.Enter(ctx);
            CreatureStateId? next = state.Tick(ctx, 1f);
            Assert.AreEqual(CreatureStateId.Patrol, next);
        }

        [Test]
        public void Tick_NoPatrolPoints_DoesNotRequestPatrol()
        {
            var ctx = Build(out _, out _, idle: 0.5f, pointCount: 0);
            var state = new IdleState();
            state.Enter(ctx);
            CreatureStateId? next = state.Tick(ctx, 1f);
            Assert.IsNull(next);
        }

        [Test]
        public void Tick_PlayerWithinDetection_RequestsAlertImmediately()
        {
            var ctx = Build(out _, out CreatureSensor sensor, idle: 100f);
            CreatureTestKit.InjectDistance(sensor, 1f); // within detection (9)
            var state = new IdleState();
            state.Enter(ctx);
            CreatureStateId? next = state.Tick(ctx, 0.02f);
            Assert.AreEqual(CreatureStateId.Alert, next);
        }
    }
}

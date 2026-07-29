using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureBondedStateTests
    {
        private readonly List<Object> temp = new List<Object>();

        // Follow band used across tests: stop within 1, resume past 2 (dead band [1, 2]).
        private const float FollowDistance = 2f;
        private const float FollowStopDistance = 1f;

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

        private static CreatureBondedState NewState() =>
            new CreatureBondedState(FollowDistance, FollowStopDistance);

        private Transform PlayerAt(CreatureContext ctx, Vector2 pos)
        {
            Transform player = CreatureTestKit.NewPoint(temp, pos);
            ctx.SetDetectedPlayer(player);
            return player;
        }

        // ── F1 carry-over: entry and stability ──

        [Test]
        public void Enter_StopsMovement()
        {
            var ctx = Build(out CreatureMovement movement);
            movement.SetDestination(new Vector2(5f, 0f));
            var state = NewState();
            state.Enter(ctx);
            Assert.IsFalse(movement.HasDestination, "Enter must not inherit a destination from Bonding.");
            Assert.IsFalse(ctx.IsMoving);
        }

        [Test]
        public void Tick_NeverRequestsTransition()
        {
            var ctx = Build(out _);
            PlayerAt(ctx, new Vector2(10f, 0f)); // far -> follows, but still no transition
            var state = NewState();
            state.Enter(ctx);
            for (int i = 0; i < 50; i++)
            {
                Assert.IsNull(state.Tick(ctx, 0.1f), "Bonded is stable: it never requests a transition.");
            }
        }

        // ── F4: follow with hysteresis ──

        [Test]
        public void Follows_WhenPlayerBeyondFollowDistance()
        {
            var ctx = Build(out CreatureMovement movement);
            Transform player = PlayerAt(ctx, new Vector2(5f, 0f)); // 5 > 2
            var state = NewState();
            state.Enter(ctx);

            state.Tick(ctx, 0.1f);

            Assert.IsTrue(movement.HasDestination, "A far player triggers following.");
            Assert.AreEqual((Vector2)player.position, movement.Destination);
        }

        [Test]
        public void StaysStopped_WhenPlayerWithinFollowDistance_FromRest()
        {
            var ctx = Build(out CreatureMovement movement);
            PlayerAt(ctx, new Vector2(1.5f, 0f)); // inside the dead band [1, 2]
            var state = NewState();
            state.Enter(ctx); // starts "not following"

            state.Tick(ctx, 0.1f);

            Assert.IsFalse(movement.HasDestination, "From rest, the dead band keeps the companion stopped.");
        }

        [Test]
        public void Stops_WhenReachingStopDistance_WhileFollowing()
        {
            var ctx = Build(out CreatureMovement movement);
            Transform player = PlayerAt(ctx, new Vector2(5f, 0f));
            var state = NewState();
            state.Enter(ctx);
            state.Tick(ctx, 0.1f);
            Assert.IsTrue(movement.HasDestination, "precondition: following a far player");

            player.position = new Vector2(0.5f, 0f); // now within stop distance (0.5 < 1)
            state.Tick(ctx, 0.1f);

            Assert.IsFalse(movement.HasDestination, "Within the stop distance the companion halts.");
        }

        [Test]
        public void Hysteresis_DeadBandKeepsTheCurrentDecision()
        {
            var ctx = Build(out CreatureMovement movement);
            Transform player = PlayerAt(ctx, new Vector2(1.5f, 0f)); // band
            var state = NewState();
            state.Enter(ctx);

            // Stopped + band -> stays stopped.
            state.Tick(ctx, 0.1f);
            Assert.IsFalse(movement.HasDestination, "band while stopped -> stays stopped");

            // Far -> starts following.
            player.position = new Vector2(5f, 0f);
            state.Tick(ctx, 0.1f);
            Assert.IsTrue(movement.HasDestination, "beyond max -> follows");

            // Back into the band while following -> keeps following (no flip-flop).
            player.position = new Vector2(1.5f, 0f);
            state.Tick(ctx, 0.1f);
            Assert.IsTrue(movement.HasDestination, "band while following -> keeps following");

            // Within stop distance -> stops.
            player.position = new Vector2(0.5f, 0f);
            state.Tick(ctx, 0.1f);
            Assert.IsFalse(movement.HasDestination, "within min -> stops");

            // Band again while stopped -> stays stopped.
            player.position = new Vector2(1.5f, 0f);
            state.Tick(ctx, 0.1f);
            Assert.IsFalse(movement.HasDestination, "band while stopped -> stays stopped (stable)");
        }

        [Test]
        public void FollowDistances_AreConfigurable_NotHardcoded()
        {
            // Same player distance (1.5), opposite decisions purely from the injected params.
            var ctxNear = Build(out CreatureMovement movementWide);
            PlayerAt(ctxNear, new Vector2(1.5f, 0f));
            var wide = new CreatureBondedState(5f, 3f); // follow only past 5 -> 1.5 stays stopped
            wide.Enter(ctxNear);
            wide.Tick(ctxNear, 0.1f);
            Assert.IsFalse(movementWide.HasDestination, "with a large follow distance, 1.5 is inside -> stopped");

            var ctxFar = Build(out CreatureMovement movementTight);
            PlayerAt(ctxFar, new Vector2(1.5f, 0f));
            var tight = new CreatureBondedState(1f, 0.5f); // follow past 1 -> 1.5 triggers following
            tight.Enter(ctxFar);
            tight.Tick(ctxFar, 0.1f);
            Assert.IsTrue(movementTight.HasDestination, "with a small follow distance, 1.5 is far -> follows");
        }

        [Test]
        public void Follow_UsesMovement_NeverMovesTransformDirectly()
        {
            var ctx = Build(out _);
            Vector3 rootBefore = ctx.Root.position;
            PlayerAt(ctx, new Vector2(5f, 0f));
            var state = NewState();
            state.Enter(ctx);
            state.Tick(ctx, 0.1f);
            Assert.AreEqual(rootBefore, ctx.Root.position, "the state must never move the Transform directly");
        }

        [Test]
        public void NoPlayer_Stops_AndStaysBonded()
        {
            var ctx = Build(out CreatureMovement movement);
            var state = NewState();
            state.Enter(ctx);
            movement.SetDestination(new Vector2(9f, 9f)); // a leftover destination
            Assert.IsNull(state.Tick(ctx, 0.1f), "no player -> no transition (stays Bonded)");
            Assert.IsFalse(movement.HasDestination, "no player -> stops");
        }

        [Test]
        public void NoMovement_DegradesSafely()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var rootGo = new GameObject("Root");
            temp.Add(rootGo);
            var ctx = new CreatureContext(id, rootGo.transform, new List<Transform>()); // no movement/sensor
            ctx.SetDetectedPlayer(CreatureTestKit.NewPoint(temp, new Vector2(5f, 0f)));
            var state = NewState();
            state.Enter(ctx);
            Assert.IsNull(state.Tick(ctx, 0.1f), "a null Movement must not throw and must not transition");
        }

        // ── Separation of responsibilities ──

        [Test]
        public void HasNoForbiddenDependencies()
        {
            foreach (FieldInfo f in typeof(CreatureBondedState).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string typeName = f.FieldType.Name;
                Assert.AreNotEqual("PlayerControlGate", typeName, "Bonded must not touch the gate: " + f.Name);
                Assert.AreNotEqual("Animator", typeName, "no Animator: " + f.Name);
                Assert.AreNotEqual("SpriteFlash", typeName, "no presentation: " + f.Name);
                Assert.AreNotEqual("SpriteRenderer", typeName, "no renderer: " + f.Name);
                Assert.AreNotEqual("Color", typeName, "no color: " + f.Name);
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.IsFalse(ns.Contains("UnityEngine.UI"), "no UI: " + f.Name);
            }
        }
    }
}

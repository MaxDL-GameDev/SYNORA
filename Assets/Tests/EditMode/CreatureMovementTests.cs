using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureMovementTests
    {
        private const float Tolerance = 0.0001f;
        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i] != null)
                {
                    Object.DestroyImmediate(temp[i]);
                }
            }
            temp.Clear();
        }

        // ─────────────────────────── Pure: ComputeVelocity ───────────────────────────

        [Test]
        public void ComputeVelocity_FarDestination_MovesAtMoveSpeedTowardTarget()
        {
            Vector2 v = CreatureMovement.ComputeVelocity(
                Vector2.zero, new Vector2(10f, 0f), 3f, 0.1f, 0.02f, out bool arrived);

            Assert.IsFalse(arrived);
            Assert.That(v.magnitude, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(v.x, Is.GreaterThan(0f));
            Assert.That(v.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void ComputeVelocity_ZeroMoveSpeed_ReturnsZero()
        {
            Vector2 v = CreatureMovement.ComputeVelocity(
                Vector2.zero, new Vector2(10f, 0f), 0f, 0.1f, 0.02f, out bool arrived);
            Assert.IsFalse(arrived);
            Assert.AreEqual(Vector2.zero, v);
        }

        [Test]
        public void ComputeVelocity_NonPositiveDeltaTime_ReturnsZero()
        {
            Vector2 zeroDt = CreatureMovement.ComputeVelocity(
                Vector2.zero, new Vector2(10f, 0f), 3f, 0.1f, 0f, out _);
            Vector2 negDt = CreatureMovement.ComputeVelocity(
                Vector2.zero, new Vector2(10f, 0f), 3f, 0.1f, -0.5f, out _);
            Assert.AreEqual(Vector2.zero, zeroDt);
            Assert.AreEqual(Vector2.zero, negDt);
        }

        [Test]
        public void ComputeVelocity_WithinArrivalThreshold_ArrivedAndZero()
        {
            Vector2 v = CreatureMovement.ComputeVelocity(
                Vector2.zero, new Vector2(0.05f, 0f), 3f, 0.1f, 0.02f, out bool arrived);
            Assert.IsTrue(arrived);
            Assert.AreEqual(Vector2.zero, v);
        }

        [Test]
        public void ComputeVelocity_LandingStep_NoOvershoot()
        {
            // maxStep (10 * 0.5 = 5) >= distance (1): land exactly, velocity * dt == toDest.
            Vector2 v = CreatureMovement.ComputeVelocity(
                Vector2.zero, new Vector2(1f, 0f), 10f, 0.01f, 0.5f, out bool arrived);
            Assert.IsFalse(arrived);
            Assert.That(v.x, Is.EqualTo(2f).Within(Tolerance)); // 1 / 0.5
            Assert.That((v * 0.5f).x, Is.EqualTo(1f).Within(Tolerance)); // lands on destination
            Assert.That(v.magnitude, Is.LessThan(10f)); // slower than full speed: no overshoot
        }

        [Test]
        public void ComputeVelocity_DirectionsAreCorrect()
        {
            float ms = 2f, dt = 0.02f, thr = 0.05f;
            Assert.That(CreatureMovement.ComputeVelocity(Vector2.zero, new Vector2(5f, 0f), ms, thr, dt, out _).x, Is.GreaterThan(0f));
            Assert.That(CreatureMovement.ComputeVelocity(Vector2.zero, new Vector2(-5f, 0f), ms, thr, dt, out _).x, Is.LessThan(0f));
            Assert.That(CreatureMovement.ComputeVelocity(Vector2.zero, new Vector2(0f, 5f), ms, thr, dt, out _).y, Is.GreaterThan(0f));
            Assert.That(CreatureMovement.ComputeVelocity(Vector2.zero, new Vector2(0f, -5f), ms, thr, dt, out _).y, Is.LessThan(0f));
        }

        // ─────────────────────────── Pure: ResolveFacing ───────────────────────────

        [Test]
        public void ResolveFacing_CardinalsAreCorrect()
        {
            Assert.AreEqual(Vector2Int.right, CreatureMovement.ResolveFacing(new Vector2(1f, 0.2f), Vector2Int.down));
            Assert.AreEqual(Vector2Int.left, CreatureMovement.ResolveFacing(new Vector2(-1f, 0.2f), Vector2Int.down));
            Assert.AreEqual(Vector2Int.up, CreatureMovement.ResolveFacing(new Vector2(0.2f, 1f), Vector2Int.down));
            Assert.AreEqual(Vector2Int.down, CreatureMovement.ResolveFacing(new Vector2(0.2f, -1f), Vector2Int.up));
        }

        [Test]
        public void ResolveFacing_ExactDiagonal_PrefersHorizontal()
        {
            Assert.AreEqual(Vector2Int.right, CreatureMovement.ResolveFacing(new Vector2(1f, 1f), Vector2Int.down));
            Assert.AreEqual(Vector2Int.left, CreatureMovement.ResolveFacing(new Vector2(-1f, 1f), Vector2Int.down));
        }

        [Test]
        public void ResolveFacing_NegligibleDirection_KeepsPrevious()
        {
            Assert.AreEqual(Vector2Int.up, CreatureMovement.ResolveFacing(Vector2.zero, Vector2Int.up));
        }

        // ─────────────────────────── MonoBehaviour: FixedTick ───────────────────────────

        private CreatureMovement NewMovement(out CreatureContext context, Vector2 startPos)
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var body = go.GetComponent<Rigidbody2D>(); // added by [RequireComponent]
            if (body == null) body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.position = startPos;
            go.transform.position = startPos;

            var move = go.AddComponent<CreatureMovement>();

            var identity = ScriptableObject.CreateInstance<CreatureIdentity>();
            temp.Add(identity);
            SetPrivate(identity, "moveSpeed", 3f);
            SetPrivate(identity, "arrivalThreshold", 0.1f);
            SetPrivate(identity, "detectionRadius", 3f);
            SetPrivate(identity, "loseRadius", 4f);

            SetPrivate(move, "body", body);
            SetPrivate(move, "identity", identity);

            context = new CreatureContext(identity, go.transform, new List<Transform>(), move);
            move.Initialize(context);
            return move;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Field not found: " + field);
            f.SetValue(target, value);
        }

        private static Rigidbody2D BodyOf(CreatureMovement move)
        {
            return (Rigidbody2D)typeof(CreatureMovement)
                .GetField("body", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(move);
        }

        [Test]
        public void FixedTick_NoDestination_ZeroVelocityNotMoving()
        {
            var move = NewMovement(out CreatureContext ctx, Vector2.zero);
            move.FixedTick(0.02f);
            Assert.IsFalse(move.IsMoving);
            Assert.IsFalse(ctx.IsMoving);
            Assert.AreEqual(Vector2.zero, BodyOf(move).linearVelocity);
        }

        [Test]
        public void SetDestination_SetsHasDestinationAndDestination()
        {
            var move = NewMovement(out _, Vector2.zero);
            move.SetDestination(new Vector2(4f, 0f));
            Assert.IsTrue(move.HasDestination);
            Assert.AreEqual(new Vector2(4f, 0f), move.Destination);
        }

        [Test]
        public void Stop_ClearsDestinationNotMovingPreservesFacing()
        {
            var move = NewMovement(out CreatureContext ctx, Vector2.zero);
            move.SetDestination(new Vector2(0f, 5f));
            move.FixedTick(0.02f);              // moving up -> facing up
            Assert.AreEqual(Vector2Int.up, ctx.Facing);

            move.Stop();
            Assert.IsFalse(move.HasDestination);
            Assert.IsFalse(ctx.IsMoving);
            Assert.AreEqual(Vector2Int.up, ctx.Facing); // preserved
            Assert.AreEqual(Vector2.zero, BodyOf(move).linearVelocity);
        }

        [Test]
        public void FixedTick_Moving_UpdatesContext_DoesNotWriteTransform()
        {
            var move = NewMovement(out CreatureContext ctx, Vector2.zero);
            Vector3 posBefore = move.transform.position;

            move.SetDestination(new Vector2(10f, 0f));
            move.FixedTick(0.02f);

            Assert.IsTrue(ctx.IsMoving);
            Assert.AreEqual(Vector2Int.right, ctx.Facing);
            Assert.That(BodyOf(move).linearVelocity.magnitude, Is.EqualTo(3f).Within(Tolerance));
            // Locomotion is via Rigidbody2D velocity, not Transform writes.
            Assert.AreEqual(posBefore, move.transform.position);
        }

        [Test]
        public void FixedTick_Arrived_StopsAndClears()
        {
            var move = NewMovement(out CreatureContext ctx, Vector2.zero);
            move.SetDestination(new Vector2(0.05f, 0f)); // within arrivalThreshold (0.1)
            move.FixedTick(0.02f);

            Assert.IsFalse(move.HasDestination);
            Assert.IsFalse(ctx.IsMoving);
            Assert.AreEqual(Vector2.zero, BodyOf(move).linearVelocity);
        }
    }
}

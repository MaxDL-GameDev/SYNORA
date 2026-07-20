using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureSensorTests
    {
        private const int PlayerLayer = 8; // "Player" in TagManager
        private const float Tolerance = 0.001f;
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

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Field not found: " + field);
            f.SetValue(target, value);
        }

        private static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "Method not found: " + method);
            m.Invoke(target, null);
        }

        private CreatureIdentity NewIdentity(float detection = 3f, float lose = 4f)
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            temp.Add(id);
            SetPrivate(id, "detectionRadius", detection);
            SetPrivate(id, "loseRadius", lose);
            return id;
        }

        private CreatureSensor NewSensor(out CreatureContext context, CreatureIdentity identity,
            int layerMask, bool invokeAwake = true)
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            go.transform.position = Vector3.zero;

            var sensor = go.AddComponent<CreatureSensor>();
            SetPrivate(sensor, "identity", identity);
            SetPrivate(sensor, "playerLayer", (LayerMask)layerMask);
            if (invokeAwake)
            {
                Invoke(sensor, "Awake");
            }

            context = new CreatureContext(identity, go.transform, new List<Transform>(), null, sensor);
            sensor.Initialize(context);
            return sensor;
        }

        private Transform NewPlayer(Vector2 position, int layer = PlayerLayer, bool withCollider = true,
            bool withRigidbody = true)
        {
            var go = new GameObject("Player") { layer = layer };
            temp.Add(go);
            go.transform.position = position;
            if (withRigidbody)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            if (withCollider)
            {
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.1f;
            }
            return go.transform;
        }

        private static void Sync() => Physics2D.SyncTransforms();

        // ─────────────────────────── Initialization ───────────────────────────

        [Test]
        public void Sense_WithoutContext_DoesNotThrow_NoDetection()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var sensor = go.AddComponent<CreatureSensor>();
            SetPrivate(sensor, "identity", NewIdentity());
            SetPrivate(sensor, "playerLayer", (LayerMask)(1 << PlayerLayer));
            Invoke(sensor, "Awake");
            // No Initialize -> context null.
            Assert.DoesNotThrow(() => sensor.Sense());
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void Sense_WithNullIdentity_DoesNotThrow()
        {
            var identity = NewIdentity();
            var sensor = NewSensor(out _, identity, 1 << PlayerLayer);
            SetPrivate(sensor, "identity", null);
            Assert.DoesNotThrow(() => sensor.Sense());
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void Awake_EmptyLayerMask_Warns()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var sensor = go.AddComponent<CreatureSensor>();
            SetPrivate(sensor, "identity", NewIdentity());
            SetPrivate(sensor, "playerLayer", (LayerMask)0);
            LogAssert.Expect(LogType.Warning, new Regex("playerLayer"));
            Invoke(sensor, "Awake");
        }

        [Test]
        public void Radii_ReflectIdentity()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 5f), 1 << PlayerLayer);
            Assert.That(sensor.DetectionRadius, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(sensor.LoseRadius, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Default_NoPlayer_SentinelDistance()
        {
            var sensor = NewSensor(out _, NewIdentity(), 1 << PlayerLayer);
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
            Assert.Less(sensor.PlayerDistanceSqr, 0f); // NoPlayer sentinel
        }

        // ─────────────────────────── Detection ───────────────────────────

        [Test]
        public void PlayerWithinDetection_Detected()
        {
            var sensor = NewSensor(out CreatureContext ctx, NewIdentity(3f, 4f), 1 << PlayerLayer);
            Transform player = NewPlayer(new Vector2(2f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);
            Assert.AreSame(player, ctx.DetectedPlayer);
            Assert.That(sensor.PlayerDistanceSqr, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void PlayerBetweenDetectionAndLose_Detected()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            NewPlayer(new Vector2(3.5f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);
            Assert.That(sensor.PlayerDistanceSqr, Is.EqualTo(12.25f).Within(Tolerance));
        }

        [Test]
        public void PlayerAtLoseRadius_Detected()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            NewPlayer(new Vector2(4f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);
            Assert.That(sensor.PlayerDistanceSqr, Is.EqualTo(16f).Within(0.2f));
        }

        [Test]
        public void PlayerBeyondLose_NotDetected()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            NewPlayer(new Vector2(6f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
            Assert.Less(sensor.PlayerDistanceSqr, 0f);
        }

        [Test]
        public void PlayerEntersThenLeaves_DetectionCleared()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            Transform player = NewPlayer(new Vector2(2f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);

            player.position = new Vector2(10f, 0f);
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void DestroyedPlayer_Cleared()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            Transform player = NewPlayer(new Vector2(2f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);

            Object.DestroyImmediate(player.gameObject);
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void DisabledCollider_Cleared()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            Transform player = NewPlayer(new Vector2(2f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);

            player.GetComponent<CircleCollider2D>().enabled = false;
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void WrongLayer_NotDetected()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            NewPlayer(new Vector2(2f, 0f), layer: 0); // Default layer, not Player
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void NoCollider_NotDetected()
        {
            var sensor = NewSensor(out _, NewIdentity(3f, 4f), 1 << PlayerLayer);
            NewPlayer(new Vector2(2f, 0f), withCollider: false);
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        [Test]
        public void OwnCreatureCollider_Ignored()
        {
            var identity = NewIdentity(3f, 4f);
            var sensor = NewSensor(out _, identity, 1 << PlayerLayer);
            // Put a Player-layer collider on the creature itself.
            sensor.gameObject.layer = PlayerLayer;
            var col = sensor.gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            Sync();
            sensor.Sense();
            Assert.IsFalse(sensor.HasDetectedPlayer);
        }

        // ─────────────────────────── Multiple colliders ───────────────────────────

        [Test]
        public void MultipleCollidersSamePlayer_ResolvesToStableRoot()
        {
            var sensor = NewSensor(out CreatureContext ctx, NewIdentity(3f, 4f), 1 << PlayerLayer);
            Transform player = NewPlayer(new Vector2(2f, 0f));
            // Second collider on the same body.
            var extra = player.gameObject.AddComponent<CircleCollider2D>();
            extra.radius = 0.2f;
            Sync();
            sensor.Sense();
            Assert.IsTrue(sensor.HasDetectedPlayer);
            Assert.AreSame(player, ctx.DetectedPlayer); // resolved to the body root, not a child collider
        }

        [Test]
        public void MultiplePlayers_NearestChosen()
        {
            var sensor = NewSensor(out CreatureContext ctx, NewIdentity(3f, 5f), 1 << PlayerLayer);
            NewPlayer(new Vector2(4f, 0f));
            Transform near = NewPlayer(new Vector2(1.5f, 0f));
            Sync();
            sensor.Sense();
            Assert.AreSame(near, ctx.DetectedPlayer);
            Assert.That(sensor.PlayerDistanceSqr, Is.EqualTo(2.25f).Within(Tolerance));
        }

        [Test]
        public void EqualDistanceCandidates_LowestEntityIdWins_Deterministic()
        {
            var sensor = NewSensor(out CreatureContext ctx, NewIdentity(3f, 4f), 1 << PlayerLayer);
            // Two Player roots exactly equidistant (both sqrDist == 4).
            Transform a = NewPlayer(new Vector2(2f, 0f));
            Transform b = NewPlayer(new Vector2(-2f, 0f));
            Transform expected = a.GetEntityId() < b.GetEntityId() ? a : b;
            Sync();

            // Repeated senses must not alternate the reference.
            for (int i = 0; i < 5; i++)
            {
                sensor.Sense();
                Assert.AreSame(expected, ctx.DetectedPlayer, "Tie-break must be deterministic across ticks.");
                Assert.That(sensor.PlayerDistanceSqr, Is.EqualTo(4f).Within(Tolerance));
            }
        }

        // ─────────────────────────── Context & lifecycle ───────────────────────────

        [Test]
        public void Detect_SetsContext_Lose_ClearsContext()
        {
            var sensor = NewSensor(out CreatureContext ctx, NewIdentity(3f, 4f), 1 << PlayerLayer);
            Transform player = NewPlayer(new Vector2(2f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsNotNull(ctx.DetectedPlayer);

            player.position = new Vector2(20f, 0f);
            Sync();
            sensor.Sense();
            Assert.IsNull(ctx.DetectedPlayer);
        }

        [Test]
        public void OnDisable_ClearsDetection()
        {
            var sensor = NewSensor(out CreatureContext ctx, NewIdentity(3f, 4f), 1 << PlayerLayer);
            NewPlayer(new Vector2(2f, 0f));
            Sync();
            sensor.Sense();
            Assert.IsNotNull(ctx.DetectedPlayer);

            Invoke(sensor, "OnDisable");
            Assert.IsNull(ctx.DetectedPlayer);
            Assert.Less(sensor.PlayerDistanceSqr, 0f);
        }
    }
}

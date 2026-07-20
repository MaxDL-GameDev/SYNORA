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
    /// <summary>Shared builders for creature state-machine EditMode tests.</summary>
    internal static class CreatureTestKit
    {
        public const int PlayerLayer = 8;

        public static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Field not found: " + field + " on " + target.GetType().Name);
            f.SetValue(target, value);
        }

        public static object GetPrivate(object target, string field)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Field not found: " + field);
            return f.GetValue(target);
        }

        public static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "Method not found: " + method);
            m.Invoke(target, null);
        }

        public static CreatureIdentity NewIdentity(List<Object> temp, float detection = 3f, float lose = 4f,
            float idle = 2f, float linger = 1.5f, float moveSpeed = 3f, float arrival = 0.1f)
        {
            var id = ScriptableObject.CreateInstance<CreatureIdentity>();
            temp.Add(id);
            SetPrivate(id, "detectionRadius", detection);
            SetPrivate(id, "loseRadius", lose);
            SetPrivate(id, "idleDuration", idle);
            SetPrivate(id, "alertLingerDuration", linger);
            SetPrivate(id, "moveSpeed", moveSpeed);
            SetPrivate(id, "arrivalThreshold", arrival);
            return id;
        }

        public static Transform NewPoint(List<Object> temp, Vector2 pos)
        {
            var go = new GameObject("Point");
            temp.Add(go);
            go.transform.position = pos;
            return go.transform;
        }

        /// <summary>Creates a creature GameObject with movement+sensor+rigidbody and a wired context.</summary>
        public static CreatureContext BuildContext(List<Object> temp, CreatureIdentity identity,
            IReadOnlyList<Transform> points, out CreatureMovement movement, out CreatureSensor sensor, out Transform root)
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            root = go.transform;

            var body = go.GetComponent<Rigidbody2D>();
            if (body == null) body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            movement = go.AddComponent<CreatureMovement>();
            SetPrivate(movement, "body", body);
            SetPrivate(movement, "identity", identity);

            sensor = go.AddComponent<CreatureSensor>();
            SetPrivate(sensor, "identity", identity);
            SetPrivate(sensor, "playerLayer", (LayerMask)(1 << PlayerLayer));
            Invoke(sensor, "Awake");

            var ctx = new CreatureContext(identity, root, points, movement, sensor);
            movement.Initialize(ctx);
            sensor.Initialize(ctx);
            return ctx;
        }

        public static CreatureBrain BuildBrain(List<Object> temp, CreatureIdentity identity, Transform[] points,
            out CreatureMovement movement, out CreatureSensor sensor)
        {
            var go = new GameObject("Creature");
            temp.Add(go);

            var body = go.GetComponent<Rigidbody2D>();
            if (body == null) body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            movement = go.AddComponent<CreatureMovement>();
            SetPrivate(movement, "body", body);
            SetPrivate(movement, "identity", identity);

            sensor = go.AddComponent<CreatureSensor>();
            SetPrivate(sensor, "identity", identity);
            SetPrivate(sensor, "playerLayer", (LayerMask)(1 << PlayerLayer));
            Invoke(sensor, "Awake");

            var brain = go.AddComponent<CreatureBrain>();
            SetPrivate(brain, "identity", identity);
            SetPrivate(brain, "movement", movement);
            SetPrivate(brain, "sensor", sensor);
            SetPrivate(brain, "root", go.transform);
            SetPrivate(brain, "patrolPoints", points ?? new Transform[0]);
            return brain;
        }

        public static void InjectDistance(CreatureSensor sensor, float distanceSqr)
            => SetPrivate(sensor, "playerDistanceSqr", distanceSqr);
    }

    public sealed class CreatureBrainTests
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

        private Transform[] OnePoint() => new[] { CreatureTestKit.NewPoint(temp, new Vector2(3f, 0f)) };

        [Test]
        public void Initialize_Valid_StartsInIdle()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            Assert.IsTrue(brain.IsInitialized);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);
        }

        [Test]
        public void Initialize_Twice_DoesNotRecreateStates()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            object statesBefore = CreatureTestKit.GetPrivate(brain, "states");
            brain.Initialize();
            object statesAfter = CreatureTestKit.GetPrivate(brain, "states");
            Assert.AreSame(statesBefore, statesAfter, "Second Initialize must not rebuild state instances.");
        }

        [Test]
        public void Tick_BeforeInitialize_NoOp()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            Assert.DoesNotThrow(() => brain.Tick(1f));
            Assert.IsFalse(brain.IsInitialized);
        }

        [Test]
        public void CurrentStateId_SyncsWithContext()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            var ctx = (CreatureContext)CreatureTestKit.GetPrivate(brain, "context");
            Assert.AreEqual(ctx.CurrentState, brain.CurrentStateId);
        }

        [Test]
        public void Transition_IdleToPatrol_AfterIdleDuration()
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: 0.5f);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            brain.Tick(1f); // no player, timer elapsed -> Patrol
            Assert.AreEqual(CreatureStateId.Patrol, brain.CurrentStateId);
        }

        [Test]
        public void Transition_PatrolToAlert_WhenPlayerDetected()
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: 0.5f);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out CreatureSensor sensor);
            brain.Initialize();
            brain.Tick(1f); // -> Patrol
            Assert.AreEqual(CreatureStateId.Patrol, brain.CurrentStateId);

            CreatureTestKit.InjectDistance(sensor, 1f); // within detection (3^2 = 9)
            brain.Tick(0.02f);
            Assert.AreEqual(CreatureStateId.Alert, brain.CurrentStateId);
        }

        [Test]
        public void Transition_AlertToPatrol_WhenPlayerLostAndLingerElapsed()
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: 0.5f, linger: 0.5f);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out CreatureSensor sensor);
            brain.Initialize();
            CreatureTestKit.InjectDistance(sensor, 1f); // within detection
            brain.Tick(0.02f); // Idle -> Alert
            Assert.AreEqual(CreatureStateId.Alert, brain.CurrentStateId);

            CreatureTestKit.InjectDistance(sensor, 100f); // far beyond lose (4^2 = 16)
            brain.Tick(1f); // linger elapses -> Patrol
            Assert.AreEqual(CreatureStateId.Patrol, brain.CurrentStateId);
        }

        [Test]
        public void SameStateRequested_StaysStable()
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: 100f); // never leaves Idle by timer
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            brain.Tick(0.1f);
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);
        }

        [Test]
        public void RequestTransition_UnknownState_IsIgnored()
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: 100f);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            LogAssert.Expect(LogType.Warning, new Regex("unknown state"));
            Assert.DoesNotThrow(() => brain.RequestTransition((CreatureStateId)999));
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);
        }

        [Test]
        public void Initialize_NullMovement_NotInitialized()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            CreatureTestKit.SetPrivate(brain, "movement", null);
            LogAssert.Expect(LogType.Warning, new Regex("CreatureMovement"));
            brain.Initialize();
            Assert.IsFalse(brain.IsInitialized);
        }

        [Test]
        public void OnDisable_StopsMovement()
        {
            var id = CreatureTestKit.NewIdentity(temp, idle: 0.5f);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out CreatureMovement movement, out _);
            brain.Initialize();
            brain.Tick(1f); // -> Patrol, sets a destination
            Assert.IsTrue(movement.HasDestination);
            CreatureTestKit.Invoke(brain, "OnDisable");
            Assert.IsFalse(movement.HasDestination);
        }

        [Test]
        public void Brain_HasNoFixedUpdate()
        {
            MethodInfo fu = typeof(CreatureBrain).GetMethod("FixedUpdate",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNull(fu, "CreatureBrain must not own a FixedUpdate (Movement/Sensor own their physics tick).");
        }

        [Test]
        public void Brain_ReusesThreeDistinctStateInstances()
        {
            var id = CreatureTestKit.NewIdentity(temp);
            var brain = CreatureTestKit.BuildBrain(temp, id, OnePoint(), out _, out _);
            brain.Initialize();
            var states = (Dictionary<CreatureStateId, ICreatureState>)CreatureTestKit.GetPrivate(brain, "states");
            Assert.AreEqual(3, states.Count);
            Assert.IsNotNull(states[CreatureStateId.Idle]);
            Assert.IsNotNull(states[CreatureStateId.Patrol]);
            Assert.IsNotNull(states[CreatureStateId.Alert]);
        }
    }
}

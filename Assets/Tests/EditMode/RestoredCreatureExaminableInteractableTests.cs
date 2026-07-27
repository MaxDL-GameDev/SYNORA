using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Interaction;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// M6 F5: the restored-creature examination is available only while the Altered Verak
    /// is Restored; executing it delivers a restored-specific ExaminableData through the
    /// normal receiver, without changing the creature's state, requesting a transition, or
    /// touching restoration. It is mutually exclusive with the restoration interactable
    /// (only in Subdued) — exclusion comes from each CanInteract. Built on the real
    /// CreatureBrain + AlteredVerakSetup provider; no scene, prefab, Animator, UI or gate.
    /// </summary>
    public sealed class RestoredCreatureExaminableInteractableTests
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

        private sealed class FakeReceiver : IInteractionReceiver
        {
            public int ShowCount;
            public ExaminableData LastData;
            public void ShowObservation(ExaminableData data) { ShowCount++; LastData = data; }
        }

        private ExaminableData NewRestoredData(string id = "creature.restored")
        {
            var d = ScriptableObject.CreateInstance<ExaminableData>();
            temp.Add(d);
            CreatureTestKit.SetPrivate(d, "interactionId", id);
            CreatureTestKit.SetPrivate(d, "observationTitle", "Restaurado (placeholder)");
            CreatureTestKit.SetPrivate(d, "observationBody", "Placeholder restored observation.");
            return d;
        }

        // Builds a real Altered Verak brain (all states safe to enter) so state can be
        // driven to any CreatureStateId, plus the restored-examine interactable wired to it.
        private CreatureBrain BuildBrain()
        {
            var brain = CreatureTestKit.BuildBrain(temp, CreatureTestKit.NewIdentity(temp), null, out _, out _);

            var resolver = brain.gameObject.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)(1 << 20));
            CreatureTestKit.Invoke(resolver, "Awake");
            var controller = brain.gameObject.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(controller, "resolver", resolver);
            var health = brain.gameObject.AddComponent<Health>();
            CreatureTestKit.SetPrivate(health, "maxHealth", 3f);
            health.ResetHealth();

            var setup = brain.gameObject.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "brain", brain);
            CreatureTestKit.SetPrivate(setup, "health", health);
            CreatureTestKit.SetPrivate(setup, "attackController", controller);
            CreatureTestKit.SetPrivate(setup, "restorationDuration", 0.2f);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();
            return brain;
        }

        private RestoredCreatureExaminableInteractable NewExaminable(
            out CreatureBrain brain, out ExaminableData data, CreatureBrain existing = null)
        {
            brain = existing != null ? existing : BuildBrain();
            data = NewRestoredData();

            var go = new GameObject("RestoredExamine");
            temp.Add(go);
            var it = go.AddComponent<RestoredCreatureExaminableInteractable>();
            CreatureTestKit.SetPrivate(it, "brain", brain);
            CreatureTestKit.SetPrivate(it, "restoredData", data);
            return it;
        }

        private CreatureRestorationInteractable NewRestoration(CreatureBrain brain)
        {
            var gateGo = new GameObject("Gate");
            temp.Add(gateGo);
            var gate = gateGo.AddComponent<PlayerControlGate>(); // left unblocked

            var go = new GameObject("Restoration");
            temp.Add(go);
            var it = go.AddComponent<CreatureRestorationInteractable>();
            CreatureTestKit.SetPrivate(it, "brain", brain);
            CreatureTestKit.SetPrivate(it, "gate", gate);
            return it;
        }

        // Applies an external transition and lets the Brain settle it. Restored is
        // registered, so it is reachable directly (RequestTransition has no source guard).
        private static void Drive(CreatureBrain brain, CreatureStateId state)
        {
            brain.RequestTransition(state);
            brain.Tick(0.0001f);
        }

        // ── Availability by state ──

        [TestCase(CreatureStateId.Idle, false)]
        [TestCase(CreatureStateId.Patrol, false)]
        [TestCase(CreatureStateId.Alert, false)]
        [TestCase(CreatureStateId.Chase, false)]
        [TestCase(CreatureStateId.Attack, false)]
        [TestCase(CreatureStateId.Subdued, false)]
        [TestCase(CreatureStateId.Restoring, false)]
        [TestCase(CreatureStateId.Restored, true)]
        public void Available_OnlyInRestored(CreatureStateId state, bool expected)
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            Drive(brain, state);
            Assert.AreEqual(state, brain.CurrentStateId, "precondition: brain in " + state);
            Assert.AreEqual(expected, it.CanInteract);
        }

        // ── Contract ──

        [Test]
        public void ImplementsIInteractable_WithConsistentContract()
        {
            var it = NewExaminable(out _, out _);
            it.transform.position = new Vector3(2f, 3f, 0f);
            Assert.IsInstanceOf<IInteractable>(it);
            Assert.AreEqual("Examinar", it.PromptText);
            Assert.AreEqual("creature.restored", it.InteractionId);
            Assert.AreEqual(new Vector2(2f, 3f), it.InteractionPosition);
        }

        [Test]
        public void NullData_CanInteractFalse()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            CreatureTestKit.SetPrivate(it, "restoredData", (ExaminableData)null);
            Drive(brain, CreatureStateId.Restored);
            Assert.IsFalse(it.CanInteract);
        }

        [Test]
        public void InvalidDataId_CanInteractFalse()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            CreatureTestKit.SetPrivate(it, "restoredData", NewRestoredData("")); // empty id
            Drive(brain, CreatureStateId.Restored);
            Assert.IsFalse(it.CanInteract);
        }

        // ── Execution ──

        [Test]
        public void Execute_FromRestored_ShowsRestoredData_Once()
        {
            var it = NewExaminable(out CreatureBrain brain, out ExaminableData data);
            Drive(brain, CreatureStateId.Restored);
            var receiver = new FakeReceiver();
            it.Execute(receiver);
            Assert.AreEqual(1, receiver.ShowCount);
            Assert.AreSame(data, receiver.LastData);
        }

        [Test]
        public void Execute_FromRestored_DoesNotChangeStateNorRequestTransition()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Restored);
            it.Execute(new FakeReceiver());
            brain.Tick(0.5f);
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId);
        }

        [Test]
        public void Execute_FromNonRestored_DoesNothing()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Subdued);
            var receiver = new FakeReceiver();
            it.Execute(receiver);
            brain.Tick(0.001f);
            Assert.AreEqual(0, receiver.ShowCount, "no observation outside Restored");
            Assert.AreEqual(CreatureStateId.Subdued, brain.CurrentStateId);
        }

        [Test]
        public void Execute_NullReceiver_DoesNotThrow()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Restored);
            Assert.DoesNotThrow(() => it.Execute(null));
        }

        // ── Revalidation ──

        [Test]
        public void Execute_RevalidatesWhenStateLeavesRestored()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Restored);
            Assert.IsTrue(it.CanInteract);
            // Force the Brain out of Restored before executing (no source guard).
            Drive(brain, CreatureStateId.Idle);
            var receiver = new FakeReceiver();
            it.Execute(receiver);
            Assert.AreEqual(0, receiver.ShowCount, "Execute must re-check the live Brain state.");
        }

        [Test]
        public void Execute_WhenInteractionDisabled_DoesNothing()
        {
            var it = NewExaminable(out CreatureBrain brain, out _);
            CreatureTestKit.SetPrivate(it, "interactionEnabled", false);
            Drive(brain, CreatureStateId.Restored);
            var receiver = new FakeReceiver();
            it.Execute(receiver);
            Assert.AreEqual(0, receiver.ShowCount);
        }

        // ── Mutual exclusion with the restoration interactable (same brain) ──

        [Test]
        public void MutualExclusion_Subdued_OnlyRestorationAvailable()
        {
            var restored = NewExaminable(out CreatureBrain brain, out _);
            var restoration = NewRestoration(brain);
            Drive(brain, CreatureStateId.Subdued);
            Assert.IsTrue(restoration.CanInteract, "restoration available in Subdued");
            Assert.IsFalse(restored.CanInteract, "restored-examine not available in Subdued");
        }

        [Test]
        public void MutualExclusion_Restoring_NeitherAvailable()
        {
            var restored = NewExaminable(out CreatureBrain brain, out _);
            var restoration = NewRestoration(brain);
            Drive(brain, CreatureStateId.Restoring);
            Assert.IsFalse(restoration.CanInteract, "restoration not available in Restoring");
            Assert.IsFalse(restored.CanInteract, "restored-examine not available in Restoring");
        }

        [Test]
        public void MutualExclusion_Restored_OnlyRestoredExamineAvailable()
        {
            var restored = NewExaminable(out CreatureBrain brain, out _);
            var restoration = NewRestoration(brain);
            Drive(brain, CreatureStateId.Restored);
            Assert.IsFalse(restoration.CanInteract, "restoration not available in Restored");
            Assert.IsTrue(restored.CanInteract, "restored-examine available in Restored");
        }

        // ── Architecture / dependencies ──

        [Test]
        public void HasNoForbiddenDependencies()
        {
            Assert.IsNull(typeof(RestoredCreatureExaminableInteractable).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                "no per-frame Update.");

            foreach (FieldInfo f in typeof(RestoredCreatureExaminableInteractable).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                Assert.AreNotEqual(typeof(PlayerControlGate), f.FieldType,
                    "examination is passive: no PlayerControlGate: " + f.Name);
                Assert.AreNotEqual(typeof(InteractionController), f.FieldType,
                    "no InteractionController reference: " + f.Name);
                Assert.AreNotEqual("Animator", f.FieldType.Name, "no Animator dependency: " + f.Name);
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.IsFalse(ns.Contains("UnityEngine.UI"), "no UI dependency: " + f.Name);
            }
        }
    }
}

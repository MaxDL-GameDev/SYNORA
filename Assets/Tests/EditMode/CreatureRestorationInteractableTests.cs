using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Interaction;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// M6 F4: the restoration interactable is available only while the Altered Verak is
    /// Subdued and the player can act (PlayerControlGate not blocked); executing it asks
    /// the Brain to transition to Restoring (never Restored, never touching the timer),
    /// then it stops being available. Built on the real CreatureBrain + AlteredVerakSetup
    /// provider; no scene, prefab, Animator or presentation.
    /// </summary>
    public sealed class CreatureRestorationInteractableTests
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

        private CreatureRestorationInteractable NewInteractable(out CreatureBrain brain, out PlayerControlGate gate)
        {
            brain = CreatureTestKit.BuildBrain(temp, CreatureTestKit.NewIdentity(temp), null, out _, out _);

            // Full altered wiring so any state's Enter is safe (Chase/Attack reference these).
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

            var gateGo = new GameObject("Gate");
            temp.Add(gateGo);
            gate = gateGo.AddComponent<PlayerControlGate>();

            var go = new GameObject("Restoration");
            temp.Add(go);
            var it = go.AddComponent<CreatureRestorationInteractable>();
            CreatureTestKit.SetPrivate(it, "brain", brain);
            CreatureTestKit.SetPrivate(it, "gate", gate);
            return it;
        }

        // Applies an external transition and lets the Brain settle it (without ticking the
        // destination state's logic afterward).
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
        [TestCase(CreatureStateId.Subdued, true)]
        [TestCase(CreatureStateId.Restoring, false)]
        [TestCase(CreatureStateId.Restored, false)]
        public void Available_OnlyInSubdued(CreatureStateId state, bool expected)
        {
            var it = NewInteractable(out CreatureBrain brain, out _);
            Drive(brain, state);
            Assert.AreEqual(state, brain.CurrentStateId, "precondition: brain in " + state);
            Assert.AreEqual(expected, it.CanInteract);
        }

        // ── Player capability (reused PlayerControlGate) ──

        [Test]
        public void Available_InSubdued_WhenGateFree()
        {
            var it = NewInteractable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Subdued);
            Assert.IsTrue(it.CanInteract);
        }

        [Test]
        public void Unavailable_WhenGateBlockedByDefeat()
        {
            var it = NewInteractable(out CreatureBrain brain, out PlayerControlGate gate);
            Drive(brain, CreatureStateId.Subdued);
            gate.Block(ControlBlockReason.Defeat);
            Assert.IsFalse(it.CanInteract, "A player who cannot act must not restore.");
        }

        [Test]
        public void Execute_WhenGateBlocked_DoesNotRequestTransition()
        {
            var it = NewInteractable(out CreatureBrain brain, out PlayerControlGate gate);
            Drive(brain, CreatureStateId.Subdued);
            gate.Block(ControlBlockReason.Defeat);
            it.Execute(null);
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Subdued, brain.CurrentStateId);
        }

        // ── Execution ──

        [Test]
        public void Execute_FromSubdued_RequestsRestoring_ViaBrain()
        {
            var it = NewInteractable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Subdued);
            it.Execute(null);
            Assert.AreEqual(CreatureStateId.Subdued, brain.CurrentStateId, "request is pending until the Brain applies it");
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);
        }

        [Test]
        public void Execute_DoesNotJumpToRestored_BrainTimerCompletesIt()
        {
            var it = NewInteractable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Subdued);
            it.Execute(null);
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId, "must not skip to Restored");
            // The Brain (via CreatureRestoringState's own timer) completes it, not the interactable.
            brain.Tick(0.2f);
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId);
        }

        [Test]
        public void Execute_FromNonSubdued_DoesNotChangeState()
        {
            var it = NewInteractable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Patrol);
            it.Execute(null);
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Patrol, brain.CurrentStateId, "restoration only from Subdued");
        }

        // ── Repetition / stability ──

        [Test]
        public void UnavailableAfterEnteringRestoring_SecondExecuteDoesNothing()
        {
            var it = NewInteractable(out CreatureBrain brain, out _);
            Drive(brain, CreatureStateId.Subdued);
            it.Execute(null);
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);
            Assert.IsFalse(it.CanInteract, "not available during Restoring");

            it.Execute(null); // second attempt must not restart or alter
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);
        }

        // ── Dependencies ──

        [Test]
        public void ImplementsIInteractable_WithoutForbiddenDependencies()
        {
            var it = NewInteractable(out _, out _);
            Assert.IsInstanceOf<IInteractable>(it);

            Assert.IsNull(typeof(CreatureRestorationInteractable).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                "no per-frame Update.");

            foreach (FieldInfo f in typeof(CreatureRestorationInteractable).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                Assert.AreNotEqual("Animator", f.FieldType.Name, "no Animator dependency: " + f.Name);
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.IsFalse(ns.Contains("UnityEngine.UI"), "no UI dependency: " + f.Name);
            }
        }
    }
}

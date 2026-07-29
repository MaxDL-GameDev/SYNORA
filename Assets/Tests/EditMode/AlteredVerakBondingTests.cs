using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F1 integration: the Altered Verak provider registers Bonding/Bonded, and the
    /// real CreatureBrain accepts an external Restored → Bonding request and drives
    /// Bonding → Bonded to completion via its normal transition mechanism — the same
    /// pattern proven for M6 restoration (AlteredVerakRestorationTests). Restored remains
    /// terminal for its own logic: it only leaves via an external request. No interaction,
    /// following, Animator, prefab or scene involved (those are F2–F6). Per-state behavior
    /// is covered by CreatureBondingStateTests / CreatureBondedStateTests.
    /// </summary>
    public sealed class AlteredVerakBondingTests
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

        // Builds an initialized Altered Verak brain with short restoration/bonding timers.
        private CreatureBrain NewAlteredBrain(float restorationDuration, float bondingDuration,
            out CreatureSensor sensor)
        {
            var brain = CreatureTestKit.BuildBrain(temp, CreatureTestKit.NewIdentity(temp), null, out _, out sensor);
            var setup = brain.gameObject.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "brain", brain);
            CreatureTestKit.SetPrivate(setup, "restorationDuration", restorationDuration);
            CreatureTestKit.SetPrivate(setup, "bondingDuration", bondingDuration);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();
            return brain;
        }

        // Drives the brain to Restored via the canonical M5/M6 path (external requests + timer).
        private CreatureBrain BrainInRestored(float bondingDuration, out CreatureSensor sensor)
        {
            CreatureBrain brain = NewAlteredBrain(0.2f, bondingDuration, out sensor);
            brain.RequestTransition(CreatureStateId.Subdued);
            brain.Tick(0.01f);
            brain.RequestTransition(CreatureStateId.Restoring);
            brain.Tick(0.01f);
            brain.Tick(0.2f); // restoration completes
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId);
            return brain;
        }

        [Test]
        public void Provider_RegistersBondingAndBonded_WithCorrectTypes()
        {
            var go = new GameObject("Setup");
            temp.Add(go);
            var setup = go.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "restorationDuration", 1.0f);
            CreatureTestKit.SetPrivate(setup, "bondingDuration", 1.0f);

            var id = CreatureTestKit.NewIdentity(temp);
            var rootGo = new GameObject("Root");
            temp.Add(rootGo);
            var ctx = new CreatureContext(id, rootGo.transform, new List<Transform>());

            IReadOnlyDictionary<CreatureStateId, ICreatureState> states = setup.BuildStates(ctx);

            Assert.IsTrue(states.ContainsKey(CreatureStateId.Bonding));
            Assert.IsInstanceOf<CreatureBondingState>(states[CreatureStateId.Bonding]);
            Assert.IsTrue(states.ContainsKey(CreatureStateId.Bonded));
            Assert.IsInstanceOf<CreatureBondedState>(states[CreatureStateId.Bonded]);

            // The whole set is preserved and additive: M3 + M5 + M6 + M7 = 10 states.
            Assert.AreEqual(10, states.Count);
        }

        [Test]
        public void Provider_BuildsBonded_WithConfiguredFollowDistances()
        {
            var go = new GameObject("Setup");
            temp.Add(go);
            var setup = go.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "followDistance", 3f);
            CreatureTestKit.SetPrivate(setup, "followStopDistance", 1.5f);

            var id = CreatureTestKit.NewIdentity(temp);
            var rootGo = new GameObject("Root");
            temp.Add(rootGo);
            var ctx = new CreatureContext(id, rootGo.transform, new List<Transform>());

            IReadOnlyDictionary<CreatureStateId, ICreatureState> states = setup.BuildStates(ctx);
            var bonded = (CreatureBondedState)states[CreatureStateId.Bonded];

            // The state stores squared thresholds; they must come from the setup, not hardcoded.
            float followSqr = (float)CreatureTestKit.GetPrivate(bonded, "followDistanceSqr");
            float stopSqr = (float)CreatureTestKit.GetPrivate(bonded, "followStopDistanceSqr");
            Assert.AreEqual(9f, followSqr, 1e-4f, "followDistance (3) must flow from the setup");
            Assert.AreEqual(2.25f, stopSqr, 1e-4f, "followStopDistance (1.5) must flow from the setup");
        }

        [Test]
        public void Brain_AcceptsExternalBondingRequest_FromRestored()
        {
            CreatureBrain brain = BrainInRestored(0.2f, out _);

            brain.RequestTransition(CreatureStateId.Bonding);
            brain.Tick(0.01f);
            Assert.AreEqual(CreatureStateId.Bonding, brain.CurrentStateId);
        }

        [Test]
        public void Brain_BondingCompletesToBonded_ThenStays()
        {
            CreatureBrain brain = BrainInRestored(0.2f, out _);
            brain.RequestTransition(CreatureStateId.Bonding);
            brain.Tick(0.01f);
            Assert.AreEqual(CreatureStateId.Bonding, brain.CurrentStateId);

            // Not yet complete before the duration.
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Bonding, brain.CurrentStateId);

            // Completes to Bonded via the Brain's normal transition mechanism.
            brain.Tick(0.2f);
            Assert.AreEqual(CreatureStateId.Bonded, brain.CurrentStateId);

            // Stable: stays Bonded across further ticks.
            brain.Tick(1f);
            brain.Tick(1f);
            Assert.AreEqual(CreatureStateId.Bonded, brain.CurrentStateId);
        }

        [Test]
        public void Bonding_DoesNotComplete_WithoutTimeElapsing()
        {
            CreatureBrain brain = BrainInRestored(0.5f, out _);
            brain.RequestTransition(CreatureStateId.Bonding);
            brain.Tick(0.01f);

            // Many zero/negative ticks never complete the process: only its timer can.
            for (int i = 0; i < 20; i++)
            {
                brain.Tick(0f);
                brain.Tick(-1f);
            }
            Assert.AreEqual(CreatureStateId.Bonding, brain.CurrentStateId,
                "Bonding must not complete without its timer accumulating real time.");
        }

        [Test]
        public void Restored_DoesNotLeaveOnItsOwn_EvenWithPlayerNear()
        {
            CreatureBrain brain = BrainInRestored(0.2f, out CreatureSensor sensor);

            // A player right on top of the creature must not pull Restored out of its state.
            CreatureTestKit.InjectDistance(sensor, 0.01f);
            for (int i = 0; i < 50; i++)
            {
                brain.Tick(0.1f);
            }
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId,
                "Restored is terminal for its own logic: no Tick/IA/proximity may leave it; only an external request can.");
        }
    }
}

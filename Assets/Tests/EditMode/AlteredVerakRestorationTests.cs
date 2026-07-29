using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    /// <summary>
    /// M6 F3 integration: the Altered Verak provider registers Restoring/Restored, and
    /// the real CreatureBrain accepts an external Subdued → Restoring request and drives
    /// Restoring → Restored to completion via its normal transition mechanism. No
    /// interaction, Animator, prefab or scene involved. The per-state behavior is covered
    /// by CreatureRestoringStateTests / CreatureRestoredStateTests.
    /// </summary>
    public sealed class AlteredVerakRestorationTests
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

        private CreatureBrain NewAlteredBrain(float restorationDuration)
        {
            var brain = CreatureTestKit.BuildBrain(temp, CreatureTestKit.NewIdentity(temp), null, out _, out _);
            var setup = brain.gameObject.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "brain", brain);
            CreatureTestKit.SetPrivate(setup, "restorationDuration", restorationDuration);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();
            return brain;
        }

        [Test]
        public void Provider_RegistersRestoringAndRestored_WithCorrectTypes()
        {
            var go = new GameObject("Setup");
            temp.Add(go);
            var setup = go.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "restorationDuration", 1.0f);

            var id = CreatureTestKit.NewIdentity(temp);
            var rootGo = new GameObject("Root");
            temp.Add(rootGo);
            var ctx = new CreatureContext(id, rootGo.transform, new List<Transform>());

            IReadOnlyDictionary<CreatureStateId, ICreatureState> states = setup.BuildStates(ctx);

            Assert.IsTrue(states.ContainsKey(CreatureStateId.Restoring));
            Assert.IsInstanceOf<CreatureRestoringState>(states[CreatureStateId.Restoring]);
            Assert.IsTrue(states.ContainsKey(CreatureStateId.Restored));
            Assert.IsInstanceOf<CreatureRestoredState>(states[CreatureStateId.Restored]);

            // Existing hostile set is preserved.
            foreach (var s in new[] { CreatureStateId.Idle, CreatureStateId.Patrol, CreatureStateId.Alert,
                CreatureStateId.Chase, CreatureStateId.Attack, CreatureStateId.Subdued })
            {
                Assert.IsTrue(states.ContainsKey(s), "missing existing state: " + s);
            }
            // M7 F1 adds Bonding/Bonded to the same provider (count 8 → 10).
            Assert.AreEqual(10, states.Count);
        }

        [Test]
        public void Brain_AcceptsExternalRestoringRequest_FromSubdued()
        {
            CreatureBrain brain = NewAlteredBrain(0.2f);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);

            // Reach Subdued via an external request (precondition; the real trigger is health depletion elsewhere).
            brain.RequestTransition(CreatureStateId.Subdued);
            brain.Tick(0.01f);
            Assert.AreEqual(CreatureStateId.Subdued, brain.CurrentStateId);

            // External restoration request is accepted (Restoring is registered).
            brain.RequestTransition(CreatureStateId.Restoring);
            brain.Tick(0.01f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);
        }

        [Test]
        public void Brain_RestoringCompletesToRestored_ThenStays()
        {
            CreatureBrain brain = NewAlteredBrain(0.2f);
            brain.RequestTransition(CreatureStateId.Subdued);
            brain.Tick(0.01f);
            brain.RequestTransition(CreatureStateId.Restoring);
            brain.Tick(0.01f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);

            // Not yet complete before the duration.
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);

            // Completes to Restored via the Brain's normal transition mechanism.
            brain.Tick(0.2f);
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId);

            // Terminal: stays Restored across further ticks.
            brain.Tick(1f);
            brain.Tick(1f);
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId);
        }
    }
}

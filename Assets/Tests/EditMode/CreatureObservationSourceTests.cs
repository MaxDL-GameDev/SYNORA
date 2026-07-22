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
    public sealed class CreatureObservationSourceTests
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

        private CreatureObservationSource NewSource(CreatureBrain brain, CreatureIdentity id, string goName)
        {
            var go = new GameObject(goName);
            temp.Add(go);
            var src = go.AddComponent<CreatureObservationSource>();
            if (brain != null) CreatureTestKit.SetPrivate(src, "brain", brain);
            if (id != null) CreatureTestKit.SetPrivate(src, "identity", id);
            return src;
        }

        private CreatureIdentity NamedIdentity(string displayName)
        {
            var id = CreatureTestKit.NewIdentity(temp);
            CreatureTestKit.SetPrivate(id, "displayName", displayName);
            return id;
        }

        [Test]
        public void DisplayName_ComesFromIdentity()
        {
            var id = NamedIdentity("Verak");
            var src = NewSource(null, id, "Verak_A");
            Assert.AreEqual("Verak", src.DisplayName);
        }

        [Test]
        public void DisplayName_EmptyIdentityName_FallsBackToGameObjectName()
        {
            var id = NamedIdentity("   "); // whitespace only
            var src = NewSource(null, id, "Verak_A");
            Assert.AreEqual("Verak_A", src.DisplayName);
        }

        [Test]
        public void DisplayName_NullIdentity_FallsBackToGameObjectName()
        {
            var src = NewSource(null, null, "Verak_B");
            Assert.AreEqual("Verak_B", src.DisplayName);
        }

        [Test]
        public void NullBrain_ReportsUnknown_WithoutException()
        {
            var src = NewSource(null, NamedIdentity("Verak"), "Verak_A");
            LogAssert.Expect(LogType.Warning, new Regex("CreatureBrain reference is not assigned"));
            Assert.AreEqual(CreatureObservationState.Unknown, src.CurrentObservationState);
        }

        [Test]
        public void IdleBrain_ReportsCalmThroughSource()
        {
            var id = NamedIdentity("Verak");
            var brain = CreatureTestKit.BuildBrain(temp, id, null, out _, out _); // uninitialized -> Idle
            var src = NewSource(brain, id, "Verak_A");
            Assert.AreEqual(CreatureObservationState.Calm, src.CurrentObservationState);
        }

        [Test]
        public void ReadingObservationState_DoesNotModifyBrainState()
        {
            var id = NamedIdentity("Verak");
            var brain = CreatureTestKit.BuildBrain(temp, id, null, out _, out _);
            CreatureStateId before = brain.CurrentStateId;
            var src = NewSource(brain, id, "Verak_A");

            // Read several times; the Brain's state must be untouched (no control).
            var _a = src.CurrentObservationState;
            var _b = src.CurrentObservationState;

            Assert.AreEqual(before, brain.CurrentStateId, "Observation must not change the Brain state.");
            Assert.AreEqual(CreatureObservationState.Calm, _b);
        }

        [Test]
        public void TwoSources_KeepIndependentNamesAndReferences()
        {
            var idA = NamedIdentity("Verak");
            var idB = NamedIdentity("Otra");
            var brainA = CreatureTestKit.BuildBrain(temp, idA, null, out _, out _);
            var brainB = CreatureTestKit.BuildBrain(temp, idB, null, out _, out _);
            var srcA = NewSource(brainA, idA, "Verak_A");
            var srcB = NewSource(brainB, idB, "Verak_B");

            Assert.AreEqual("Verak", srcA.DisplayName);
            Assert.AreEqual("Otra", srcB.DisplayName);
            Assert.AreNotSame(srcA, srcB);
        }

        [Test]
        public void Source_HasNoUpdateMethod()
        {
            MethodInfo update = typeof(CreatureObservationSource).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNull(update, "CreatureObservationSource must not have Update (no per-frame polling).");
        }

        [Test]
        public void Source_DoesNotReferenceUiOrInteractionTypes()
        {
            FieldInfo[] fields = typeof(CreatureObservationSource).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo f in fields)
            {
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.IsFalse(ns.Contains("Interaction") || ns.Contains("UI"),
                    "Observation source must not reference UI/interaction types: " + f.Name + " (" + ns + ")");
            }
        }

        [Test]
        public void Reading_DoesNotTouchTimeScale()
        {
            Assert.AreEqual(1f, Time.timeScale, "Precondition: timeScale is 1.");
            var id = NamedIdentity("Verak");
            var brain = CreatureTestKit.BuildBrain(temp, id, null, out _, out _);
            var src = NewSource(brain, id, "Verak_A");
            _ = src.CurrentObservationState;
            _ = src.DisplayName;
            Assert.AreEqual(1f, Time.timeScale, "Observation must not change Time.timeScale.");
        }

        [Test]
        public void Source_ImplementsObservationContract()
        {
            var src = NewSource(null, NamedIdentity("Verak"), "Verak_A");
            Assert.IsInstanceOf<ICreatureObservationSource>(src);
        }
    }
}

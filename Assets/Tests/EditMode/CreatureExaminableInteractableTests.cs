using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Interaction;

namespace Synora.Tests
{
    public sealed class CreatureExaminableInteractableTests
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

        /// <summary>Fake observation source; counts state reads, never mutated by Execute.</summary>
        private sealed class FakeSource : ICreatureObservationSource
        {
            public string Name = "Verak";
            public CreatureObservationState State = CreatureObservationState.Calm;
            public int StateReadCount;

            public string DisplayName => Name;
            public CreatureObservationState CurrentObservationState
            {
                get { StateReadCount++; return State; }
            }
        }

        private sealed class FakeReceiver : IInteractionReceiver
        {
            public int ShowCount;
            public ExaminableData LastData;
            public void ShowObservation(ExaminableData data) { ShowCount++; LastData = data; }
        }

        private ExaminableData NewData(string id)
        {
            var d = ScriptableObject.CreateInstance<ExaminableData>();
            temp.Add(d);
            CreatureTestKit.SetPrivate(d, "interactionId", id);
            CreatureTestKit.SetPrivate(d, "observationTitle", "Title-" + id);
            CreatureTestKit.SetPrivate(d, "observationBody", "Body-" + id);
            return d;
        }

        private CreatureExaminableInteractable NewAdapter(
            out FakeSource src,
            ExaminableData baseData,
            ExaminableData calm = null,
            ExaminableData roaming = null,
            ExaminableData watchful = null,
            bool enabled = true,
            bool injectSource = true,
            string goName = "Verak")
        {
            var go = new GameObject(goName);
            temp.Add(go);
            var a = go.AddComponent<CreatureExaminableInteractable>();
            if (baseData != null) CreatureTestKit.SetPrivate(a, "baseData", baseData);
            if (calm != null) CreatureTestKit.SetPrivate(a, "calmData", calm);
            if (roaming != null) CreatureTestKit.SetPrivate(a, "roamingData", roaming);
            if (watchful != null) CreatureTestKit.SetPrivate(a, "watchfulData", watchful);
            CreatureTestKit.SetPrivate(a, "interactionEnabled", enabled);

            src = new FakeSource();
            // Inject the fake source by reflection (project test pattern), since the
            // production seam is the serialized reference and there is no public setter.
            if (injectSource) CreatureTestKit.SetPrivate(a, "injectedSource", src);
            return a;
        }

        [Test]
        public void ImplementsIInteractable()
        {
            var a = NewAdapter(out _, NewData("verak"));
            Assert.IsInstanceOf<IInteractable>(a);
        }

        [Test]
        public void ContractProperties_FollowM2Pattern()
        {
            var baseData = NewData("verak");
            var a = NewAdapter(out _, baseData);
            CreatureTestKit.SetPrivate(a, "priority", 7);
            a.transform.position = new Vector3(2f, 3f, 0f);

            Assert.AreEqual("Examinar", a.PromptText);
            Assert.AreEqual(7, a.Priority);
            Assert.AreEqual(new Vector2(2f, 3f), a.InteractionPosition);
            Assert.AreEqual("verak", a.InteractionId);
        }

        [Test]
        public void Calm_SelectsCalmData()
        {
            var baseData = NewData("verak");
            var calm = NewData("calm");
            var a = NewAdapter(out FakeSource src, baseData, calm: calm);
            src.State = CreatureObservationState.Calm;
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreSame(calm, receiver.LastData);
        }

        [Test]
        public void Roaming_SelectsRoamingData()
        {
            var baseData = NewData("verak");
            var roaming = NewData("roaming");
            var a = NewAdapter(out FakeSource src, baseData, roaming: roaming);
            src.State = CreatureObservationState.Roaming;
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreSame(roaming, receiver.LastData);
        }

        [Test]
        public void Watchful_SelectsWatchfulData()
        {
            var baseData = NewData("verak");
            var watchful = NewData("watchful");
            var a = NewAdapter(out FakeSource src, baseData, watchful: watchful);
            src.State = CreatureObservationState.Watchful;
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreSame(watchful, receiver.LastData);
        }

        [Test]
        public void Unknown_SelectsBaseData()
        {
            var baseData = NewData("verak");
            var a = NewAdapter(out FakeSource src, baseData, calm: NewData("calm"));
            src.State = CreatureObservationState.Unknown;
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreSame(baseData, receiver.LastData);
        }

        [Test]
        public void StateWithoutSpecificData_FallsBackToBase()
        {
            var baseData = NewData("verak");
            // Calm state but no calmData assigned -> base.
            var a = NewAdapter(out FakeSource src, baseData);
            src.State = CreatureObservationState.Calm;
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreSame(baseData, receiver.LastData);
        }

        [Test]
        public void NullSource_CanInteractFalse()
        {
            var a = NewAdapter(out _, NewData("verak"), injectSource: false); // no source at all
            Assert.IsFalse(a.CanInteract);
        }

        [Test]
        public void NullBaseData_CanInteractFalse()
        {
            var a = NewAdapter(out _, null);
            Assert.IsFalse(a.CanInteract);
        }

        [Test]
        public void InteractionDisabled_CanInteractFalse()
        {
            var a = NewAdapter(out _, NewData("verak"), enabled: false);
            Assert.IsFalse(a.CanInteract);
        }

        [Test]
        public void DisabledComponent_CanInteractFalse()
        {
            var a = NewAdapter(out _, NewData("verak"));
            a.enabled = false;
            Assert.IsFalse(a.CanInteract);
        }

        [Test]
        public void InvalidInteractionId_CanInteractFalse()
        {
            var baseData = NewData(""); // empty id -> HasValidInteractionId false
            var a = NewAdapter(out _, baseData);
            Assert.IsFalse(a.CanInteract);
        }

        [Test]
        public void Execute_RevalidatesCanInteract()
        {
            var a = NewAdapter(out _, NewData("verak"), enabled: false); // CanInteract false
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreEqual(0, receiver.ShowCount, "Execute must not proceed when CanInteract is false.");
        }

        [Test]
        public void Execute_NullReceiver_DoesNotThrow()
        {
            var a = NewAdapter(out _, NewData("verak"));
            Assert.DoesNotThrow(() => a.Execute(null));
        }

        [Test]
        public void Execute_CallsShowObservationExactlyOnce()
        {
            var a = NewAdapter(out FakeSource src, NewData("verak"), calm: NewData("calm"));
            src.State = CreatureObservationState.Calm;
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreEqual(1, receiver.ShowCount);
        }

        [Test]
        public void Execute_ReadsObservationStateExactlyOnce()
        {
            var a = NewAdapter(out FakeSource src, NewData("verak"), calm: NewData("calm"));
            src.State = CreatureObservationState.Calm;
            a.Execute(new FakeReceiver());
            Assert.AreEqual(1, src.StateReadCount, "The observable state must be read exactly once per Execute.");
        }

        [Test]
        public void Execute_DoesNotMutateSource()
        {
            var a = NewAdapter(out FakeSource src, NewData("verak"), calm: NewData("calm"));
            src.State = CreatureObservationState.Calm;
            src.Name = "Verak";
            a.Execute(new FakeReceiver());
            Assert.AreEqual("Verak", src.Name);
            Assert.AreEqual(CreatureObservationState.Calm, src.State, "Execute must not change the source state.");
        }

        [Test]
        public void Adapter_HasNoBrainField()
        {
            // Structural: the adapter must not reference the Brain at all (only the
            // observation interface). No field may be a CreatureBrain.
            FieldInfo[] fields = typeof(CreatureExaminableInteractable).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo f in fields)
            {
                Assert.AreNotEqual(typeof(CreatureBrain), f.FieldType,
                    "Adapter must not hold a CreatureBrain reference: " + f.Name);
            }
        }

        [Test]
        public void Execute_DoesNotTouchTimeScale()
        {
            Assert.AreEqual(1f, Time.timeScale);
            var a = NewAdapter(out FakeSource src, NewData("verak"), calm: NewData("calm"));
            src.State = CreatureObservationState.Calm;
            a.Execute(new FakeReceiver());
            Assert.AreEqual(1f, Time.timeScale, "Execute must not change Time.timeScale.");
        }

        [Test]
        public void TwoAdapters_AreIndependent()
        {
            var baseA = NewData("verak-a");
            var baseB = NewData("verak-b");
            var a = NewAdapter(out FakeSource srcA, baseA, calm: NewData("calm-a"), goName: "Verak_A");
            var b = NewAdapter(out FakeSource srcB, baseB, calm: NewData("calm-b"), goName: "Verak_B");
            srcA.State = CreatureObservationState.Calm;
            srcB.State = CreatureObservationState.Unknown; // b -> base

            var rA = new FakeReceiver();
            var rB = new FakeReceiver();
            a.Execute(rA);
            b.Execute(rB);

            Assert.AreEqual("calm-a", rA.LastData.InteractionId);
            Assert.AreEqual("verak-b", rB.LastData.InteractionId);
        }

        [Test]
        public void Adapter_HasNoUiOrControllerFields()
        {
            FieldInfo[] fields = typeof(CreatureExaminableInteractable).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo f in fields)
            {
                Assert.AreNotEqual(typeof(InteractionController), f.FieldType,
                    "Adapter must not reference InteractionController: " + f.Name);
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.IsFalse(ns.Contains("UnityEngine.UI"),
                    "Adapter must not reference concrete UI: " + f.Name);
            }
        }

        [Test]
        public void InteractionId_AlwaysComesFromBaseData()
        {
            var a = NewAdapter(out _, NewData("verak"), calm: NewData("calm-id"));
            Assert.AreEqual("verak", a.InteractionId);
        }

        [Test]
        public void InteractionId_UnchangedAcrossObservableStates()
        {
            var a = NewAdapter(out FakeSource src, NewData("verak"),
                calm: NewData("calm-id"), roaming: NewData("roaming-id"), watchful: NewData("watchful-id"));

            src.State = CreatureObservationState.Calm;
            Assert.AreEqual("verak", a.InteractionId);
            src.State = CreatureObservationState.Roaming;
            Assert.AreEqual("verak", a.InteractionId);
            src.State = CreatureObservationState.Watchful;
            Assert.AreEqual("verak", a.InteractionId);
        }

        [Test]
        public void ContextualDataWithDifferentId_DoesNotChangeIdentity_ButDeliversItsContent()
        {
            var baseData = NewData("verak");
            var calm = NewData("totally-different-id");
            var a = NewAdapter(out FakeSource src, baseData, calm: calm);
            src.State = CreatureObservationState.Calm;

            // Identity stays baseData...
            Assert.AreEqual("verak", a.InteractionId);

            // ...but the contextual content is still delivered, id difference notwithstanding.
            var receiver = new FakeReceiver();
            a.Execute(receiver);
            Assert.AreSame(calm, receiver.LastData);
        }

        [Test]
        public void CanInteract_IndependentOfContextualDataIds()
        {
            // Contextual asset with an empty/invalid id must NOT affect CanInteract:
            // only baseData's id is authoritative.
            var baseData = NewData("verak");           // valid
            var calm = NewData("");                    // invalid id, but only presentation
            var a = NewAdapter(out FakeSource src, baseData, calm: calm);
            src.State = CreatureObservationState.Calm;
            Assert.IsTrue(a.CanInteract, "CanInteract must depend only on baseData, not contextual ids.");
        }

        [Test]
        public void NoPublicSetObservationSourceMethod()
        {
            MethodInfo setter = typeof(CreatureExaminableInteractable).GetMethod(
                "SetObservationSource", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNull(setter,
                "The observation source must be set via the serialized reference, not a public runtime setter.");
        }
    }
}

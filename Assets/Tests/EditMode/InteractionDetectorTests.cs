using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Synora.Data;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Interaction;
using Synora.Gameplay.Player;

namespace Synora.Tests
{
    // Covers the M4 Fase 3 generalization: the detector registers any MonoBehaviour that
    // implements IInteractable (not only ExaminableInteractable), with no creature knowledge.
    public sealed class InteractionDetectorTests
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

        private sealed class NotInteractable : MonoBehaviour { }

        private ExaminableData NewData(string id)
        {
            var d = ScriptableObject.CreateInstance<ExaminableData>();
            temp.Add(d);
            CreatureTestKit.SetPrivate(d, "interactionId", id);
            CreatureTestKit.SetPrivate(d, "observationTitle", "T-" + id);
            CreatureTestKit.SetPrivate(d, "observationBody", "B-" + id);
            return d;
        }

        private InteractionDetector BuildDetector(List<MonoBehaviour> entries)
        {
            var host = new GameObject("Detector");
            temp.Add(host);
            var detector = host.AddComponent<InteractionDetector>();
            var orientation = host.AddComponent<PlayerOrientation>();
            var origin = new GameObject("Origin");
            temp.Add(origin);

            CreatureTestKit.SetPrivate(detector, "playerOrientation", orientation);
            CreatureTestKit.SetPrivate(detector, "originPoint", origin.transform);
            CreatureTestKit.SetPrivate(detector, "interactableLayer", (LayerMask)~0);
            CreatureTestKit.SetPrivate(detector, "sceneExaminables", entries);

            CreatureTestKit.Invoke(detector, "Awake");
            return detector;
        }

        private Dictionary<Collider2D, IInteractable> Lookup(InteractionDetector detector)
        {
            return (Dictionary<Collider2D, IInteractable>)typeof(InteractionDetector)
                .GetField("colliderLookup", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(detector);
        }

        private ExaminableInteractable NewExaminable(string id)
        {
            var go = new GameObject("Examinable");
            temp.Add(go);
            var ex = go.AddComponent<ExaminableInteractable>(); // RequireComponent adds BoxCollider2D
            go.GetComponent<Collider2D>().isTrigger = true;
            CreatureTestKit.SetPrivate(ex, "data", NewData(id));
            return ex;
        }

        private CreatureExaminableInteractable NewCreatureAdapter(string id)
        {
            var go = new GameObject("Verak");
            temp.Add(go);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var adapter = go.AddComponent<CreatureExaminableInteractable>();
            CreatureTestKit.SetPrivate(adapter, "baseData", NewData(id));
            return adapter;
        }

        [Test]
        public void ExaminableInteractable_IsRegistered()
        {
            var ex = NewExaminable("node");
            var detector = BuildDetector(new List<MonoBehaviour> { ex });
            var lookup = Lookup(detector);
            var col = ex.GetComponent<Collider2D>();
            Assert.IsTrue(lookup.ContainsKey(col));
            Assert.AreSame(ex, lookup[col]);
        }

        [Test]
        public void CreatureAdapter_IsRegisteredAsIInteractable()
        {
            var adapter = NewCreatureAdapter("verak");
            var detector = BuildDetector(new List<MonoBehaviour> { adapter });
            var lookup = Lookup(detector);
            var col = adapter.GetComponent<Collider2D>();
            Assert.IsTrue(lookup.ContainsKey(col));
            Assert.AreSame(adapter, lookup[col]);
            Assert.IsInstanceOf<IInteractable>(lookup[col]);
        }

        [Test]
        public void DifferentImplementations_Coexist()
        {
            var ex = NewExaminable("node");
            var adapter = NewCreatureAdapter("verak");
            var detector = BuildDetector(new List<MonoBehaviour> { ex, adapter });
            var lookup = Lookup(detector);
            Assert.AreSame(ex, lookup[ex.GetComponent<Collider2D>()]);
            Assert.AreSame(adapter, lookup[adapter.GetComponent<Collider2D>()]);
        }

        [Test]
        public void NonIInteractableEntry_IsIgnored_AndReported()
        {
            var go = new GameObject("NotInteractable");
            temp.Add(go);
            var bad = go.AddComponent<NotInteractable>();

            LogAssert.Expect(LogType.Error, new Regex("does not implement IInteractable"));
            var detector = BuildDetector(new List<MonoBehaviour> { bad });
            Assert.AreEqual(0, Lookup(detector).Count, "A non-IInteractable entry must not be registered.");
        }

        [Test]
        public void NullEntry_NoException_AndReported()
        {
            LogAssert.Expect(LogType.Error, new Regex("contains a null element"));
            Assert.DoesNotThrow(() => BuildDetector(new List<MonoBehaviour> { null }));
        }

        [Test]
        public void Candidates_ExposeIInteractable()
        {
            var detector = BuildDetector(new List<MonoBehaviour> { NewExaminable("node") });
            Assert.IsInstanceOf<IReadOnlyList<IInteractable>>(detector.Candidates);
        }

        [Test]
        public void ColliderLookup_ValueTypeIsIInteractable()
        {
            var ex = NewExaminable("node");
            var detector = BuildDetector(new List<MonoBehaviour> { ex });
            foreach (var kvp in Lookup(detector))
            {
                Assert.IsInstanceOf<IInteractable>(kvp.Value);
            }
        }

        [Test]
        public void SharedInteractionId_DistinctInstances_BothRegistered_NoError()
        {
            // Same content id on two distinct entries is legitimate; no duplicate error.
            var a = NewExaminable("shared.id");
            var b = NewExaminable("shared.id");
            var detector = BuildDetector(new List<MonoBehaviour> { a, b });
            var lookup = Lookup(detector);
            Assert.AreSame(a, lookup[a.GetComponent<Collider2D>()]);
            Assert.AreSame(b, lookup[b.GetComponent<Collider2D>()]);
            Assert.AreEqual(2, lookup.Count);
        }

        [Test]
        public void TwoCreatureAdapters_SharedBaseDataId_BothRegistered()
        {
            // The real Verak case: two adapters sharing "creature.verak".
            var a = NewCreatureAdapter("creature.verak");
            var b = NewCreatureAdapter("creature.verak");
            var detector = BuildDetector(new List<MonoBehaviour> { a, b });
            var lookup = Lookup(detector);
            Assert.AreSame(a, lookup[a.GetComponent<Collider2D>()]);
            Assert.AreSame(b, lookup[b.GetComponent<Collider2D>()]);
            Assert.AreEqual(2, lookup.Count);
        }

        [Test]
        public void SameEntryTwice_ReportedOnce_AndRegisteredOnce()
        {
            var ex = NewExaminable("node");
            LogAssert.Expect(LogType.Error, new Regex("same interactable entry is registered more than once"));
            var detector = BuildDetector(new List<MonoBehaviour> { ex, ex });
            var lookup = Lookup(detector);
            Assert.AreEqual(1, lookup.Count, "The duplicate reference must not add a second mapping.");
            Assert.AreSame(ex, lookup[ex.GetComponent<Collider2D>()]);
        }

        [Test]
        public void NodePlusTwoVerak_SharedId_AllCoexist()
        {
            var node = NewExaminable("claro_exterior.nodo_inactivo");
            var verakA = NewCreatureAdapter("creature.verak");
            var verakB = NewCreatureAdapter("creature.verak");
            var detector = BuildDetector(new List<MonoBehaviour> { node, verakA, verakB });
            var lookup = Lookup(detector);
            Assert.AreEqual(3, lookup.Count);
            Assert.AreSame(node, lookup[node.GetComponent<Collider2D>()]);
            Assert.AreSame(verakA, lookup[verakA.GetComponent<Collider2D>()]);
            Assert.AreSame(verakB, lookup[verakB.GetComponent<Collider2D>()]);
        }

        [Test]
        public void MultipleColliders_MapToSameInteractable()
        {
            var ex = NewExaminable("node");
            var extra = ex.gameObject.AddComponent<BoxCollider2D>();
            extra.isTrigger = true;
            var detector = BuildDetector(new List<MonoBehaviour> { ex });
            var lookup = Lookup(detector);
            var colliders = ex.GetComponents<Collider2D>();
            Assert.GreaterOrEqual(colliders.Length, 2);
            foreach (var c in colliders)
            {
                Assert.AreSame(ex, lookup[c], "Every collider of one interactable maps to that same reference.");
            }
        }

        [Test]
        public void EmptyInteractionId_StillReported()
        {
            var ex = NewExaminable(""); // invalid id
            LogAssert.Expect(LogType.Error, new Regex("empty InteractionId"));
            var detector = BuildDetector(new List<MonoBehaviour> { ex });
            Assert.AreEqual(1, Lookup(detector).Count, "Empty id is diagnosed but the entry still registers (M2 behavior).");
        }

        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void SharedId_ScalesToNInstances(int n)
        {
            var entries = new List<MonoBehaviour>(n);
            for (int i = 0; i < n; i++)
            {
                entries.Add(NewCreatureAdapter("creature.verak"));
            }
            var detector = BuildDetector(entries);
            var lookup = Lookup(detector);
            Assert.AreEqual(n, lookup.Count, "All N distinct instances sharing an id must register.");
            var distinct = new HashSet<IInteractable>();
            foreach (var kvp in lookup) distinct.Add(kvp.Value);
            Assert.AreEqual(n, distinct.Count, "Each instance must stay an independent reference.");
        }
    }
}

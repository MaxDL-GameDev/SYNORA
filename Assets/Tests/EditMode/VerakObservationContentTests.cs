using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Synora.Data;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Interaction;

namespace Synora.Tests
{
    // Editorial protection for Verak's M4 observation content. The expected strings are
    // frozen from Docs/Design/SYNORA_M4_GDD_v0.1.md; any drift in text, id, path, or type
    // fails here. These tests validate data assets only (no scene, no PlayMode).
    public sealed class VerakObservationContentTests
    {
        private const string Folder = "Assets/Data/Creatures/Verak/Observation/";
        private const string BasePath = Folder + "Verak_Observation_Base.asset";
        private const string CalmPath = Folder + "Verak_Observation_Calm.asset";
        private const string RoamingPath = Folder + "Verak_Observation_Roaming.asset";
        private const string WatchfulPath = Folder + "Verak_Observation_Watchful.asset";

        private const string CanonicalId = "creature.verak";
        private const string Title = "Verak";

        // Exact frozen texts from the GDD (§5), without the document's markdown emphasis.
        private const string BaseBody = "Una criatura acorazada de porte bajo, cubierta de placas pétreas de tono pardo. A lo largo del lomo le crecen espinas cristalinas de un verde azulado que atrapan la luz. Se mueve sin prisa, tan parte del paisaje como las rocas entre las que habita.";
        private const string CalmBody = "Permanece quieto, casi confundido con el terreno. Solo el lento subir y bajar de su costado delata que está vivo.";
        private const string RoamingBody = "Recorre su tramo de siempre con paso medido, revisando los límites de un territorio que solo él parece conocer.";
        private const string WatchfulBody = "Se ha detenido. Las espinas del lomo se le erizan apenas y su mirada ámbar sigue algo cercano. No amenaza: solo observa, tan atento como quien lo observa.";

        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            // Only scene objects created by tests are destroyed here; project assets
            // loaded via AssetDatabase must never be destroyed.
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i] != null) Object.DestroyImmediate(temp[i]);
            }
            temp.Clear();
        }

        private sealed class FakeSource : ICreatureObservationSource
        {
            public CreatureObservationState State = CreatureObservationState.Calm;
            public string DisplayName => "Verak";
            public CreatureObservationState CurrentObservationState => State;
        }

        private sealed class FakeReceiver : IInteractionReceiver
        {
            public ExaminableData Last;
            public void ShowObservation(ExaminableData data) { Last = data; }
        }

        private static ExaminableData Load(string path) =>
            AssetDatabase.LoadAssetAtPath<ExaminableData>(path);

        private static string[] AllPaths() => new[] { BasePath, CalmPath, RoamingPath, WatchfulPath };

        [Test]
        public void AllFourAssets_ExistAsExaminableData()
        {
            foreach (var p in AllPaths())
            {
                Assert.IsNotNull(Load(p), "Missing or wrong-typed ExaminableData at " + p);
            }
        }

        [Test]
        public void Base_HasValidCanonicalInteractionId()
        {
            var d = Load(BasePath);
            Assert.IsTrue(d.HasValidInteractionId);
            Assert.AreEqual(CanonicalId, d.InteractionId);
        }

        [Test]
        public void Base_TitleAndBody_MatchGdd()
        {
            var d = Load(BasePath);
            Assert.AreEqual(Title, d.ObservationTitle);
            Assert.AreEqual(BaseBody, d.ObservationBody);
        }

        [Test]
        public void Calm_TitleAndBody_MatchGdd()
        {
            var d = Load(CalmPath);
            Assert.AreEqual(Title, d.ObservationTitle);
            Assert.AreEqual(CalmBody, d.ObservationBody);
        }

        [Test]
        public void Roaming_TitleAndBody_MatchGdd()
        {
            var d = Load(RoamingPath);
            Assert.AreEqual(Title, d.ObservationTitle);
            Assert.AreEqual(RoamingBody, d.ObservationBody);
        }

        [Test]
        public void Watchful_TitleAndBody_MatchGdd()
        {
            var d = Load(WatchfulPath);
            Assert.AreEqual(Title, d.ObservationTitle);
            Assert.AreEqual(WatchfulBody, d.ObservationBody);
        }

        [Test]
        public void NoTrailingWhitespace_NoDoubleSpaces()
        {
            foreach (var p in AllPaths())
            {
                var d = Load(p);
                Assert.AreEqual(d.ObservationBody.TrimEnd(), d.ObservationBody, "trailing whitespace in body of " + p);
                Assert.AreEqual(d.ObservationTitle.Trim(), d.ObservationTitle, "surrounding whitespace in title of " + p);
                Assert.IsFalse(d.ObservationBody.Contains("  "), "double space in body of " + p);
            }
        }

        [Test]
        public void Bodies_AreSingleParagraph_NoLineBreaks()
        {
            foreach (var p in AllPaths())
            {
                Assert.IsFalse(Load(p).ObservationBody.Contains("\n"), "unexpected line break in " + p);
            }
        }

        [Test]
        public void NoMojibake_AccentsIntact()
        {
            foreach (var p in AllPaths())
            {
                var b = Load(p).ObservationBody;
                Assert.IsFalse(b.Contains("�"), "replacement char (broken UTF-8) in " + p);
                Assert.IsFalse(b.Contains("Ã") || b.Contains("Â"), "mojibake sequence in " + p);
            }
            Assert.IsTrue(Load(BasePath).ObservationBody.Contains("pétreas"));
            Assert.IsTrue(Load(CalmPath).ObservationBody.Contains("está"));
            Assert.IsTrue(Load(RoamingPath).ObservationBody.Contains("límites"));
            Assert.IsTrue(Load(WatchfulPath).ObservationBody.Contains("ámbar"));
        }

        [Test]
        public void FourAssets_HaveDistinctGuids()
        {
            var guids = new HashSet<string>
            {
                AssetDatabase.AssetPathToGUID(BasePath),
                AssetDatabase.AssetPathToGUID(CalmPath),
                AssetDatabase.AssetPathToGUID(RoamingPath),
                AssetDatabase.AssetPathToGUID(WatchfulPath)
            };
            Assert.AreEqual(4, guids.Count, "The four assets must have distinct GUIDs.");
        }

        [Test]
        public void AllAssets_ShareCanonicalId_NoDistinctFunctionalIdentity()
        {
            foreach (var p in AllPaths())
            {
                Assert.AreEqual(CanonicalId, Load(p).InteractionId,
                    "Contextual assets must not introduce a distinct functional identity: " + p);
            }
        }

        [Test]
        public void Base_IsSuitableUnknownFallback()
        {
            var d = Load(BasePath);
            Assert.IsTrue(d.HasValidInteractionId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.ObservationTitle));
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.ObservationBody));
        }

        [Test]
        public void Assets_WireIntoAdapter_AndResolvePerState()
        {
            var go = new GameObject("Verak");
            temp.Add(go);
            var adapter = go.AddComponent<CreatureExaminableInteractable>();
            CreatureTestKit.SetPrivate(adapter, "baseData", Load(BasePath));
            CreatureTestKit.SetPrivate(adapter, "calmData", Load(CalmPath));
            CreatureTestKit.SetPrivate(adapter, "roamingData", Load(RoamingPath));
            CreatureTestKit.SetPrivate(adapter, "watchfulData", Load(WatchfulPath));

            var src = new FakeSource();
            CreatureTestKit.SetPrivate(adapter, "injectedSource", src);

            Assert.IsTrue(adapter.CanInteract, "Adapter should be interactable with a valid baseData and source.");
            AssertDelivers(adapter, src, CreatureObservationState.Calm, CalmBody);
            AssertDelivers(adapter, src, CreatureObservationState.Roaming, RoamingBody);
            AssertDelivers(adapter, src, CreatureObservationState.Watchful, WatchfulBody);
            AssertDelivers(adapter, src, CreatureObservationState.Unknown, BaseBody);
        }

        private void AssertDelivers(CreatureExaminableInteractable adapter, FakeSource src,
            CreatureObservationState state, string expectedBody)
        {
            src.State = state;
            var receiver = new FakeReceiver();
            adapter.Execute(receiver);
            Assert.IsNotNull(receiver.Last, "No data delivered for state " + state);
            Assert.AreEqual(expectedBody, receiver.Last.ObservationBody, "Wrong content for state " + state);
        }
    }
}

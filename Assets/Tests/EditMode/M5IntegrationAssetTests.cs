using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Player;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    /// <summary>
    /// M5 Fase 7 asset-integration checks: verifies the Player prefab, the normal Verak
    /// prefab, the Altered Verak variant, and the ClaroExterior scene are wired as the
    /// integration requires. Structure-only (EditMode, no PlayMode); the actual damage
    /// exchange / defeat / subdued flow is a runtime behavior validated by the manual
    /// checklist, not here.
    /// </summary>
    public sealed class M5IntegrationAssetTests
    {
        private const string PlayerPath = "Assets/Prefabs/Player.prefab";
        private const string VerakPath = "Assets/Prefabs/Creatures/Verak.prefab";
        private const string AlteredPath = "Assets/Prefabs/Creatures/VerakAltered.prefab";
        private const string ScenePath = "Assets/Scenes/ClaroExterior.unity";

        private const int PlayerLayerBit = 1 << 8;   // Player
        private const int CreaturesLayerBit = 1 << 12; // Creatures

        private static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(go, "Prefab not found: " + path);
            return go;
        }

        private static SerializedObject SO(Object o) => new SerializedObject(o);

        private static bool RefSet(Object comp, string field) =>
            SO(comp).FindProperty(field).objectReferenceValue != null;

        // ─────────────────────────── Player ───────────────────────────

        [Test]
        public void Player_HasExactlyOneOfEachCombatComponent()
        {
            var p = Load(PlayerPath);
            Assert.AreEqual(1, p.GetComponents<Health>().Length, "one Health");
            Assert.AreEqual(1, p.GetComponents<PlayerAttack>().Length, "one PlayerAttack");
            Assert.AreEqual(1, p.GetComponents<PlayerAttackHitResolver>().Length, "one resolver");
            Assert.AreEqual(1, p.GetComponents<PlayerTemporaryDefeat>().Length, "one defeat");
        }

        [Test]
        public void Player_AttackAction_IsWiredToGameplayAttack()
        {
            var reader = Load(PlayerPath).GetComponent<PlayerInputReader>();
            Object attackRef = SO(reader).FindProperty("attackAction").objectReferenceValue;
            Assert.IsNotNull(attackRef, "attackAction must be assigned.");

            // Reflection avoids a compile-time InputSystem reference in the test asmdef.
            object action = attackRef.GetType().GetProperty("action").GetValue(attackRef);
            string actionName = (string)action.GetType().GetProperty("name").GetValue(action);
            Assert.AreEqual("Attack", actionName, "attackAction must reference the Attack action.");
        }

        [Test]
        public void Player_Attack_DependenciesComplete()
        {
            var attack = Load(PlayerPath).GetComponent<PlayerAttack>();
            Assert.IsTrue(RefSet(attack, "inputReader"), "inputReader");
            Assert.IsTrue(RefSet(attack, "orientation"), "orientation");
            Assert.IsTrue(RefSet(attack, "gate"), "gate");
        }

        [Test]
        public void Player_Resolver_TargetsCreatures_ExcludesPlayer()
        {
            var resolver = Load(PlayerPath).GetComponent<PlayerAttackHitResolver>();
            var so = SO(resolver);
            int mask = so.FindProperty("targetLayers").intValue;
            Assert.AreNotEqual(0, mask & CreaturesLayerBit, "must target Creatures.");
            Assert.AreEqual(0, mask & PlayerLayerBit, "must NOT target Player (no self-damage by config).");
            Assert.IsTrue(RefSet(resolver, "attack"), "resolver.attack");
            Assert.Greater(so.FindProperty("damageAmount").floatValue, 0f);
        }

        [Test]
        public void Player_Health_And_Defeat_Configured()
        {
            var p = Load(PlayerPath);
            var health = p.GetComponent<Health>();
            Assert.Greater(SO(health).FindProperty("maxHealth").floatValue, 0f);
            var defeat = p.GetComponent<PlayerTemporaryDefeat>();
            Assert.IsTrue(RefSet(defeat, "health"), "defeat.health");
            Assert.IsTrue(RefSet(defeat, "gate"), "defeat.gate");
        }

        // ─────────────────────────── Normal Verak (untouched) ───────────────────────────

        [Test]
        public void NormalVerak_HasNoHostileComponents()
        {
            var v = Load(VerakPath);
            Assert.AreEqual(0, v.GetComponentsInChildren<AlteredVerakSetup>(true).Length);
            Assert.AreEqual(0, v.GetComponentsInChildren<CreatureAttackController>(true).Length);
            Assert.AreEqual(0, v.GetComponentsInChildren<CreatureAttackHitResolver>(true).Length);
            Assert.AreEqual(0, v.GetComponentsInChildren<Health>(true).Length);
            var brain = v.GetComponent<CreatureBrain>();
            Assert.IsFalse(RefSet(brain, "stateProvider"), "normal Verak has no state provider (ambient set).");
        }

        // ─────────────────────────── Altered Verak ───────────────────────────

        [Test]
        public void AlteredVerak_HasExactlyOneOfEachHostileComponent_WithRefs()
        {
            var a = Load(AlteredPath);
            Assert.AreEqual(1, a.GetComponents<CreatureBrain>().Length, "one Brain");
            Assert.AreEqual(1, a.GetComponents<Health>().Length, "one Health");
            Assert.AreEqual(1, a.GetComponents<AlteredVerakSetup>().Length, "one setup");
            Assert.AreEqual(1, a.GetComponents<CreatureAttackController>().Length, "one controller");
            Assert.AreEqual(1, a.GetComponents<CreatureAttackHitResolver>().Length, "one resolver");

            var brain = a.GetComponent<CreatureBrain>();
            Assert.IsTrue(RefSet(brain, "stateProvider"), "brain.stateProvider must be the setup.");

            var setup = a.GetComponent<AlteredVerakSetup>();
            Assert.IsTrue(RefSet(setup, "brain"), "setup.brain");
            Assert.IsTrue(RefSet(setup, "health"), "setup.health");
            Assert.IsTrue(RefSet(setup, "attackController"), "setup.attackController");
            Assert.Greater(SO(setup).FindProperty("attackRange").floatValue, 0f);

            Assert.IsTrue(RefSet(a.GetComponent<CreatureAttackController>(), "resolver"), "controller.resolver");
            Assert.Greater(SO(a.GetComponent<Health>()).FindProperty("maxHealth").floatValue, 0f);
        }

        [Test]
        public void AlteredVerak_Resolver_TargetsPlayer_ExcludesCreatures()
        {
            var resolver = Load(AlteredPath).GetComponent<CreatureAttackHitResolver>();
            var so = SO(resolver);
            int mask = so.FindProperty("targetLayers").intValue;
            Assert.AreNotEqual(0, mask & PlayerLayerBit, "must target Player.");
            Assert.AreEqual(0, mask & CreaturesLayerBit, "must NOT target Creatures (no self-damage).");
            Assert.Greater(so.FindProperty("damageAmount").floatValue, 0f);
            Assert.Greater(so.FindProperty("attackRange").floatValue, 0f);
        }

        [Test]
        public void AlteredVerak_IsNotExaminable()
        {
            var ex = Load(AlteredPath).GetComponentInChildren<CreatureExaminableInteractable>(true);
            if (ex == null)
            {
                Assert.Pass("No examinable component (also acceptable).");
                return;
            }
            Assert.IsFalse(SO(ex).FindProperty("interactionEnabled").boolValue,
                "Altered Verak must not be examinable while hostile (SPEC §9).");
        }

        // ─────────────────────────── Scene ───────────────────────────

        [Test]
        public void ClaroExterior_ContainsPlayerAndAltered_NoMissingScripts()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                bool hasPlayer = false, hasAltered = false;
                int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        if (t.GetComponent<PlayerInputReader>() != null) hasPlayer = true;
                        if (t.GetComponent<AlteredVerakSetup>() != null) hasAltered = true;
                    }
                }
                Assert.IsTrue(hasPlayer, "scene contains the Player.");
                Assert.IsTrue(hasAltered, "scene contains a VerakAltered (with AlteredVerakSetup).");
                Assert.AreEqual(0, missing, "scene has no missing scripts.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}

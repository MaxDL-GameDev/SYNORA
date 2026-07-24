using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Player;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    /// <summary>M5 Fase 8 asset-integration checks for presentation, health UI and recovery wiring.</summary>
    public sealed class M5F8AssetTests
    {
        private const string PlayerPath = "Assets/Prefabs/Player.prefab";
        private const string AlteredPath = "Assets/Prefabs/Creatures/VerakAltered.prefab";
        private const string VerakPath = "Assets/Prefabs/Creatures/Verak.prefab";
        private const string ScenePath = "Assets/Scenes/ClaroExterior.unity";

        private static GameObject Load(string p) { var g = AssetDatabase.LoadAssetAtPath<GameObject>(p); Assert.IsNotNull(g, p); return g; }
        private static bool Ref(Object c, string f) => new SerializedObject(c).FindProperty(f).objectReferenceValue != null;

        [Test]
        public void Player_PresentationAndRecovery_Wired()
        {
            var p = Load(PlayerPath);
            Assert.AreEqual(1, p.GetComponents<SpriteFlash>().Length, "one SpriteFlash");
            Assert.AreEqual(1, p.GetComponents<PlayerCombatPresentation>().Length, "one PlayerCombatPresentation");
            Assert.IsTrue(Ref(p.GetComponent<SpriteFlash>(), "spriteRenderer"), "flash.spriteRenderer");
            var pres = p.GetComponent<PlayerCombatPresentation>();
            Assert.IsTrue(Ref(pres, "attack") && Ref(pres, "health") && Ref(pres, "defeat") && Ref(pres, "flash"), "presentation refs");
            Assert.IsTrue(Ref(p.GetComponent<PlayerTemporaryDefeat>(), "body"), "defeat.body wired for recovery");
        }

        [Test]
        public void AlteredVerak_Presentation_Wired_StillVariant()
        {
            var a = Load(AlteredPath);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(a), "still a Variant");
            Assert.AreEqual(1, a.GetComponents<SpriteFlash>().Length);
            Assert.AreEqual(1, a.GetComponents<AlteredVerakPresentation>().Length);
            var pres = a.GetComponent<AlteredVerakPresentation>();
            Assert.IsTrue(Ref(pres, "attackController") && Ref(pres, "health") && Ref(pres, "brain") && Ref(pres, "flash"), "verak presentation refs");
        }

        [Test]
        public void NormalVerak_HasNoPresentationNorHealth()
        {
            var v = Load(VerakPath);
            Assert.AreEqual(0, v.GetComponentsInChildren<SpriteFlash>(true).Length);
            Assert.AreEqual(0, v.GetComponentsInChildren<AlteredVerakPresentation>(true).Length);
            Assert.AreEqual(0, v.GetComponentsInChildren<Health>(true).Length);
        }

        [Test]
        public void Scene_HasHudHealthBarBound_RecoveryPoint_NoMissingScripts()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                bool bar = false, recovery = false; int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        var b = t.GetComponent<PlayerHealthBar>();
                        if (b != null) bar = Ref(b, "health") && Ref(b, "fill");
                        var d = t.GetComponent<PlayerTemporaryDefeat>();
                        if (d != null) recovery = Ref(d, "recoveryPoint");
                    }
                }
                Assert.IsTrue(bar, "scene health bar bound to Health + fill Image");
                Assert.IsTrue(recovery, "player recovery point wired in scene");
                Assert.AreEqual(0, missing, "no missing scripts");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}

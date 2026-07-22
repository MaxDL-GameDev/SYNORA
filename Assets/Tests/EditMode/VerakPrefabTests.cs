using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Synora.Data;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    // Editorial validation of the Verak prefab + CreatureIdentity asset (M3 Fase 6).
    public sealed class VerakPrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/Creatures/Verak.prefab";
        private const string IdentityPath = "Assets/Data/Creatures/Verak.asset";

        private static GameObject LoadPrefab() => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        [Test]
        public void Prefab_Exists()
        {
            Assert.IsNotNull(LoadPrefab(), "Verak.prefab must exist.");
        }

        [Test]
        public void Prefab_RootComponentsPresent_OnCreaturesLayer()
        {
            var go = LoadPrefab();
            Assert.IsNotNull(go.GetComponent<Rigidbody2D>());
            Assert.IsNotNull(go.GetComponent<Collider2D>());
            Assert.IsNotNull(go.GetComponent<CreatureMovement>());
            Assert.IsNotNull(go.GetComponent<CreatureSensor>());
            Assert.IsNotNull(go.GetComponent<CreatureBrain>());
            Assert.AreEqual(LayerMask.NameToLayer("Creatures"), go.layer, "Root must be on the Creatures layer.");
        }

        [Test]
        public void Prefab_Rigidbody_NoGravity_RotationFrozen()
        {
            var rb = LoadPrefab().GetComponent<Rigidbody2D>();
            Assert.AreEqual(0f, rb.gravityScale);
            Assert.AreNotEqual(0, (int)(rb.constraints & RigidbodyConstraints2D.FreezeRotation));
        }

        [Test]
        public void Prefab_Visual_HasAnimatorControllerAndNoRootMotion()
        {
            var go = LoadPrefab();
            var animator = go.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsNotNull(animator.runtimeAnimatorController, "Verak.controller must be assigned.");
            Assert.IsFalse(animator.applyRootMotion, "Apply Root Motion must be off.");
            Assert.IsNotNull(go.GetComponentInChildren<SpriteRenderer>());
            Assert.IsNotNull(go.GetComponentInChildren<CreatureAnimator>());
        }

        [Test]
        public void Prefab_BrainSerializedReferences_AreAssigned()
        {
            var brain = LoadPrefab().GetComponent<CreatureBrain>();
            var so = new SerializedObject(brain);
            Assert.IsNotNull(so.FindProperty("identity").objectReferenceValue, "Brain.identity");
            Assert.IsNotNull(so.FindProperty("movement").objectReferenceValue, "Brain.movement");
            Assert.IsNotNull(so.FindProperty("sensor").objectReferenceValue, "Brain.sensor");
            Assert.IsNotNull(so.FindProperty("animator").objectReferenceValue, "Brain.animator");
            Assert.IsNotNull(so.FindProperty("root").objectReferenceValue, "Brain.root");
        }

        [Test]
        public void Prefab_MovementAndSensor_ReferencesAssigned()
        {
            var go = LoadPrefab();
            var moveSo = new SerializedObject(go.GetComponent<CreatureMovement>());
            Assert.IsNotNull(moveSo.FindProperty("body").objectReferenceValue, "Movement.body");
            Assert.IsNotNull(moveSo.FindProperty("identity").objectReferenceValue, "Movement.identity");
            var sensorSo = new SerializedObject(go.GetComponent<CreatureSensor>());
            Assert.IsNotNull(sensorSo.FindProperty("identity").objectReferenceValue, "Sensor.identity");
            Assert.AreNotEqual(0, sensorSo.FindProperty("playerLayer").intValue, "Sensor.playerLayer must target the Player layer.");
        }

        [Test]
        public void Identity_Exists_DetectionLessThanLose()
        {
            var id = AssetDatabase.LoadAssetAtPath<CreatureIdentity>(IdentityPath);
            Assert.IsNotNull(id, "Verak CreatureIdentity must exist.");
            Assert.Greater(id.DetectionRadius, 0f);
            Assert.GreaterOrEqual(id.LoseRadius, id.DetectionRadius);
        }
    }
}

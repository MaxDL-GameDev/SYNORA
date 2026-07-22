using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Synora.Data;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Interaction;
using Synora.Gameplay.Player;

namespace Synora.Tests
{
    // Editorial validation of the Verak prefab + CreatureIdentity asset (M3 Fase 6),
    // extended with the M4 observation/interaction surface (M4 Fase 5C).
    public sealed class VerakPrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/Creatures/Verak.prefab";
        private const string IdentityPath = "Assets/Data/Creatures/Verak.asset";

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

        private static GameObject LoadPrefab() => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        private GameObject Instantiate()
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab());
            temp.Add(go);
            return go;
        }

        private static Transform InteractionChild(GameObject verak) => verak.transform.Find("Interaction");

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

        // ── M4 Fase 5C: observation/interaction surface ──

        [Test]
        public void Prefab_HasSingleObservationSource_OnRoot()
        {
            var go = LoadPrefab();
            Assert.AreEqual(1, go.GetComponentsInChildren<CreatureObservationSource>(true).Length);
            Assert.IsNotNull(go.GetComponent<CreatureObservationSource>(), "ObservationSource must be on the root.");
        }

        [Test]
        public void Prefab_HasSingleAdapter_OnInteractionChild_NotOnRoot()
        {
            var go = LoadPrefab();
            Assert.AreEqual(1, go.GetComponentsInChildren<CreatureExaminableInteractable>(true).Length);
            Assert.IsNull(go.GetComponent<CreatureExaminableInteractable>(), "Adapter must NOT be on the root.");
            Assert.IsNotNull(InteractionChild(go), "An 'Interaction' child must exist.");
            Assert.IsNotNull(InteractionChild(go).GetComponent<CreatureExaminableInteractable>(),
                "Adapter must live on the Interaction child.");
        }

        [Test]
        public void Prefab_InteractionChild_Layer_Trigger_NoRigidbodyNoVisual()
        {
            var child = InteractionChild(LoadPrefab());
            Assert.AreEqual(LayerMask.NameToLayer("Interactables"), child.gameObject.layer,
                "Interaction child must be on the Interactables layer.");
            var col = child.GetComponent<Collider2D>();
            Assert.IsNotNull(col, "Interaction child must have a Collider2D.");
            Assert.IsTrue(col.isTrigger, "Interaction collider must be a trigger.");
            Assert.IsNull(child.GetComponent<Rigidbody2D>(), "Interaction child must not add a Rigidbody2D.");
            Assert.IsNull(child.GetComponent<SpriteRenderer>(), "Interaction child must have no visuals.");
            Assert.IsNull(child.GetComponent<Animator>(), "Interaction child must have no Animator.");
            Assert.AreEqual(Vector3.zero, child.localPosition);
            Assert.AreEqual(Vector3.one, child.localScale);
        }

        [Test]
        public void Prefab_AdapterAndTrigger_OnSameGameObject()
        {
            var child = InteractionChild(LoadPrefab());
            var adapter = child.GetComponent<CreatureExaminableInteractable>();
            var col = child.GetComponent<Collider2D>();
            Assert.IsNotNull(adapter);
            Assert.IsNotNull(col);
            Assert.AreSame(adapter.gameObject, col.gameObject,
                "The detector reads GetComponents<Collider2D>() on the interactable's own GameObject.");
            Assert.AreEqual(1, adapter.GetComponents<Collider2D>().Length,
                "Exactly one interaction collider on the adapter GameObject.");
        }

        [Test]
        public void Prefab_Adapter_SourceCrossRefToRoot_AndFourDataAssigned()
        {
            var go = LoadPrefab();
            var adapter = InteractionChild(go).GetComponent<CreatureExaminableInteractable>();
            var so = new SerializedObject(adapter);
            var src = so.FindProperty("source").objectReferenceValue as CreatureObservationSource;
            Assert.IsNotNull(src, "Adapter.source must be assigned.");
            Assert.AreSame(go.GetComponent<CreatureObservationSource>(), src,
                "Adapter.source must cross-reference the ObservationSource on the root.");
            Assert.IsNotNull(so.FindProperty("baseData").objectReferenceValue, "baseData");
            Assert.IsNotNull(so.FindProperty("calmData").objectReferenceValue, "calmData");
            Assert.IsNotNull(so.FindProperty("roamingData").objectReferenceValue, "roamingData");
            Assert.IsNotNull(so.FindProperty("watchfulData").objectReferenceValue, "watchfulData");
        }

        [Test]
        public void Prefab_ObservationSource_WiredToBrainAndIdentity()
        {
            var go = LoadPrefab();
            var so = new SerializedObject(go.GetComponent<CreatureObservationSource>());
            Assert.IsNotNull(so.FindProperty("brain").objectReferenceValue, "obs.brain");
            Assert.IsNotNull(so.FindProperty("identity").objectReferenceValue, "obs.identity");
        }

        [Test]
        public void Prefab_PhysicsCapsule_Unchanged_OnRoot()
        {
            var cap = LoadPrefab().GetComponent<CapsuleCollider2D>();
            Assert.IsNotNull(cap, "Root physics CapsuleCollider2D must remain.");
            Assert.IsFalse(cap.isTrigger, "Physics capsule must stay a solid (non-trigger) collider.");
            Assert.AreEqual(new Vector2(1.2f, 0.5f), cap.size);
            Assert.AreEqual(new Vector2(0f, 0.25f), cap.offset);
            Assert.AreEqual(CapsuleDirection2D.Horizontal, cap.direction);
        }

        [Test]
        public void Prefab_NoMissingScripts()
        {
            var go = LoadPrefab();
            int missing = 0;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) missing++;
                }
            }
            Assert.AreEqual(0, missing, "Prefab must have no Missing Scripts.");
        }

        [Test]
        public void Prefab_Instance_IdCanonical_AndCanInteract()
        {
            var adapter = InteractionChild(Instantiate()).GetComponent<CreatureExaminableInteractable>();
            Assert.AreEqual("creature.verak", adapter.InteractionId);
            Assert.IsTrue(adapter.CanInteract, "An instantiated Verak must be interactable.");
        }

        [Test]
        public void Prefab_TwoInstances_RegisterAsDistinct_PhysicsCapsuleNotRegistered()
        {
            var a = InteractionChild(Instantiate()).GetComponent<CreatureExaminableInteractable>();
            var b = InteractionChild(Instantiate()).GetComponent<CreatureExaminableInteractable>();

            var host = new GameObject("Detector");
            temp.Add(host);
            var detector = host.AddComponent<InteractionDetector>();
            var orientation = host.AddComponent<PlayerOrientation>();
            var origin = new GameObject("Origin");
            temp.Add(origin);
            CreatureTestKit.SetPrivate(detector, "playerOrientation", orientation);
            CreatureTestKit.SetPrivate(detector, "originPoint", origin.transform);
            CreatureTestKit.SetPrivate(detector, "interactableLayer", (LayerMask)~0);
            CreatureTestKit.SetPrivate(detector, "sceneExaminables", new List<MonoBehaviour> { a, b });
            CreatureTestKit.Invoke(detector, "Awake");

            var lookup = (Dictionary<Collider2D, IInteractable>)typeof(InteractionDetector)
                .GetField("colliderLookup", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(detector);

            // Only the two child triggers register (one per instance); the root physics
            // capsules are on a different GameObject and are never read.
            Assert.AreEqual(2, lookup.Count, "Only the two interaction triggers must register (not the physics capsules).");
            Assert.AreSame(a, lookup[a.GetComponent<Collider2D>()]);
            Assert.AreSame(b, lookup[b.GetComponent<Collider2D>()]);
            Assert.AreNotSame(a, b, "Two instances sharing creature.verak stay distinct references.");
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Is = NUnit.Framework.Is;

namespace Synora.Tests
{
    /// <summary>
    /// EditMode coverage of the Altered Verak's hit channel. The resolver is driven by
    /// explicit BeginWindow()/ResolveHits(facing) calls (as the controller drives it),
    /// so there is no window flag inside the resolver. Windowed behavior (windup does
    /// not damage, cancel stops damage) is covered through a real CreatureAttackController.
    /// Uses real Physics2D overlap with SyncTransforms and a fake IDamageable probe.
    /// </summary>
    public sealed class CreatureAttackHitResolverTests
    {
        private const int TargetLayer = 8; // reuse "Player": the creature hits the player
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

        private sealed class DamageProbe : MonoBehaviour, IDamageable
        {
            public int Calls { get; private set; }
            public DamageInfo Last { get; private set; }

            public void ApplyDamage(in DamageInfo damage)
            {
                Calls++;
                Last = damage;
            }
        }

        private CreatureAttackHitResolver NewResolver(float range = 2f, float width = 2f,
            float amount = 4f, int mask = 1 << TargetLayer)
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            go.transform.position = Vector3.zero;
            var resolver = go.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)mask);
            CreatureTestKit.SetPrivate(resolver, "attackRange", range);
            CreatureTestKit.SetPrivate(resolver, "attackWidth", width);
            CreatureTestKit.SetPrivate(resolver, "damageAmount", amount);
            CreatureTestKit.Invoke(resolver, "Awake");
            return resolver;
        }

        private DamageProbe NewTarget(Vector2 position, int layer = TargetLayer, bool withProbe = true)
        {
            var go = new GameObject("Target") { layer = layer };
            temp.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one * 0.5f;
            return withProbe ? go.AddComponent<DamageProbe>() : null;
        }

        private static void Sync() => Physics2D.SyncTransforms();

        // ─────────────────────────── Detection ───────────────────────────

        [Test]
        public void SingleTarget_HitOnce_SourceIsCreature()
        {
            var resolver = NewResolver(amount: 4f);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(1, probe.Calls);
            Assert.AreEqual(DamageSourceKind.Creature, probe.Last.SourceKind);
            Assert.AreEqual(4f, probe.Last.Amount);
        }

        [Test]
        public void TwoTargets_BothHit()
        {
            var resolver = NewResolver();
            DamageProbe a = NewTarget(new Vector2(0.8f, 0.3f));
            DamageProbe b = NewTarget(new Vector2(1.2f, -0.3f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(1, a.Calls);
            Assert.AreEqual(1, b.Calls);
        }

        [Test]
        public void OutOfBox_NotHit()
        {
            var resolver = NewResolver();
            DamageProbe behind = NewTarget(new Vector2(-3f, 0f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(0, behind.Calls);
        }

        [Test]
        public void ColliderWithoutDamageable_Ignored_OthersHit()
        {
            var resolver = NewResolver();
            NewTarget(new Vector2(0.9f, 0f), withProbe: false);
            DamageProbe probe = NewTarget(new Vector2(1.1f, 0f));
            Sync();
            resolver.BeginWindow();
            Assert.DoesNotThrow(() => resolver.ResolveHits(Vector2Int.right));
            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void WrongLayer_NotHit()
        {
            var resolver = NewResolver();
            DamageProbe probe = NewTarget(new Vector2(1f, 0f), layer: 0);
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(0, probe.Calls);
        }

        [Test]
        public void ZeroFacing_NoOverlap()
        {
            var resolver = NewResolver();
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.zero);
            Assert.AreEqual(0, probe.Calls);
        }

        [Test]
        public void VerticalFacing_UsesVerticalBox()
        {
            var resolver = NewResolver(range: 2f, width: 1f);
            DamageProbe up = NewTarget(new Vector2(0f, 1f));   // in front when facing up
            DamageProbe side = NewTarget(new Vector2(1f, 0f)); // outside the narrow width
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.up);
            Assert.AreEqual(1, up.Calls);
            Assert.AreEqual(0, side.Calls, "Facing up: the box is narrow horizontally.");
        }

        // ─────────────────────────── Dedupe / window ───────────────────────────

        [Test]
        public void SameTarget_ManyFrames_HitOncePerWindow()
        {
            var resolver = NewResolver();
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.BeginWindow();
            for (int i = 0; i < 5; i++) resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void MultipleCollidersSameBody_HitOnce()
        {
            var resolver = NewResolver();
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            var extra = probe.gameObject.AddComponent<BoxCollider2D>();
            extra.isTrigger = true;
            extra.size = Vector2.one * 0.5f;
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void NewWindow_ResetsDedupe_HitAgain()
        {
            var resolver = NewResolver();
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(1, probe.Calls);

            resolver.BeginWindow(); // new attack window
            resolver.ResolveHits(Vector2Int.right);
            Assert.AreEqual(2, probe.Calls);
        }

        [Test]
        public void DestroyedTarget_MidWindow_NoThrow()
        {
            var resolver = NewResolver();
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);
            Object.DestroyImmediate(probe.gameObject);
            Sync();
            Assert.DoesNotThrow(() => resolver.ResolveHits(Vector2Int.right));
        }

        [Test]
        public void Resolve_SteadyState_DoesNotAllocate()
        {
            var resolver = NewResolver();
            NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right); // warm cache

            Assert.That(() => resolver.ResolveHits(Vector2Int.right), Is.Not.AllocatingGCMemory());
        }

        // ─────────────────────────── Controller-driven window ───────────────────────────

        private CreatureAttackController NewController(CreatureAttackHitResolver resolver,
            float windup = 0.2f, float active = 0.2f, float cooldown = 0.4f)
        {
            var controller = resolver.gameObject.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(controller, "resolver", resolver);
            CreatureTestKit.SetPrivate(controller, "windupDuration", windup);
            CreatureTestKit.SetPrivate(controller, "activeDuration", active);
            CreatureTestKit.SetPrivate(controller, "cooldownDuration", cooldown);
            return controller;
        }

        [Test]
        public void Controller_Windup_DoesNotDamage_ActiveDoes()
        {
            var resolver = NewResolver();
            var controller = NewController(resolver);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();

            Assert.IsTrue(controller.TryStartAttack(Vector2Int.right));
            controller.Tick(0.1f); // still windup
            Assert.AreEqual(0, probe.Calls, "No damage during windup.");
            controller.Tick(0.15f); // crosses into the active window
            Assert.AreEqual(1, probe.Calls, "Damage lands during the active window.");
            controller.Tick(0.2f); // active ends -> cooldown
            controller.Tick(0.1f); // cooldown, no damage
            Assert.AreEqual(1, probe.Calls, "Cooldown does not damage; one hit per window.");
        }

        [Test]
        public void Controller_Cancel_StopsDamage()
        {
            var resolver = NewResolver();
            var controller = NewController(resolver);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();

            Assert.IsTrue(controller.TryStartAttack(Vector2Int.right));
            controller.Cancel();
            controller.Tick(0.5f);
            Assert.AreEqual(0, probe.Calls, "A cancelled attack never opens its window.");
        }
    }
}

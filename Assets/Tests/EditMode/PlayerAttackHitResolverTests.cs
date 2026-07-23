using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Player;
using Synora.Systems;
using Is = NUnit.Framework.Is;

namespace Synora.Tests
{
    /// <summary>
    /// EditMode coverage of the M5 Fase 5 hit channel. Uses real Physics2D overlap with
    /// Physics2D.SyncTransforms (same pattern as CreatureSensorTests) and a fake
    /// IDamageable probe. The resolver's timing/gate behavior is exercised through a
    /// real PlayerAttack, since the resolver only reads its window/facing.
    /// </summary>
    public sealed class PlayerAttackHitResolverTests
    {
        private const int TargetLayer = 12; // "Creatures"
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
            // Auto-properties (not serialized fields) so Unity's serialization analyzer
            // does not flag the DamageInfo type (UAC1001).
            public int Calls { get; private set; }
            public DamageInfo Last { get; private set; }

            public void ApplyDamage(in DamageInfo damage)
            {
                Calls++;
                Last = damage;
            }
        }

        private PlayerAttackHitResolver NewResolver(out PlayerAttack attack, out PlayerControlGate gate,
            out PlayerOrientation orientation, float range = 2f, float width = 2f, float amount = 5f,
            int mask = 1 << TargetLayer)
        {
            var go = new GameObject("Player");
            temp.Add(go);
            go.transform.position = Vector3.zero;

            orientation = go.AddComponent<PlayerOrientation>();
            gate = go.AddComponent<PlayerControlGate>();
            attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(attack, "windowDuration", 0.2f);
            CreatureTestKit.SetPrivate(attack, "cooldownDuration", 0.5f);
            // Face right by default so targets sit on +X.
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);

            var resolver = go.AddComponent<PlayerAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "attack", attack);
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
        public void NoTargets_NoDamage_NoThrow()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            Assert.IsTrue(attack.TryAttack());
            Assert.DoesNotThrow(() => resolver.Resolve());
        }

        [Test]
        public void SingleTarget_HitOnce_SourceIsPlayer()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _, amount: 5f);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(1, probe.Calls);
            Assert.AreEqual(DamageSourceKind.Player, probe.Last.SourceKind);
            Assert.AreEqual(5f, probe.Last.Amount);
        }

        [Test]
        public void TwoTargets_BothHitOnce()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe a = NewTarget(new Vector2(0.8f, 0.3f));
            DamageProbe b = NewTarget(new Vector2(1.2f, -0.3f));
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(1, a.Calls);
            Assert.AreEqual(1, b.Calls);
        }

        [Test]
        public void OutOfBoxTarget_NotHit()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe behind = NewTarget(new Vector2(-3f, 0f)); // opposite the facing
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(0, behind.Calls);
        }

        [Test]
        public void ColliderWithoutDamageable_Ignored_OthersStillHit()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            NewTarget(new Vector2(0.9f, 0f), withProbe: false); // collider, no IDamageable
            DamageProbe probe = NewTarget(new Vector2(1.1f, 0f));
            Sync();
            Assert.IsTrue(attack.TryAttack());
            Assert.DoesNotThrow(() => resolver.Resolve());
            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void WrongLayer_NotHit()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f), layer: 0); // Default, outside mask
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(0, probe.Calls);
        }

        // ─────────────────────────── Dedupe ───────────────────────────

        [Test]
        public void SameTarget_ManyFrames_HitOnlyOncePerWindow()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            Assert.IsTrue(attack.TryAttack());
            for (int i = 0; i < 5; i++)
            {
                resolver.Resolve();
            }
            Assert.AreEqual(1, probe.Calls, "One hit per target per window.");
        }

        [Test]
        public void MultipleCollidersSameBody_HitOnce()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            var extra = probe.gameObject.AddComponent<BoxCollider2D>(); // second collider, same IDamageable
            extra.isTrigger = true;
            extra.size = Vector2.one * 0.5f;
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(1, probe.Calls, "Two colliders resolving to the same IDamageable = one hit.");
        }

        [Test]
        public void NewWindow_ResetsDedupe_TargetHitAgain()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();

            Assert.IsTrue(attack.TryAttack()); // window 1
            resolver.Resolve();
            Assert.AreEqual(1, probe.Calls);

            // Close window 1 and its cooldown, resolving each step so the resolver
            // observes the window go inactive before the next attack.
            attack.Tick(0.2f); resolver.Resolve(); // -> cooldown
            attack.Tick(0.5f); resolver.Resolve(); // -> idle

            Assert.IsTrue(attack.TryAttack()); // window 2
            resolver.Resolve();
            Assert.AreEqual(2, probe.Calls, "A new window re-arms damage for the same target.");
        }

        [Test]
        public void DestroyedTarget_MidWindow_NoThrow()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(1, probe.Calls);

            Object.DestroyImmediate(probe.gameObject); // target gone mid-window
            Sync();
            Assert.DoesNotThrow(() => resolver.Resolve(), "A destroyed target must not break resolution.");
        }

        // ─────────────────────────── Window / gate ───────────────────────────

        [Test]
        public void ClosedWindow_NoDamage()
        {
            var resolver = NewResolver(out PlayerAttack _, out _, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            resolver.Resolve(); // no attack started -> window inactive
            Assert.AreEqual(0, probe.Calls);
        }

        [Test]
        public void GateBlocked_NoAttack_NoDamage()
        {
            var resolver = NewResolver(out PlayerAttack attack, out PlayerControlGate gate, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            gate.Block(ControlBlockReason.Observation);
            Assert.IsFalse(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(0, probe.Calls);
        }

        [Test]
        public void CancelledWindow_NotResolved()
        {
            var resolver = NewResolver(out PlayerAttack attack, out PlayerControlGate gate, out _);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();
            Assert.IsTrue(attack.TryAttack());   // window opens
            gate.Block(ControlBlockReason.Observation);
            attack.Tick(0.01f);                  // gate blocked -> window cancelled
            resolver.Resolve();
            Assert.AreEqual(0, probe.Calls, "A cancelled window must not resolve hits.");
        }

        // ─────────────────────────── Allocations ───────────────────────────

        [Test]
        public void Resolve_SteadyState_DoesNotAllocate()
        {
            var resolver = NewResolver(out PlayerAttack attack, out _, out _);
            NewTarget(new Vector2(1f, 0f));
            Sync();
            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve(); // warm the resolve cache and dedupe set

            Assert.That(() => resolver.Resolve(), Is.Not.AllocatingGCMemory());
        }
    }
}

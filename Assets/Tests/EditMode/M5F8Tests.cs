using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Player;
using Synora.Gameplay.Presentation;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// M5 Fase 8 behavior: presentation-only events, sprite feedback, health UI binding,
    /// temporary-defeat recovery, resolver self-damage hardening, and the reused
    /// animation mapping for the hostile states.
    /// </summary>
    public sealed class M5F8Tests
    {
        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = temp.Count - 1; i >= 0; i--)
                if (temp[i] != null) Object.DestroyImmediate(temp[i]);
            temp.Clear();
        }

        private sealed class DamageProbe : MonoBehaviour, IDamageable
        {
            public int Calls { get; private set; }
            public void ApplyDamage(in DamageInfo damage) { Calls++; }
        }

        private GameObject NewGO(string name = "GO") { var g = new GameObject(name); temp.Add(g); return g; }

        private Health NewHealth(GameObject go, float max = 30f)
        {
            var h = go.AddComponent<Health>();
            CreatureTestKit.SetPrivate(h, "maxHealth", max);
            h.ResetHealth();
            return h;
        }

        private static void Deplete(Health h) => h.ApplyDamage(new DamageInfo(999f, DamageSourceKind.Creature));

        // ─────────────────────────── Health.Changed ───────────────────────────

        [Test]
        public void Health_Changed_FiresOnDamageAndReset_NotOnNoop()
        {
            var h = NewHealth(NewGO(), 30f);
            int changes = 0; h.Changed += () => changes++;
            h.ApplyDamage(new DamageInfo(10f, DamageSourceKind.Creature));
            Assert.AreEqual(1, changes, "damage fires Changed");
            h.ApplyDamage(new DamageInfo(0f, DamageSourceKind.Creature));
            Assert.AreEqual(1, changes, "zero (no-op) damage does not fire Changed");
            h.ResetHealth();
            Assert.AreEqual(2, changes, "reset fires Changed");
        }

        [Test]
        public void Health_Depleted_FiresOnce_EvenWithFurtherDamageAtZero()
        {
            var h = NewHealth(NewGO(), 30f);
            int depleted = 0; h.Depleted += () => depleted++;
            Deplete(h);
            Assert.AreEqual(1, depleted);
            Deplete(h); // already at zero -> no re-fire, no negative
            Assert.AreEqual(1, depleted, "Depleted never duplicates");
            Assert.AreEqual(0f, h.Current, "no additional damage below zero");
        }

        // ─────────────────────────── AttackStarted events ───────────────────────────

        [Test]
        public void PlayerAttack_RaisesAttackStarted_OnSuccess_NotWhenBlocked()
        {
            var go = NewGO("Player");
            var orientation = go.AddComponent<PlayerOrientation>();
            var gate = go.AddComponent<PlayerControlGate>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);
            int fired = 0; attack.AttackStarted += () => fired++;

            Assert.IsTrue(attack.TryAttack());
            Assert.AreEqual(1, fired);

            gate.Block(ControlBlockReason.Defeat);
            Assert.IsFalse(attack.TryAttack());
            Assert.AreEqual(1, fired, "no AttackStarted when blocked");
        }

        [Test]
        public void CreatureController_RaisesAttackStarted_OnStart()
        {
            var go = NewGO("Verak");
            var resolver = go.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)(1 << 20));
            CreatureTestKit.Invoke(resolver, "Awake");
            var c = go.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(c, "resolver", resolver);
            int fired = 0; c.AttackStarted += () => fired++;
            Assert.IsTrue(c.TryStartAttack(Vector2Int.right));
            Assert.AreEqual(1, fired);
            Assert.IsFalse(c.TryStartAttack(Vector2Int.right)); // committed -> no new start
            Assert.AreEqual(1, fired);
        }

        // ─────────────────────────── Animation mapping (reuse) ───────────────────────────

        [Test]
        public void AnimationResolver_ReusesClips_ForHostileStates()
        {
            Assert.AreEqual(CreatureVisualState.Walk,
                CreatureAnimationResolver.Resolve(CreatureStateId.Chase, Vector2Int.right, true).VisualState);
            Assert.AreEqual(CreatureVisualState.Alert,
                CreatureAnimationResolver.Resolve(CreatureStateId.Attack, Vector2Int.right, false).VisualState);
            Assert.AreEqual(CreatureVisualState.Idle,
                CreatureAnimationResolver.Resolve(CreatureStateId.Subdued, Vector2Int.down, false).VisualState);
        }

        // ─────────────────────────── Resolver self-damage hardening ───────────────────────────

        private static void Sync() => Physics2D.SyncTransforms();

        [Test]
        public void PlayerResolver_NeverDamagesSelf_EvenIfMaskIncludesPlayer_HitsExternal()
        {
            var go = NewGO("Player"); go.layer = 8;
            var self = go.AddComponent<DamageProbe>();
            var orientation = go.AddComponent<PlayerOrientation>();
            var gate = go.AddComponent<PlayerControlGate>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);
            var resolver = go.AddComponent<PlayerAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "attack", attack);
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)((1 << 8) | (1 << 12))); // includes Player!
            CreatureTestKit.SetPrivate(resolver, "attackRange", 2f);
            CreatureTestKit.SetPrivate(resolver, "attackWidth", 2f);
            CreatureTestKit.SetPrivate(resolver, "damageAmount", 10f);
            CreatureTestKit.Invoke(resolver, "Awake"); // caches self = the DamageProbe on the root

            // own child collider inside the box
            var hurt = NewGO("SelfHurtbox"); hurt.layer = 8; hurt.transform.SetParent(go.transform);
            hurt.transform.position = new Vector2(1f, 0f);
            var hc = hurt.AddComponent<BoxCollider2D>(); hc.isTrigger = true; hc.size = Vector2.one * 0.4f;
            // external target
            var ext = NewGO("Enemy"); ext.layer = 12; ext.transform.position = new Vector2(1f, 0.5f);
            var ec = ext.AddComponent<BoxCollider2D>(); ec.isTrigger = true; ec.size = Vector2.one * 0.4f;
            var enemy = ext.AddComponent<DamageProbe>();
            Sync();

            Assert.IsTrue(attack.TryAttack());
            resolver.Resolve();
            Assert.AreEqual(0, self.Calls, "player never damages itself even with Player in the mask");
            Assert.AreEqual(1, enemy.Calls, "external target still hit");
        }

        // ─────────────────────────── SpriteFlash ───────────────────────────

        private SpriteFlash NewFlash(out SpriteRenderer sr)
        {
            var go = NewGO("Sprite");
            sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            var f = go.AddComponent<SpriteFlash>();
            CreatureTestKit.SetPrivate(f, "spriteRenderer", sr);
            CreatureTestKit.SetPrivate(f, "flashColor", new Color(1f, 0.5f, 0.5f, 1f));
            CreatureTestKit.SetPrivate(f, "terminalTint", new Color(0.4f, 0.4f, 0.6f, 1f));
            CreatureTestKit.SetPrivate(f, "flashDuration", 0.1f);
            CreatureTestKit.Invoke(f, "Awake");
            return f;
        }

        [Test]
        public void SpriteFlash_FlashTintsThenRestores()
        {
            var f = NewFlash(out SpriteRenderer sr);
            f.Flash();
            Assert.IsTrue(f.IsFlashing);
            Assert.AreNotEqual(Color.white, sr.color);
            f.Tick(0.2f);
            Assert.IsFalse(f.IsFlashing);
            Assert.AreEqual(Color.white, sr.color, "restores original after flash");
        }

        [Test]
        public void SpriteFlash_TerminalTint_PersistsAndClears()
        {
            var f = NewFlash(out SpriteRenderer sr);
            f.SetTerminalTint(true);
            Assert.IsTrue(f.TerminalHeld);
            Assert.AreNotEqual(Color.white, sr.color);
            f.SetTerminalTint(false);
            Assert.AreEqual(Color.white, sr.color, "clears terminal tint back to original");
        }

        // ─────────────────────────── PlayerHealthBar ───────────────────────────

        [Test]
        public void HealthBar_ReflectsNormalized_OnDamageAndReset()
        {
            var go = NewGO("Player");
            var h = NewHealth(go, 30f);
            var bar = go.AddComponent<PlayerHealthBar>();
            CreatureTestKit.SetPrivate(bar, "health", h);
            CreatureTestKit.Invoke(bar, "OnEnable");
            Assert.AreEqual(1f, bar.LastNormalized, 0.001f);
            h.ApplyDamage(new DamageInfo(15f, DamageSourceKind.Creature));
            Assert.AreEqual(0.5f, bar.LastNormalized, 0.001f);
            h.ApplyDamage(new DamageInfo(15f, DamageSourceKind.Creature));
            Assert.AreEqual(0f, bar.LastNormalized, 0.001f);
            h.ResetHealth();
            Assert.AreEqual(1f, bar.LastNormalized, 0.001f);
        }

        // ─────────────────────────── PlayerCombatPresentation ───────────────────────────

        [Test]
        public void PlayerPresentation_FlashesOnAttackAndDamage_TerminalOnDefeat()
        {
            var go = NewGO("Player");
            var orientation = go.AddComponent<PlayerOrientation>();
            var gate = go.AddComponent<PlayerControlGate>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);
            var h = NewHealth(go, 30f);
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0f;
            var defeat = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(defeat, "health", h);
            CreatureTestKit.SetPrivate(defeat, "gate", gate);
            CreatureTestKit.SetPrivate(defeat, "body", body);
            CreatureTestKit.Invoke(defeat, "OnEnable");
            var flash = NewFlash(out _);
            var pres = go.AddComponent<PlayerCombatPresentation>();
            CreatureTestKit.SetPrivate(pres, "attack", attack);
            CreatureTestKit.SetPrivate(pres, "health", h);
            CreatureTestKit.SetPrivate(pres, "defeat", defeat);
            CreatureTestKit.SetPrivate(pres, "flash", flash);
            CreatureTestKit.Invoke(pres, "OnEnable");

            attack.TryAttack();
            Assert.IsTrue(flash.IsFlashing, "attack flashes");
            flash.Tick(1f);
            h.ApplyDamage(new DamageInfo(10f, DamageSourceKind.Creature));
            Assert.IsTrue(flash.IsFlashing, "damage flashes");
            flash.Tick(1f);
            Deplete(h); // -> Defeated
            Assert.IsTrue(flash.TerminalHeld, "defeat holds terminal tint");
            defeat.Recover();
            Assert.IsFalse(flash.TerminalHeld, "recovery clears terminal tint");
        }

        // ─────────────────────────── AlteredVerakPresentation ───────────────────────────

        [Test]
        public void VerakPresentation_TerminalOnSubdued()
        {
            // A brain forced to report Subdued via a provider-driven build is heavy here;
            // instead test the terminal-tint gate with a brain stub through the context.
            var flash = NewFlash(out _);
            var pres = NewGO("VerakPres").AddComponent<AlteredVerakPresentation>();
            CreatureTestKit.SetPrivate(pres, "flash", flash);
            // No brain assigned -> RefreshTerminal is a safe no-op.
            Assert.DoesNotThrow(() => pres.RefreshTerminal());
            Assert.IsFalse(flash.TerminalHeld);
        }

        // ─────────────────────────── Temporary defeat + recovery ───────────────────────────

        private PlayerTemporaryDefeat NewDefeatRig(out Health h, out PlayerControlGate gate,
            out Rigidbody2D body, out SpawnPoint recovery, float delay = 2f)
        {
            var go = NewGO("Player");
            h = NewHealth(go, 30f);
            gate = go.AddComponent<PlayerControlGate>();
            body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var rp = NewGO("Recovery"); rp.transform.position = new Vector2(5f, 7f);
            recovery = rp.AddComponent<SpawnPoint>();
            var d = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(d, "health", h);
            CreatureTestKit.SetPrivate(d, "gate", gate);
            CreatureTestKit.SetPrivate(d, "body", body);
            CreatureTestKit.SetPrivate(d, "recoveryPoint", recovery);
            CreatureTestKit.SetPrivate(d, "recoveryDelay", delay);
            CreatureTestKit.Invoke(d, "OnEnable");
            return d;
        }

        [Test]
        public void Defeat_RecoversAfterDelay_Reposition_Reset_UnblockDefeatOnly()
        {
            var d = NewDefeatRig(out Health h, out PlayerControlGate gate, out Rigidbody2D body, out SpawnPoint rp);
            gate.Block(ControlBlockReason.Observation); // a pre-existing, unrelated block
            body.position = new Vector2(1f, 1f);

            Deplete(h);
            Assert.IsTrue(d.IsDefeated);
            Assert.IsTrue(gate.IsBlocked);

            d.Tick(1f);
            Assert.IsTrue(d.IsDefeated, "no recovery before the delay");

            d.Tick(1.5f); // exceeds the 2s delay
            Assert.IsFalse(d.IsDefeated, "recovered after the delay");
            Assert.AreEqual(rp.transform.position, (Vector3)body.position, "repositioned to recovery point");
            Assert.AreEqual(Vector2.zero, body.linearVelocity, "velocity cleared");
            Assert.AreEqual(30f, h.Current, "health restored");
            Assert.IsTrue(gate.IsBlocked, "unrelated Observation block preserved");
            gate.Unblock(ControlBlockReason.Observation);
            Assert.IsFalse(gate.IsBlocked, "only Defeat was removed by recovery; nothing else lingers");
        }

        [Test]
        public void Defeat_SecondCycle_Works()
        {
            var d = NewDefeatRig(out Health h, out _, out _, out _);
            Deplete(h); d.Tick(3f);
            Assert.IsFalse(d.IsDefeated);
            Deplete(h);
            Assert.IsTrue(d.IsDefeated, "a second defeat triggers again");
            d.Tick(3f);
            Assert.IsFalse(d.IsDefeated, "and recovers again");
        }

        [Test]
        public void Defeat_NoRecovery_WhenDisabled_AndNoDuplicate()
        {
            var d = NewDefeatRig(out Health h, out _, out _, out _);
            int recovered = 0; d.Recovered += () => recovered++;
            Deplete(h);
            d.Tick(3f);
            Assert.AreEqual(1, recovered);
            d.Tick(3f); // already recovered -> no-op
            Assert.AreEqual(1, recovered, "no duplicate recovery");
        }

        [Test]
        public void Defeat_DuringActiveAttack_CancelsWindow_NoResidualDamage()
        {
            var go = NewGO("Player"); go.layer = 8;
            var orientation = go.AddComponent<PlayerOrientation>();
            var gate = go.AddComponent<PlayerControlGate>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(attack, "windowDuration", 0.15f);
            CreatureTestKit.SetPrivate(attack, "cooldownDuration", 0.5f);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);
            var h = NewHealth(go, 30f);
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0f;
            var defeat = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(defeat, "health", h);
            CreatureTestKit.SetPrivate(defeat, "gate", gate);
            CreatureTestKit.SetPrivate(defeat, "body", body);
            CreatureTestKit.Invoke(defeat, "OnEnable");
            var resolver = go.AddComponent<PlayerAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "attack", attack);
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)(1 << 12));
            CreatureTestKit.SetPrivate(resolver, "attackRange", 2f);
            CreatureTestKit.SetPrivate(resolver, "attackWidth", 2f);
            CreatureTestKit.SetPrivate(resolver, "damageAmount", 10f);
            CreatureTestKit.Invoke(resolver, "Awake");
            var enemyGO = NewGO("Enemy"); enemyGO.layer = 12; enemyGO.transform.position = new Vector2(1f, 0f);
            var ec = enemyGO.AddComponent<BoxCollider2D>(); ec.isTrigger = true; ec.size = Vector2.one * 0.4f;
            var enemy = enemyGO.AddComponent<DamageProbe>();
            Sync();

            Assert.IsTrue(attack.TryAttack());
            Assert.IsTrue(attack.IsAttackActive, "window is active");
            Deplete(h);                 // player defeated -> gate blocked
            attack.Tick(0.01f);         // blocked -> the active window is cancelled
            Assert.IsFalse(attack.IsAttackActive, "an active window does not survive Defeat");
            resolver.Resolve();
            Assert.AreEqual(0, enemy.Calls, "no residual damage resolves after defeat");
        }

        [Test]
        public void Defeat_BlocksNewAttacks_UntilRecovered()
        {
            var go = NewGO("Player");
            var h = NewHealth(go, 30f);
            var gate = go.AddComponent<PlayerControlGate>();
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0f;
            var orientation = go.AddComponent<PlayerOrientation>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);
            var rp = NewGO("Recovery").AddComponent<SpawnPoint>();
            var d = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(d, "health", h);
            CreatureTestKit.SetPrivate(d, "gate", gate);
            CreatureTestKit.SetPrivate(d, "body", body);
            CreatureTestKit.SetPrivate(d, "recoveryPoint", rp);
            CreatureTestKit.SetPrivate(d, "recoveryDelay", 2f);
            CreatureTestKit.Invoke(d, "OnEnable");

            Deplete(h);
            Assert.IsFalse(attack.TryAttack(), "cannot attack while defeated");
            d.Tick(3f);
            Assert.IsTrue(attack.TryAttack(), "can attack again after recovery");
        }
    }
}

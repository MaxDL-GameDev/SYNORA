using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Synora.Data;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Player;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// Reinforced robustness coverage for M5 Fase 6: window-never-skipped on large
    /// deltaTime, self-damage exclusion, Subdued priority + cancellation, Health
    /// subscription cycles, gate flags, multi-creature independence, and observation
    /// mappings.
    /// </summary>
    public sealed class AlteredVerakRobustnessTests
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

        private sealed class DamageProbe : MonoBehaviour, IDamageable
        {
            public int Calls { get; private set; }
            public DamageInfo Last { get; private set; }
            public void ApplyDamage(in DamageInfo damage) { Calls++; Last = damage; }
        }

        private static void Sync() => Physics2D.SyncTransforms();

        private DamageProbe NewTarget(Vector2 pos, int layer = 8)
        {
            var go = new GameObject("Target") { layer = layer };
            temp.Add(go);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one * 0.4f;
            return go.AddComponent<DamageProbe>();
        }

        private CreatureAttackHitResolver NewResolver(GameObject go, int mask = 1 << 8,
            float range = 2f, float width = 2f, float amount = 4f)
        {
            var resolver = go.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)mask);
            CreatureTestKit.SetPrivate(resolver, "attackRange", range);
            CreatureTestKit.SetPrivate(resolver, "attackWidth", width);
            CreatureTestKit.SetPrivate(resolver, "damageAmount", amount);
            CreatureTestKit.Invoke(resolver, "Awake");
            return resolver;
        }

        private CreatureAttackController NewController(GameObject go, CreatureAttackHitResolver resolver,
            float windup = 0.2f, float active = 0.2f, float cooldown = 0.4f)
        {
            var c = go.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(c, "resolver", resolver);
            CreatureTestKit.SetPrivate(c, "windupDuration", windup);
            CreatureTestKit.SetPrivate(c, "activeDuration", active);
            CreatureTestKit.SetPrivate(c, "cooldownDuration", cooldown);
            return c;
        }

        private Health NewHealth(GameObject go, float max = 3f)
        {
            var h = go.AddComponent<Health>();
            CreatureTestKit.SetPrivate(h, "maxHealth", max);
            h.ResetHealth();
            return h;
        }

        // ─────────────────────────── §7 large deltaTime ───────────────────────────

        [Test]
        public void HugeDelta_StillLandsExactlyOneHit()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var resolver = NewResolver(go);
            var controller = NewController(go, resolver);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();

            Assert.IsTrue(controller.TryStartAttack(Vector2Int.right));
            controller.Tick(100f); // one absurdly large step
            Assert.AreEqual(1, probe.Calls, "A positive active window must never be skipped by a large deltaTime.");
        }

        [Test]
        public void ZeroActiveDuration_NeverDamages()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var resolver = NewResolver(go);
            var controller = NewController(go, resolver, windup: 0.1f, active: 0f, cooldown: 0.2f);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();

            controller.TryStartAttack(Vector2Int.right);
            controller.Tick(100f);
            Assert.AreEqual(0, probe.Calls, "A zero-length active window deals no damage.");
        }

        // ─────────────────────────── §8 self-damage ───────────────────────────

        [Test]
        public void Resolver_NeverDamagesOwnDamageable_EvenIfMaskIncludesSelf()
        {
            var go = new GameObject("Creature") { layer = 8 }; // own layer inside the mask
            temp.Add(go);
            go.transform.position = Vector3.zero;
            var selfHealth = go.AddComponent<DamageProbe>(); // the attacker's own IDamageable
            var resolver = NewResolver(go, mask: 1 << 8);

            // Own hurtbox collider, child of the attacker, inside the attack box.
            var hurtbox = new GameObject("OwnHurtbox") { layer = 8 };
            temp.Add(hurtbox);
            hurtbox.transform.SetParent(go.transform);
            hurtbox.transform.position = new Vector2(1f, 0f);
            var hcol = hurtbox.AddComponent<BoxCollider2D>();
            hcol.isTrigger = true;
            hcol.size = Vector2.one * 0.4f;

            DamageProbe enemy = NewTarget(new Vector2(1f, 0.5f)); // a valid external target
            Sync();

            resolver.BeginWindow();
            resolver.ResolveHits(Vector2Int.right);

            Assert.AreEqual(0, selfHealth.Calls, "The attacker must never damage itself.");
            Assert.AreEqual(1, enemy.Calls, "A valid external target is still hit.");
        }

        // ─────────────────────────── §6 cancel / no reopen ───────────────────────────

        [Test]
        public void Cancel_IsIdempotent_AndPreventsReopen()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var resolver = NewResolver(go);
            var controller = NewController(go, resolver);
            DamageProbe probe = NewTarget(new Vector2(1f, 0f));
            Sync();

            controller.Cancel();               // cancel while Ready: no-op
            Assert.IsTrue(controller.CanStart);
            controller.TryStartAttack(Vector2Int.right);
            controller.Cancel();
            controller.Cancel();               // idempotent
            Assert.IsTrue(controller.CanStart);

            // No window can reopen without a fresh TryStartAttack.
            for (int i = 0; i < 5; i++) controller.Tick(0.2f);
            Assert.AreEqual(0, probe.Calls, "A cancelled attack never reopens its window.");
        }

        // ─────────────────────────── §5 Subdued priority ───────────────────────────

        [Test]
        public void AttackState_EnterWhileDepleted_DoesNotStartAttack()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var resolver = NewResolver(go);
            var controller = NewController(go, resolver);
            Health health = NewHealth(go);
            health.ApplyDamage(new DamageInfo(99f, DamageSourceKind.Player));

            var ctx = new CreatureContext(CreatureTestKit.NewIdentity(temp), go.transform,
                new List<Transform>(), null, null);
            ctx.SetFacing(Vector2Int.right);
            var attack = new CreatureAttackState(health, controller);
            attack.Enter(ctx);
            Assert.IsFalse(controller.IsSequenceActive, "Depleted on entry: no attack starts.");
            Assert.AreEqual(CreatureStateId.Subdued, attack.Tick(ctx, 0.1f));
        }

        [Test]
        public void DepletedDuringActiveWindow_StopsFurtherDamage()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var resolver = NewResolver(go, mask: 1 << 9); // hit "player" layer 9, not self
            var controller = NewController(go, resolver);
            Health health = NewHealth(go);
            DamageProbe player = NewTarget(new Vector2(1f, 0f), layer: 9);
            Sync();

            var ctx = new CreatureContext(CreatureTestKit.NewIdentity(temp), go.transform,
                new List<Transform>(), null, null);
            ctx.SetFacing(Vector2Int.right);
            var attack = new CreatureAttackState(health, controller);
            attack.Enter(ctx);
            controller.Tick(0.2f); // open the window
            controller.Tick(0.05f); // active: one hit lands
            int hitsBefore = player.Calls;
            Assert.AreEqual(1, hitsBefore);

            health.ApplyDamage(new DamageInfo(99f, DamageSourceKind.Player)); // neutralized
            Assert.AreEqual(CreatureStateId.Subdued, attack.Tick(ctx, 0.05f)); // cancels the window
            controller.Tick(0.05f); // any further ticks must not damage
            Assert.AreEqual(hitsBefore, player.Calls, "No impact resolves after depletion.");
        }

        // ─────────────────────────── §9 facing ───────────────────────────

        [Test]
        public void TryStartAttack_ZeroFacing_Rejected()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var resolver = NewResolver(go);
            var controller = NewController(go, resolver);
            Assert.IsFalse(controller.TryStartAttack(Vector2Int.zero));
            Assert.IsTrue(controller.CanStart, "A rejected start leaves the controller ready.");
        }

        // ─────────────────────────── §10 hostile vs ambient Alert ───────────────────────────

        [Test]
        public void HostileAlert_SharesAmbientContract_DiffersOnlyInTransition()
        {
            CreatureIdentity identity = CreatureTestKit.NewIdentity(temp);
            var ambientCtx = NewMovingContext(identity, out CreatureMovement ambientMove);
            var hostileCtx = NewMovingContext(identity, out CreatureMovement hostileMove);
            ambientMove.SetDestination(new Vector2(5f, 0f));
            hostileMove.SetDestination(new Vector2(5f, 0f));
            ambientCtx.SetFacing(Vector2Int.left);
            hostileCtx.SetFacing(Vector2Int.left);

            var ambient = new AlertState();
            var hostile = new CreatureHostileAlertState(NewHealthOnNewGo(), 0f);
            ambient.Enter(ambientCtx);
            hostile.Enter(hostileCtx);

            // Shared contract: preserves entry facing, stops moving, no attack on entry.
            Assert.AreEqual(Vector2Int.left, ambientCtx.Facing);
            Assert.AreEqual(Vector2Int.left, hostileCtx.Facing);
            Assert.IsFalse(ambientMove.HasDestination);
            Assert.IsFalse(hostileMove.HasDestination);

            // Differ only in the destination transition: hostile pursues the player.
            hostileCtx.SetDetectedPlayer(NewTransformAt(new Vector2(2f, 0f)));
            Assert.AreEqual(CreatureStateId.Chase, hostile.Tick(hostileCtx, 0.1f));
        }

        // ─────────────────────────── §11 player defeat blocks input ───────────────────────────

        [Test]
        public void Defeat_BlocksNewAttacks_AndCoexistsWithObservation()
        {
            var go = new GameObject("Player");
            temp.Add(go);
            Health health = NewHealth(go);
            var gate = go.AddComponent<PlayerControlGate>();
            gate.Block(ControlBlockReason.Observation); // pre-existing reason
            var orientation = go.AddComponent<PlayerOrientation>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);
            var defeat = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(defeat, "health", health);
            CreatureTestKit.SetPrivate(defeat, "gate", gate);
            CreatureTestKit.Invoke(defeat, "OnEnable");

            health.ApplyDamage(new DamageInfo(99f, DamageSourceKind.Creature));
            Assert.IsTrue(defeat.IsDefeated);
            Assert.IsFalse(attack.TryAttack(), "No new attack can start while defeated (gate blocked).");

            gate.Unblock(ControlBlockReason.Defeat);
            Assert.IsTrue(gate.IsBlocked, "Removing Defeat preserves the pre-existing Observation block.");
            gate.Unblock(ControlBlockReason.Observation);
            Assert.IsFalse(gate.IsBlocked);
        }

        // ─────────────────────────── §12 gate flags regression ───────────────────────────

        [Test]
        public void Gate_Flags_AreIndependentBits()
        {
            var go = new GameObject("Gate");
            temp.Add(go);
            var gate = go.AddComponent<PlayerControlGate>();
            Assert.IsFalse(gate.IsBlocked);
            gate.Block(ControlBlockReason.Observation);
            gate.Block(ControlBlockReason.Observation); // idempotent
            gate.Block(ControlBlockReason.Defeat);
            Assert.IsTrue(gate.IsBlocked);
            gate.Unblock(ControlBlockReason.Observation);
            Assert.IsTrue(gate.IsBlocked, "Defeat still active.");
            gate.Unblock(ControlBlockReason.Defeat);
            Assert.IsFalse(gate.IsBlocked, "Removing the last reason unblocks.");
        }

        // ─────────────────────────── §13 multiple creatures ───────────────────────────

        [Test]
        public void TwoCreatures_HaveIndependentAttackState()
        {
            var goA = new GameObject("A"); temp.Add(goA);
            var goB = new GameObject("B"); temp.Add(goB);
            var a = NewController(goA, NewResolver(goA));
            var b = NewController(goB, NewResolver(goB));

            a.TryStartAttack(Vector2Int.right);
            b.TryStartAttack(Vector2Int.left);
            Assert.IsFalse(a.CanStart);
            Assert.IsFalse(b.CanStart);

            a.Cancel();
            Assert.IsTrue(a.CanStart, "A is cancelled.");
            Assert.IsFalse(b.CanStart, "B is unaffected by A's cancellation.");
            Assert.AreEqual(Vector2Int.left, b.CapturedFacing, "Captured facings are independent.");
        }

        // ─────────────────────────── §4 subscription cycle ───────────────────────────

        [Test]
        public void Defeat_EnableDisableEnable_StillFiresOnce()
        {
            var go = new GameObject("Player");
            temp.Add(go);
            Health health = NewHealth(go);
            var gate = go.AddComponent<PlayerControlGate>();
            var defeat = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(defeat, "health", health);
            CreatureTestKit.SetPrivate(defeat, "gate", gate);

            CreatureTestKit.Invoke(defeat, "OnEnable");
            CreatureTestKit.Invoke(defeat, "OnDisable");
            CreatureTestKit.Invoke(defeat, "OnEnable"); // re-subscribed exactly once
            health.ApplyDamage(new DamageInfo(99f, DamageSourceKind.Creature));
            Assert.IsTrue(defeat.IsDefeated);
            Assert.IsTrue(gate.IsBlocked);
        }

        // ─────────────────────────── §3 brain provider guards ───────────────────────────

        [Test]
        public void Brain_ProviderNotImplementingInterface_NotInitialized()
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CreatureIdentity identity = CreatureTestKit.NewIdentity(temp);
            var movement = go.AddComponent<CreatureMovement>();
            CreatureTestKit.SetPrivate(movement, "body", body);
            CreatureTestKit.SetPrivate(movement, "identity", identity);
            var sensor = go.AddComponent<CreatureSensor>();
            CreatureTestKit.SetPrivate(sensor, "identity", identity);
            CreatureTestKit.SetPrivate(sensor, "playerLayer", (LayerMask)(1 << 8));
            CreatureTestKit.Invoke(sensor, "Awake");
            var brain = go.AddComponent<CreatureBrain>();
            CreatureTestKit.SetPrivate(brain, "identity", identity);
            CreatureTestKit.SetPrivate(brain, "movement", movement);
            CreatureTestKit.SetPrivate(brain, "sensor", sensor);
            CreatureTestKit.SetPrivate(brain, "root", go.transform);
            // A MonoBehaviour that is NOT an ICreatureStateProvider.
            CreatureTestKit.SetPrivate(brain, "stateProvider", movement);

            LogAssert.Expect(LogType.Warning, new Regex("ICreatureStateProvider"));
            brain.Initialize();
            Assert.IsFalse(brain.IsInitialized, "A bad provider must not silently initialize.");
        }

        [Test]
        public void NormalBrain_TransitionToUnregisteredCombatState_Ignored()
        {
            CreatureBrain brain = CreatureTestKit.BuildBrain(temp,
                CreatureTestKit.NewIdentity(temp), new Transform[0], out _, out _);
            brain.Initialize();
            LogAssert.Expect(LogType.Warning, new Regex("unknown state"));
            brain.RequestTransition(CreatureStateId.Chase); // not registered for an ambient creature
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId, "Unregistered transition is ignored.");
        }

        // ─────────────────────────── Reload-transient guard (F8) ───────────────────────────

        [Test]
        public void Brain_Tick_WithNullCurrent_DoesNotThrow()
        {
            CreatureBrain brain = CreatureTestKit.BuildBrain(temp,
                CreatureTestKit.NewIdentity(temp), new Transform[0], out _, out _);
            brain.Initialize();
            // Simulate a half-reloaded instance: current cleared while still "initialized".
            CreatureTestKit.SetPrivate(brain, "current", null);
            Assert.DoesNotThrow(() => brain.Tick(0.1f), "a half-initialized tick must be a safe no-op");
        }

        // ─────────────────────────── §15 observation mappings ───────────────────────────

        [Test]
        public void CombatStates_MapToExistingObservationCategories()
        {
            Assert.AreEqual(CreatureObservationState.Watchful, CreatureObservationSource.Resolve(CreatureStateId.Chase));
            Assert.AreEqual(CreatureObservationState.Watchful, CreatureObservationSource.Resolve(CreatureStateId.Attack));
            Assert.AreEqual(CreatureObservationState.Calm, CreatureObservationSource.Resolve(CreatureStateId.Subdued));
        }

        // ── helpers ──

        private CreatureContext NewMovingContext(CreatureIdentity identity, out CreatureMovement movement)
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            movement = go.AddComponent<CreatureMovement>();
            CreatureTestKit.SetPrivate(movement, "body", body);
            CreatureTestKit.SetPrivate(movement, "identity", identity);
            var ctx = new CreatureContext(identity, go.transform, new List<Transform>(), movement, null);
            movement.Initialize(ctx);
            return ctx;
        }

        private Health NewHealthOnNewGo()
        {
            var go = new GameObject("Health");
            temp.Add(go);
            return NewHealth(go);
        }

        private Transform NewTransformAt(Vector2 pos)
        {
            var go = new GameObject("T");
            temp.Add(go);
            go.transform.position = pos;
            return go.transform;
        }
    }
}

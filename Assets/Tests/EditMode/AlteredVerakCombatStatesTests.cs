using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    /// <summary>
    /// EditMode coverage of the Altered Verak's behavior: the new states as units (with
    /// a hand-built CreatureContext) plus CreatureBrain integration proving the provider
    /// hosts the hostile set and Alert leads into Chase. Health-zero → Subdued priority
    /// is verified from the combat states (the states self-check).
    /// </summary>
    public sealed class AlteredVerakCombatStatesTests
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

        // ── builders ──

        private CreatureContext NewContext(out CreatureMovement movement, out Transform root)
        {
            var go = new GameObject("Creature");
            temp.Add(go);
            root = go.transform;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CreatureIdentity identity = CreatureTestKit.NewIdentity(temp);
            movement = go.AddComponent<CreatureMovement>();
            CreatureTestKit.SetPrivate(movement, "body", body);
            CreatureTestKit.SetPrivate(movement, "identity", identity);
            var ctx = new CreatureContext(identity, root, new List<Transform>(), movement, null);
            movement.Initialize(ctx);
            return ctx;
        }

        private Transform NewTransform(Vector2 pos)
        {
            var go = new GameObject("T");
            temp.Add(go);
            go.transform.position = pos;
            return go.transform;
        }

        private Health NewHealth(float max = 3f, bool deplete = false)
        {
            var go = new GameObject("Health");
            temp.Add(go);
            var h = go.AddComponent<Health>();
            CreatureTestKit.SetPrivate(h, "maxHealth", max);
            h.ResetHealth(); // Awake does not run on AddComponent in EditMode
            if (deplete)
            {
                h.ApplyDamage(new DamageInfo(max + 1f, DamageSourceKind.Player));
            }
            return h;
        }

        private CreatureAttackController NewController(out CreatureAttackHitResolver resolver,
            float windup = 0.2f, float active = 0.2f, float cooldown = 0.4f)
        {
            var go = new GameObject("Attack");
            temp.Add(go);
            resolver = go.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)(1 << 20)); // unused: no hits
            CreatureTestKit.Invoke(resolver, "Awake");
            var c = go.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(c, "resolver", resolver);
            CreatureTestKit.SetPrivate(c, "windupDuration", windup);
            CreatureTestKit.SetPrivate(c, "activeDuration", active);
            CreatureTestKit.SetPrivate(c, "cooldownDuration", cooldown);
            return c;
        }

        // ─────────────────────────── Hostile Alert ───────────────────────────

        [Test]
        public void Alert_PreservesEntryFacing()
        {
            CreatureContext ctx = NewContext(out _, out _);
            ctx.SetFacing(Vector2Int.right);
            var alert = new CreatureHostileAlertState(NewHealth());
            alert.Enter(ctx);
            Assert.AreEqual(Vector2Int.right, ctx.Facing, "Alert must not snap facing.");
        }

        [Test]
        public void Alert_PlayerPresent_RequestsChase()
        {
            CreatureContext ctx = NewContext(out _, out _);
            ctx.SetDetectedPlayer(NewTransform(new Vector2(2f, 0f)));
            var alert = new CreatureHostileAlertState(NewHealth());
            Assert.AreEqual(CreatureStateId.Chase, alert.Tick(ctx, 0.1f));
        }

        [Test]
        public void Alert_PlayerLost_ReturnsIdle()
        {
            CreatureContext ctx = NewContext(out _, out _);
            ctx.ClearDetectedPlayer();
            var alert = new CreatureHostileAlertState(NewHealth());
            Assert.AreEqual(CreatureStateId.Idle, alert.Tick(ctx, 0.1f));
        }

        [Test]
        public void Alert_HealthZero_RequestsSubdued()
        {
            CreatureContext ctx = NewContext(out _, out _);
            ctx.SetDetectedPlayer(NewTransform(Vector2.zero));
            var alert = new CreatureHostileAlertState(NewHealth(deplete: true));
            Assert.AreEqual(CreatureStateId.Subdued, alert.Tick(ctx, 0.1f));
        }

        // ─────────────────────────── Chase ───────────────────────────

        [Test]
        public void Chase_OutOfRange_MovesTowardPlayer()
        {
            CreatureContext ctx = NewContext(out CreatureMovement movement, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.SetDetectedPlayer(NewTransform(new Vector2(5f, 0f)));
            var chase = new CreatureChaseState(NewHealth(), controller, 1f);
            Assert.IsNull(chase.Tick(ctx, 0.1f));
            Assert.IsTrue(movement.HasDestination, "Out of range: chase toward the player.");
        }

        [Test]
        public void Chase_FacesPlayer()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.SetDetectedPlayer(NewTransform(new Vector2(0f, 5f)));
            var chase = new CreatureChaseState(NewHealth(), controller, 1f);
            chase.Tick(ctx, 0.1f);
            Assert.AreEqual(Vector2Int.up, ctx.Facing);
        }

        [Test]
        public void Chase_InRange_ControllerReady_RequestsAttack()
        {
            CreatureContext ctx = NewContext(out CreatureMovement movement, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.SetDetectedPlayer(NewTransform(new Vector2(0.5f, 0f)));
            var chase = new CreatureChaseState(NewHealth(), controller, 1f);
            Assert.AreEqual(CreatureStateId.Attack, chase.Tick(ctx, 0.1f));
            Assert.IsFalse(movement.HasDestination, "In range: stop before attacking.");
        }

        [Test]
        public void Chase_InRange_OnCooldown_Waits()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            controller.TryStartAttack(Vector2Int.right); // now committed -> not ready
            ctx.SetDetectedPlayer(NewTransform(new Vector2(0.5f, 0f)));
            var chase = new CreatureChaseState(NewHealth(), controller, 1f);
            Assert.IsNull(chase.Tick(ctx, 0.1f), "In range but not ready: hold, do not spam Attack.");
        }

        [Test]
        public void Chase_PlayerLost_ReturnsIdle()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.ClearDetectedPlayer();
            var chase = new CreatureChaseState(NewHealth(), controller, 1f);
            Assert.AreEqual(CreatureStateId.Idle, chase.Tick(ctx, 0.1f));
        }

        [Test]
        public void Chase_HealthZero_RequestsSubdued()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.SetDetectedPlayer(NewTransform(new Vector2(0.5f, 0f)));
            var chase = new CreatureChaseState(NewHealth(deplete: true), controller, 1f);
            Assert.AreEqual(CreatureStateId.Subdued, chase.Tick(ctx, 0.1f));
        }

        // ─────────────────────────── Attack ───────────────────────────

        [Test]
        public void Attack_Enter_StartsSequence_CapturesFacing_Stops()
        {
            CreatureContext ctx = NewContext(out CreatureMovement movement, out _);
            movement.SetDestination(new Vector2(3f, 0f));
            CreatureAttackController controller = NewController(out _);
            ctx.SetFacing(Vector2Int.right);
            var attack = new CreatureAttackState(NewHealth(), controller);
            attack.Enter(ctx);
            Assert.IsTrue(controller.IsSequenceActive);
            Assert.AreEqual(Vector2Int.right, controller.CapturedFacing);
            Assert.IsFalse(movement.HasDestination, "Attack stops movement on entry.");
        }

        [Test]
        public void Attack_KeepsCapturedFacing_WhenCreatureTurns()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.SetFacing(Vector2Int.right);
            var attack = new CreatureAttackState(NewHealth(), controller);
            attack.Enter(ctx);
            ctx.SetFacing(Vector2Int.up); // facing changes after the attack started
            Assert.AreEqual(Vector2Int.right, controller.CapturedFacing);
        }

        [Test]
        public void Attack_StaysWhileSequenceActive_ThenReturnsChase()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _, windup: 0.2f, active: 0.2f, cooldown: 0.4f);
            ctx.SetFacing(Vector2Int.right);
            var attack = new CreatureAttackState(NewHealth(), controller);
            attack.Enter(ctx);
            Assert.IsNull(attack.Tick(ctx, 0.1f), "Does not leave while the sequence runs (no chasing).");

            controller.Tick(0.2f); // finish windup
            controller.Tick(0.2f); // finish active -> cooldown (sequence over)
            Assert.AreEqual(CreatureStateId.Chase, attack.Tick(ctx, 0.1f));
        }

        [Test]
        public void Attack_HealthZero_RequestsSubdued()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            ctx.SetFacing(Vector2Int.right);
            var attack = new CreatureAttackState(NewHealth(deplete: true), controller);
            attack.Enter(ctx);
            Assert.AreEqual(CreatureStateId.Subdued, attack.Tick(ctx, 0.1f));
        }

        // ─────────────────────────── Subdued ───────────────────────────

        [Test]
        public void Subdued_Enter_StopsMovement_CancelsAttack()
        {
            CreatureContext ctx = NewContext(out CreatureMovement movement, out _);
            movement.SetDestination(new Vector2(3f, 0f));
            CreatureAttackController controller = NewController(out _);
            controller.TryStartAttack(Vector2Int.right);
            var subdued = new CreatureSubduedState(controller);
            subdued.Enter(ctx);
            Assert.IsFalse(movement.HasDestination, "Subdued stops movement.");
            Assert.IsTrue(controller.CanStart, "Subdued cancels the active attack.");
        }

        [Test]
        public void Subdued_IsTerminal()
        {
            CreatureContext ctx = NewContext(out _, out _);
            CreatureAttackController controller = NewController(out _);
            var subdued = new CreatureSubduedState(controller);
            subdued.Enter(ctx);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsNull(subdued.Tick(ctx, 0.5f), "Subdued never leaves.");
            }
        }

        // ─────────────────────────── Brain integration ───────────────────────────

        private CreatureBrain NewAlteredBrain(out CreatureSensor sensor, out Health health, out Transform playerT)
        {
            var go = new GameObject("AlteredVerak");
            temp.Add(go);
            go.transform.position = Vector3.zero;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CreatureIdentity identity = CreatureTestKit.NewIdentity(temp, 3f, 4f);

            var movement = go.AddComponent<CreatureMovement>();
            CreatureTestKit.SetPrivate(movement, "body", body);
            CreatureTestKit.SetPrivate(movement, "identity", identity);

            sensor = go.AddComponent<CreatureSensor>();
            CreatureTestKit.SetPrivate(sensor, "identity", identity);
            CreatureTestKit.SetPrivate(sensor, "playerLayer", (LayerMask)(1 << CreatureTestKit.PlayerLayer));
            CreatureTestKit.Invoke(sensor, "Awake");

            var resolver = go.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)(1 << 20));
            CreatureTestKit.Invoke(resolver, "Awake");
            var controller = go.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(controller, "resolver", resolver);

            health = go.AddComponent<Health>();
            CreatureTestKit.SetPrivate(health, "maxHealth", 3f);
            health.ResetHealth();

            var setup = go.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "health", health);
            CreatureTestKit.SetPrivate(setup, "attackController", controller);
            CreatureTestKit.SetPrivate(setup, "attackRange", 1f);

            var brain = go.AddComponent<CreatureBrain>();
            CreatureTestKit.SetPrivate(brain, "identity", identity);
            CreatureTestKit.SetPrivate(brain, "movement", movement);
            CreatureTestKit.SetPrivate(brain, "sensor", sensor);
            CreatureTestKit.SetPrivate(brain, "root", go.transform);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            CreatureTestKit.SetPrivate(setup, "brain", brain);

            var pgo = new GameObject("Player") { layer = CreatureTestKit.PlayerLayer };
            temp.Add(pgo);
            var pcol = pgo.AddComponent<CircleCollider2D>();
            pcol.radius = 0.1f;
            playerT = pgo.transform;

            brain.Initialize();
            return brain;
        }

        [Test]
        public void AlteredBrain_InitialState_IsIdle()
        {
            CreatureBrain brain = NewAlteredBrain(out _, out _, out _);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);
        }

        [Test]
        public void AlteredBrain_Detect_Alert_Chase()
        {
            CreatureBrain brain = NewAlteredBrain(out CreatureSensor sensor, out _, out Transform playerT);
            playerT.position = new Vector2(2f, 0f); // within detection radius 3
            Physics2D.SyncTransforms();

            sensor.Sense();
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Alert, brain.CurrentStateId, "Detection enters (hostile) Alert.");

            sensor.Sense();
            brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Chase, brain.CurrentStateId, "Hostile Alert leads into Chase, not Patrol.");
        }

        [Test]
        public void AlteredBrain_HealthZero_BecomesSubdued_AndStays()
        {
            CreatureBrain brain = NewAlteredBrain(out CreatureSensor sensor, out Health health, out Transform playerT);
            playerT.position = new Vector2(2f, 0f);
            Physics2D.SyncTransforms();
            sensor.Sense(); brain.Tick(0.1f); // Alert
            sensor.Sense(); brain.Tick(0.1f); // Chase

            health.ApplyDamage(new DamageInfo(99f, DamageSourceKind.Player)); // neutralized
            sensor.Sense(); brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Subdued, brain.CurrentStateId);

            // Terminal: even with the player still present, it never re-engages.
            sensor.Sense(); brain.Tick(0.1f);
            Assert.AreEqual(CreatureStateId.Subdued, brain.CurrentStateId);
        }

        [Test]
        public void NormalBrain_NoProvider_StillInitializesToIdle()
        {
            CreatureBrain brain = CreatureTestKit.BuildBrain(temp,
                CreatureTestKit.NewIdentity(temp), new Transform[0], out _, out _);
            brain.Initialize();
            Assert.IsTrue(brain.IsInitialized);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId, "Ambient creatures keep the default set.");
        }
    }
}

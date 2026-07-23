using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Player;
using Synora.Systems;

namespace Synora.Tests
{
    public sealed class PlayerAttackTests
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

        private PlayerAttack NewAttack(out PlayerOrientation orientation, out PlayerControlGate gate,
            float window = 0.2f, float cooldown = 0.5f)
        {
            var go = new GameObject("Player");
            temp.Add(go);
            orientation = go.AddComponent<PlayerOrientation>();
            gate = go.AddComponent<PlayerControlGate>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(attack, "windowDuration", window);
            CreatureTestKit.SetPrivate(attack, "cooldownDuration", cooldown);
            return attack;
        }

        private static void SetFacing(PlayerOrientation o, Vector2Int f) =>
            CreatureTestKit.SetPrivate(o, "facing", f);

        [Test]
        public void StartsWhenUnblockedAndIdle()
        {
            var a = NewAttack(out _, out _);
            Assert.IsTrue(a.TryAttack());
            Assert.IsTrue(a.IsAttackActive);
        }

        [Test]
        public void CapturesFacing_AllFourDirections()
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                var a = NewAttack(out PlayerOrientation o, out _);
                SetFacing(o, d);
                Assert.IsTrue(a.TryAttack());
                Assert.AreEqual(d, a.CapturedFacing);
            }
        }

        [Test]
        public void OrientationChangeAfterStart_DoesNotChangeCaptured()
        {
            var a = NewAttack(out PlayerOrientation o, out _);
            SetFacing(o, Vector2Int.left);
            a.TryAttack();
            SetFacing(o, Vector2Int.up); // change after starting
            Assert.AreEqual(Vector2Int.left, a.CapturedFacing);
        }

        [Test]
        public void DoesNotStart_WhenGateBlocked()
        {
            var a = NewAttack(out _, out PlayerControlGate gate);
            gate.Block(ControlBlockReason.Observation);
            Assert.IsFalse(a.TryAttack());
            Assert.IsFalse(a.IsAttackActive);
        }

        [Test]
        public void DoesNotStart_DuringActiveWindow()
        {
            var a = NewAttack(out _, out _);
            Assert.IsTrue(a.TryAttack());
            Assert.IsFalse(a.TryAttack(), "No attack while a window is active.");
        }

        [Test]
        public void DoesNotStart_DuringCooldown()
        {
            var a = NewAttack(out _, out _, window: 0.2f, cooldown: 0.5f);
            a.TryAttack();
            a.Tick(0.2f); // -> cooldown
            Assert.IsFalse(a.TryAttack());
        }

        [Test]
        public void FailedAttempt_DoesNotResetCooldown()
        {
            var a = NewAttack(out _, out _, window: 0.2f, cooldown: 0.5f);
            a.TryAttack();
            a.Tick(0.2f);        // enter cooldown (0.5 remaining)
            a.Tick(0.3f);        // cooldown 0.2 remaining
            Assert.IsFalse(a.TryAttack()); // failed attempt during cooldown
            a.Tick(0.2f);        // cooldown should now expire (not reset by the failed attempt)
            Assert.IsTrue(a.TryAttack(), "Failed attempt must not reset/extend cooldown.");
        }

        [Test]
        public void GateBlockDuringWindow_CancelsCleanly_NotStuck()
        {
            var a = NewAttack(out _, out PlayerControlGate gate);
            Assert.IsTrue(a.TryAttack());
            gate.Block(ControlBlockReason.Observation);
            a.Tick(0.05f); // blocked -> cancel
            Assert.IsFalse(a.IsAttackActive);
            gate.Unblock(ControlBlockReason.Observation);
            Assert.IsTrue(a.TryAttack(), "After unblock, attacking must be possible again (not stuck).");
        }

        [Test]
        public void Unblock_DoesNotAutoFire()
        {
            var a = NewAttack(out _, out PlayerControlGate gate);
            gate.Block(ControlBlockReason.Observation);
            a.Tick(0.05f);
            gate.Unblock(ControlBlockReason.Observation);
            a.Tick(0.05f);
            Assert.IsFalse(a.IsAttackActive, "Unblocking must not start an attack by itself.");
        }

        [Test]
        public void HeldTicks_DoNotAttackPerFrame()
        {
            var a = NewAttack(out _, out _);
            // Simulate many frames without a new intent: Tick alone never opens a window.
            for (int i = 0; i < 20; i++) a.Tick(0.016f);
            Assert.IsFalse(a.IsAttackActive, "Ticking (holding) must not produce attacks without a new intent.");
        }

        [Test]
        public void DoesNotDependOnForbiddenTypes()
        {
            FieldInfo[] fields = typeof(PlayerAttack).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo f in fields)
            {
                string typeName = f.FieldType.Name;
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.AreNotEqual("PlayerMotor", typeName, "PlayerAttack must not reference PlayerMotor: " + f.Name);
                Assert.AreNotEqual("Animator", typeName, "No Animator dependency: " + f.Name);
                Assert.IsFalse(typeName.Contains("Collider2D"), "No Collider2D dependency: " + f.Name);
                Assert.IsFalse(typeName.Contains("Rigidbody"), "No Rigidbody dependency: " + f.Name);
                Assert.IsFalse(ns.Contains("Combat"), "No combat/damage dependency in Fase 4: " + f.Name);
            }
        }
    }
}

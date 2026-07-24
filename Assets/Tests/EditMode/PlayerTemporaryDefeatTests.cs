using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Player;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// EditMode coverage of non-lethal temporary defeat. Awake/OnEnable/OnDisable do not
    /// run on AddComponent in EditMode, so subscription is driven explicitly. Movement
    /// stop and attack cancellation are properties of PlayerControlGate consumers
    /// (PlayerMotor/PlayerAttack) and are asserted through the gate being blocked and,
    /// for the attack, through a real PlayerAttack.
    /// </summary>
    public sealed class PlayerTemporaryDefeatTests
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

        private PlayerTemporaryDefeat NewDefeat(out Health health, out PlayerControlGate gate,
            out GameObject go, bool enable = true)
        {
            go = new GameObject("Player");
            temp.Add(go);
            health = go.AddComponent<Health>();
            CreatureTestKit.SetPrivate(health, "maxHealth", 3f);
            health.ResetHealth();
            gate = go.AddComponent<PlayerControlGate>();
            var defeat = go.AddComponent<PlayerTemporaryDefeat>();
            CreatureTestKit.SetPrivate(defeat, "health", health);
            CreatureTestKit.SetPrivate(defeat, "gate", gate);
            if (enable)
            {
                CreatureTestKit.Invoke(defeat, "OnEnable");
            }
            return defeat;
        }

        private static void Deplete(Health health) =>
            health.ApplyDamage(new DamageInfo(99f, DamageSourceKind.Creature));

        [Test]
        public void HealthZero_EntersDefeat_BlocksControl()
        {
            PlayerTemporaryDefeat defeat = NewDefeat(out Health health, out PlayerControlGate gate, out _);
            Assert.IsFalse(defeat.IsDefeated);
            Deplete(health);
            Assert.IsTrue(defeat.IsDefeated);
            Assert.IsTrue(gate.IsBlocked, "Defeat blocks control through the gate.");
        }

        [Test]
        public void Defeat_CancelsActiveAttack_ViaGate()
        {
            PlayerTemporaryDefeat defeat = NewDefeat(out Health health, out _, out GameObject go);
            var orientation = go.AddComponent<PlayerOrientation>();
            var gate = go.GetComponent<PlayerControlGate>();
            var attack = go.AddComponent<PlayerAttack>();
            CreatureTestKit.SetPrivate(attack, "orientation", orientation);
            CreatureTestKit.SetPrivate(attack, "gate", gate);
            CreatureTestKit.SetPrivate(attack, "windowDuration", 0.2f);
            CreatureTestKit.SetPrivate(attack, "cooldownDuration", 0.5f);
            CreatureTestKit.SetPrivate(orientation, "facing", Vector2Int.right);

            Assert.IsTrue(attack.TryAttack());
            Deplete(health);            // defeat blocks the gate
            attack.Tick(0.01f);         // PlayerAttack cancels its window while blocked
            Assert.IsFalse(attack.IsAttackActive, "Defeat cancels the active attack via the gate.");
        }

        [Test]
        public void Defeat_DoesNotResetHealth_NorDestroy_NorAutoRecover()
        {
            PlayerTemporaryDefeat defeat = NewDefeat(out Health health, out PlayerControlGate gate, out GameObject go);
            Deplete(health);
            Assert.AreEqual(0f, health.Current, "Health is not reset.");
            Assert.IsTrue(go != null, "The player is not destroyed.");
            Assert.IsTrue(gate.IsBlocked, "No automatic recovery: control stays blocked.");
        }

        [Test]
        public void Defeat_EntersOnce()
        {
            PlayerTemporaryDefeat defeat = NewDefeat(out Health health, out PlayerControlGate gate, out _);
            Deplete(health);
            Assert.IsTrue(gate.IsBlocked);

            // Simulate an external recovery, then a second depletion: the once-guard must
            // not re-block on a repeated Depleted signal.
            gate.Unblock(ControlBlockReason.Defeat);
            Assert.IsFalse(gate.IsBlocked);
            health.ResetHealth();
            Deplete(health); // Depleted fires again
            Assert.IsFalse(gate.IsBlocked, "Defeat is entered exactly once.");
        }

        [Test]
        public void OnDisable_Unsubscribes_NoDefeatAfter()
        {
            PlayerTemporaryDefeat defeat = NewDefeat(out Health health, out PlayerControlGate gate, out GameObject go);
            CreatureTestKit.Invoke(defeat, "OnDisable");
            Deplete(health);
            Assert.IsFalse(defeat.IsDefeated, "After OnDisable the depletion is not observed.");
            Assert.IsFalse(gate.IsBlocked);
        }

        [Test]
        public void ReEnable_DoesNotDoubleSubscribe()
        {
            PlayerTemporaryDefeat defeat = NewDefeat(out Health health, out PlayerControlGate gate, out _);
            CreatureTestKit.Invoke(defeat, "OnEnable"); // second enable: guarded, no double subscription
            Deplete(health);
            Assert.IsTrue(defeat.IsDefeated);
            Assert.IsTrue(gate.IsBlocked);
        }
    }
}

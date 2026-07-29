using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F3: while the creature is in Bonding, player control is blocked through the
    /// existing PlayerControlGate (additive Bonding reason); the block is released when
    /// Bonding ends and on disable, and it never disturbs other reasons. Built on the real
    /// CreatureBrain + AlteredVerakSetup provider. Mirrors PlayerTemporaryDefeat's ownership.
    /// </summary>
    public sealed class CreatureBondingControlBlockTests
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

        private CreatureBondingControlBlock NewBlock(out CreatureBrain brain, out PlayerControlGate gate)
        {
            brain = CreatureTestKit.BuildBrain(temp, CreatureTestKit.NewIdentity(temp), null, out _, out _);

            var resolver = brain.gameObject.AddComponent<CreatureAttackHitResolver>();
            CreatureTestKit.SetPrivate(resolver, "targetLayers", (LayerMask)(1 << 20));
            CreatureTestKit.Invoke(resolver, "Awake");
            var controller = brain.gameObject.AddComponent<CreatureAttackController>();
            CreatureTestKit.SetPrivate(controller, "resolver", resolver);
            var health = brain.gameObject.AddComponent<Health>();
            CreatureTestKit.SetPrivate(health, "maxHealth", 3f);
            health.ResetHealth();

            var setup = brain.gameObject.AddComponent<AlteredVerakSetup>();
            CreatureTestKit.SetPrivate(setup, "brain", brain);
            CreatureTestKit.SetPrivate(setup, "health", health);
            CreatureTestKit.SetPrivate(setup, "attackController", controller);
            CreatureTestKit.SetPrivate(setup, "restorationDuration", 0.2f);
            CreatureTestKit.SetPrivate(setup, "bondingDuration", 0.2f);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();

            var gateGo = new GameObject("Gate");
            temp.Add(gateGo);
            gate = gateGo.AddComponent<PlayerControlGate>();

            var go = new GameObject("BondingBlock");
            temp.Add(go);
            var block = go.AddComponent<CreatureBondingControlBlock>();
            CreatureTestKit.SetPrivate(block, "brain", brain);
            CreatureTestKit.SetPrivate(block, "gate", gate);
            return block;
        }

        private static void Enter(CreatureBrain brain, CreatureStateId state)
        {
            brain.RequestTransition(state);
            brain.Tick(0.0001f);
        }

        [Test]
        public void NotBonding_DoesNotBlock()
        {
            var block = NewBlock(out CreatureBrain brain, out PlayerControlGate gate);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);
            block.Sync();
            Assert.IsFalse(gate.IsBlocked);
        }

        [Test]
        public void EnteringBonding_BlocksPlayer()
        {
            var block = NewBlock(out CreatureBrain brain, out PlayerControlGate gate);
            Enter(brain, CreatureStateId.Bonding);
            Assert.AreEqual(CreatureStateId.Bonding, brain.CurrentStateId);
            block.Sync();
            Assert.IsTrue(gate.IsBlocked, "the player is blocked while the creature bonds");
        }

        [Test]
        public void LeavingBonding_ReleasesPlayer()
        {
            var block = NewBlock(out CreatureBrain brain, out PlayerControlGate gate);
            Enter(brain, CreatureStateId.Bonding);
            block.Sync();
            Assert.IsTrue(gate.IsBlocked);

            brain.Tick(0.3f); // the Brain's own timer completes Bonding → Bonded
            Assert.AreEqual(CreatureStateId.Bonded, brain.CurrentStateId);
            block.Sync();
            Assert.IsFalse(gate.IsBlocked, "control is released once bonding ends");
        }

        [Test]
        public void Disable_WhileBonding_ReleasesPlayer()
        {
            var block = NewBlock(out CreatureBrain brain, out PlayerControlGate gate);
            Enter(brain, CreatureStateId.Bonding);
            block.Sync();
            Assert.IsTrue(gate.IsBlocked);

            CreatureTestKit.Invoke(block, "OnDisable");
            Assert.IsFalse(gate.IsBlocked, "a creature disabled mid-bonding must not leave the player stuck");
        }

        [Test]
        public void ReleasesOnlyBonding_PreservesOtherReasons()
        {
            var block = NewBlock(out CreatureBrain brain, out PlayerControlGate gate);
            gate.Block(ControlBlockReason.Observation); // an independent reason is active

            Enter(brain, CreatureStateId.Bonding);
            block.Sync();
            Assert.IsTrue(gate.IsBlocked);

            brain.Tick(0.3f);
            Assert.AreEqual(CreatureStateId.Bonded, brain.CurrentStateId);
            block.Sync();
            Assert.IsTrue(gate.IsBlocked, "releasing Bonding must leave Observation intact");
        }

        [Test]
        public void Sync_IsIdempotent()
        {
            var block = NewBlock(out CreatureBrain brain, out PlayerControlGate gate);
            Enter(brain, CreatureStateId.Bonding);
            block.Sync();
            block.Sync();
            Assert.IsTrue(gate.IsBlocked);

            brain.Tick(0.3f);
            block.Sync();
            block.Sync();
            Assert.IsFalse(gate.IsBlocked);
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Systems;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F6 correction: the session flag is owned by a NON-visual coordinator, independent
    /// of the F5 presentation. It observes CreatureBrain.CurrentStateId and marks
    /// BondSessionState only on Bonded — never presents, changes state, or requests a
    /// transition. Built on the real CreatureBrain + AlteredVerakSetup provider.
    /// </summary>
    public sealed class CreatureBondSessionCoordinatorTests
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

        private CreatureBrain BuildBrain()
        {
            var brain = CreatureTestKit.BuildBrain(temp, CreatureTestKit.NewIdentity(temp), null, out _, out _);
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
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();
            return brain;
        }

        private CreatureBondSessionCoordinator NewCoordinator(out CreatureBrain brain, out BondSessionState session)
        {
            brain = BuildBrain();
            var sessGo = new GameObject("Session");
            temp.Add(sessGo);
            session = sessGo.AddComponent<BondSessionState>();

            var go = new GameObject("BondSessionCoordinator");
            temp.Add(go);
            var coord = go.AddComponent<CreatureBondSessionCoordinator>();
            CreatureTestKit.SetPrivate(coord, "brain", brain);
            CreatureTestKit.SetPrivate(coord, "session", session);
            return coord;
        }

        private static void Drive(CreatureBrain brain, CreatureStateId state)
        {
            brain.RequestTransition(state);
            brain.Tick(0.0001f);
        }

        [Test]
        public void Session_StartsFalse()
        {
            NewCoordinator(out _, out BondSessionState session);
            Assert.IsFalse(session.IsBonded);
        }

        [TestCase(CreatureStateId.Idle)]        // Calm
        [TestCase(CreatureStateId.Restoring)]
        [TestCase(CreatureStateId.Restored)]
        [TestCase(CreatureStateId.Bonding)]
        [TestCase(CreatureStateId.Subdued)]
        public void NonBondedStates_DoNotMark(CreatureStateId state)
        {
            var coord = NewCoordinator(out CreatureBrain brain, out BondSessionState session);
            Drive(brain, state);
            coord.Sync();
            Assert.IsFalse(session.IsBonded, "only Bonded marks the session, not " + state);
        }

        [Test]
        public void Bonded_MarksSession()
        {
            var coord = NewCoordinator(out CreatureBrain brain, out BondSessionState session);
            Drive(brain, CreatureStateId.Bonded);
            coord.Sync();
            Assert.IsTrue(session.IsBonded);
        }

        [Test]
        public void StayingInBonded_KeepsTrue()
        {
            var coord = NewCoordinator(out CreatureBrain brain, out BondSessionState session);
            Drive(brain, CreatureStateId.Bonded);
            for (int i = 0; i < 5; i++)
            {
                coord.Sync();
            }
            Assert.IsTrue(session.IsBonded);
        }

        [Test]
        public void LeavingBonded_DoesNotRevert()
        {
            var coord = NewCoordinator(out CreatureBrain brain, out BondSessionState session);
            Drive(brain, CreatureStateId.Bonded);
            coord.Sync();
            Assert.IsTrue(session.IsBonded);

            Drive(brain, CreatureStateId.Idle); // test-only forced exit
            coord.Sync();
            Assert.IsTrue(session.IsBonded, "verak_vinculado records that the bond happened this session");
        }

        [Test]
        public void NullBrain_IsSafe()
        {
            var coord = NewCoordinator(out _, out _);
            CreatureTestKit.SetPrivate(coord, "brain", (CreatureBrain)null);
            Assert.DoesNotThrow(() => coord.Sync());
        }

        [Test]
        public void NullSession_IsSafe()
        {
            var coord = NewCoordinator(out CreatureBrain brain, out _);
            CreatureTestKit.SetPrivate(coord, "session", (BondSessionState)null);
            Drive(brain, CreatureStateId.Bonded);
            Assert.DoesNotThrow(() => coord.Sync());
        }

        [Test]
        public void HasNoPresentationOrGameplayDependencies()
        {
            foreach (FieldInfo f in typeof(CreatureBondSessionCoordinator).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string typeName = f.FieldType.Name;
                Assert.AreNotEqual("BondEstablishedPresenter", typeName, "no UI: " + f.Name);
                Assert.AreNotEqual("EcoSignal", typeName, "no ECO: " + f.Name);
                Assert.AreNotEqual("SpriteFlash", typeName, "no flash: " + f.Name);
                Assert.AreNotEqual("SpriteRenderer", typeName, "no renderer: " + f.Name);
                Assert.AreNotEqual("CreatureMovement", typeName, "no movement: " + f.Name);
                Assert.AreNotEqual("PlayerControlGate", typeName, "no gate: " + f.Name);
            }
        }
    }
}

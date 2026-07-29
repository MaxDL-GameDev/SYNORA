using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Synora.Data;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F5: entering Bonded fires the one-shot ficha + ECO exactly once — never on
    /// Bonding, never repeating every Update, never on disable/reactivate of the same
    /// instance. It never changes state or moves the creature, and holds no color/movement/
    /// gate dependency (the glow is a separate component). Built on the real
    /// CreatureBrain + AlteredVerakSetup provider and real UI/ECO components.
    /// </summary>
    public sealed class CreatureBondedFeedbackTests
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
            CreatureTestKit.SetPrivate(setup, "bondingDuration", 0.2f);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();
            return brain;
        }

        private BondEstablishedPresenter NewPanel(out Text label)
        {
            var rootGo = new GameObject("BondPanelRoot");
            temp.Add(rootGo);
            var labelGo = new GameObject("BondLabel");
            temp.Add(labelGo);
            label = labelGo.AddComponent<Text>();
            var presGo = new GameObject("BondPresenter");
            temp.Add(presGo);
            presGo.SetActive(false);
            var pres = presGo.AddComponent<BondEstablishedPresenter>();
            CreatureTestKit.SetPrivate(pres, "panelRoot", rootGo);
            CreatureTestKit.SetPrivate(pres, "label", label);
            CreatureTestKit.SetPrivate(pres, "displayDuration", 3f);
            rootGo.SetActive(false);
            presGo.SetActive(true);
            return pres;
        }

        private EcoSignal NewEco()
        {
            var go = new GameObject("Eco");
            temp.Add(go);
            return go.AddComponent<EcoSignal>();
        }

        private CreatureBondedFeedback NewCoordinator(
            out CreatureBrain brain, out BondEstablishedPresenter panel, out EcoSignal eco, out Text label)
        {
            brain = BuildBrain();
            panel = NewPanel(out label);
            eco = NewEco();

            var identity = CreatureTestKit.NewIdentity(temp);
            CreatureTestKit.SetPrivate(identity, "displayName", "Verak");

            var go = new GameObject("BondedFeedback");
            temp.Add(go);
            var fb = go.AddComponent<CreatureBondedFeedback>();
            CreatureTestKit.SetPrivate(fb, "brain", brain);
            CreatureTestKit.SetPrivate(fb, "identity", identity);
            CreatureTestKit.SetPrivate(fb, "panel", panel);
            CreatureTestKit.SetPrivate(fb, "eco", eco);
            CreatureTestKit.SetPrivate(fb, "title", "Vínculo establecido");
            CreatureTestKit.SetPrivate(fb, "provisionalAffinity", "provisional");
            return fb;
        }

        private static void Drive(CreatureBrain brain, CreatureStateId state)
        {
            brain.RequestTransition(state);
            brain.Tick(0.0001f);
        }

        [Test]
        public void NotBonded_DoesNotFire()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out BondEstablishedPresenter panel, out EcoSignal eco, out _);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);
            fb.Sync();
            Assert.IsFalse(panel.IsShown);
            Assert.AreEqual(0, eco.EmitCount);
        }

        [Test]
        public void EnteringBonding_DoesNotFire()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out BondEstablishedPresenter panel, out EcoSignal eco, out _);
            Drive(brain, CreatureStateId.Bonding);
            fb.Sync();
            Assert.IsFalse(panel.IsShown, "the ficha/ECO fire on Bonded, not on Bonding");
            Assert.AreEqual(0, eco.EmitCount);
        }

        [Test]
        public void EnteringBonded_FiresFichaAndEcoOnce()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out BondEstablishedPresenter panel, out EcoSignal eco, out Text label);
            Drive(brain, CreatureStateId.Bonded);
            fb.Sync();
            Assert.IsTrue(panel.IsShown);
            Assert.AreEqual("Vínculo establecido\nVerak\nAfinidad: provisional", label.text);
            Assert.AreEqual(1, eco.EmitCount);
        }

        [Test]
        public void WhileBonded_DoesNotRepeatEveryUpdate()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out BondEstablishedPresenter panel, out EcoSignal eco, out _);
            Drive(brain, CreatureStateId.Bonded);
            fb.Sync();
            panel.Hide();
            for (int i = 0; i < 5; i++)
            {
                fb.Sync();
            }
            Assert.IsFalse(panel.IsShown, "must not re-show the ficha");
            Assert.AreEqual(1, eco.EmitCount, "must not re-emit ECO");
        }

        [Test]
        public void NoOnDisable_SoDisableReactivateDoesNotResetOneShotLatch()
        {
            // No OnDisable => the firedForBond latch survives a disable/reactivate of the same
            // instance, so the feedback never replays while it stays Bonded.
            Assert.IsNull(typeof(CreatureBondedFeedback).GetMethod(
                "OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                "CreatureBondedFeedback must not reset its one-shot latch on disable");
        }

        [Test]
        public void Sync_DoesNotChangeBrainState()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out _, out _, out _);
            Drive(brain, CreatureStateId.Bonded);
            fb.Sync();
            brain.Tick(1f);
            Assert.AreEqual(CreatureStateId.Bonded, brain.CurrentStateId);
        }

        [Test]
        public void NullReferences_AreSafe()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out _, out _, out _);
            CreatureTestKit.SetPrivate(fb, "panel", (BondEstablishedPresenter)null);
            CreatureTestKit.SetPrivate(fb, "eco", (EcoSignal)null);
            Drive(brain, CreatureStateId.Bonded);
            Assert.DoesNotThrow(() => fb.Sync());
        }

        [Test]
        public void HasNoForbiddenDependencies_AndNoSessionState()
        {
            foreach (FieldInfo f in typeof(CreatureBondedFeedback).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string typeName = f.FieldType.Name;
                Assert.AreNotEqual("CreatureMovement", typeName, "must not touch movement: " + f.Name);
                Assert.AreNotEqual("PlayerControlGate", typeName, "must not touch the gate: " + f.Name);
                Assert.AreNotEqual("SpriteRenderer", typeName, "no direct renderer: " + f.Name);
                Assert.AreNotEqual("SpriteFlash", typeName, "the glow is a separate component: " + f.Name);
                Assert.AreNotEqual("Color", typeName, "no direct color: " + f.Name);
                // Presentation must NOT own session state (F6 correction): the flag is the
                // CreatureBondSessionCoordinator's responsibility.
                Assert.AreNotEqual("BondSessionState", typeName, "feedback must not touch session state: " + f.Name);
            }
        }
    }
}

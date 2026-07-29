using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F5: entering Bonded fires the bond-established feedback exactly once — a one-shot
    /// SpriteFlash.Flash() (SpriteFlash remains the only writer of SpriteRenderer.color), the
    /// "Vínculo establecido" UI notification, and the ECO signal — never repeating every
    /// Update, never changing state or moving the creature. Built on the real
    /// CreatureBrain + AlteredVerakSetup provider and real SpriteFlash/UI/ECO components.
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

        private SpriteFlash NewFlash()
        {
            var go = new GameObject("Sprite");
            temp.Add(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = go.AddComponent<SpriteFlash>();
            CreatureTestKit.SetPrivate(f, "spriteRenderer", sr);
            CreatureTestKit.SetPrivate(f, "flashColor", new Color(1f, 0.5f, 0.5f, 1f));
            CreatureTestKit.SetPrivate(f, "flashDuration", 0.1f);
            CreatureTestKit.Invoke(f, "Awake");
            return f;
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
            out CreatureBrain brain, out SpriteFlash flash, out BondEstablishedPresenter panel,
            out EcoSignal eco, out Text label)
        {
            brain = BuildBrain();
            flash = NewFlash();
            panel = NewPanel(out label);
            eco = NewEco();

            var go = new GameObject("BondedFeedback");
            temp.Add(go);
            var fb = go.AddComponent<CreatureBondedFeedback>();
            CreatureTestKit.SetPrivate(fb, "brain", brain);
            CreatureTestKit.SetPrivate(fb, "flash", flash);
            CreatureTestKit.SetPrivate(fb, "panel", panel);
            CreatureTestKit.SetPrivate(fb, "eco", eco);
            CreatureTestKit.SetPrivate(fb, "message", "Vínculo establecido");
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
            var fb = NewCoordinator(out CreatureBrain brain, out SpriteFlash flash,
                out BondEstablishedPresenter panel, out EcoSignal eco, out _);
            Assert.AreEqual(CreatureStateId.Idle, brain.CurrentStateId);

            fb.Sync();

            Assert.IsFalse(flash.IsFlashing);
            Assert.IsFalse(panel.IsShown);
            Assert.AreEqual(0, eco.EmitCount);
        }

        [Test]
        public void EnteringBonded_FiresAllThreeChannelsOnce()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out SpriteFlash flash,
                out BondEstablishedPresenter panel, out EcoSignal eco, out Text label);
            Drive(brain, CreatureStateId.Bonded);

            fb.Sync();

            Assert.IsTrue(flash.IsFlashing, "visual: a one-shot flash through SpriteFlash");
            Assert.IsTrue(panel.IsShown, "UI: the notification is shown");
            Assert.AreEqual("Vínculo establecido", label.text);
            Assert.AreEqual(1, eco.EmitCount, "ECO: emitted once");
        }

        [Test]
        public void WhileBonded_DoesNotRepeatEveryUpdate()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out SpriteFlash flash,
                out BondEstablishedPresenter panel, out EcoSignal eco, out _);
            Drive(brain, CreatureStateId.Bonded);
            fb.Sync();
            Assert.AreEqual(1, eco.EmitCount);

            // Consume the one-shot feedback, then keep syncing while still Bonded.
            flash.Tick(1f);        // end the flash
            panel.Hide();          // dismiss the notification
            Assert.IsFalse(flash.IsFlashing);
            Assert.IsFalse(panel.IsShown);

            for (int i = 0; i < 5; i++)
            {
                fb.Sync();
            }

            Assert.IsFalse(flash.IsFlashing, "must not re-flash every Update");
            Assert.IsFalse(panel.IsShown, "must not re-show the notification");
            Assert.AreEqual(1, eco.EmitCount, "must not re-emit ECO");
        }

        [Test]
        public void Sync_DoesNotChangeBrainState()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out _, out _, out _, out _);
            Drive(brain, CreatureStateId.Bonded);
            fb.Sync();
            brain.Tick(1f);
            Assert.AreEqual(CreatureStateId.Bonded, brain.CurrentStateId,
                "feedback is presentation-only; it never changes state");
        }

        [Test]
        public void NullReferences_AreSafe()
        {
            var fb = NewCoordinator(out CreatureBrain brain, out _, out _, out _, out _);
            CreatureTestKit.SetPrivate(fb, "flash", (SpriteFlash)null);
            CreatureTestKit.SetPrivate(fb, "panel", (BondEstablishedPresenter)null);
            CreatureTestKit.SetPrivate(fb, "eco", (EcoSignal)null);
            Drive(brain, CreatureStateId.Bonded);
            Assert.DoesNotThrow(() => fb.Sync());
        }

        [Test]
        public void HasNoForbiddenDependencies()
        {
            foreach (FieldInfo f in typeof(CreatureBondedFeedback).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string typeName = f.FieldType.Name;
                Assert.AreNotEqual("CreatureMovement", typeName, "must not touch movement: " + f.Name);
                Assert.AreNotEqual("PlayerControlGate", typeName, "must not touch the gate: " + f.Name);
                Assert.AreNotEqual("SpriteRenderer", typeName, "color goes only through SpriteFlash: " + f.Name);
                Assert.AreNotEqual("Color", typeName, "no direct color: " + f.Name);
            }
        }
    }
}

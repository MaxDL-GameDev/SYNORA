using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    /// <summary>
    /// M7 F5: the persistent bond glow holds a stable tint through SpriteFlash (the single
    /// color compositor) while the creature is in Bonding OR Bonded, set once and cleared
    /// once (owns latch), cleared on leaving both states and on disable, without clearing a
    /// foreign tint it never owned. Mirrors CreatureRestorationPresentation's policy.
    /// </summary>
    public sealed class CreatureBondPresentationTests
    {
        private readonly List<Object> temp = new List<Object>();

        private static readonly Color BaseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        private static readonly Color BondTint = new Color(1f, 0.85f, 0.4f, 1f);
        private const float BondIntensity = 0.4f;

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

        private SpriteFlash NewFlash(out SpriteRenderer sr)
        {
            var go = new GameObject("Sprite");
            temp.Add(go);
            sr = go.AddComponent<SpriteRenderer>();
            sr.color = BaseColor;
            var f = go.AddComponent<SpriteFlash>();
            CreatureTestKit.SetPrivate(f, "spriteRenderer", sr);
            CreatureTestKit.SetPrivate(f, "flashColor", new Color(1f, 0.5f, 0.5f, 1f));
            CreatureTestKit.SetPrivate(f, "flashDuration", 0.1f);
            CreatureTestKit.Invoke(f, "Awake");
            return f;
        }

        private CreatureBondPresentation NewPresentation(
            out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr)
        {
            brain = BuildBrain();
            flash = NewFlash(out sr);
            var go = new GameObject("BondPresentation");
            temp.Add(go);
            var pres = go.AddComponent<CreatureBondPresentation>();
            CreatureTestKit.SetPrivate(pres, "brain", brain);
            CreatureTestKit.SetPrivate(pres, "flash", flash);
            CreatureTestKit.SetPrivate(pres, "bondTint", BondTint);
            CreatureTestKit.SetPrivate(pres, "bondIntensity", BondIntensity);
            return pres;
        }

        private static void Drive(CreatureBrain brain, CreatureStateId state)
        {
            brain.RequestTransition(state);
            brain.Tick(0.0001f);
        }

        private static Color Compose(Color baseColor, Color tint, float intensity)
        {
            Color c = Color.Lerp(baseColor, tint, Mathf.Clamp01(intensity));
            c.a = baseColor.a;
            return c;
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f, "r");
            Assert.AreEqual(expected.g, actual.g, 0.001f, "g");
            Assert.AreEqual(expected.b, actual.b, 0.001f, "b");
        }

        [Test]
        public void AppliesTint_OnEnteringBonding()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Drive(brain, CreatureStateId.Bonding);
            pres.Apply();
            AssertColor(Compose(BaseColor, BondTint, BondIntensity), sr.color);
        }

        [Test]
        public void Persists_FromBondingToBonded_WithoutClearingBetween()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Drive(brain, CreatureStateId.Bonding);
            pres.Apply();
            Color bonding = sr.color;
            Drive(brain, CreatureStateId.Bonded);
            pres.Apply();
            AssertColor(bonding, sr.color); // same stable tint, not cleared/re-computed
            AssertColor(Compose(BaseColor, BondTint, BondIntensity), sr.color);
        }

        [Test]
        public void NotReapplied_EveryUpdate()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            Drive(brain, CreatureStateId.Bonded);
            pres.Apply(); // sets the tint once (owns)
            flash.ClearPersistentTint(); // simulate an external clear of the shared slot
            Assert.AreEqual(BaseColor, sr.color);
            pres.Apply(); // still Bonded, already owns -> must NOT re-apply
            Assert.AreEqual(BaseColor, sr.color, "the tint is set once, not re-applied every Update");
        }

        [Test]
        public void ClearsTint_OnLeavingBondStates()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Drive(brain, CreatureStateId.Bonded);
            pres.Apply();
            Assert.AreNotEqual(BaseColor, sr.color);
            Drive(brain, CreatureStateId.Idle); // test-only forced exit
            pres.Apply();
            Assert.AreEqual(BaseColor, sr.color, "leaving Bonding/Bonded clears the bond tint");
        }

        [Test]
        public void OnDisable_ClearsTint_WhenOwned()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Drive(brain, CreatureStateId.Bonded);
            pres.Apply();
            Assert.AreNotEqual(BaseColor, sr.color);
            CreatureTestKit.Invoke(pres, "OnDisable");
            Assert.AreEqual(BaseColor, sr.color, "disable clears the owned bond tint");
        }

        [Test]
        public void OnDisable_LeavesForeignTintIntact_WhenNeverOwned()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            flash.SetTerminalTint(true); // a foreign (Subdued) tint
            Color foreign = sr.color;
            Drive(brain, CreatureStateId.Subdued);
            pres.Apply(); // not in bond states -> never owns
            CreatureTestKit.Invoke(pres, "OnDisable");
            Assert.AreEqual(foreign, sr.color, "disable must not clear a tint the bond glow never owned");
        }

        [Test]
        public void NullRefs_DoNotThrow()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out _);
            CreatureTestKit.SetPrivate(pres, "flash", (SpriteFlash)null);
            Drive(brain, CreatureStateId.Bonded);
            Assert.DoesNotThrow(() => pres.Apply());
            CreatureTestKit.SetPrivate(pres, "brain", (CreatureBrain)null);
            Assert.DoesNotThrow(() => pres.Apply());
        }
    }
}

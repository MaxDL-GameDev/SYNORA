using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;
using Synora.Gameplay.Creatures;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    /// <summary>
    /// M6 F6: the restoration presentation drives a restoration tint through SpriteFlash
    /// (the single color compositor) from CreatureBrain.CurrentStateId — a deterministic
    /// pulse in Restoring, a stable tint in Restored, and nothing in other states. It never
    /// changes state or requests a transition, coexists with flashes, replaces the Subdued
    /// terminal tint on entering Restoring, and restores safely on disable. Uses the real
    /// CreatureBrain + AlteredVerakSetup provider and a real SpriteRenderer/SpriteFlash; no
    /// scene, prefab, Animator or materials.
    /// </summary>
    public sealed class CreatureRestorationPresentationTests
    {
        private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;
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
            CreatureTestKit.SetPrivate(setup, "restorationDuration", 0.2f);
            CreatureTestKit.SetPrivate(brain, "stateProvider", setup);
            brain.Initialize();
            return brain;
        }

        private SpriteFlash NewFlash(out SpriteRenderer sr, Color baseColor)
        {
            var go = new GameObject("Sprite");
            temp.Add(go);
            sr = go.AddComponent<SpriteRenderer>();
            sr.color = baseColor;
            var f = go.AddComponent<SpriteFlash>();
            CreatureTestKit.SetPrivate(f, "spriteRenderer", sr);
            CreatureTestKit.SetPrivate(f, "flashColor", new Color(1f, 0.5f, 0.5f, 1f));
            CreatureTestKit.SetPrivate(f, "terminalTint", new Color(0.4f, 0.4f, 0.6f, 1f));
            CreatureTestKit.SetPrivate(f, "flashDuration", 0.1f);
            CreatureTestKit.Invoke(f, "Awake");
            return f;
        }

        private CreatureRestorationPresentation NewPresentation(
            out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr,
            Color? baseColor = null, CreatureBrain existing = null, SpriteFlash existingFlash = null)
        {
            brain = existing != null ? existing : BuildBrain();
            if (existingFlash != null) { flash = existingFlash; sr = existingFlash.GetComponent<SpriteRenderer>(); }
            else { flash = NewFlash(out sr, baseColor ?? new Color(0.2f, 0.3f, 0.4f, 1f)); }

            var go = new GameObject("RestorationPresentation");
            temp.Add(go);
            var pres = go.AddComponent<CreatureRestorationPresentation>();
            CreatureTestKit.SetPrivate(pres, "brain", brain);
            CreatureTestKit.SetPrivate(pres, "flash", flash);
            CreatureTestKit.SetPrivate(pres, "restoringTint", new Color(0.6f, 0.85f, 1f, 1f));
            CreatureTestKit.SetPrivate(pres, "pulseMinIntensity", 0.15f);
            CreatureTestKit.SetPrivate(pres, "pulseMaxIntensity", 0.5f);
            CreatureTestKit.SetPrivate(pres, "pulseSpeed", 6f);
            CreatureTestKit.SetPrivate(pres, "restoredTint", new Color(0.7f, 0.9f, 0.75f, 1f));
            CreatureTestKit.SetPrivate(pres, "restoredIntensity", 0.35f);
            return pres;
        }

        private static void Drive(CreatureBrain brain, CreatureStateId state)
        {
            brain.RequestTransition(state);
            brain.Tick(0.0001f);
        }

        private static void Enable(CreatureRestorationPresentation pres) =>
            pres.GetType().GetMethod("OnEnable", Priv).Invoke(pres, null);

        private static void Disable(CreatureRestorationPresentation pres) =>
            pres.GetType().GetMethod("OnDisable", Priv).Invoke(pres, null);

        private static void Apply(CreatureRestorationPresentation pres, float dt) =>
            pres.GetType().GetMethod("Apply", Priv).Invoke(pres, new object[] { dt });

        private static Color Compose(Color baseColor, Color tint, float intensity)
        {
            Color c = Color.Lerp(baseColor, tint, Mathf.Clamp01(intensity));
            c.a = baseColor.a;
            return c;
        }

        private static readonly Color BaseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        private static readonly Color RestoredTint = new Color(0.7f, 0.9f, 0.75f, 1f);

        // ── Non-restorative states leave the base intact ──

        [TestCase(CreatureStateId.Idle)]
        [TestCase(CreatureStateId.Patrol)]
        [TestCase(CreatureStateId.Alert)]
        [TestCase(CreatureStateId.Chase)]
        [TestCase(CreatureStateId.Attack)]
        public void NonRestorativeStates_KeepBaseColor(CreatureStateId state)
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, state);
            Apply(pres, 0.016f);
            Assert.AreEqual(state, brain.CurrentStateId);
            Assert.AreEqual(BaseColor, sr.color, "no restoration tint in " + state);
        }

        [Test]
        public void Subdued_KeepsApprovedTerminalBehavior_PresentationDoesNotClearIt()
        {
            // Simulate the Subdued terminal tint set elsewhere (AlteredVerakPresentation).
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            flash.SetTerminalTint(true);
            Color subduedColor = sr.color;
            Enable(pres);
            Drive(brain, CreatureStateId.Subdued);
            Apply(pres, 0.016f);
            Assert.AreEqual(subduedColor, sr.color, "presentation must not clear the Subdued terminal tint");
            Assert.IsTrue(flash.TerminalHeld);
        }

        // ── Restoring: deterministic pulse ──

        [Test]
        public void Restoring_AppliesPulse_ThatChangesOverTime_WithinBounds()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);

            Apply(pres, 0f);
            Color a = sr.color;
            Apply(pres, 0.2f);
            Color b = sr.color;
            Assert.AreNotEqual(a, b, "the pulse changes the color as time advances");

            // Within the configured intensity bounds [0.15, 0.5] of restoringTint over base.
            var restoringTint = new Color(0.6f, 0.85f, 1f, 1f);
            for (int i = 0; i < 40; i++)
            {
                Apply(pres, 0.05f);
                Color lo = Compose(BaseColor, restoringTint, 0.15f);
                Color hi = Compose(BaseColor, restoringTint, 0.5f);
                Assert.GreaterOrEqual(sr.color.r + 1e-4f, Mathf.Min(lo.r, hi.r));
                Assert.LessOrEqual(sr.color.r - 1e-4f, Mathf.Max(lo.r, hi.r));
            }
        }

        [Test]
        public void Restoring_ZeroDeltaTime_DoesNotAdvancePulse()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.1f);
            Color c1 = sr.color;
            Apply(pres, 0f);
            Color c2 = sr.color;
            Assert.AreEqual(c1, c2, "deltaTime == 0 must not advance the pulse");
        }

        [Test]
        public void Restoring_DoesNotChangeStateNorRequestTransition()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out _);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);
            brain.Tick(0.001f);
            Assert.AreEqual(CreatureStateId.Restoring, brain.CurrentStateId);
        }

        [Test]
        public void SubduedToRestoring_ReplacesTerminalTint()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            flash.SetTerminalTint(true);        // Subdued terminal tint present
            Color subduedColor = sr.color;
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);
            Assert.AreNotEqual(subduedColor, sr.color, "entering Restoring replaces the Subdued terminal tint");
        }

        // ── Restored: stable tint ──

        [Test]
        public void Restored_AppliesStableTint_ThatDoesNotChangeOverTime()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            Color c1 = sr.color;
            Apply(pres, 0.5f);
            Color c2 = sr.color;
            Assert.AreEqual(c1, c2, "the restored tint is stable (no pulse)");
            AssertColorApprox(Compose(BaseColor, RestoredTint, 0.35f), sr.color);
        }

        [Test]
        public void Restored_DoesNotChangeState()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out _);
            Enable(pres);
            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            brain.Tick(0.5f);
            Assert.AreEqual(CreatureStateId.Restored, brain.CurrentStateId);
        }

        // ── Transitions ──

        [Test]
        public void RestoringToRestored_SwitchesPulseToStableTint()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);

            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            Color first = sr.color;
            Apply(pres, 0.5f);
            Assert.AreEqual(first, sr.color, "after Restored the tint is stable");
            AssertColorApprox(Compose(BaseColor, RestoredTint, 0.35f), sr.color);
        }

        [Test]
        public void RestoredToOtherState_RestoresBase()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            Assert.AreNotEqual(BaseColor, sr.color);

            Drive(brain, CreatureStateId.Idle); // forced non-terminal exit (test-only)
            Apply(pres, 0.05f);
            Assert.AreEqual(BaseColor, sr.color, "leaving Restored restores the base color");
        }

        // ── Flash coexistence ──

        [Test]
        public void FlashDuringRestoring_EndsToPulseTint()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);
            flash.Flash();
            Assert.IsTrue(flash.IsFlashing, "flash dominates during Restoring");
            flash.Tick(1f);
            // After the flash, the latest restoration pulse tint is shown (not the base).
            Assert.AreNotEqual(BaseColor, sr.color, "flash end returns to the restoration tint");
        }

        [Test]
        public void FlashDuringRestored_EndsToStableTint()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            flash.Flash();
            flash.Tick(1f);
            AssertColorApprox(Compose(BaseColor, RestoredTint, 0.35f), sr.color);
        }

        [Test]
        public void RestoringToRestored_DuringFlash_EndsToRestoredTint()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);
            flash.Flash();
            // Transition while the flash is active.
            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            Assert.IsTrue(flash.IsFlashing, "flash still dominates");
            flash.Tick(1f);
            AssertColorApprox(Compose(BaseColor, RestoredTint, 0.35f), sr.color);
        }

        // ── Lifecycle ──

        [Test]
        public void OnEnable_InRestored_ReflectsImmediately()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Drive(brain, CreatureStateId.Restored);
            Enable(pres);
            AssertColorApprox(Compose(BaseColor, RestoredTint, 0.35f), sr.color);
        }

        [Test]
        public void OnDisable_DuringRestoring_RestoresBase()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);
            Assert.AreNotEqual(BaseColor, sr.color);
            Disable(pres);
            Assert.AreEqual(BaseColor, sr.color, "disable during Restoring restores the base");
        }

        [Test]
        public void OnDisable_DuringRestored_RestoresBase()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restored);
            Apply(pres, 0.05f);
            Disable(pres);
            Assert.AreEqual(BaseColor, sr.color, "disable during Restored restores the base");
        }

        [Test]
        public void ReEnable_ReflectsCurrentState()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out SpriteRenderer sr);
            Enable(pres);
            Drive(brain, CreatureStateId.Restoring);
            Apply(pres, 0.05f);
            Disable(pres);
            Assert.AreEqual(BaseColor, sr.color);

            Drive(brain, CreatureStateId.Restored);
            Enable(pres);
            AssertColorApprox(Compose(BaseColor, RestoredTint, 0.35f), sr.color);
        }

        [Test]
        public void OnDisable_WhenNeverOwned_LeavesForeignTintIntact()
        {
            var pres = NewPresentation(out CreatureBrain brain, out SpriteFlash flash, out SpriteRenderer sr);
            flash.SetTerminalTint(true); // foreign (Subdued) tint
            Color foreign = sr.color;
            Enable(pres);
            Drive(brain, CreatureStateId.Subdued);
            Apply(pres, 0.016f);
            Disable(pres);
            Assert.AreEqual(foreign, sr.color, "disable must not clear a tint the presentation never owned");
        }

        // ── Null safety ──

        [Test]
        public void NullBrain_DoesNotThrow_AndDoesNotTint()
        {
            var pres = NewPresentation(out _, out _, out SpriteRenderer sr);
            CreatureTestKit.SetPrivate(pres, "brain", (CreatureBrain)null);
            Assert.DoesNotThrow(() => Enable(pres));
            Assert.AreEqual(BaseColor, sr.color);
        }

        [Test]
        public void NullFlash_DoesNotThrow()
        {
            var pres = NewPresentation(out CreatureBrain brain, out _, out _);
            CreatureTestKit.SetPrivate(pres, "flash", (SpriteFlash)null);
            Drive(brain, CreatureStateId.Restoring);
            Assert.DoesNotThrow(() => Apply(pres, 0.05f));
            Drive(brain, CreatureStateId.Idle);
            Assert.DoesNotThrow(() => Apply(pres, 0.05f));
        }

        // ── Architecture ──

        [Test]
        public void HasNoForbiddenDependencies()
        {
            System.Type t = typeof(CreatureRestorationPresentation);
            Assert.IsNull(t.GetMethod("Coroutine", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string tn = f.FieldType.Name;
                Assert.AreNotEqual("Animator", tn, "no Animator: " + f.Name);
                Assert.AreNotEqual("PlayerControlGate", tn, "no gate: " + f.Name);
                Assert.AreNotEqual("CreatureRestoreTimer", tn, "no timer: " + f.Name);
                Assert.AreNotEqual("Health", tn, "no Health: " + f.Name);
                Assert.AreNotEqual("AlteredVerakSetup", tn, "no provider: " + f.Name);
                Assert.AreNotEqual("CreatureRestorationInteractable", tn, "no interactable: " + f.Name);
                string ns = f.FieldType.Namespace ?? string.Empty;
                Assert.IsFalse(ns.Contains("UnityEngine.UI"), "no UI: " + f.Name);
            }
        }

        private static void AssertColorApprox(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f, "r");
            Assert.AreEqual(expected.g, actual.g, 0.001f, "g");
            Assert.AreEqual(expected.b, actual.b, 0.001f, "b");
            Assert.AreEqual(expected.a, actual.a, 0.001f, "a");
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Presentation;

namespace Synora.Tests
{
    /// <summary>
    /// M6 F6: SpriteFlash as the single color compositor — base color → persistent tint →
    /// temporary flash. Verifies layering priority (flash dominates, then latest persistent
    /// tint, then base), alpha preservation, non-white bases, capture-once semantics, and
    /// safe restore on disable. The legacy SetTerminalTint(bool) path keeps working.
    /// </summary>
    public sealed class SpriteFlashCompositionTests
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

        private SpriteFlash NewFlash(out SpriteRenderer sr, Color? baseColor = null, float flashDuration = 0.1f)
        {
            var go = new GameObject("Sprite");
            temp.Add(go);
            sr = go.AddComponent<SpriteRenderer>();
            sr.color = baseColor ?? Color.white;
            var f = go.AddComponent<SpriteFlash>();
            CreatureTestKit.SetPrivate(f, "spriteRenderer", sr);
            CreatureTestKit.SetPrivate(f, "flashColor", new Color(1f, 0.5f, 0.5f, 1f));
            CreatureTestKit.SetPrivate(f, "terminalTint", new Color(0.4f, 0.4f, 0.6f, 1f));
            CreatureTestKit.SetPrivate(f, "flashDuration", flashDuration);
            CreatureTestKit.Invoke(f, "Awake"); // captures the (possibly non-white) base
            return f;
        }

        private static void AssertColorApprox(Color expected, Color actual, string msg)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f, msg + " (r)");
            Assert.AreEqual(expected.g, actual.g, 0.001f, msg + " (g)");
            Assert.AreEqual(expected.b, actual.b, 0.001f, msg + " (b)");
            Assert.AreEqual(expected.a, actual.a, 0.001f, msg + " (a)");
        }

        private static Color Compose(Color baseColor, Color tint, float intensity)
        {
            Color c = Color.Lerp(baseColor, tint, Mathf.Clamp01(intensity));
            c.a = baseColor.a;
            return c;
        }

        // ── Persistent tint over base ──

        [Test]
        public void PersistentTint_ComposesOverBase_PreservesAlpha()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 0.8f); // non-white, non-1 alpha
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            var tint = new Color(0.6f, 0.9f, 0.75f, 1f);
            f.SetPersistentTint(tint, 0.5f);
            AssertColorApprox(Compose(baseColor, tint, 0.5f), sr.color, "persistent tint composes over base");
            Assert.AreEqual(baseColor.a, sr.color.a, 0.001f, "alpha preserved from base");
        }

        [Test]
        public void ClearPersistentTint_RestoresBase()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            f.SetPersistentTint(new Color(1f, 0f, 0f, 1f), 0.7f);
            f.ClearPersistentTint();
            AssertColorApprox(baseColor, sr.color, "clearing returns to the captured base");
        }

        // ── Flash priority ──

        [Test]
        public void Flash_DominatesWhileActive_ThenShowsPersistentTint()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            var tint = new Color(0.7f, 0.9f, 0.75f, 1f);
            f.SetPersistentTint(tint, 0.4f);

            f.Flash();
            Assert.IsTrue(f.IsFlashing);
            Assert.AreNotEqual(Compose(baseColor, tint, 0.4f), sr.color, "flash dominates over the persistent tint");

            f.Tick(1f);
            Assert.IsFalse(f.IsFlashing);
            AssertColorApprox(Compose(baseColor, tint, 0.4f), sr.color, "after flash, the persistent tint shows");
        }

        [Test]
        public void Flash_WithNoPersistentTint_RestoresBase()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            f.Flash();
            f.Tick(1f);
            AssertColorApprox(baseColor, sr.color, "no persistent tint -> flash restores base");
        }

        [Test]
        public void SetPersistentTint_DuringFlash_UsesLatestWhenFlashEnds()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            f.Flash();
            var first = new Color(1f, 0f, 0f, 1f);
            var second = new Color(0f, 1f, 0f, 1f);
            f.SetPersistentTint(first, 0.5f);   // ignored visually while flashing
            f.SetPersistentTint(second, 0.5f);  // latest wins
            Assert.IsTrue(f.IsFlashing, "still flashing; persistent writes do not disturb it");
            f.Tick(1f);
            AssertColorApprox(Compose(baseColor, second, 0.5f), sr.color, "flash end applies the most recent tint");
        }

        [Test]
        public void ChangePersistentTint_DuringFlash_DoesNotInterruptFlash()
        {
            var f = NewFlash(out SpriteRenderer sr, new Color(0.2f, 0.3f, 0.4f, 1f));
            f.Flash();
            Color flashing = sr.color;
            f.SetPersistentTint(new Color(0f, 1f, 0f, 1f), 0.6f);
            Assert.AreEqual(flashing, sr.color, "the visible flash color is unchanged by a persistent write");
        }

        // ── Capture-once ──

        [Test]
        public void Base_CapturedOnce_NeverACompositedOrTempColor()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            // Apply a tint, then a flash, then clear — the restored base must be the original.
            f.SetPersistentTint(new Color(1f, 0f, 0f, 1f), 0.9f);
            f.Flash();
            f.Tick(1f);
            f.ClearPersistentTint();
            AssertColorApprox(baseColor, sr.color, "base is the original, never a temp/composed color");
        }

        // ── Legacy terminal tint still works ──

        [Test]
        public void LegacyTerminalTint_PersistsAndClears()
        {
            var f = NewFlash(out SpriteRenderer sr, Color.white);
            f.SetTerminalTint(true);
            Assert.IsTrue(f.TerminalHeld);
            Assert.AreNotEqual(Color.white, sr.color);
            f.SetTerminalTint(false);
            AssertColorApprox(Color.white, sr.color, "terminal tint clears back to base");
            Assert.IsFalse(f.TerminalHeld);
        }

        // ── Null safety ──

        [Test]
        public void NullRenderer_DoesNotThrow()
        {
            var go = new GameObject("NoSprite");
            temp.Add(go);
            var f = go.AddComponent<SpriteFlash>();
            CreatureTestKit.SetPrivate(f, "flashDuration", 0.1f);
            CreatureTestKit.Invoke(f, "Awake");
            Assert.DoesNotThrow(() =>
            {
                f.SetPersistentTint(Color.red, 0.5f);
                f.Flash();
                f.Tick(1f);
                f.ClearPersistentTint();
            });
        }

        [Test]
        public void OnDisable_RestoresSafely_NotStuckOnFlash()
        {
            var baseColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            var f = NewFlash(out SpriteRenderer sr, baseColor);
            f.SetPersistentTint(new Color(0.7f, 0.9f, 0.75f, 1f), 0.4f);
            f.Flash();
            Assert.IsTrue(f.IsFlashing);
            CreatureTestKit.Invoke(f, "OnDisable");
            AssertColorApprox(Compose(baseColor, new Color(0.7f, 0.9f, 0.75f, 1f), 0.4f), sr.color,
                "disable ends the flash and shows the persistent tint, never a stuck flash color");
        }
    }
}

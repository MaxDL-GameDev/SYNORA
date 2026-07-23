using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Combat;

namespace Synora.Tests
{
    public sealed class HealthTests
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

        private Health NewHealth(float max)
        {
            var go = new GameObject("Health");
            temp.Add(go);
            var h = go.AddComponent<Health>();
            CreatureTestKit.SetPrivate(h, "maxHealth", max);
            h.ResetHealth(); // deterministic initialization (Awake is not auto-run in EditMode)
            return h;
        }

        // ── Health component ──

        [Test]
        public void StartsAtFullHealth()
        {
            var h = NewHealth(5f);
            Assert.AreEqual(5f, h.Max);
            Assert.AreEqual(5f, h.Current);
            Assert.IsFalse(h.IsZero);
            Assert.AreEqual(1f, h.Normalized);
        }

        [Test]
        public void ReceivesValidDamage()
        {
            var h = NewHealth(5f);
            h.ApplyDamage(new DamageInfo(2f));
            Assert.AreEqual(3f, h.Current);
            Assert.IsFalse(h.IsZero);
        }

        [Test]
        public void ZeroDamage_DoesNotChangeHealth()
        {
            var h = NewHealth(5f);
            h.ApplyDamage(new DamageInfo(0f));
            Assert.AreEqual(5f, h.Current);
        }

        [Test]
        public void NegativeDamage_DoesNotHeal()
        {
            var h = NewHealth(5f);
            h.ApplyDamage(new DamageInfo(2f)); // -> 3
            h.ApplyDamage(new DamageInfo(-10f)); // normalized to 0 -> stays 3
            Assert.AreEqual(3f, h.Current);
        }

        [Test]
        public void ExcessDamage_ClampsToZero()
        {
            var h = NewHealth(3f);
            h.ApplyDamage(new DamageInfo(100f));
            Assert.AreEqual(0f, h.Current);
            Assert.IsTrue(h.IsZero);
        }

        [Test]
        public void MultipleHits_Accumulate()
        {
            var h = NewHealth(5f);
            h.ApplyDamage(new DamageInfo(2f));
            h.ApplyDamage(new DamageInfo(2f));
            Assert.AreEqual(1f, h.Current);
        }

        [Test]
        public void StaysAtZero_AfterDepleted()
        {
            var h = NewHealth(3f);
            h.ApplyDamage(new DamageInfo(3f));
            Assert.AreEqual(0f, h.Current);
            h.ApplyDamage(new DamageInfo(2f));
            Assert.AreEqual(0f, h.Current);
        }

        [Test]
        public void ResetHealth_RestoresFull()
        {
            var h = NewHealth(5f);
            h.ApplyDamage(new DamageInfo(4f)); // -> 1
            h.ResetHealth();
            Assert.AreEqual(5f, h.Current);
        }

        [Test]
        public void TwoInstances_HaveIndependentState()
        {
            var a = NewHealth(5f);
            var b = NewHealth(5f);
            a.ApplyDamage(new DamageInfo(3f));
            Assert.AreEqual(2f, a.Current);
            Assert.AreEqual(5f, b.Current, "Instances must not share state.");
        }

        [Test]
        public void InvalidMax_NormalizesDeterministically()
        {
            var negative = NewHealth(-3f);
            Assert.AreEqual(1f, negative.Max);
            Assert.AreEqual(1f, negative.Current);

            var zero = NewHealth(0f);
            Assert.AreEqual(1f, zero.Max);
            Assert.AreEqual(1f, zero.Current);
        }

        [Test]
        public void IDamageable_MatchesDirectCall_AndDoesNotInterpretSource()
        {
            var h = NewHealth(5f);
            IDamageable d = h;
            d.ApplyDamage(new DamageInfo(2f, DamageSourceKind.Player));
            // Health applies the amount and ignores the source (it does not interpret it).
            Assert.AreEqual(3f, h.Current);
        }

        [Test]
        public void NoAutoRestore_NoEnableDisableHooks()
        {
            // Policy: health is not restored on enable/disable. Proven structurally:
            // Health defines no OnEnable/OnDisable that could refill it.
            Assert.IsNull(typeof(Health).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            Assert.IsNull(typeof(Health).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

            var h = NewHealth(5f);
            h.ApplyDamage(new DamageInfo(2f));
            h.gameObject.SetActive(false);
            h.gameObject.SetActive(true);
            Assert.AreEqual(3f, h.Current);
        }

        // ── Zero signal (Depleted) ──

        [Test]
        public void Depleted_RaisedOnce_WhenCrossingToZero()
        {
            var h = NewHealth(3f);
            int count = 0;
            h.Depleted += () => count++;
            h.ApplyDamage(new DamageInfo(3f));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Depleted_NotRaised_OnPartialDamage()
        {
            var h = NewHealth(5f);
            int count = 0;
            h.Depleted += () => count++;
            h.ApplyDamage(new DamageInfo(2f));
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Depleted_NotRaised_OnZeroDamage()
        {
            var h = NewHealth(5f);
            int count = 0;
            h.Depleted += () => count++;
            h.ApplyDamage(new DamageInfo(0f));
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Depleted_NotRaisedAgain_WhenAlreadyZero()
        {
            var h = NewHealth(3f);
            int count = 0;
            h.Depleted += () => count++;
            h.ApplyDamage(new DamageInfo(3f)); // -> 0, raises once
            h.ApplyDamage(new DamageInfo(2f)); // already zero, no raise
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ResetHealth_ReArmsDepleted()
        {
            var h = NewHealth(3f);
            int count = 0;
            h.Depleted += () => count++;
            h.ApplyDamage(new DamageInfo(3f)); // raise 1
            h.ResetHealth();                    // no raise, re-arms
            h.ApplyDamage(new DamageInfo(3f)); // raise 2
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Depleted_IsPerInstance()
        {
            var a = NewHealth(3f);
            var b = NewHealth(3f);
            int aCount = 0, bCount = 0;
            a.Depleted += () => aCount++;
            b.Depleted += () => bCount++;
            a.ApplyDamage(new DamageInfo(3f));
            Assert.AreEqual(1, aCount);
            Assert.AreEqual(0, bCount, "Signals must be independent per instance.");
        }

        // ── Pure logic (static) ──

        [Test]
        public void NormalizeMaxHealth_Cases()
        {
            Assert.AreEqual(5f, Health.NormalizeMaxHealth(5f));
            Assert.AreEqual(1f, Health.NormalizeMaxHealth(0f));
            Assert.AreEqual(1f, Health.NormalizeMaxHealth(-4f));
        }

        [Test]
        public void ComputeDamaged_WithinRange()
        {
            Assert.AreEqual(3f, Health.ComputeDamaged(5f, 2f, 5f));
        }

        [Test]
        public void ComputeDamaged_ExcessClampsToZero()
        {
            Assert.AreEqual(0f, Health.ComputeDamaged(3f, 100f, 3f));
        }

        [Test]
        public void ComputeDamaged_ZeroAmount_Unchanged()
        {
            Assert.AreEqual(5f, Health.ComputeDamaged(5f, 0f, 5f));
        }

        [Test]
        public void ComputeDamaged_NegativeAmount_TreatedAsZero()
        {
            Assert.AreEqual(5f, Health.ComputeDamaged(5f, -10f, 5f));
        }

        [Test]
        public void ComputeDamaged_NeverExceedsMax()
        {
            Assert.AreEqual(5f, Health.ComputeDamaged(8f, 0f, 5f));
        }
    }
}

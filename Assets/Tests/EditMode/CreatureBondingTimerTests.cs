using NUnit.Framework;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureBondingTimerTests
    {
        [Test]
        public void NotComplete_BeforeReachingDuration()
        {
            var t = new CreatureBondingTimer(1.25f);
            t.Tick(0.5f);
            Assert.IsFalse(t.IsComplete);
        }

        [Test]
        public void Complete_AtExactDuration()
        {
            var t = new CreatureBondingTimer(1.0f);
            t.Tick(1.0f);
            Assert.IsTrue(t.IsComplete);
        }

        [Test]
        public void Complete_OnOvershoot()
        {
            var t = new CreatureBondingTimer(1.0f);
            t.Tick(5.0f);
            Assert.IsTrue(t.IsComplete);
        }

        [Test]
        public void AccumulatesMultipleSteps()
        {
            var t = new CreatureBondingTimer(1.0f);
            t.Tick(0.4f);
            t.Tick(0.4f);
            Assert.IsFalse(t.IsComplete, "0.8 < 1.0");
            t.Tick(0.4f);
            Assert.IsTrue(t.IsComplete, "1.2 >= 1.0");
        }

        [Test]
        public void ZeroDuration_CompleteImmediately()
        {
            var t = new CreatureBondingTimer(0f);
            Assert.IsTrue(t.IsComplete, "A zero duration is complete before any tick.");
        }

        [Test]
        public void NegativeDuration_NormalizedToZero_CompleteImmediately()
        {
            var t = new CreatureBondingTimer(-3f);
            Assert.IsTrue(t.IsComplete);
        }

        [Test]
        public void TickAfterComplete_StaysComplete()
        {
            var t = new CreatureBondingTimer(1.0f);
            t.Tick(1.0f);
            Assert.IsTrue(t.IsComplete);
            t.Tick(1.0f); // idempotent
            Assert.IsTrue(t.IsComplete);
        }

        [Test]
        public void NegativeDeltaTime_DoesNotAdvance()
        {
            var t = new CreatureBondingTimer(1.0f);
            t.Tick(-5f);
            Assert.IsFalse(t.IsComplete, "Negative dt must not advance (nor push backwards).");
            t.Tick(1.0f);
            Assert.IsTrue(t.IsComplete, "A valid step after a negative one still completes normally.");
        }

        [Test]
        public void Deterministic_SameStepsSameResult()
        {
            var a = new CreatureBondingTimer(1.0f);
            var b = new CreatureBondingTimer(1.0f);
            float[] steps = { 0.3f, 0.3f, 0.3f, 0.3f };
            foreach (float s in steps)
            {
                a.Tick(s);
                b.Tick(s);
                Assert.AreEqual(a.IsComplete, b.IsComplete, "Same inputs must yield the same state (no real-time/frame dependency).");
            }
            Assert.IsTrue(a.IsComplete);
        }

        [Test]
        public void Normalize_Cases()
        {
            Assert.AreEqual(1.25f, CreatureBondingTimer.Normalize(1.25f));
            Assert.AreEqual(0f, CreatureBondingTimer.Normalize(0f));
            Assert.AreEqual(0f, CreatureBondingTimer.Normalize(-2f));
        }
    }
}

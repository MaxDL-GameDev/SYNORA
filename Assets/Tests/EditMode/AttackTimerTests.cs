using NUnit.Framework;
using Synora.Gameplay.Player;

namespace Synora.Tests
{
    public sealed class AttackTimerTests
    {
        [Test]
        public void StartFromIdle_OpensWindow()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            Assert.IsTrue(t.CanStart);
            Assert.IsTrue(t.TryStart());
            Assert.AreEqual(AttackPhase.ActiveWindow, t.Phase);
            Assert.IsTrue(t.IsActiveWindow);
        }

        [Test]
        public void CannotStart_WhileActive_NoOverlap()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            Assert.IsTrue(t.TryStart());
            Assert.IsFalse(t.TryStart(), "No overlapping attacks.");
        }

        [Test]
        public void Window_ClosesAfterDuration_EntersCooldown()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            t.TryStart();
            t.Tick(0.2f);
            Assert.AreEqual(AttackPhase.Cooldown, t.Phase);
            Assert.IsFalse(t.IsActiveWindow);
        }

        [Test]
        public void Cooldown_ClosesAfterDuration_ReturnsIdle()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            t.TryStart();
            t.Tick(0.2f); // -> cooldown
            t.Tick(0.5f); // -> idle
            Assert.AreEqual(AttackPhase.Idle, t.Phase);
            Assert.IsTrue(t.CanStart);
        }

        [Test]
        public void CannotStart_DuringCooldown()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            t.TryStart();
            t.Tick(0.2f); // cooldown
            Assert.IsFalse(t.TryStart());
        }

        [Test]
        public void CanStart_AfterCooldownExpires()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            t.TryStart();
            t.Tick(0.2f);
            t.Tick(0.5f);
            Assert.IsTrue(t.TryStart());
        }

        [Test]
        public void ZeroWindow_ClosesOnFirstTick()
        {
            var t = new AttackTimer(0f, 0.5f);
            t.TryStart();
            Assert.IsTrue(t.IsActiveWindow, "Zero window is active until the first tick.");
            t.Tick(0.01f);
            Assert.AreEqual(AttackPhase.Cooldown, t.Phase);
        }

        [Test]
        public void ZeroCooldown_ReturnsIdleAfterWindow()
        {
            var t = new AttackTimer(0.2f, 0f);
            t.TryStart();
            t.Tick(0.2f);
            Assert.AreEqual(AttackPhase.Idle, t.Phase, "No cooldown -> straight back to Idle.");
            Assert.IsTrue(t.TryStart());
        }

        [Test]
        public void NegativeDurations_NormalizedToZero()
        {
            Assert.AreEqual(0f, AttackTimer.Normalize(-3f));
            var t = new AttackTimer(-1f, -1f);
            t.TryStart();
            t.Tick(0.001f);
            Assert.AreEqual(AttackPhase.Idle, t.Phase);
        }

        [Test]
        public void NegativeDt_TreatedAsZero()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            t.TryStart();
            t.Tick(-5f);
            Assert.AreEqual(AttackPhase.ActiveWindow, t.Phase, "Negative dt must not advance timing.");
        }

        [Test]
        public void Cancel_ReturnsToIdle()
        {
            var t = new AttackTimer(0.2f, 0.5f);
            t.TryStart();
            t.Cancel();
            Assert.AreEqual(AttackPhase.Idle, t.Phase);
            Assert.IsTrue(t.CanStart);
        }
    }
}

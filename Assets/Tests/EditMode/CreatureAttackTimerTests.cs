using NUnit.Framework;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureAttackTimerTests
    {
        private static CreatureAttackTimer New(float windup = 0.25f, float active = 0.15f, float cooldown = 0.8f)
            => new CreatureAttackTimer(windup, active, cooldown);

        [Test]
        public void Initial_IsReady()
        {
            var t = New();
            Assert.AreEqual(CreatureAttackPhase.Ready, t.Phase);
            Assert.IsTrue(t.CanStart);
            Assert.IsFalse(t.IsSequenceActive);
            Assert.IsFalse(t.IsActiveWindow);
        }

        [Test]
        public void Start_EntersWindup_SequenceActive_NotYetDamaging()
        {
            var t = New();
            Assert.IsTrue(t.TryStart());
            Assert.AreEqual(CreatureAttackPhase.Windup, t.Phase);
            Assert.IsTrue(t.IsSequenceActive);
            Assert.IsFalse(t.IsActiveWindow, "Windup does not damage.");
            Assert.IsFalse(t.CanStart);
        }

        [Test]
        public void CannotStart_WhileSequenceActive_NoOverlap()
        {
            var t = New();
            Assert.IsTrue(t.TryStart());
            Assert.IsFalse(t.TryStart(), "No overlapping attacks.");
        }

        [Test]
        public void Windup_Elapses_OpensActiveWindow()
        {
            var t = New(0.2f, 0.15f, 0.5f);
            t.TryStart();
            t.Tick(0.2f);
            Assert.AreEqual(CreatureAttackPhase.Active, t.Phase);
            Assert.IsTrue(t.IsActiveWindow);
        }

        [Test]
        public void Active_Elapses_EntersCooldown()
        {
            var t = New(0.2f, 0.15f, 0.5f);
            t.TryStart();
            t.Tick(0.2f); // -> active
            t.Tick(0.15f); // -> cooldown
            Assert.AreEqual(CreatureAttackPhase.Cooldown, t.Phase);
            Assert.IsFalse(t.IsActiveWindow);
            Assert.IsFalse(t.IsSequenceActive);
        }

        [Test]
        public void Cooldown_Elapses_ReturnsReady()
        {
            var t = New(0.2f, 0.15f, 0.5f);
            t.TryStart();
            t.Tick(0.2f);
            t.Tick(0.15f);
            t.Tick(0.5f);
            Assert.AreEqual(CreatureAttackPhase.Ready, t.Phase);
            Assert.IsTrue(t.CanStart);
        }

        [Test]
        public void CannotStart_DuringCooldown()
        {
            var t = New(0.2f, 0.15f, 0.5f);
            t.TryStart();
            t.Tick(0.2f);
            t.Tick(0.15f); // cooldown
            Assert.IsFalse(t.CanStart);
            Assert.IsFalse(t.TryStart());
        }

        [Test]
        public void BigDelta_StopsAtActiveOpen_WindowNeverSkipped()
        {
            var t = New(0.2f, 0.2f, 0.4f);
            t.TryStart();
            // A huge step crosses windup but MUST stop at the active open — a positive
            // active window is never skipped, however large the step.
            t.Tick(5f);
            Assert.AreEqual(CreatureAttackPhase.Active, t.Phase);
            Assert.IsTrue(t.IsActiveWindow);
            // Only the active window is stop-protected; a second huge step consumes the
            // rest (active + cooldown) straight to Ready.
            t.Tick(5f);
            Assert.AreEqual(CreatureAttackPhase.Ready, t.Phase);
        }

        [Test]
        public void ZeroActive_NeverObservable_CollapsesThrough()
        {
            var t = New(0.1f, 0f, 0.2f);
            t.TryStart();
            t.Tick(0.1f); // finish windup; zero active collapses straight to cooldown
            Assert.IsFalse(t.IsActiveWindow, "A zero-length active window is never observable.");
            Assert.AreEqual(CreatureAttackPhase.Cooldown, t.Phase);
        }

        [Test]
        public void ZeroWindup_ActiveOpensOnFirstTick()
        {
            var t = New(0f, 0.2f, 0.5f);
            t.TryStart();
            Assert.AreEqual(CreatureAttackPhase.Windup, t.Phase, "Zero windup is still committed until the first tick.");
            t.Tick(0.01f);
            Assert.AreEqual(CreatureAttackPhase.Active, t.Phase);
        }

        [Test]
        public void ZeroCooldown_ReturnsReadyAfterActive()
        {
            var t = New(0.1f, 0.1f, 0f);
            t.TryStart();
            t.Tick(0.1f); // -> active
            t.Tick(0.1f); // active done, zero cooldown -> ready
            Assert.AreEqual(CreatureAttackPhase.Ready, t.Phase);
            Assert.IsTrue(t.CanStart);
        }

        [Test]
        public void NegativeDurations_NormalizedToZero()
        {
            Assert.AreEqual(0f, CreatureAttackTimer.Normalize(-2f));
            var t = new CreatureAttackTimer(-1f, -1f, -1f);
            t.TryStart();
            t.Tick(0.001f);
            Assert.AreEqual(CreatureAttackPhase.Ready, t.Phase, "All-zero durations collapse straight back to Ready.");
        }

        [Test]
        public void NegativeDt_DoesNotAdvance()
        {
            var t = New(0.2f, 0.2f, 0.2f);
            t.TryStart();
            t.Tick(-5f);
            Assert.AreEqual(CreatureAttackPhase.Windup, t.Phase);
        }

        [Test]
        public void Cancel_ReturnsToReady()
        {
            var t = New();
            t.TryStart();
            t.Tick(0.25f); // active
            t.Cancel();
            Assert.AreEqual(CreatureAttackPhase.Ready, t.Phase);
            Assert.IsTrue(t.CanStart);
        }
    }
}

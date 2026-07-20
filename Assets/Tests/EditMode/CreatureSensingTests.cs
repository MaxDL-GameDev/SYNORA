using NUnit.Framework;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreatureSensingTests
    {
        // detection = 3, lose = 4  => detectionSqr = 9, loseSqr = 16
        private const float Detection = 3f;
        private const float Lose = 4f;
        private const float Linger = 1.5f;

        [Test]
        public void Evaluate_NoPlayer_NotAlert_RemainsCalm()
        {
            var v = CreatureSensing.Evaluate(false, -1f, Detection, Lose, 0f, Linger);
            Assert.AreEqual(SensorVerdict.RemainCalm, v);
        }

        [Test]
        public void Evaluate_PlayerExactlyAtDetectionRadius_BecomesAlert()
        {
            var v = CreatureSensing.Evaluate(false, Detection * Detection, Detection, Lose, 0f, Linger);
            Assert.AreEqual(SensorVerdict.BecomeAlert, v);
        }

        [Test]
        public void Evaluate_PlayerJustOutsideDetection_RemainsCalm()
        {
            float justOutside = Detection * Detection + 0.01f;
            var v = CreatureSensing.Evaluate(false, justOutside, Detection, Lose, 0f, Linger);
            Assert.AreEqual(SensorVerdict.RemainCalm, v);
        }

        [Test]
        public void Evaluate_Alert_PlayerInsideLose_RemainsAlert()
        {
            // Between detection and lose radii: still tracked while alert.
            float between = 3.5f * 3.5f;
            var v = CreatureSensing.Evaluate(true, between, Detection, Lose, 0f, Linger);
            Assert.AreEqual(SensorVerdict.RemainAlert, v);
        }

        [Test]
        public void Evaluate_Alert_PlayerExactlyAtLoseRadius_RemainsAlert()
        {
            var v = CreatureSensing.Evaluate(true, Lose * Lose, Detection, Lose, 0f, Linger);
            Assert.AreEqual(SensorVerdict.RemainAlert, v);
        }

        [Test]
        public void Evaluate_Alert_PlayerOutsideLose_LingerNotExpired_RemainsAlert()
        {
            float outside = Lose * Lose + 1f;
            var v = CreatureSensing.Evaluate(true, outside, Detection, Lose, 0.5f, Linger);
            Assert.AreEqual(SensorVerdict.RemainAlert, v);
        }

        [Test]
        public void Evaluate_Alert_PlayerOutsideLose_LingerExactlyExpired_ReturnsToPatrol()
        {
            float outside = Lose * Lose + 1f;
            var v = CreatureSensing.Evaluate(true, outside, Detection, Lose, Linger, Linger);
            Assert.AreEqual(SensorVerdict.ReturnToPatrol, v);
        }

        [Test]
        public void Evaluate_Alert_PlayerAbsent_LingerExpired_ReturnsToPatrol()
        {
            var v = CreatureSensing.Evaluate(true, -1f, Detection, Lose, Linger + 1f, Linger);
            Assert.AreEqual(SensorVerdict.ReturnToPatrol, v);
        }

        [Test]
        public void Evaluate_Alert_PlayerReentersBeforeExpiry_RemainsAlert()
        {
            // Reentry inside lose while linger was counting -> stays alert.
            float inside = 2f * 2f;
            var v = CreatureSensing.Evaluate(true, inside, Detection, Lose, 1.0f, Linger);
            Assert.AreEqual(SensorVerdict.RemainAlert, v);
        }

        [Test]
        public void Evaluate_NegativeParameters_HandledSafely()
        {
            // Negative radii clamp to 0; negative linger clamps to 0.
            var v = CreatureSensing.Evaluate(false, 0f, -3f, -1f, 0f, -2f);
            // detection clamped to 0 -> only exact-on (distSqr <= 0) triggers; 0 <= 0 => alert.
            Assert.AreEqual(SensorVerdict.BecomeAlert, v);
        }

        [Test]
        public void Evaluate_LoseSmallerThanDetection_ForcedToDetection()
        {
            // lose (1) < detection (3): enforced lose = detection = 3 => loseSqr = 9.
            float atThree = 3f * 3f;
            var v = CreatureSensing.Evaluate(true, atThree, 3f, 1f, 0f, Linger);
            Assert.AreEqual(SensorVerdict.RemainAlert, v);
        }

        [Test]
        public void ShouldResetLinger_InsideLose_True_OutsideOrAbsent_False()
        {
            Assert.IsTrue(CreatureSensing.ShouldResetLinger(2f * 2f, Lose));
            Assert.IsTrue(CreatureSensing.ShouldResetLinger(Lose * Lose, Lose)); // border inside
            Assert.IsFalse(CreatureSensing.ShouldResetLinger(Lose * Lose + 1f, Lose));
            Assert.IsFalse(CreatureSensing.ShouldResetLinger(-1f, Lose));        // absent
        }
    }
}

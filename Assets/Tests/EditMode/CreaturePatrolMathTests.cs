using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    public sealed class CreaturePatrolMathTests
    {
        [Test]
        public void HasArrived_InsideThreshold_ReturnsTrue()
        {
            Assert.IsTrue(CreaturePatrolMath.HasArrived(new Vector2(0f, 0f), new Vector2(0.05f, 0f), 0.1f));
        }

        [Test]
        public void HasArrived_OutsideThreshold_ReturnsFalse()
        {
            Assert.IsFalse(CreaturePatrolMath.HasArrived(new Vector2(0f, 0f), new Vector2(0.5f, 0f), 0.1f));
        }

        [Test]
        public void HasArrived_ExactlyAtThreshold_ReturnsTrue()
        {
            // Distance == threshold must count as arrived (<=). Uses exactly
            // representable floats (0.5, 0.25) to test the boundary without ULP noise.
            Assert.IsTrue(CreaturePatrolMath.HasArrived(new Vector2(0f, 0f), new Vector2(0.5f, 0f), 0.5f));
        }

        [Test]
        public void HasArrived_InvalidThreshold_TreatedAsExactMatch()
        {
            // Non-positive threshold -> arrived only when exactly on target.
            Assert.IsFalse(CreaturePatrolMath.HasArrived(new Vector2(0f, 0f), new Vector2(0.01f, 0f), -1f));
            Assert.IsTrue(CreaturePatrolMath.HasArrived(new Vector2(2f, 2f), new Vector2(2f, 2f), -1f));
        }

        [Test]
        public void NextPingPongIndex_ZeroPoints_ReturnsZeroSafely()
        {
            int dir = -3;
            Assert.AreEqual(0, CreaturePatrolMath.NextPingPongIndex(5, 0, ref dir));
            Assert.AreEqual(1, dir);
        }

        [Test]
        public void NextPingPongIndex_OnePoint_ReturnsZero()
        {
            int dir = 1;
            Assert.AreEqual(0, CreaturePatrolMath.NextPingPongIndex(0, 1, ref dir));
        }

        [Test]
        public void NextPingPongIndex_TwoPoints_Oscillates()
        {
            int dir = 1;
            int i = 0;
            i = CreaturePatrolMath.NextPingPongIndex(i, 2, ref dir); // 0 -> 1
            Assert.AreEqual(1, i);
            Assert.AreEqual(1, dir);
            i = CreaturePatrolMath.NextPingPongIndex(i, 2, ref dir); // 1 -> 0 (bounce at high end)
            Assert.AreEqual(0, i);
            Assert.AreEqual(-1, dir);                                // direction inverted at the high end
            i = CreaturePatrolMath.NextPingPongIndex(i, 2, ref dir); // 0 -> 1 (bounce at low end)
            Assert.AreEqual(1, i);
            Assert.AreEqual(1, dir);
        }

        [Test]
        public void NextPingPongIndex_NPoints_FullCycleStaysInRange()
        {
            const int count = 4;
            int dir = 1;
            int i = 0;
            int[] expected = { 1, 2, 3, 2, 1, 0, 1 };
            foreach (int exp in expected)
            {
                i = CreaturePatrolMath.NextPingPongIndex(i, count, ref dir);
                Assert.AreEqual(exp, i);
                Assert.GreaterOrEqual(i, 0);
                Assert.Less(i, count);
            }
        }

        [Test]
        public void NextPingPongIndex_BouncesAtHighEnd()
        {
            int dir = 1;
            int i = CreaturePatrolMath.NextPingPongIndex(2, 3, ref dir); // last -> N-2
            Assert.AreEqual(1, i);
            Assert.AreEqual(-1, dir);
        }

        [Test]
        public void NextPingPongIndex_InvalidDirectionNormalizedToForward()
        {
            int dir = 0; // invalid -> +1
            int i = CreaturePatrolMath.NextPingPongIndex(0, 3, ref dir);
            Assert.AreEqual(1, i);
            Assert.AreEqual(1, dir);
        }

        [Test]
        public void NextPingPongIndex_OutOfRangeInputClampedNeverOutOfRange()
        {
            int dir = 1;
            int i = CreaturePatrolMath.NextPingPongIndex(99, 3, ref dir); // clamp 99 -> 2, then bounce
            Assert.GreaterOrEqual(i, 0);
            Assert.Less(i, 3);

            dir = 1;
            i = CreaturePatrolMath.NextPingPongIndex(-99, 3, ref dir); // clamp -99 -> 0, then +1
            Assert.AreEqual(1, i);
        }
    }
}

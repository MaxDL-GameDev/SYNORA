using NUnit.Framework;
using Synora.Gameplay.Combat;

namespace Synora.Tests
{
    public sealed class DamageInfoTests
    {
        [Test]
        public void PositiveAmount_IsPreserved()
        {
            Assert.AreEqual(5f, new DamageInfo(5f).Amount);
        }

        [Test]
        public void ZeroAmount_IsZero()
        {
            Assert.AreEqual(0f, new DamageInfo(0f).Amount);
        }

        [Test]
        public void NegativeAmount_NormalizesToZero()
        {
            // Damage never heals.
            Assert.AreEqual(0f, new DamageInfo(-7f).Amount);
        }

        [Test]
        public void SourceKind_IsPreserved()
        {
            Assert.AreEqual(DamageSourceKind.Player, new DamageInfo(3f, DamageSourceKind.Player).SourceKind);
            Assert.AreEqual(DamageSourceKind.Creature, new DamageInfo(3f, DamageSourceKind.Creature).SourceKind);
        }

        [Test]
        public void DefaultSourceKind_IsSafeUnknown()
        {
            Assert.AreEqual(DamageSourceKind.Unknown, new DamageInfo(3f).SourceKind);
            // Default struct value is also Unknown / zero amount.
            Assert.AreEqual(DamageSourceKind.Unknown, default(DamageInfo).SourceKind);
            Assert.AreEqual(0f, default(DamageInfo).Amount);
        }

        [Test]
        public void IsImmutable_ValuesStable()
        {
            var d = new DamageInfo(3f, DamageSourceKind.Player);
            Assert.AreEqual(3f, d.Amount);
            Assert.AreEqual(DamageSourceKind.Player, d.SourceKind);
            // read-only struct: no mutation path exists
            Assert.AreEqual(3f, d.Amount);
        }
    }
}

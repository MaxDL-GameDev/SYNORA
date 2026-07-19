using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Interaction;

namespace Synora.Tests
{
    public sealed class InteractionGeometryTests
    {
        private const float Range = 1.25f;
        private const float HalfWidth = 0.4f;

        [Test]
        public void InteractionGeometry_CandidateBehind_ReturnsFalse()
        {
            // Facing up; the candidate sits behind the origin (negative forward).
            Vector2 origin = Vector2.zero;
            Vector2 facing = Vector2.up;
            Vector2 candidate = new Vector2(0f, -1f);

            bool inside = InteractionGeometry.IsInsideFrontZone(
                origin, facing, candidate, Range, HalfWidth);

            Assert.IsFalse(inside,
                "A candidate behind the facing direction must be outside the front zone.");
        }

        [Test]
        public void InteractionGeometry_CandidateBeyondRange_ReturnsFalse()
        {
            // Facing up; the candidate is directly ahead but just past the range.
            Vector2 origin = Vector2.zero;
            Vector2 facing = Vector2.up;
            Vector2 candidate = new Vector2(0f, Range + 0.05f);

            bool inside = InteractionGeometry.IsInsideFrontZone(
                origin, facing, candidate, Range, HalfWidth);

            Assert.IsFalse(inside,
                "A candidate beyond the detection range must be outside the front zone.");
        }
    }
}

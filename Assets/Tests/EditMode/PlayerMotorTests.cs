using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Player;

namespace Synora.Tests
{
    public sealed class PlayerMotorTests
    {
        private const float MoveSpeed = 4.5f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void CalculateVelocity_DiagonalInputDoesNotIncreaseSpeed()
        {
            // Arrange & Act
            Vector2 diagonal = PlayerMotor.CalculateVelocity(new Vector2(1f, 1f), MoveSpeed);
            Vector2 cardinal = PlayerMotor.CalculateVelocity(new Vector2(1f, 0f), MoveSpeed);

            // Assert
            Assert.That(diagonal.magnitude, Is.EqualTo(MoveSpeed).Within(Tolerance),
                "Diagonal velocity magnitude should equal MoveSpeed.");
            Assert.That(diagonal.magnitude, Is.LessThanOrEqualTo(cardinal.magnitude + Tolerance),
                "Diagonal must not be faster than a full cardinal input.");
        }

        [Test]
        public void CalculateVelocity_InputAtOrAboveUnitMagnitudeUsesMoveSpeed()
        {
            // Arrange
            Vector2[] inputs =
            {
                new Vector2(1f, 0f),
                new Vector2(0f, -1f),
                new Vector2(2f, 0f),
                new Vector2(-3f, 4f),
            };

            // Act & Assert
            foreach (Vector2 input in inputs)
            {
                Vector2 result = PlayerMotor.CalculateVelocity(input, MoveSpeed);
                Assert.That(result.magnitude, Is.EqualTo(MoveSpeed).Within(Tolerance),
                    "Input " + input + " (magnitude >= 1) should produce MoveSpeed magnitude.");
            }

            Vector2 zero = PlayerMotor.CalculateVelocity(Vector2.zero, MoveSpeed);
            Assert.That(zero.magnitude, Is.EqualTo(0f).Within(Tolerance),
                "Zero input should produce zero velocity.");
        }
    }
}

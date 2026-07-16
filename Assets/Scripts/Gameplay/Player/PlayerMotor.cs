using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Physical locomotion. Consumes the input reader's value in FixedUpdate and
    /// drives the Rigidbody2D velocity. Never writes Transform.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private PlayerMovement movement;

        [SerializeField]
        private Rigidbody2D body;

        private void FixedUpdate()
        {
            body.linearVelocity = CalculateVelocity(inputReader.MoveInput, movement.MoveSpeed);
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Deterministic velocity: input magnitude clamped to 1, scaled by speed.
        /// Diagonals never exceed cardinal speed. No MonoBehaviour, Time, Input
        /// System or global state involved.
        /// </summary>
        public static Vector2 CalculateVelocity(Vector2 input, float moveSpeed)
        {
            if (input.sqrMagnitude > 1f)
            {
                input = input.normalized;
            }

            return input * moveSpeed;
        }
    }
}

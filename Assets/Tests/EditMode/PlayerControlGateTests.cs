using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Data;
using Synora.Gameplay.Player;
using Synora.Systems;

namespace Synora.Tests
{
    public sealed class PlayerControlGateTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float Tolerance = 0.0001f;

        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i] != null)
                {
                    Object.DestroyImmediate(temp[i]);
                }
            }
            temp.Clear();
        }

        [Test]
        public void PlayerControlGate_ObservationBlockStopsMotorAndClearsVelocity()
        {
            // Arrange: build the GameObject inactive so no lifecycle (OnEnable/Awake)
            // runs while dependencies are being wired.
            GameObject go = new GameObject("PlayerMotorGateTest");
            temp.Add(go);
            go.SetActive(false);

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            PlayerControlGate gate = go.AddComponent<PlayerControlGate>();
            PlayerInputReader inputReader = go.AddComponent<PlayerInputReader>();
            PlayerMotor motor = go.AddComponent<PlayerMotor>();

            PlayerMovement movement = ScriptableObject.CreateInstance<PlayerMovement>();
            temp.Add(movement);

            typeof(PlayerMotor).GetField("inputReader", PrivateInstance).SetValue(motor, inputReader);
            typeof(PlayerMotor).GetField("movement", PrivateInstance).SetValue(motor, movement);
            typeof(PlayerMotor).GetField("body", PrivateInstance).SetValue(motor, body);
            typeof(PlayerMotor).GetField("gate", PrivateInstance).SetValue(motor, gate);

            body.linearVelocity = new Vector2(3f, -2f);

            // Act: block control, then run one FixedUpdate via the project's
            // established reflection pattern for private lifecycle methods.
            gate.Block(ControlBlockReason.Observation);
            Assert.IsTrue(gate.IsBlocked, "Gate should be blocked after Block(Observation).");

            typeof(PlayerMotor).GetMethod("FixedUpdate", PrivateInstance).Invoke(motor, null);

            // Assert: a blocked gate stops the motor and clears velocity.
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(0f).Within(Tolerance),
                "A blocked gate must clear the Rigidbody2D velocity.");

            // Unblock releases only the received reason.
            gate.Unblock(ControlBlockReason.Observation);
            Assert.IsFalse(gate.IsBlocked, "Gate should be unblocked after Unblock(Observation).");
        }
    }
}

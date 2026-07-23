using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Player;

namespace Synora.Tests
{
    // Structural checks that PlayerInputReader gained a discrete Attack event without
    // changing Move (polling) or absorbing Interact, plus a content guard on the
    // Attack input action. Raw Input System firing is not unit-tested (project convention).
    public sealed class PlayerInputReaderTests
    {
        [Test]
        public void HasDiscreteAttackPressedEvent()
        {
            EventInfo e = typeof(PlayerInputReader).GetEvent("AttackPressed");
            Assert.IsNotNull(e, "PlayerInputReader must expose a discrete AttackPressed event.");
        }

        [Test]
        public void HasAttackActionReferenceField()
        {
            FieldInfo f = typeof(PlayerInputReader).GetField("attackAction",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, "PlayerInputReader must serialize an attackAction reference.");
            Assert.AreEqual("InputActionReference", f.FieldType.Name);
        }

        [Test]
        public void MoveStaysPolling()
        {
            Assert.IsNotNull(typeof(PlayerInputReader).GetProperty("MoveInput"),
                "Move must remain exposed as a polled value.");
            Assert.IsNotNull(typeof(PlayerInputReader).GetField("moveAction",
                BindingFlags.Instance | BindingFlags.NonPublic), "moveAction field must remain.");
        }

        [Test]
        public void DoesNotAbsorbInteract()
        {
            // Interact stays in InteractionInputReader; PlayerInputReader must not gain it.
            FieldInfo[] fields = typeof(PlayerInputReader).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo f in fields)
            {
                Assert.IsFalse(f.Name.ToLowerInvariant().Contains("interact"),
                    "Interact must not move into PlayerInputReader: " + f.Name);
            }
        }

        [Test]
        public void AttackAction_ExistsInGameplayMap_DistinctFromInteract()
        {
            string path = Application.dataPath + "/Input/Controls.inputactions";
            Assert.IsTrue(System.IO.File.Exists(path), "Controls.inputactions must exist.");
            string json = System.IO.File.ReadAllText(path);

            Assert.IsTrue(json.Contains("\"name\": \"Attack\""), "An Attack action must exist.");
            Assert.IsTrue(json.Contains("\"action\": \"Attack\""), "Attack must have at least one binding.");
            Assert.IsTrue(json.Contains("<Keyboard>/j"), "Attack keyboard binding expected.");

            // Interact remains, and its binding is distinct from Attack's.
            Assert.IsTrue(json.Contains("\"action\": \"Interact\""), "Interact action must remain.");
            Assert.IsTrue(json.Contains("<Keyboard>/e"), "Interact keyboard binding must remain.");
        }
    }
}

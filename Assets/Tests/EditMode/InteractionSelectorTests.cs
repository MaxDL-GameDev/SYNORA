using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Synora.Gameplay.Interaction;

namespace Synora.Tests
{
    public sealed class InteractionSelectorTests
    {
        /// <summary>Pure IInteractable stub — no GameObject, no MonoBehaviour.</summary>
        private sealed class FakeInteractable : IInteractable
        {
            public string InteractionId { get; set; } = string.Empty;
            public int Priority { get; set; }
            public bool CanInteract { get; set; } = true;
            public Vector2 InteractionPosition { get; set; }
            public string PromptText => "Examinar";
            public void Execute(IInteractionReceiver receiver) { }
        }

        [Test]
        public void InteractionSelector_HigherPriorityWins()
        {
            var near = new FakeInteractable { InteractionId = "near", Priority = 1, InteractionPosition = new Vector2(0.5f, 0f) };
            var farHigh = new FakeInteractable { InteractionId = "far", Priority = 5, InteractionPosition = new Vector2(5f, 0f) };
            var candidates = new List<IInteractable> { near, farHigh };

            IInteractable result = InteractionSelector.SelectTarget(candidates, null, Vector2.zero);

            Assert.AreSame(farHigh, result, "Higher priority must win even when farther away.");
        }

        [Test]
        public void InteractionSelector_EqualPriorityNearestWins()
        {
            var far = new FakeInteractable { InteractionId = "far", Priority = 0, InteractionPosition = new Vector2(2f, 0f) };
            var near = new FakeInteractable { InteractionId = "near", Priority = 0, InteractionPosition = new Vector2(1f, 0f) };
            var candidates = new List<IInteractable> { far, near };

            IInteractable result = InteractionSelector.SelectTarget(candidates, null, Vector2.zero);

            Assert.AreSame(near, result, "With equal priority, the nearest candidate must win.");
        }

        [Test]
        public void InteractionSelector_EqualPriorityAndDistanceOrdinalIdWins()
        {
            // Equal priority and equal squared distance; lower ordinal id must win,
            // independent of input order.
            var a = new FakeInteractable { InteractionId = "a", Priority = 0, InteractionPosition = new Vector2(1f, 0f) };
            var b = new FakeInteractable { InteractionId = "b", Priority = 0, InteractionPosition = new Vector2(-1f, 0f) };

            IInteractable forward = InteractionSelector.SelectTarget(
                new List<IInteractable> { a, b }, null, Vector2.zero);
            IInteractable reversed = InteractionSelector.SelectTarget(
                new List<IInteractable> { b, a }, null, Vector2.zero);

            Assert.AreSame(a, forward, "Lower ordinal InteractionId must win.");
            Assert.AreSame(a, reversed, "The result must not depend on input order.");
        }

        [Test]
        public void InteractionSelector_CurrentValidTargetIsPreserved()
        {
            var current = new FakeInteractable { InteractionId = "current", Priority = 0, CanInteract = true, InteractionPosition = new Vector2(2f, 0f) };
            var moreAttractive = new FakeInteractable { InteractionId = "better", Priority = 9, CanInteract = true, InteractionPosition = new Vector2(0.1f, 0f) };
            var candidates = new List<IInteractable> { current, moreAttractive };

            IInteractable result = InteractionSelector.SelectTarget(candidates, current, Vector2.zero);

            Assert.AreSame(current, result,
                "A valid current target present by reference must be preserved (sticky), even against a more attractive candidate.");
        }

        [Test]
        public void InteractionSelector_InvalidCurrentTargetSelectsReplacement()
        {
            var current = new FakeInteractable { InteractionId = "current", Priority = 0, CanInteract = false, InteractionPosition = new Vector2(2f, 0f) };
            var replacement = new FakeInteractable { InteractionId = "replacement", Priority = 0, CanInteract = true, InteractionPosition = new Vector2(1f, 0f) };
            var candidates = new List<IInteractable> { current, replacement };

            IInteractable result = InteractionSelector.SelectTarget(candidates, current, Vector2.zero);

            Assert.AreSame(replacement, result,
                "A current target that can no longer interact must be discarded and replaced.");
        }
    }
}

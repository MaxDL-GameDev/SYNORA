using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Synora.Data;
using Synora.Gameplay.Interaction;
using Synora.Systems;

namespace Synora.Tests
{
    public sealed class InteractionControllerTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<Object> temp = new List<Object>();

        private InteractionController controller;
        private PlayerControlGate gate;
        private ObservationPanelPresenter panelPresenter;
        private GameObject promptRoot;

        /// <summary>Pure IInteractable stub that routes Execute back to the receiver.</summary>
        private sealed class FakeInteractable : IInteractable
        {
            public string InteractionId { get; set; } = "fake";
            public int Priority { get; set; }
            public bool CanInteract { get; set; } = true;
            public Vector2 InteractionPosition { get; set; }
            public string PromptText => "Examinar";
            public ExaminableData Data;
            public int ExecuteCallCount;

            public void Execute(IInteractionReceiver receiver)
            {
                ExecuteCallCount++;
                receiver.ShowObservation(Data);
            }
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
        }

        private static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, PrivateInstance).Invoke(target, null);
        }

        private GameObject NewInactive(string name)
        {
            GameObject go = new GameObject(name);
            go.SetActive(false);
            temp.Add(go);
            return go;
        }

        [SetUp]
        public void SetUp()
        {
            // Hosts built inactive so no Awake/OnEnable runs while wiring.
            GameObject host = NewInactive("InteractionHost");

            InteractionDetector detector = host.AddComponent<InteractionDetector>();
            InteractionInputReader inputReader = host.AddComponent<InteractionInputReader>();
            gate = host.AddComponent<PlayerControlGate>();
            InteractionPromptPresenter promptPresenter = host.AddComponent<InteractionPromptPresenter>();
            panelPresenter = host.AddComponent<ObservationPanelPresenter>();
            controller = host.AddComponent<InteractionController>();

            GameObject originGo = NewInactive("Origin");
            originGo.transform.position = Vector3.zero;
            SetPrivate(detector, "originPoint", originGo.transform);

            ExaminableData data = ScriptableObject.CreateInstance<ExaminableData>();
            temp.Add(data);
            SetPrivate(data, "interactionId", "diagnostic-terminal");
            SetPrivate(data, "observationTitle", "Terminal");
            SetPrivate(data, "observationBody", "A diagnostic terminal.");

            // Inject one valid candidate into the detector's existing buffer.
            var fake = new FakeInteractable
            {
                InteractionId = "diagnostic-terminal",
                CanInteract = true,
                InteractionPosition = Vector2.zero,
                Data = data
            };
            var buffer = (List<IInteractable>)detector.GetType()
                .GetField("candidateBuffer", PrivateInstance).GetValue(detector);
            buffer.Add(fake);

            // Prompt UI.
            promptRoot = NewInactive("PromptRoot");
            Text label = NewInactive("PromptLabel").AddComponent<Text>();
            SetPrivate(promptPresenter, "promptRoot", promptRoot);
            SetPrivate(promptPresenter, "label", label);

            // Panel UI.
            GameObject panelRoot = NewInactive("PanelRoot");
            Text titleLabel = NewInactive("Title").AddComponent<Text>();
            Text bodyLabel = NewInactive("Body").AddComponent<Text>();
            SetPrivate(panelPresenter, "panelRoot", panelRoot);
            SetPrivate(panelPresenter, "titleLabel", titleLabel);
            SetPrivate(panelPresenter, "bodyLabel", bodyLabel);

            // Controller wiring.
            SetPrivate(controller, "detector", detector);
            SetPrivate(controller, "inputReader", inputReader);
            SetPrivate(controller, "gate", gate);
            SetPrivate(controller, "promptPresenter", promptPresenter);
            SetPrivate(controller, "panelPresenter", panelPresenter);

            // Acquire the target and enter ExploringWithTarget.
            Invoke(controller, "Update");
        }

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
        public void InteractionController_SinglePressOpensWithoutImmediateClose()
        {
            Assert.AreEqual(InteractionController.State.ExploringWithTarget, controller.CurrentState,
                "After Update, a valid target must yield ExploringWithTarget.");
            Assert.IsTrue(promptRoot.activeSelf, "The prompt must be visible with a target.");
            Assert.IsFalse(panelPresenter.IsOpen, "The panel must start closed.");
            Assert.IsFalse(gate.IsBlocked, "The gate must start unblocked.");

            Invoke(controller, "HandleInteractPressed");

            Assert.AreEqual(InteractionController.State.ObservationOpen, controller.CurrentState,
                "A single press must open the observation.");
            Assert.IsTrue(panelPresenter.IsOpen, "A single press must open the panel.");
            Assert.IsTrue(gate.IsBlocked, "A single press must block control.");
            Assert.IsFalse(promptRoot.activeSelf, "The prompt must hide while observing.");
        }

        [Test]
        public void InteractionController_SecondSeparatePressClosesAndRestoresControl()
        {
            Invoke(controller, "HandleInteractPressed");
            Assert.AreEqual(InteractionController.State.ObservationOpen, controller.CurrentState);
            Assert.IsTrue(panelPresenter.IsOpen);
            Assert.IsTrue(gate.IsBlocked);

            Invoke(controller, "HandleInteractPressed");

            Assert.IsFalse(panelPresenter.IsOpen, "The second press must close the panel.");
            Assert.IsFalse(gate.IsBlocked, "Control must be restored after closing.");
            Assert.AreEqual(InteractionController.State.ExploringWithTarget, controller.CurrentState,
                "The still-valid target must be re-acquired.");
            Assert.IsTrue(promptRoot.activeSelf, "The prompt must be visible again.");
        }
    }
}

using UnityEngine;
using Synora.Data;
using Synora.Systems;

namespace Synora.Gameplay.Interaction
{
    public sealed class InteractionController : MonoBehaviour, IInteractionReceiver
    {
        [SerializeField]
        private InteractionDetector detector;

        [SerializeField]
        private InteractionInputReader inputReader;

        [SerializeField]
        private PlayerControlGate gate;

        [SerializeField]
        private InteractionPromptPresenter promptPresenter;

        [SerializeField]
        private ObservationPanelPresenter panelPresenter;

        public enum State
        {
            ExploringWithoutTarget,
            ExploringWithTarget,
            ObservationOpen
        }

        private IInteractable currentTarget;
        private State state = State.ExploringWithoutTarget;

        public State CurrentState => state;

        private void Awake()
        {
            if (detector == null)
            {
                Debug.LogError("InteractionController: InteractionDetector reference is not assigned.", this);
            }

            if (inputReader == null)
            {
                Debug.LogError("InteractionController: InteractionInputReader reference is not assigned.", this);
            }

            if (gate == null)
            {
                Debug.LogError("InteractionController: PlayerControlGate reference is not assigned.", this);
            }

            if (promptPresenter == null)
            {
                Debug.LogError("InteractionController: InteractionPromptPresenter reference is not assigned.", this);
            }

            if (panelPresenter == null)
            {
                Debug.LogError("InteractionController: ObservationPanelPresenter reference is not assigned.", this);
            }
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.InteractPressed += HandleInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.InteractPressed -= HandleInteractPressed;
            }

            if (panelPresenter != null)
            {
                panelPresenter.Close();
            }

            if (promptPresenter != null)
            {
                promptPresenter.Hide();
            }

            if (gate != null)
            {
                gate.Unblock(ControlBlockReason.Observation);
            }

            currentTarget = null;
            state = State.ExploringWithoutTarget;
        }

        private void Update()
        {
            if (state == State.ObservationOpen)
            {
                return;
            }

            if (detector == null)
            {
                return;
            }

            RefreshTarget(false);
        }

        private void RefreshTarget(bool forcePresentation)
        {
            if (detector == null)
            {
                currentTarget = null;
                state = State.ExploringWithoutTarget;
                if (promptPresenter != null)
                {
                    promptPresenter.Hide();
                }
                return;
            }

            IInteractable selectedTarget =
                InteractionSelector.SelectTarget(
                    detector.Candidates,
                    currentTarget,
                    detector.OriginPosition);

            bool changed = !ReferenceEquals(selectedTarget, currentTarget);
            currentTarget = selectedTarget;

            if (!changed && !forcePresentation)
            {
                return;
            }

            if (InteractionTargetUtility.IsAlive(currentTarget))
            {
                state = State.ExploringWithTarget;
                if (promptPresenter != null)
                {
                    promptPresenter.Show(currentTarget.PromptText);
                }
            }
            else
            {
                currentTarget = null;
                state = State.ExploringWithoutTarget;
                if (promptPresenter != null)
                {
                    promptPresenter.Hide();
                }
            }
        }

        private void HandleInteractPressed()
        {
            State entryState = state;

            switch (entryState)
            {
                case State.ExploringWithoutTarget:
                    break;

                case State.ExploringWithTarget:
                    if (InteractionTargetUtility.IsAlive(currentTarget))
                    {
                        currentTarget.Execute(this);
                    }
                    break;

                case State.ObservationOpen:
                    CloseObservation();
                    break;
            }
        }

        public void ShowObservation(ExaminableData data)
        {
            if (state != State.ExploringWithTarget) return;
            if (!InteractionTargetUtility.IsAlive(currentTarget)) return;
            if (!currentTarget.CanInteract) return;
            if (data == null) return;
            if (panelPresenter == null) return;
            if (promptPresenter == null) return;
            if (gate == null) return;

            panelPresenter.Open(data);
            promptPresenter.Hide();
            gate.Block(ControlBlockReason.Observation);
            state = State.ObservationOpen;
        }

        private void CloseObservation()
        {
            if (panelPresenter != null)
            {
                panelPresenter.Close();
            }

            if (gate != null)
            {
                gate.Unblock(ControlBlockReason.Observation);
            }

            state = State.ExploringWithoutTarget;
            RefreshTarget(true);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Synora.Gameplay.Presentation
{
    /// <summary>
    /// Transient "Vínculo establecido" notification (M7 F5). Reuses the project's panel-
    /// presenter pattern (a panel root + a UI Text toggled via SetActive, like
    /// ObservationPanelPresenter) — no new notification system and no permanent UI: Show
    /// activates the panel and auto-hides after a configurable duration via a deterministic
    /// Tick. It renders text only; it never reads creature state, moves anything, changes
    /// states, or writes SpriteRenderer.color.
    /// </summary>
    public sealed class BondEstablishedPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text label;
        [SerializeField, Min(0f)] private float displayDuration = 3f;

        private float remaining;

        public bool IsShown => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot == null)
            {
                Debug.LogError("BondEstablishedPresenter: panel root is not assigned.", this);
            }

            if (label == null)
            {
                Debug.LogError("BondEstablishedPresenter: label is not assigned.", this);
            }
        }

        public void Show(string message)
        {
            if (panelRoot == null || label == null)
            {
                return;
            }

            label.text = message ?? string.Empty;
            panelRoot.SetActive(true);
            remaining = displayDuration;
        }

        public void Hide()
        {
            remaining = 0f;
            if (panelRoot != null && panelRoot.activeSelf)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>Counts down the visible window and hides on elapse. Public for deterministic tests.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsShown)
            {
                return;
            }

            remaining -= deltaTime > 0f ? deltaTime : 0f;
            if (remaining <= 0f)
            {
                Hide();
            }
        }
    }
}

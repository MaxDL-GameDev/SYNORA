using System;
using UnityEngine;
using UnityEngine.UI;

namespace Synora.Gameplay.Interaction
{
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField]
        private GameObject promptRoot;

        [SerializeField]
        private Text label;

        private const string Prefix = "[E] ";
        private string currentPromptText;

        private void Awake()
        {
            if (promptRoot == null)
            {
                Debug.LogError("InteractionPromptPresenter: Prompt root is not assigned.", this);
            }

            if (label == null)
            {
                Debug.LogError("InteractionPromptPresenter: Label is not assigned.", this);
            }
        }

        public void Show(string promptText)
        {
            if (promptRoot == null || label == null)
            {
                return;
            }

            string raw = promptText ?? string.Empty;

            if (!string.Equals(raw, currentPromptText, StringComparison.Ordinal))
            {
                currentPromptText = raw;
                label.text = Prefix + raw;
            }

            if (!promptRoot.activeSelf)
            {
                promptRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (promptRoot == null)
            {
                return;
            }

            if (promptRoot.activeSelf)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}

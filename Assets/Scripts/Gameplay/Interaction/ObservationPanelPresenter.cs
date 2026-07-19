using UnityEngine;
using UnityEngine.UI;
using Synora.Data;

namespace Synora.Gameplay.Interaction
{
    public sealed class ObservationPanelPresenter : MonoBehaviour
    {
        [SerializeField]
        private GameObject panelRoot;

        [SerializeField]
        private Text titleLabel;

        [SerializeField]
        private Text bodyLabel;

        public bool IsOpen =>
            panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot == null)
            {
                Debug.LogError("ObservationPanelPresenter: Panel root is not assigned.", this);
            }

            if (titleLabel == null)
            {
                Debug.LogError("ObservationPanelPresenter: Title label is not assigned.", this);
            }

            if (bodyLabel == null)
            {
                Debug.LogError("ObservationPanelPresenter: Body label is not assigned.", this);
            }
        }

        public void Open(ExaminableData data)
        {
            if (data == null)
            {
                return;
            }

            if (panelRoot == null || titleLabel == null || bodyLabel == null)
            {
                return;
            }

            if (IsOpen)
            {
                return;
            }

            titleLabel.text = data.ObservationTitle;
            bodyLabel.text = data.ObservationBody;
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot == null)
            {
                return;
            }

            if (panelRoot.activeSelf)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}

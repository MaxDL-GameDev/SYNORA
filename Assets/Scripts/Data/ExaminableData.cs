using UnityEngine;

namespace Synora.Data
{
    [CreateAssetMenu(fileName = "ExaminableData", menuName = "SYNORA/Examinable Data")]
    public sealed class ExaminableData : ScriptableObject
    {
        [SerializeField] private string interactionId;
        [SerializeField] private string displayName;
        [SerializeField] private string observationTitle;
        [SerializeField, TextArea] private string observationBody;

        public string InteractionId => interactionId;
        public string DisplayName => displayName;
        public string ObservationTitle => observationTitle;
        public string ObservationBody => observationBody;

        public bool HasValidInteractionId => !string.IsNullOrWhiteSpace(interactionId);

        private void OnValidate()
        {
            if (interactionId != null)
            {
                interactionId = interactionId.Trim();
            }

            if (!HasValidInteractionId)
            {
                Debug.LogWarning("ExaminableData: interactionId is empty.", this);
            }

            if (string.IsNullOrWhiteSpace(observationTitle))
            {
                Debug.LogWarning("ExaminableData: observationTitle is empty.", this);
            }

            if (string.IsNullOrWhiteSpace(observationBody))
            {
                Debug.LogWarning("ExaminableData: observationBody is empty.", this);
            }

            // displayName vacío se permite: solo es un dato auxiliar de Inspector,
            // no se muestra al jugador ni participa en CanInteract.
        }
    }
}

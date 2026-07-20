using UnityEngine;

namespace Synora.Data
{
    /// <summary>
    /// Per-species stable identity and tuning for a creature. Holds only what M3
    /// requires; no out-of-scope/future fields, no scene or runtime component
    /// references. Validation lives in OnValidate and never throws.
    /// </summary>
    [CreateAssetMenu(fileName = "CreatureIdentity", menuName = "SYNORA/Creature Identity")]
    public sealed class CreatureIdentity : ScriptableObject
    {
        [SerializeField] private string creatureId = "creature";
        [SerializeField] private string displayName = "Creature";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private string species = "unknown";
        [SerializeField] private string biome = "unknown";

        [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
        [SerializeField, Min(0f)] private float idleDuration = 2f;
        [SerializeField, Min(0f)] private float patrolPauseDuration = 1f;
        [SerializeField, Min(0.0001f)] private float arrivalThreshold = 0.1f;

        [SerializeField, Min(0.0001f)] private float detectionRadius = 3f;
        [SerializeField, Min(0.0001f)] private float loseRadius = 4f;
        [SerializeField, Min(0f)] private float alertLingerDuration = 1.5f;

        [SerializeField] private bool isSymmetric = true;
        [SerializeField, Min(0.0001f)] private float spriteScale = 1f;

        public string CreatureId => creatureId;
        public string DisplayName => displayName;
        public string Description => description;
        public string Species => species;
        public string Biome => biome;

        public float MoveSpeed => moveSpeed;
        public float IdleDuration => idleDuration;
        public float PatrolPauseDuration => patrolPauseDuration;
        public float ArrivalThreshold => arrivalThreshold;

        public float DetectionRadius => detectionRadius;
        public float LoseRadius => loseRadius;
        public float AlertLingerDuration => alertLingerDuration;

        public bool IsSymmetric => isSymmetric;
        public float SpriteScale => spriteScale;

        public bool HasValidCreatureId => !string.IsNullOrWhiteSpace(creatureId);

        private void OnValidate()
        {
            creatureId = Trim(creatureId);
            displayName = Trim(displayName);
            species = Trim(species);
            biome = Trim(biome);

            if (!HasValidCreatureId)
            {
                Debug.LogWarning("CreatureIdentity: creatureId is empty.", this);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogWarning("CreatureIdentity: displayName is empty.", this);
            }

            if (string.IsNullOrWhiteSpace(species))
            {
                Debug.LogWarning("CreatureIdentity: species is empty.", this);
            }

            // Non-negative durations and speed.
            if (moveSpeed < 0f) moveSpeed = 0f;
            if (idleDuration < 0f) idleDuration = 0f;
            if (patrolPauseDuration < 0f) patrolPauseDuration = 0f;
            if (alertLingerDuration < 0f) alertLingerDuration = 0f;

            // Strictly positive thresholds/scale.
            if (arrivalThreshold <= 0f) arrivalThreshold = 0.0001f;
            if (spriteScale <= 0f) spriteScale = 0.0001f;

            // Radii: detection positive; lose must be >= detection (spatial hysteresis).
            if (detectionRadius <= 0f) detectionRadius = 0.0001f;
            if (loseRadius < detectionRadius) loseRadius = detectionRadius;
        }

        private static string Trim(string value) => value != null ? value.Trim() : value;
    }
}

using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Per-creature physical perception. Polls Physics2D for the Player within the
    /// creature's maximum perception radius and publishes the detected Player plus
    /// its squared distance to the shared context. Decides NOTHING about behavior:
    /// alert / linger / return-to-patrol decisions belong to CreatureSensing +
    /// CreatureBrain (a later phase). This component is the single owner of its
    /// physics polling, running from its own FixedUpdate; the Brain reads the last
    /// published data from a separate logical tick (a one-tick latency between
    /// perception and decision is accepted, avoiding Script Execution Order coupling).
    /// </summary>
    public sealed class CreatureSensor : MonoBehaviour
    {
        /// <summary>Sentinel squared-distance meaning "no Player detected".</summary>
        public const float NoPlayer = -1f;

        // Squared-distance window under which two candidates count as equidistant.
        // Tiny at gameplay scale (radii up to a few units, so sqr up to tens): it
        // only catches genuine ties, then GetEntityId breaks them deterministically.
        private const float TieToleranceSqr = 1e-4f;

        [SerializeField] private CreatureIdentity identity;
        [SerializeField] private LayerMask playerLayer;

        private readonly Collider2D[] overlapBuffer = new Collider2D[8];
        private ContactFilter2D playerFilter;
        private CreatureContext context;
        private float playerDistanceSqr = NoPlayer;
        private bool hasLoggedBufferFull;

        public bool HasDetectedPlayer => context != null && context.DetectedPlayer != null;
        public Transform DetectedPlayer => context != null ? context.DetectedPlayer : null;
        public float PlayerDistanceSqr => playerDistanceSqr;
        public float DetectionRadius => identity != null ? Mathf.Max(0f, identity.DetectionRadius) : 0f;
        public float LoseRadius => identity != null ? Mathf.Max(DetectionRadius, identity.LoseRadius) : 0f;

        private void Awake()
        {
            if (playerLayer.value == 0)
            {
                Debug.LogWarning("CreatureSensor: playerLayer mask is empty; no Player will be detected.", this);
            }

            playerFilter = new ContactFilter2D();
            playerFilter.useLayerMask = true;
            playerFilter.SetLayerMask(playerLayer);
            playerFilter.useTriggers = true;
        }

        /// <summary>Binds the per-creature context (the shared source of truth for the detected Player).</summary>
        public void Initialize(CreatureContext creatureContext)
        {
            context = creatureContext;
        }

        private void FixedUpdate()
        {
            Sense();
        }

        /// <summary>
        /// One physical perception poll. Publishes the nearest Player within the query
        /// radius to the shared context, or clears it. Public so tests can drive it
        /// deterministically (with Physics2D.SyncTransforms). Safe when context or
        /// identity are missing.
        /// </summary>
        public void Sense()
        {
            if (context == null || identity == null)
            {
                return;
            }

            float radius = LoseRadius; // == max(detectionRadius, loseRadius)
            Vector2 origin = transform.position;

            int count = Physics2D.OverlapCircle(origin, radius, playerFilter, overlapBuffer);

            if (count == overlapBuffer.Length && !hasLoggedBufferFull)
            {
                Debug.LogWarning("CreatureSensor: overlap buffer full; the nearest of the available colliders is chosen.", this);
                hasLoggedBufferFull = true;
            }

            Transform nearest = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = overlapBuffer[i];
                if (collider == null)
                {
                    continue;
                }

                // Resolve every collider of the same body to a single stable root, so
                // a Player with several colliders never flips the detected reference.
                Transform candidate = collider.attachedRigidbody != null
                    ? collider.attachedRigidbody.transform
                    : collider.transform;

                if (candidate == transform)
                {
                    continue; // never detect our own creature
                }

                float sqr = ((Vector2)candidate.position - origin).sqrMagnitude;
                if (nearest == null)
                {
                    nearest = candidate;
                    nearestSqr = sqr;
                    continue;
                }

                float diff = sqr - nearestSqr;
                if (diff < -TieToleranceSqr)
                {
                    // Strictly closer.
                    nearest = candidate;
                    nearestSqr = sqr;
                }
                else if (diff <= TieToleranceSqr && candidate.GetEntityId() < nearest.GetEntityId())
                {
                    // Equidistant within tolerance: lowest EntityId wins (order-independent).
                    nearest = candidate;
                    nearestSqr = sqr;
                }
            }

            if (nearest != null)
            {
                playerDistanceSqr = nearestSqr;
                context.SetDetectedPlayer(nearest);
            }
            else
            {
                playerDistanceSqr = NoPlayer;
                context.ClearDetectedPlayer();
            }
        }

        /// <summary>Clears the detected Player from both this sensor and the shared context.</summary>
        public void ClearDetection()
        {
            playerDistanceSqr = NoPlayer;
            context?.ClearDetectedPlayer();
        }

        private void OnDisable()
        {
            ClearDetection();
        }

        private void OnDrawGizmosSelected()
        {
            if (identity == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, identity.DetectionRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, identity.LoseRadius);
        }
    }
}

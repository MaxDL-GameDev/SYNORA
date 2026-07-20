using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Physical locomotion for a creature. Drives the Rigidbody2D velocity toward a
    /// destination during the physics step; never writes Transform, never uses
    /// AddForce, NavMesh or pathfinding. Decides nothing about behavior: the Brain
    /// and its states (a later phase) only call SetDestination/Stop. This component
    /// is the single owner of its physics execution, running from its own
    /// FixedUpdate. Visual presentation is added later (Animator phase).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CreatureMovement : MonoBehaviour
    {
        // Below this squared speed a tick produces no facing change (keeps previous).
        private const float MinFacingSpeedSqr = 1e-6f;

        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CreatureIdentity identity;

        private CreatureContext context;
        private Vector2 destination;
        private bool hasDestination;

        public bool HasDestination => hasDestination;
        public Vector2 Destination => destination;

        /// <summary>True while an active destination is pending (not yet reached).</summary>
        public bool IsMoving => hasDestination;

        private void Awake()
        {
            if (body == null)
            {
                Debug.LogError("CreatureMovement: Rigidbody2D reference is not assigned.", this);
                return;
            }

            // Top-down controlled body: no gravity, frozen rotation. We warn (never
            // overwrite the designer's asset silently) if the prefab is misconfigured.
            if (!Mathf.Approximately(body.gravityScale, 0f))
            {
                Debug.LogWarning("CreatureMovement: Rigidbody2D gravityScale should be 0.", this);
            }

            if ((body.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                Debug.LogWarning("CreatureMovement: Rigidbody2D should freeze Z rotation.", this);
            }
        }

        /// <summary>Binds the per-creature context. Reporting (IsMoving/Facing) is skipped when null.</summary>
        public void Initialize(CreatureContext creatureContext)
        {
            context = creatureContext;
        }

        public void SetDestination(Vector2 target)
        {
            destination = target;
            hasDestination = true;
        }

        public void Stop()
        {
            hasDestination = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            context?.SetMoving(false);
            // Facing is intentionally preserved on stop.
        }

        private void FixedUpdate()
        {
            FixedTick(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Advances locomotion by one physics step. Invoked by this component's own
        /// FixedUpdate (the single production execution path); also public so tests
        /// can drive it deterministically. Safe with no destination, zero/negative
        /// deltaTime, or zero MoveSpeed.
        /// </summary>
        public void FixedTick(float fixedDeltaTime)
        {
            if (body == null)
            {
                return;
            }

            if (!hasDestination)
            {
                body.linearVelocity = Vector2.zero;
                context?.SetMoving(false);
                return;
            }

            float moveSpeed = identity != null ? identity.MoveSpeed : 0f;
            float arrivalThreshold = identity != null ? identity.ArrivalThreshold : 0f;

            Vector2 current = body.position;
            Vector2 velocity = ComputeVelocity(
                current, destination, moveSpeed, arrivalThreshold, fixedDeltaTime, out bool arrived);

            body.linearVelocity = velocity;

            if (arrived)
            {
                hasDestination = false;
                context?.SetMoving(false);
                return;
            }

            context?.SetMoving(true);

            if (context != null && velocity.sqrMagnitude >= MinFacingSpeedSqr)
            {
                context.SetFacing(ResolveFacing(velocity, context.Facing));
            }
        }

        /// <summary>
        /// Deterministic velocity toward a destination, clamped so a single step
        /// never overshoots. Returns zero (arrived=true) when within the arrival
        /// threshold, and zero (arrived unchanged) when speed or deltaTime are
        /// non-positive. Pure: no MonoBehaviour, Time or global state.
        /// </summary>
        public static Vector2 ComputeVelocity(
            Vector2 current, Vector2 destination, float moveSpeed,
            float arrivalThreshold, float fixedDeltaTime, out bool arrived)
        {
            arrived = CreaturePatrolMath.HasArrived(current, destination, arrivalThreshold);
            if (arrived)
            {
                return Vector2.zero;
            }

            if (moveSpeed <= 0f || fixedDeltaTime <= 0f)
            {
                return Vector2.zero;
            }

            Vector2 toDestination = destination - current;
            float distance = toDestination.magnitude;
            if (distance <= 0f)
            {
                arrived = true;
                return Vector2.zero;
            }

            float maxStep = moveSpeed * fixedDeltaTime;
            if (maxStep >= distance)
            {
                // Would reach/overshoot this step: land exactly on the destination.
                return toDestination / fixedDeltaTime;
            }

            return (toDestination / distance) * moveSpeed;
        }

        /// <summary>
        /// Dominant-axis 4-way facing from a movement direction. Horizontal wins on
        /// an exact diagonal tie. Returns <paramref name="previous"/> when the
        /// direction is negligible.
        /// </summary>
        public static Vector2Int ResolveFacing(Vector2 direction, Vector2Int previous)
        {
            if (direction.sqrMagnitude < MinFacingSpeedSqr)
            {
                return previous;
            }

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f ? Vector2Int.right : Vector2Int.left;
            }

            return direction.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Local per-creature container of validated dependencies and mutable runtime
    /// state. Not a service locator: no statics, no singletons, no Find, no
    /// GetComponent, no DontDestroyOnLoad. Built and owned by CreatureBrain.
    ///
    /// Runtime component references are added to the permanent-reference section as
    /// each is implemented, without changing the mutable-state API. Movement is
    /// present as of this phase; Sensor and Animator arrive in later phases.
    /// </summary>
    public sealed class CreatureContext
    {
        // ── Permanent references (assigned once at construction; read-only) ──
        public CreatureIdentity Identity { get; }
        public Transform Root { get; }
        public IReadOnlyList<Transform> PatrolPoints { get; }
        public CreatureMovement Movement { get; }
        public CreatureSensor Sensor { get; }

        // ── Mutable per-instance runtime state (public read; write via API only) ──
        public int PatrolIndex { get; private set; }
        public int PatrolDirection { get; private set; }
        public float StateTimer { get; private set; }
        public Vector2Int Facing { get; private set; }
        public Transform DetectedPlayer { get; private set; }
        public bool IsMoving { get; private set; }

        public int PatrolPointCount => PatrolPoints != null ? PatrolPoints.Count : 0;

        public CreatureContext(
            CreatureIdentity identity,
            Transform root,
            IReadOnlyList<Transform> patrolPoints,
            CreatureMovement movement = null,
            CreatureSensor sensor = null)
        {
            Identity = identity;
            Root = root;
            PatrolPoints = patrolPoints;
            Movement = movement;
            Sensor = sensor;

            // Safe initial state.
            PatrolIndex = 0;
            PatrolDirection = 1;          // PingPong starts forward
            StateTimer = 0f;
            Facing = Vector2Int.down;     // default facing
            DetectedPlayer = null;
            IsMoving = false;
        }

        public void ResetStateTimer() => StateTimer = 0f;

        public void AdvanceStateTimer(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                StateTimer += deltaTime;
            }
        }

        public void SetDetectedPlayer(Transform player) => DetectedPlayer = player;

        public void ClearDetectedPlayer() => DetectedPlayer = null;

        public void SetFacing(Vector2Int facing) => Facing = facing;

        public void SetMoving(bool moving) => IsMoving = moving;

        /// <summary>Advances the patrol cursor deterministically (PingPong).</summary>
        public void AdvancePatrolPoint()
        {
            int direction = PatrolDirection;
            PatrolIndex = CreaturePatrolMath.NextPingPongIndex(PatrolIndex, PatrolPointCount, ref direction);
            PatrolDirection = direction;
        }

        /// <summary>Sets the patrol cursor to a specific point, clamped into range.</summary>
        public void SetPatrolIndex(int index)
        {
            int count = PatrolPointCount;
            if (count <= 0)
            {
                PatrolIndex = 0;
                return;
            }

            if (index < 0) index = 0;
            else if (index >= count) index = count - 1;
            PatrolIndex = index;
        }
    }
}

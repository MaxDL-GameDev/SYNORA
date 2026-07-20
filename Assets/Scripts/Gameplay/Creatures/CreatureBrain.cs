using System.Collections.Generic;
using UnityEngine;
using Synora.Data;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Logical orchestrator of creature behavior via a polymorphic state machine
    /// (Idle / Patrol / Alert). Decides behavior only: it never moves the Transform,
    /// never queries Physics2D, never plays animations, and never drives the
    /// FixedUpdate of Movement or Sensor (those own their physics tick). The Brain
    /// runs a logical tick from Update, reading the last data published by the
    /// sensor (a one-tick perception latency is accepted, avoiding execution-order
    /// coupling). States return a neutral CreatureStateId token; the Brain is the
    /// single point that applies transitions.
    /// </summary>
    public sealed class CreatureBrain : MonoBehaviour
    {
        [SerializeField] private CreatureIdentity identity;
        [SerializeField] private CreatureMovement movement;
        [SerializeField] private CreatureSensor sensor;
        [SerializeField] private Transform root;
        [SerializeField] private Transform[] patrolPoints = new Transform[0];

        private CreatureContext context;
        private Dictionary<CreatureStateId, ICreatureState> states;
        private ICreatureState current;
        private CreatureStateId? pendingTransition;
        private bool isInitialized;
        private bool hasWarnedUnknownState;

        public bool IsInitialized => isInitialized;

        /// <summary>Single source of truth for the active state id (lives in the context).</summary>
        public CreatureStateId CurrentStateId => context != null ? context.CurrentState : CreatureStateId.Idle;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Validates dependencies, builds the context, wires Movement/Sensor, builds
        /// the state instances once, and enters the initial state (Idle). Idempotent.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            if (identity == null)
            {
                Debug.LogWarning("CreatureBrain: CreatureIdentity is not assigned; not initialized.", this);
                return;
            }
            if (movement == null)
            {
                Debug.LogWarning("CreatureBrain: CreatureMovement is not assigned; not initialized.", this);
                return;
            }
            if (sensor == null)
            {
                Debug.LogWarning("CreatureBrain: CreatureSensor is not assigned; not initialized.", this);
                return;
            }

            Transform effectiveRoot = root != null ? root : transform;
            IReadOnlyList<Transform> points = patrolPoints != null
                ? (IReadOnlyList<Transform>)patrolPoints
                : System.Array.Empty<Transform>();

            context = new CreatureContext(identity, effectiveRoot, points, movement, sensor);
            movement.Initialize(context);
            sensor.Initialize(context);

            states = new Dictionary<CreatureStateId, ICreatureState>(3)
            {
                { CreatureStateId.Idle, new IdleState() },
                { CreatureStateId.Patrol, new PatrolState() },
                { CreatureStateId.Alert, new AlertState() },
            };

            // Canonical initial state. CreatureIdentity defines no explicit initial
            // state, so Idle is the documented fallback.
            const CreatureStateId initial = CreatureStateId.Idle;
            context.SetCurrentState(initial);
            current = states[initial];
            current.Enter(context);

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        /// <summary>
        /// One logical behavior tick. Runs the current state's Tick, funnels any
        /// requested transition through RequestTransition, then applies at most one
        /// transition. Public for deterministic tests. No-op before Initialize;
        /// negative deltaTime is treated as 0.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isInitialized)
            {
                return;
            }

            float dt = deltaTime > 0f ? deltaTime : 0f;

            CreatureStateId? requested = current.Tick(context, dt);
            if (requested.HasValue)
            {
                RequestTransition(requested.Value);
            }

            ApplyPendingTransition();
        }

        /// <summary>
        /// The single authorized way to request a state change. Stores at most one
        /// pending transition, applied at the end of the current Tick. Unknown states
        /// are ignored safely.
        /// </summary>
        public void RequestTransition(CreatureStateId next)
        {
            if (states == null || !states.ContainsKey(next))
            {
                if (!hasWarnedUnknownState)
                {
                    Debug.LogWarning("CreatureBrain: requested transition to an unknown state; ignored.", this);
                    hasWarnedUnknownState = true;
                }
                return;
            }

            pendingTransition = next;
        }

        private void ApplyPendingTransition()
        {
            if (!pendingTransition.HasValue)
            {
                return;
            }

            CreatureStateId next = pendingTransition.Value;
            pendingTransition = null;

            if (next == context.CurrentState)
            {
                return; // same state: no Exit/Enter churn
            }

            current.Exit(context);
            context.SetCurrentState(next);
            current = states[next];
            current.Enter(context); // the new state does not Tick until the next logical tick
        }

        private void OnDisable()
        {
            if (movement != null)
            {
                movement.Stop();
            }
        }
    }
}

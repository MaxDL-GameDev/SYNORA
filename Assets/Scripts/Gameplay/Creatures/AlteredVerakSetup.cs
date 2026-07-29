using System.Collections.Generic;
using UnityEngine;
using Synora.Gameplay.Combat;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Per-creature wiring for the Altered Verak: supplies CreatureBrain with the
    /// hostile state set {Idle, Patrol, Alert(hostile), Chase, Attack, Subdued} and
    /// injects Health + the attack controller into the states that need them. Idle and
    /// Patrol are reused unchanged; Alert is mapped to the hostile variant so
    /// detection leads into Chase.
    ///
    /// It also backstops the Subdued priority for the reused non-combat states: on
    /// Health.Depleted it asks the Brain (the sole transition owner) to go Subdued. The
    /// hostile combat states additionally self-check Health each tick, so a depletion
    /// during Chase/Attack transitions immediately regardless of event timing. Health
    /// never references states or the Brain.
    /// </summary>
    public sealed class AlteredVerakSetup : MonoBehaviour, ICreatureStateProvider
    {
        [SerializeField] private CreatureBrain brain;
        [SerializeField] private Health health;
        [SerializeField] private CreatureAttackController attackController;
        [SerializeField] private float attackRange = 1f;
        [SerializeField, Min(0f)] private float alertDuration = 0.4f;
        [SerializeField, Min(0f)] private float restorationDuration = 1.25f;
        [SerializeField, Min(0f)] private float bondingDuration = 1.25f;
        // Companion follow band (M7 F4): resume following past followDistance (max),
        // stop within followStopDistance (min); keep stopDistance < followDistance.
        [SerializeField, Min(0f)] private float followDistance = 2f;
        [SerializeField, Min(0f)] private float followStopDistance = 1f;

        private bool subscribed;

        public CreatureStateId InitialState => CreatureStateId.Idle;

        public IReadOnlyDictionary<CreatureStateId, ICreatureState> BuildStates(CreatureContext context)
        {
            return new Dictionary<CreatureStateId, ICreatureState>(10)
            {
                { CreatureStateId.Idle, new IdleState() },
                { CreatureStateId.Patrol, new PatrolState() },
                { CreatureStateId.Alert, new CreatureHostileAlertState(health, alertDuration) },
                { CreatureStateId.Chase, new CreatureChaseState(health, attackController, attackRange) },
                { CreatureStateId.Attack, new CreatureAttackState(health, attackController) },
                { CreatureStateId.Subdued, new CreatureSubduedState(attackController) },
                // M6 restoration flow: Subdued → Restoring → Restored. Restoring is only
                // reachable via an external RequestTransition (the interactive origin
                // arrives in a later phase); it completes to Restored on its own timer.
                { CreatureStateId.Restoring, new CreatureRestoringState(restorationDuration) },
                { CreatureStateId.Restored, new CreatureRestoredState() },
                // M7 bonding flow: Restored → Bonding → Bonded. Bonding is only reachable
                // via an external RequestTransition (the interactive origin arrives in F2);
                // it completes to Bonded on its own timer. Bonded is stable (following
                // arrives in F4). Restored stays terminal for its own logic.
                { CreatureStateId.Bonding, new CreatureBondingState(bondingDuration) },
                { CreatureStateId.Bonded, new CreatureBondedState(followDistance, followStopDistance) },
            };
        }

        private void Awake()
        {
            if (brain == null)
            {
                Debug.LogError("AlteredVerakSetup: CreatureBrain reference is not assigned.", this);
            }

            if (health == null)
            {
                Debug.LogError("AlteredVerakSetup: Health reference is not assigned.", this);
            }

            if (attackController == null)
            {
                Debug.LogError("AlteredVerakSetup: CreatureAttackController reference is not assigned.", this);
            }

            if (attackRange <= 0f)
            {
                Debug.LogWarning("AlteredVerakSetup: attackRange should be greater than zero.", this);
            }
        }

        private void OnEnable()
        {
            if (health != null && !subscribed)
            {
                health.Depleted += OnHealthDepleted;
                subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (health != null && subscribed)
            {
                health.Depleted -= OnHealthDepleted;
                subscribed = false;
            }
        }

        // Backstop for the reused non-combat states (Idle/Patrol/Alert): the Brain owns
        // the transition. Combat states self-check, so this only matters for a rare
        // pre-combat hit.
        private void OnHealthDepleted()
        {
            brain?.RequestTransition(CreatureStateId.Subdued);
        }
    }
}

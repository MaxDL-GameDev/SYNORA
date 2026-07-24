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

        private bool subscribed;

        public CreatureStateId InitialState => CreatureStateId.Idle;

        public IReadOnlyDictionary<CreatureStateId, ICreatureState> BuildStates(CreatureContext context)
        {
            return new Dictionary<CreatureStateId, ICreatureState>(6)
            {
                { CreatureStateId.Idle, new IdleState() },
                { CreatureStateId.Patrol, new PatrolState() },
                { CreatureStateId.Alert, new CreatureHostileAlertState(health) },
                { CreatureStateId.Chase, new CreatureChaseState(health, attackController, attackRange) },
                { CreatureStateId.Attack, new CreatureAttackState(health, attackController) },
                { CreatureStateId.Subdued, new CreatureSubduedState(attackController) },
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

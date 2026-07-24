using System.Collections.Generic;

namespace Synora.Gameplay.Creatures
{
    /// <summary>
    /// Optional per-creature state-set provider for <see cref="CreatureBrain"/>. When a
    /// creature (e.g. the Altered Verak) needs a state set other than the ambient
    /// default {Idle, Patrol, Alert}, it supplies one through this contract. The Brain
    /// stays the single owner of transitions; the provider only builds the instances
    /// and names the initial state. A creature with no provider keeps the ambient set,
    /// so M3 behavior is unchanged.
    /// </summary>
    public interface ICreatureStateProvider
    {
        CreatureStateId InitialState { get; }

        /// <summary>Builds the state instances for this creature, bound to its context.</summary>
        IReadOnlyDictionary<CreatureStateId, ICreatureState> BuildStates(CreatureContext context);
    }
}

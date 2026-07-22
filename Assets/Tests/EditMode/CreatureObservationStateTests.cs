using System;
using NUnit.Framework;
using Synora.Gameplay.Creatures;

namespace Synora.Tests
{
    // Pure mapping tests for CreatureObservationSource.Resolve (no MonoBehaviour needed).
    public sealed class CreatureObservationStateTests
    {
        [Test]
        public void Idle_MapsTo_Calm()
        {
            Assert.AreEqual(CreatureObservationState.Calm,
                CreatureObservationSource.Resolve(CreatureStateId.Idle));
        }

        [Test]
        public void Patrol_MapsTo_Roaming()
        {
            Assert.AreEqual(CreatureObservationState.Roaming,
                CreatureObservationSource.Resolve(CreatureStateId.Patrol));
        }

        [Test]
        public void Alert_MapsTo_Watchful()
        {
            Assert.AreEqual(CreatureObservationState.Watchful,
                CreatureObservationSource.Resolve(CreatureStateId.Alert));
        }

        [Test]
        public void UnrecognizedStateId_MapsTo_Unknown()
        {
            // A value outside the defined enum (e.g. a future gameplay state) must
            // degrade to Unknown, never be mislabeled as Calm.
            Assert.AreEqual(CreatureObservationState.Unknown,
                CreatureObservationSource.Resolve((CreatureStateId)999));
        }

        [Test]
        public void EveryDefinedState_ResolvesToNonUnknown()
        {
            foreach (CreatureStateId state in Enum.GetValues(typeof(CreatureStateId)))
            {
                Assert.AreNotEqual(CreatureObservationState.Unknown,
                    CreatureObservationSource.Resolve(state),
                    "Every currently defined CreatureStateId must have an observable mapping: " + state);
            }
        }
    }
}

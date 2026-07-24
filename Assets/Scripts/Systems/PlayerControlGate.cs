using System;
using UnityEngine;

namespace Synora.Systems
{
    [Flags]
    public enum ControlBlockReason
    {
        None = 0,
        Observation = 1 << 0,
        // Player temporary defeat (M5 §7). Additive flag: existing Observation
        // consumers are unaffected; defeat blocks control independently.
        Defeat = 1 << 1
    }

    public sealed class PlayerControlGate : MonoBehaviour
    {
        private ControlBlockReason activeReasons;

        public bool IsBlocked =>
            activeReasons != ControlBlockReason.None;

        public void Block(ControlBlockReason reason)
        {
            activeReasons |= reason;
        }

        public void Unblock(ControlBlockReason reason)
        {
            activeReasons &= ~reason;
        }
    }
}

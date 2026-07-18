using System;
using UnityEngine;

namespace Synora.Systems
{
    [Flags]
    public enum ControlBlockReason
    {
        None = 0,
        Observation = 1 << 0
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

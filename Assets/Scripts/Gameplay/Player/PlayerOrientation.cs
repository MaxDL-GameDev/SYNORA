using UnityEngine;
using Synora.Systems;

namespace Synora.Gameplay.Player
{
    /// <summary>
    /// Tracks the last non-zero cardinal facing (4 directions) from the input
    /// reader. Does not read input devices directly, nor move anything.
    /// </summary>
    public sealed class PlayerOrientation : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private Vector2Int facing = Vector2Int.down;

        [SerializeField]
        private PlayerControlGate gate;

        /// <summary>Current cardinal facing; starts facing down.</summary>
        public Vector2Int Facing => facing;

        private void Awake()
        {
            if (gate == null)
            {
                Debug.LogError(
                    "PlayerOrientation: PlayerControlGate reference is not assigned.",
                    this);
            }
        }

        private void Update()
        {
            if (gate != null && gate.IsBlocked)
            {
                return;
            }

            facing = Resolve(inputReader.MoveInput, facing);
        }

        private static Vector2Int Resolve(Vector2 input, Vector2Int previous)
        {
            if (input == Vector2.zero)
            {
                return previous;
            }

            float ax = Mathf.Abs(input.x);
            float ay = Mathf.Abs(input.y);

            if (ax > ay)
            {
                return new Vector2Int((int)Mathf.Sign(input.x), 0);
            }

            if (ay > ax)
            {
                return new Vector2Int(0, (int)Mathf.Sign(input.y));
            }

            Vector2Int horizontal = new Vector2Int((int)Mathf.Sign(input.x), 0);
            Vector2Int vertical = new Vector2Int(0, (int)Mathf.Sign(input.y));

            if (previous == horizontal || previous == vertical)
            {
                return previous;
            }

            return vertical;
        }
    }
}

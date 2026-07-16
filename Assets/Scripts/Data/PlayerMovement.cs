using UnityEngine;

namespace Synora.Data
{
    /// <summary>
    /// Movement tuning data for the player. Holds only what M1 requires.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovement", menuName = "SYNORA/Player Movement")]
    public sealed class PlayerMovement : ScriptableObject
    {
        [SerializeField, Min(0.01f)]
        private float moveSpeed = 4.5f;

        /// <summary>Constant movement speed, in units per second.</summary>
        public float MoveSpeed => moveSpeed;

        private void OnValidate()
        {
            if (moveSpeed <= 0f)
            {
                moveSpeed = 0.01f;
            }
        }
    }
}

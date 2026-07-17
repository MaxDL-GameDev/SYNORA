using UnityEngine;

namespace Synora.Systems
{
    /// <summary>
    /// A named spawn marker in a scene. Pure data + editor gizmo; it never moves
    /// the player, reads transition context, loads scenes, or uses colliders.
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private bool isDefault;

        /// <summary>Unique spawn identifier within its scene.</summary>
        public string Id => id;

        /// <summary>Whether this is the scene's default spawn.</summary>
        public bool IsDefault => isDefault;

        private void OnValidate()
        {
            if (id != null)
            {
                id = id.Trim();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isDefault ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}

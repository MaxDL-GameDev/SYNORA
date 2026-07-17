using UnityEngine;

namespace Synora.Systems
{
    /// <summary>
    /// A rectangular camera-bounds region in world space. Pure data + gizmo;
    /// no physics, no colliders, no global lookups.
    /// </summary>
    public sealed class CameraBounds2D : MonoBehaviour
    {
        [SerializeField]
        private Vector2 center = Vector2.zero;

        [SerializeField]
        private Vector2 size = new Vector2(16f, 9f);

        /// <summary>World-space center, accounting for the Transform position.</summary>
        public Vector2 WorldCenter => (Vector2)transform.position + center;

        /// <summary>Full rectangle size (width, height) in world units.</summary>
        public Vector2 Size => size;

        /// <summary>World-space bounds rectangle.</summary>
        public Bounds WorldBounds => new Bounds(WorldCenter, size);

        private void OnValidate()
        {
            if (size.x < 0.01f) size.x = 0.01f;
            if (size.y < 0.01f) size.y = 0.01f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(WorldCenter, size);
        }
    }
}

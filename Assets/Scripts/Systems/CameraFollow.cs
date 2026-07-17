using UnityEngine;

namespace Synora.Systems
{
    /// <summary>
    /// Follows a target and clamps the orthographic camera center to a room
    /// rectangle. Centers on any axis where the room is not larger than the
    /// viewport. Immediate follow at smoothTime 0. No Cinemachine.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private CameraBounds2D roomBounds;

        [SerializeField]
        private Camera controlledCamera;

        [SerializeField, Min(0f)]
        private float smoothTime = 0f;

        private Vector3 smoothVelocity;
        private bool disabled;

        private void OnValidate()
        {
            if (smoothTime < 0f) smoothTime = 0f;
        }

        private void LateUpdate()
        {
            if (disabled) return;

            if (target == null || roomBounds == null || controlledCamera == null)
            {
                Debug.LogError("CameraFollow: missing required reference(s); disabling component.", this);
                disabled = true;
                enabled = false;
                return;
            }

            float halfHeight = controlledCamera.orthographicSize;
            float halfWidth = halfHeight * controlledCamera.aspect;
            Bounds room = roomBounds.WorldBounds;

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            desired.x = ClampAxis(desired.x, room.min.x, room.max.x, halfWidth);
            desired.y = ClampAxis(desired.y, room.min.y, room.max.y, halfHeight);

            Vector3 result;
            if (smoothTime <= 0f)
            {
                result = desired;
            }
            else
            {
                result = Vector3.SmoothDamp(transform.position, desired, ref smoothVelocity, smoothTime);
                result.z = desired.z;
            }

            result.x = ClampAxis(result.x, room.min.x, room.max.x, halfWidth);
            result.y = ClampAxis(result.y, room.min.y, room.max.y, halfHeight);
            transform.position = result;
        }

        /// <summary>
        /// Clamps the camera center on one axis. If the room span is not larger
        /// than the viewport (2 * halfExtent), the axis is centered on the room.
        /// </summary>
        private static float ClampAxis(float desired, float min, float max, float halfExtent)
        {
            if (max - min <= halfExtent * 2f)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(desired, min + halfExtent, max - halfExtent);
        }
    }
}

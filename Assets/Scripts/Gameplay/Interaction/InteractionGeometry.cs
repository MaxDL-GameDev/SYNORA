using UnityEngine;

namespace Synora.Gameplay.Interaction
{
    public static class InteractionGeometry
    {
        public static bool IsInsideFrontZone(
            Vector2 origin,
            Vector2 facing,
            Vector2 candidatePosition,
            float range,
            float halfWidth)
        {
            Vector2 delta = candidatePosition - origin;
            float forwardDistance = Vector2.Dot(delta, facing);
            Vector2 perpendicular = new Vector2(-facing.y, facing.x);
            float lateralDistance =
                Mathf.Abs(Vector2.Dot(delta, perpendicular));

            return forwardDistance > 0f
                && forwardDistance <= range
                && lateralDistance <= halfWidth;
        }
    }
}

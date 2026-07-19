namespace Synora.Gameplay.Interaction
{
    public static class InteractionTargetUtility
    {
        public static bool IsAlive(IInteractable target)
        {
            if (ReferenceEquals(target, null))
            {
                return false;
            }

            return target is not UnityEngine.Object unityObject
                || unityObject != null;
        }
    }
}

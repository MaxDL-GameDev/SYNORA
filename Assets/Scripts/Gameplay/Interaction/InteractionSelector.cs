using System.Collections.Generic;
using UnityEngine;

namespace Synora.Gameplay.Interaction
{
    public static class InteractionSelector
    {
        public static IInteractable SelectTarget(
            IReadOnlyList<IInteractable> candidates,
            IInteractable currentTarget,
            Vector2 playerPosition)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            // Sticky target: keep the current one only when it is alive,
            // interactable, and still present by reference (in this order).
            if (InteractionTargetUtility.IsAlive(currentTarget)
                && currentTarget.CanInteract
                && ContainsByReference(candidates, currentTarget))
            {
                return currentTarget;
            }

            IInteractable best = null;
            int bestPriority = 0;
            float bestSqrDistance = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                IInteractable candidate = candidates[i];

                if (!InteractionTargetUtility.IsAlive(candidate))
                {
                    continue;
                }

                if (!candidate.CanInteract)
                {
                    continue;
                }

                float sqrDistance =
                    (candidate.InteractionPosition - playerPosition).sqrMagnitude;

                if (best == null
                    || IsBetter(candidate.Priority, sqrDistance, candidate.InteractionId,
                        bestPriority, bestSqrDistance, best.InteractionId))
                {
                    best = candidate;
                    bestPriority = candidate.Priority;
                    bestSqrDistance = sqrDistance;
                }
            }

            return best;
        }

        private static bool ContainsByReference(
            IReadOnlyList<IInteractable> candidates, IInteractable target)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (ReferenceEquals(candidates[i], target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBetter(
            int candidatePriority, float candidateSqrDistance, string candidateId,
            int bestPriority, float bestSqrDistance, string bestId)
        {
            // 1. Higher priority wins.
            if (candidatePriority != bestPriority)
            {
                return candidatePriority > bestPriority;
            }

            // 2. Nearer (smaller squared distance) wins.
            if (candidateSqrDistance != bestSqrDistance)
            {
                return candidateSqrDistance < bestSqrDistance;
            }

            // 3. Lower ordinal InteractionId wins (input-order independent).
            return string.CompareOrdinal(candidateId, bestId) < 0;
        }
    }
}

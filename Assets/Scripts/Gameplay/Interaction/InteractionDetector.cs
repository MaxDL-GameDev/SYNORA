using System;
using System.Collections.Generic;
using UnityEngine;
using Synora.Gameplay.Player;

namespace Synora.Gameplay.Interaction
{
    public sealed class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private PlayerOrientation playerOrientation;
        [SerializeField] private Transform originPoint;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private float detectionRange = 1.25f;
        [SerializeField] private float frontalHalfWidth = 0.4f;
        // Explicitly registered interactables in the scene. Typed as MonoBehaviour
        // (not List<IInteractable>, which Unity cannot serialize) and validated to
        // IInteractable in Awake. The field name is preserved on purpose: scenes
        // override it via prefab modifications keyed by the property path
        // "sceneExaminables.Array.data[N]"; renaming would silently drop those
        // overrides (FormerlySerializedAs does not remap prefab-modification paths).
        // The detector treats every entry uniformly as an IInteractable; it has no
        // knowledge of what a concrete entry actually is.
        [SerializeField] private List<MonoBehaviour> sceneExaminables =
            new List<MonoBehaviour>();

        private readonly Collider2D[] overlapBuffer = new Collider2D[8];
        private readonly List<IInteractable> candidateBuffer =
            new List<IInteractable>(8);
        private readonly Dictionary<Collider2D, IInteractable> colliderLookup =
            new Dictionary<Collider2D, IInteractable>();

        private ContactFilter2D interactableFilter;
        private bool hasLoggedBufferFull;

        public IReadOnlyList<IInteractable> Candidates => candidateBuffer;

        public Vector2 OriginPosition =>
            originPoint != null
                ? (Vector2)originPoint.position
                : Vector2.zero;

        private void Awake()
        {
            if (playerOrientation == null)
            {
                Debug.LogError("InteractionDetector: PlayerOrientation reference is not assigned.", this);
            }

            if (originPoint == null)
            {
                Debug.LogError("InteractionDetector: originPoint reference is not assigned.", this);
            }

            HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < sceneExaminables.Count; i++)
            {
                MonoBehaviour behaviour = sceneExaminables[i];
                if (behaviour == null)
                {
                    Debug.LogError("InteractionDetector: sceneExaminables contains a null element.", this);
                    continue;
                }

                IInteractable interactable = behaviour as IInteractable;
                if (interactable == null)
                {
                    Debug.LogError("InteractionDetector: a registered entry does not implement IInteractable: " + behaviour.GetType().Name, behaviour);
                    continue;
                }

                string id = interactable.InteractionId;
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogError("InteractionDetector: a registered interactable has an empty InteractionId.", behaviour);
                }
                else if (!seenIds.Add(id))
                {
                    Debug.LogError("InteractionDetector: duplicate InteractionId '" + id + "'.", behaviour);
                }

                if ((interactableLayer.value & (1 << behaviour.gameObject.layer)) == 0)
                {
                    Debug.LogWarning("InteractionDetector: a registered interactable GameObject layer is not included in interactableLayer.", behaviour);
                }

                Collider2D[] colliders = behaviour.GetComponents<Collider2D>();
                if (colliders.Length == 0)
                {
                    Debug.LogError("InteractionDetector: a registered interactable has no Collider2D.", behaviour);
                }

                for (int c = 0; c < colliders.Length; c++)
                {
                    Collider2D collider = colliders[c];
                    if (!collider.isTrigger)
                    {
                        Debug.LogWarning("InteractionDetector: a registered interactable Collider2D is not a trigger.", behaviour);
                    }

                    colliderLookup[collider] = interactable;
                }
            }

            interactableFilter = new ContactFilter2D();
            interactableFilter.useLayerMask = true;
            interactableFilter.SetLayerMask(interactableLayer);
            interactableFilter.useTriggers = true;
        }

        private void FixedUpdate()
        {
            candidateBuffer.Clear();

            if (playerOrientation == null || originPoint == null)
            {
                return;
            }

            Vector2 origin = originPoint.position;
            Vector2 facing = new Vector2(playerOrientation.Facing.x, playerOrientation.Facing.y);
            Vector2 point = origin + facing * (detectionRange / 2f);
            Vector2 size = (facing.x != 0f)
                ? new Vector2(detectionRange, frontalHalfWidth * 2f)
                : new Vector2(frontalHalfWidth * 2f, detectionRange);

            int count = Physics2D.OverlapBox(point, size, 0f, interactableFilter, overlapBuffer);

            if (count == overlapBuffer.Length && !hasLoggedBufferFull)
            {
                Debug.LogWarning("InteractionDetector: overlap buffer full; some candidates may be ignored.", this);
                hasLoggedBufferFull = true;
            }

            for (int i = 0; i < count; i++)
            {
                if (!colliderLookup.TryGetValue(overlapBuffer[i], out IInteractable candidate))
                {
                    continue;
                }

                bool alreadyAdded = false;
                for (int j = 0; j < candidateBuffer.Count; j++)
                {
                    if (ReferenceEquals(candidateBuffer[j], candidate))
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (alreadyAdded)
                {
                    continue;
                }

                if (!candidate.CanInteract)
                {
                    continue;
                }

                if (InteractionGeometry.IsInsideFrontZone(origin, facing, candidate.InteractionPosition, detectionRange, frontalHalfWidth))
                {
                    candidateBuffer.Add(candidate);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (originPoint == null || playerOrientation == null)
            {
                return;
            }

            Vector2 origin = originPoint.position;
            Vector2 facing = new Vector2(playerOrientation.Facing.x, playerOrientation.Facing.y);
            if (facing == Vector2.zero)
            {
                return;
            }

            Vector2 point = origin + facing * (detectionRange / 2f);
            Vector2 size = (facing.x != 0f)
                ? new Vector2(detectionRange, frontalHalfWidth * 2f)
                : new Vector2(frontalHalfWidth * 2f, detectionRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(point, size);
        }
    }
}

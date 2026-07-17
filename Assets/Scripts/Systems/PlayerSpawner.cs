using System.Collections.Generic;
using UnityEngine;
using Synora.Data;

namespace Synora.Systems
{
    /// <summary>
    /// Places the local player on the requested spawn (or the Default) during
    /// Awake, then consumes the transition context. Uses an explicit serialized
    /// spawn list; never searches the scene.
    /// </summary>
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D player;

        [SerializeField]
        private SceneTransitionContext context;

        [SerializeField]
        private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        private void Awake()
        {
            if (player == null || context == null)
            {
                Debug.LogError("PlayerSpawner: required references are missing.", this);
                return;
            }

            string requested = context.HasPendingSpawnRequest ? context.PendingSpawnId : string.Empty;
            SpawnPoint target = null;

            if (!string.IsNullOrEmpty(requested))
            {
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    if (spawnPoints[i] != null && spawnPoints[i].Id == requested)
                    {
                        target = spawnPoints[i];
                        break;
                    }
                }

                if (target == null)
                {
                    Debug.LogError("PlayerSpawner: spawn id '" + requested + "' not found; falling back to Default.", this);
                }
            }

            if (target == null)
            {
                target = GetDefault();
            }

            if (target != null)
            {
                player.position = target.transform.position;
                player.linearVelocity = Vector2.zero;
            }

            context.ConsumePendingSpawn();
        }

        private SpawnPoint GetDefault()
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null && spawnPoints[i].IsDefault)
                {
                    return spawnPoints[i];
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (player == null)
            {
                Debug.LogWarning("PlayerSpawner: Player Rigidbody2D is not assigned.", this);
            }

            if (context == null)
            {
                Debug.LogWarning("PlayerSpawner: SceneTransitionContext is not assigned.", this);
            }

            if (spawnPoints == null)
            {
                Debug.LogWarning("PlayerSpawner: spawn list is null.", this);
                return;
            }

            int defaults = 0;
            var seen = new HashSet<string>();
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint sp = spawnPoints[i];
                if (sp == null)
                {
                    Debug.LogWarning("PlayerSpawner: spawn list contains a null element.", this);
                    continue;
                }

                if (string.IsNullOrEmpty(sp.Id))
                {
                    Debug.LogWarning("PlayerSpawner: a SpawnPoint has an empty id.", this);
                }
                else if (!seen.Add(sp.Id))
                {
                    Debug.LogWarning("PlayerSpawner: duplicate spawn id '" + sp.Id + "'.", this);
                }

                if (sp.IsDefault)
                {
                    defaults++;
                }
            }

            if (defaults != 1)
            {
                Debug.LogWarning("PlayerSpawner: expected exactly one Default SpawnPoint, found " + defaults + ".", this);
            }
        }
    }
}

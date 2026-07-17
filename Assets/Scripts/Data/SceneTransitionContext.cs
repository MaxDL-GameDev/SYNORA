using UnityEngine;

namespace Synora.Data
{
    /// <summary>
    /// Transient shared state that carries a pending spawn id across a single
    /// Single-mode scene load. Runtime state is never serialized into the asset.
    /// Only SceneLoader writes it; only PlayerSpawner consumes it.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionContext", menuName = "SYNORA/Scene Transition Context")]
    public sealed class SceneTransitionContext : ScriptableObject
    {
        [System.NonSerialized]
        private bool hasPendingSpawnRequest;

        [System.NonSerialized]
        private string pendingSpawnId = string.Empty;

        /// <summary>True while a spawn request is waiting to be consumed.</summary>
        public bool HasPendingSpawnRequest => hasPendingSpawnRequest;

        /// <summary>Pending spawn id; empty means "use the Default spawn".</summary>
        public string PendingSpawnId => pendingSpawnId;

        public void SetPendingSpawn(string spawnId)
        {
            pendingSpawnId = spawnId == null ? string.Empty : spawnId.Trim();
            hasPendingSpawnRequest = true;
        }

        public string ConsumePendingSpawn()
        {
            if (!hasPendingSpawnRequest)
            {
                return string.Empty;
            }

            string id = pendingSpawnId;
            pendingSpawnId = string.Empty;
            hasPendingSpawnRequest = false;
            return id;
        }

        public void Reset()
        {
            pendingSpawnId = string.Empty;
            hasPendingSpawnRequest = false;
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using Synora.Data;

namespace Synora.Systems
{
    /// <summary>
    /// The single authority that decides and starts scene loads. Writes the
    /// spawn id and flips the load lock atomically; rejects duplicate requests.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        private SceneTransitionContext context;

        private bool isLoading;

        /// <summary>True while a load is in progress.</summary>
        public bool IsLoading => isLoading;

        /// <summary>
        /// Attempts to load a scene. Returns false (leaving state untouched) if a
        /// load is already active or validation fails.
        /// </summary>
        public bool TryLoad(string destinationScene, string destinationSpawnId)
        {
            if (isLoading)
            {
                return false;
            }

            if (context == null)
            {
                Debug.LogError("SceneLoader: SceneTransitionContext is not assigned.", this);
                return false;
            }

            string scene = destinationScene == null ? string.Empty : destinationScene.Trim();
            if (scene.Length == 0)
            {
                Debug.LogError("SceneLoader: destination scene is empty.", this);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("SceneLoader: scene '" + scene + "' is not available in the build.", this);
                return false;
            }

            context.SetPendingSpawn(destinationSpawnId);
            isLoading = true;

            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            }
            catch (System.Exception)
            {
                operation = null;
            }

            if (operation == null)
            {
                isLoading = false;
                context.Reset();
                Debug.LogError("SceneLoader: failed to start loading '" + scene + "'.", this);
                return false;
            }

            return true;
        }
    }
}

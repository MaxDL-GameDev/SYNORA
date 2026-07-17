using UnityEngine;
using Synora.Data;
using Synora.Systems;

namespace Synora.Core
{
    /// <summary>
    /// Minimal entry point: resets the transition context and requests the first
    /// area (Default spawn). Does not persist past the first load.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private SceneTransitionContext context;

        [SerializeField]
        private SceneLoader sceneLoader;

        [SerializeField]
        private string initialScene = "CamaraPreservacion";

        private void Start()
        {
            if (context == null || sceneLoader == null)
            {
                Debug.LogError("GameBootstrap: required references are missing.", this);
                return;
            }

            context.Reset();

            if (!sceneLoader.TryLoad(initialScene, string.Empty))
            {
                Debug.LogError("GameBootstrap: failed to load initial scene '" + initialScene + "'.", this);
            }
        }
    }
}

using UnityEngine;

namespace Synora.Systems
{
    /// <summary>
    /// A trigger volume that asks the scene's SceneLoader to load another area.
    /// It only detects and requests; SceneLoader owns the lock and the context.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class AreaTransition : MonoBehaviour
    {
        [SerializeField]
        private SceneLoader sceneLoader;

        [SerializeField]
        private string destinationScene;

        [SerializeField]
        private string destinationSpawnId;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (sceneLoader == null)
            {
                return;
            }

            sceneLoader.TryLoad(destinationScene, destinationSpawnId);
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Synora.Data;
using Synora.Systems;

namespace Synora.Tests
{
    public sealed class TransitionSystemTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float Tolerance = 0.0001f;

        private readonly List<Object> temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i] != null)
                {
                    Object.DestroyImmediate(temp[i]);
                }
            }
            temp.Clear();
        }

        [Test]
        public void TryLoad_WhenAlreadyLoading_RejectsWithoutChangingContext()
        {
            // Arrange
            SceneTransitionContext context = ScriptableObject.CreateInstance<SceneTransitionContext>();
            temp.Add(context);
            context.SetPendingSpawn("OriginalSpawn");

            GameObject go = new GameObject("SceneLoaderTest");
            temp.Add(go);
            SceneLoader loader = go.AddComponent<SceneLoader>();
            typeof(SceneLoader).GetField("context", PrivateInstance).SetValue(loader, context);
            typeof(SceneLoader).GetField("isLoading", PrivateInstance).SetValue(loader, true);

            // Act
            bool result = loader.TryLoad("ClaroExterior", "ReplacementSpawn");

            // Assert
            Assert.IsFalse(result, "TryLoad must reject while a load is already active.");
            Assert.IsTrue(context.HasPendingSpawnRequest, "The pending request must remain.");
            Assert.AreEqual("OriginalSpawn", context.PendingSpawnId, "The spawn id must not be overwritten.");
        }

        [Test]
        public void PlayerSpawner_AfterSpawn_ConsumesContextAndClearsVelocity()
        {
            // Arrange
            SceneTransitionContext context = ScriptableObject.CreateInstance<SceneTransitionContext>();
            temp.Add(context);
            context.SetPendingSpawn("SpawnA");

            GameObject playerGo = new GameObject("PlayerTest");
            temp.Add(playerGo);
            Rigidbody2D body = playerGo.AddComponent<Rigidbody2D>();
            body.linearVelocity = new Vector2(3f, -2f);

            GameObject spawnGo = new GameObject("SpawnA");
            temp.Add(spawnGo);
            spawnGo.transform.position = new Vector3(5f, 3f, 0f);
            SpawnPoint spawn = spawnGo.AddComponent<SpawnPoint>();
            typeof(SpawnPoint).GetField("id", PrivateInstance).SetValue(spawn, "SpawnA");

            GameObject spawnerGo = new GameObject("PlayerSpawnerTest");
            temp.Add(spawnerGo);
            // AddComponent triggers OnValidate before the fields are wired, which
            // legitimately warns; declare those warnings as expected test noise.
            LogAssert.Expect(LogType.Warning, "PlayerSpawner: Player Rigidbody2D is not assigned.");
            LogAssert.Expect(LogType.Warning, "PlayerSpawner: SceneTransitionContext is not assigned.");
            LogAssert.Expect(LogType.Warning, "PlayerSpawner: expected exactly one Default SpawnPoint, found 0.");
            PlayerSpawner spawner = spawnerGo.AddComponent<PlayerSpawner>();
            typeof(PlayerSpawner).GetField("player", PrivateInstance).SetValue(spawner, body);
            typeof(PlayerSpawner).GetField("context", PrivateInstance).SetValue(spawner, context);
            typeof(PlayerSpawner).GetField("spawnPoints", PrivateInstance).SetValue(spawner, new List<SpawnPoint> { spawn });

            // Act (run the same spawn logic Awake performs)
            typeof(PlayerSpawner).GetMethod("Awake", PrivateInstance).Invoke(spawner, null);

            // Assert
            Assert.That(Vector2.Distance(body.position, new Vector2(5f, 3f)), Is.LessThan(Tolerance),
                "Player should be placed on the requested spawn point.");
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(Tolerance),
                "Velocity should be zeroed on spawn.");
            Assert.IsFalse(context.HasPendingSpawnRequest, "The context request should be consumed.");
            Assert.AreEqual(string.Empty, context.PendingSpawnId, "The pending spawn id should be cleared.");
        }
    }
}

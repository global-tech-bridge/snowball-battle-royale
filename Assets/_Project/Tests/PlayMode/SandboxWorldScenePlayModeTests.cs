using System.Collections;
using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace SnowballBattleRoyale.Tests.PlayMode
{
    /// <summary>
    /// Plays the shipped Sandbox_World scene end to end: its WorldGenerator must be wired to the
    /// world assets, generate on load, and land the player on the ground at the spawn point.
    /// Catches scene wiring mistakes that tests building their own world cannot see.
    /// </summary>
    public sealed class SandboxWorldScenePlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_World.unity";

        private Scene _scene;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            // Additive, so the scene can be unloaded again without disturbing other fixtures.
            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return load;
            _scene = SceneManager.GetSceneByPath(ScenePath);
#else
            Assert.Ignore("Loads the scene by asset path; Editor only.");
            yield break;
#endif
        }

        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
        }

        [UnityTest]
        public IEnumerator SceneGeneratesTheWorldAndLandsThePlayerAtSpawn()
        {
            Assert.That(_scene.isLoaded, Is.True, $"{ScenePath} did not load.");

            WorldGenerator generator = _scene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<WorldGenerator>())
                .FirstOrDefault(g => g != null);
            Assert.That(generator, Is.Not.Null, "Sandbox_World has no WorldGenerator.");
            Assert.That(generator.IsGenerated, Is.True, "WorldGenerator did not generate — are its assets assigned?");

            GameObject player = _scene.GetRootGameObjects().FirstOrDefault(root => root.CompareTag("Player"));
            Assert.That(player, Is.Not.Null, "Sandbox_World has no Player.");

            // Let the controller settle onto the terrain.
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Vector2 spawn = generator.Blueprint.Layout.SpawnPoint;
            Vector3 position = player.transform.position;
            float ground = generator.GroundPoint(spawn).y;

            Assert.That(new Vector2(position.x, position.z), Is.EqualTo(spawn).Using(new Vector2Comparer(0.5f)));
            Assert.That(position.y, Is.EqualTo(ground).Within(0.5f), "Player should stand on the terrain at spawn.");
        }

        private sealed class Vector2Comparer : System.Collections.Generic.IEqualityComparer<Vector2>
        {
            private readonly float _tolerance;

            public Vector2Comparer(float tolerance) => _tolerance = tolerance;

            public bool Equals(Vector2 a, Vector2 b) => Vector2.Distance(a, b) <= _tolerance;

            public int GetHashCode(Vector2 v) => 0;
        }
    }
}

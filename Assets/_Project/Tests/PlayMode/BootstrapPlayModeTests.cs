using System.Collections;
using NUnit.Framework;
using SnowballBattleRoyale.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SnowballBattleRoyale.Tests.PlayMode
{
    /// <summary>
    /// Integration coverage for the boot wiring: the scenes <see cref="Bootstrap"/> expects must
    /// actually be registered in Build Settings, and the runtime loader must see them.
    /// </summary>
    public sealed class BootstrapPlayModeTests
    {
        private const string PersistentSceneName = "01_Persistent";

        /// <summary>
        /// Tests here load the persistent scene (directly, or indirectly via <see cref="Bootstrap"/>).
        /// Unload it between tests so each one starts from the same state regardless of run order.
        /// </summary>
        [UnityTearDown]
        public IEnumerator UnloadPersistentScene()
        {
            Scene scene = SceneManager.GetSceneByName(PersistentSceneName);
            if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [Test]
        public void PersistentScene_IsRegisteredInBuildSettings()
        {
            var loader = new SceneManagerLoader();

            Assert.That(
                loader.SceneExistsInBuild(PersistentSceneName),
                Is.True,
                $"'{PersistentSceneName}' must be in the Build Settings scene list for Bootstrap to load it.");
        }

        [Test]
        public void SceneExistsInBuild_IsFalseForAnUnknownScene()
        {
            var loader = new SceneManagerLoader();

            Assert.That(loader.SceneExistsInBuild("99_NoSuchScene"), Is.False);
        }

        [UnityTest]
        public IEnumerator LoadAdditive_MakesTheSceneVisibleToIsSceneLoaded()
        {
            var loader = new SceneManagerLoader();
            Assume.That(loader.IsSceneLoaded(PersistentSceneName), Is.False);

            loader.LoadAdditive(PersistentSceneName);
            yield return null;

            Assert.That(loader.IsSceneLoaded(PersistentSceneName), Is.True);
        }

        [UnityTest]
        public IEnumerator Bootstrap_SurvivesSceneUnloadViaDontDestroyOnLoad()
        {
            var go = new GameObject("BootstrapUnderTest");
            go.AddComponent<Bootstrap>();
            yield return null;

            Assert.That(go.scene.name, Is.EqualTo("DontDestroyOnLoad"));

            Object.Destroy(go);
            yield return null;
        }
    }
}

using UnityEngine.SceneManagement;

namespace SnowballBattleRoyale.Core
{
    /// <summary>Runtime <see cref="ISceneLoader"/> backed by Unity's <see cref="SceneManager"/>.</summary>
    public sealed class SceneManagerLoader : ISceneLoader
    {
        public bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        public bool SceneExistsInBuild(string sceneName)
        {
            return SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0;
        }

        public void LoadAdditive(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}

namespace SnowballBattleRoyale.Core
{
    /// <summary>
    /// Scene operations needed by <see cref="BootSequence"/>. Abstracted so the boot logic can be
    /// unit tested without entering play mode.
    /// </summary>
    public interface ISceneLoader
    {
        bool IsSceneLoaded(string sceneName);

        bool SceneExistsInBuild(string sceneName);

        void LoadAdditive(string sceneName);
    }
}

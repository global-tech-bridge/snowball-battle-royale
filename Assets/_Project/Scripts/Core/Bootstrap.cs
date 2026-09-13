using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.Core
{
    /// <summary>
    /// Entry point placed in <c>00_Boot</c>. Additively loads the persistent scenes and then gets out
    /// of the way. All decision logic lives in <see cref="BootSequence"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Bootstrap : MonoBehaviour
    {
        private const string LogCategory = "Boot";

        [Tooltip("Scenes loaded additively on top of the boot scene, in order.")]
        [SerializeField]
        private string[] _persistentScenes = { "01_Persistent" };

        [Tooltip("Target frame rate applied at startup. 0 leaves the platform default untouched.")]
        [SerializeField]
        private int _targetFrameRate = 60;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_targetFrameRate > 0)
            {
                Application.targetFrameRate = _targetFrameRate;
            }

            SBLog.Info(
                $"Booting Snowball Battle Royale (Unity {Application.unityVersion}).",
                this,
                LogCategory);

            BootResult result = new BootSequence(new SceneManagerLoader(), _persistentScenes).Run();

            if (result.HasErrors)
            {
                SBLog.Error(
                    $"Boot finished with {result.MissingScenes.Count} missing scene(s). " +
                    "Add them under File > Build Profiles > Scene List.",
                    this,
                    LogCategory);
                return;
            }

            SBLog.Info($"Boot complete ({result.LoadedScenes.Count} scene(s) loaded).", this, LogCategory);
        }
    }
}

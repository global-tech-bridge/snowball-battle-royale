using System;
using System.Collections.Generic;
using SnowballBattleRoyale.Domain;

namespace SnowballBattleRoyale.Core
{
    /// <summary>
    /// Loads the persistent scenes that every play session needs, on top of the boot scene.
    ///
    /// Pure C# so it is covered by EditMode tests; <see cref="Bootstrap"/> is the thin MonoBehaviour
    /// that wires it to Unity at runtime.
    /// </summary>
    public sealed class BootSequence
    {
        private const string LogCategory = "Boot";

        private readonly ISceneLoader _loader;
        private readonly IReadOnlyList<string> _sceneNames;

        public BootSequence(ISceneLoader loader, IReadOnlyList<string> sceneNames)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _sceneNames = sceneNames ?? Array.Empty<string>();
        }

        public BootResult Run()
        {
            var loaded = new List<string>();
            var skipped = new List<string>();
            var missing = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string sceneName in _sceneNames)
            {
                if (string.IsNullOrWhiteSpace(sceneName) || !seen.Add(sceneName))
                {
                    continue;
                }

                if (!_loader.SceneExistsInBuild(sceneName))
                {
                    missing.Add(sceneName);
                    SBLog.Error(
                        $"Scene '{sceneName}' is not in Build Settings; skipping.",
                        category: LogCategory);
                    continue;
                }

                if (_loader.IsSceneLoaded(sceneName))
                {
                    skipped.Add(sceneName);
                    continue;
                }

                _loader.LoadAdditive(sceneName);
                loaded.Add(sceneName);
                SBLog.Info($"Loaded '{sceneName}' additively.", category: LogCategory);
            }

            return new BootResult(loaded, skipped, missing);
        }
    }
}

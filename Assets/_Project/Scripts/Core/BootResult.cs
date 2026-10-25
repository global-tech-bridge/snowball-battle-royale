using System.Collections.Generic;

namespace SnowballBattleRoyale.Core
{
    /// <summary>Outcome of one <see cref="BootSequence.Run"/> call.</summary>
    public sealed class BootResult
    {
        public BootResult(
            IReadOnlyList<string> loadedScenes,
            IReadOnlyList<string> skippedScenes,
            IReadOnlyList<string> missingScenes)
        {
            LoadedScenes = loadedScenes;
            SkippedScenes = skippedScenes;
            MissingScenes = missingScenes;
        }

        /// <summary>Scenes this run loaded additively.</summary>
        public IReadOnlyList<string> LoadedScenes { get; }

        /// <summary>Scenes that were already loaded and therefore left alone.</summary>
        public IReadOnlyList<string> SkippedScenes { get; }

        /// <summary>Scenes that are not registered in Build Settings.</summary>
        public IReadOnlyList<string> MissingScenes { get; }

        public bool HasErrors => MissingScenes.Count > 0;
    }
}

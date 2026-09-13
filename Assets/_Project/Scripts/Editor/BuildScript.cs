using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// Command-line player build, used to verify the project still builds.
    ///
    ///   Unity -batchmode -quit -projectPath . \
    ///         -executeMethod SnowballBattleRoyale.EditorTools.BuildScript.BuildStandalone
    ///
    /// Optional args: <c>-buildTarget &lt;target&gt;</c>, <c>-buildOutput &lt;path&gt;</c>.
    /// </summary>
    public static class BuildScript
    {
        public static void BuildStandalone()
        {
            BuildTarget target = ResolveTarget();
            string output = ResolveOutput(target);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No enabled scenes in Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] Build {summary.result} with {summary.totalErrors} error(s).");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(
                $"[BuildScript] Build succeeded: {output} " +
                $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime:mm\\:ss}).");
        }

        private static BuildTarget ResolveTarget()
        {
            string value = GetArg("-buildTarget");
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, out BuildTarget parsed))
            {
                return parsed;
            }

            return EditorUserBuildSettings.activeBuildTarget;
        }

        private static string ResolveOutput(BuildTarget target)
        {
            string value = GetArg("-buildOutput");
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            return target switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64
                    => "Builds/Windows/SnowballBattleRoyale.exe",
                BuildTarget.StandaloneLinux64
                    => "Builds/Linux/SnowballBattleRoyale",
                _ => "Builds/macOS/SnowballBattleRoyale.app",
            };
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}

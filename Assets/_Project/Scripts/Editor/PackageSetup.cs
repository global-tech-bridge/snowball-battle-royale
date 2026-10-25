using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// One-shot batch-mode helper that installs the packages required by Phase 1.
    /// Run with: Unity -batchmode -quit -executeMethod SnowballBattleRoyale.EditorTools.PackageSetup.InstallRequiredPackages
    /// </summary>
    public static class PackageSetup
    {
        private static readonly string[] RequiredPackages =
        {
            "com.unity.inputsystem",
            "com.unity.render-pipelines.universal",
            "com.unity.test-framework",
            "com.unity.localization",
            "com.unity.ugui",
        };

        public static void InstallRequiredPackages()
        {
            var failed = new List<string>();

            AddAndRemoveRequest request = Client.AddAndRemove(RequiredPackages, null);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError($"[PackageSetup] Batch add failed: {request.Error?.message}. Falling back to one-by-one.");
                foreach (string id in RequiredPackages)
                {
                    if (!AddSingle(id))
                    {
                        failed.Add(id);
                    }
                }
            }

            if (failed.Count > 0)
            {
                Debug.LogError($"[PackageSetup] Failed packages: {string.Join(", ", failed)}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[PackageSetup] All required packages installed.");
        }


        /// <summary>
        /// Installs the Unity AI Assistant package, which provides the built-in Unity MCP server
        /// used to connect external agents (Claude Code, Cursor, ...) to this Editor.
        /// </summary>
        [MenuItem("Tools/Snowball/Install Unity MCP (AI Assistant)")]
        public static void InstallUnityMcp()
        {
            if (!AddSingle("com.unity.ai.assistant"))
            {
                EditorApplication.Exit(1);
            }
        }

        private static bool AddSingle(string id)
        {
            AddRequest req = Client.Add(id);
            while (!req.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (req.Status == StatusCode.Success)
            {
                Debug.Log($"[PackageSetup] Installed {req.Result.packageId}");
                return true;
            }

            Debug.LogError($"[PackageSetup] Could not install {id}: {req.Error?.message}");
            return false;
        }
    }
}

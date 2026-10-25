using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnowballBattleRoyale.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// Applies the Phase 1 project configuration (render pipeline, player settings, boot scenes).
    ///
    /// Idempotent: safe to re-run. Available from the menu or in batch mode via
    /// <c>-executeMethod SnowballBattleRoyale.EditorTools.ProjectSetup.ConfigureProject</c>.
    /// </summary>
    public static class ProjectSetup
    {
        private const string ConfigDir = "Assets/_Project/Configs/Rendering";
        private const string RendererAssetPath = ConfigDir + "/SB_UniversalRenderer.asset";
        private const string PipelineAssetPath = ConfigDir + "/SB_UniversalRenderPipeline.asset";
        private const string ScenesDir = "Assets/_Project/Scenes";
        private const string BootScenePath = ScenesDir + "/00_Boot.unity";
        private const string PersistentScenePath = ScenesDir + "/01_Persistent.unity";

        [MenuItem("Tools/Snowball/Configure Project (Phase 1)")]
        public static void ConfigureProject()
        {
            ConfigurePlayerSettings();
            ConfigureRenderPipeline();
            ConfigureScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetup] Project configuration complete.");
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Global Tech Bridge";
            PlayerSettings.productName = "Snowball Battle Royale";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            var standalone = NamedBuildTarget.Standalone;
            PlayerSettings.SetApiCompatibilityLevel(standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);

            SetActiveInputHandler(InputHandlerNew);

            Debug.Log("[ProjectSetup] Player settings applied (Linear color space, .NET Standard 2.1, new Input System).");
        }

        private const int InputHandlerNew = 1;

        /// <summary>
        /// <c>activeInputHandler</c> has no public API, so it is written through the serialized
        /// ProjectSettings asset. 0 = old, 1 = new (Input System package), 2 = both.
        /// </summary>
        private static void SetActiveInputHandler(int value)
        {
            Object settings = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .FirstOrDefault();

            if (settings == null)
            {
                Debug.LogWarning("[ProjectSetup] Could not load ProjectSettings.asset; input handler unchanged.");
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty property = serialized.FindProperty("activeInputHandler");

            if (property == null)
            {
                Debug.LogWarning("[ProjectSetup] 'activeInputHandler' property not found; input handler unchanged.");
                return;
            }

            if (property.intValue == value)
            {
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureRenderPipeline()
        {
            EnsureFolder(ConfigDir);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            Debug.Log("[ProjectSetup] URP asset assigned to Graphics and all Quality levels.");
        }

        private static void ConfigureScenes()
        {
            EnsureFolder(ScenesDir);

            if (!File.Exists(BootScenePath))
            {
                Scene boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var bootstrapGo = new GameObject("Bootstrap");
                bootstrapGo.AddComponent<Bootstrap>();
                SceneManager.MoveGameObjectToScene(bootstrapGo, boot);
                EditorSceneManager.SaveScene(boot, BootScenePath);
                Debug.Log($"[ProjectSetup] Created {BootScenePath}");
            }

            if (!File.Exists(PersistentScenePath))
            {
                Scene persistent = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var systems = new GameObject("Systems");
                SceneManager.MoveGameObjectToScene(systems, persistent);

                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
                cameraGo.transform.position = new Vector3(0f, 2f, -10f);
                SceneManager.MoveGameObjectToScene(cameraGo, persistent);

                var lightGo = new GameObject("Directional Light");
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                SceneManager.MoveGameObjectToScene(lightGo, persistent);

                EditorSceneManager.SaveScene(persistent, PersistentScenePath);
                Debug.Log($"[ProjectSetup] Created {PersistentScenePath}");
            }

            var wanted = new List<string> { BootScenePath, PersistentScenePath };
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(s => !wanted.Contains(s.path))
                .ToList();
            scenes.InsertRange(0, wanted.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            Debug.Log("[ProjectSetup] Build Settings scene list updated.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

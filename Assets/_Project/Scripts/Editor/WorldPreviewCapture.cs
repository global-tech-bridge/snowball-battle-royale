using System;
using System.IO;
using System.Linq;
using SnowballBattleRoyale.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// Generates <c>Sandbox_World</c> in edit mode and renders reference shots to PNG: a top-down map
    /// and eye-level views of each biome. Lets a reviewer (or an agent in batch mode) see the world
    /// without entering Play Mode.
    ///
    ///   Unity -batchmode -quit -projectPath . \
    ///         -executeMethod SnowballBattleRoyale.EditorTools.WorldPreviewCapture.Capture \
    ///         [-previewOut path]
    ///
    /// Must run with graphics (no -nographics). Output defaults to Logs/WorldPreview.
    /// </summary>
    public static class WorldPreviewCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_World.unity";
        private const string DefaultOutput = "Logs/WorldPreview";

        [MenuItem("Tools/Snowball/Capture World Preview")]
        public static void Capture()
        {
            string output = GetArg("-previewOut") ?? DefaultOutput;
            Directory.CreateDirectory(output);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var generator = Object.FindAnyObjectByType<WorldGenerator>();
            if (generator == null)
            {
                Debug.LogError("[WorldPreview] No WorldGenerator in Sandbox_World.");
                return;
            }

            generator.Generate();
            WorldBlueprint blueprint = generator.Blueprint;

            var cameraObject = new GameObject("PreviewCamera") { hideFlags = HideFlags.DontSave };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.farClipPlane = 1500f;
            camera.clearFlags = CameraClearFlags.Skybox;

            bool fog = RenderSettings.fog;
            try
            {
                RenderSettings.fog = false;
                camera.orthographic = true;
                camera.orthographicSize = blueprint.Layout.HalfSize + 10f;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 500f, 0f), Quaternion.Euler(90f, 0f, 0f));
                Render(camera, 1400, 1400, Path.Combine(output, "00_overview.png"));

                RenderSettings.fog = fog;
                camera.orthographic = false;
                camera.fieldOfView = 60f;

                Shot(generator, camera, output, "01_spawn", blueprint.Layout.SpawnPoint, 25f, 9f);
                Shot(generator, camera, output, "02_settlement", blueprint.Layout.SettlementCenters[0], 45f, 14f);
                foreach (BiomeType biome in new[] { BiomeType.Forest, BiomeType.Mountain, BiomeType.FrozenLake, BiomeType.DeepSnow, BiomeType.Valley })
                {
                    BiomeRegion region = blueprint.Layout.Regions.First(r => r.Type == biome);
                    float distance = region.Radius * (biome == BiomeType.Mountain ? 2.2f : 1.4f);
                    float height = biome == BiomeType.Mountain ? 25f : 10f;
                    Shot(generator, camera, output, $"03_{biome}", region.Center, distance, height);
                }

                Debug.Log($"[WorldPreview] Wrote previews to {Path.GetFullPath(output)} (generated in {generator.GenerationMilliseconds} ms).");
            }
            finally
            {
                RenderSettings.fog = fog;
                Object.DestroyImmediate(cameraObject);
                generator.Clear();
            }
        }

        /// <summary>Eye-level view of <paramref name="target"/> from <paramref name="distance"/> metres to its south-west.</summary>
        private static void Shot(WorldGenerator generator, Camera camera, string output, string name, Vector2 target, float distance, float height)
        {
            Vector2 from = target + (new Vector2(-0.7f, -0.7f) * distance);
            Vector3 eye = generator.GroundPoint(from) + (Vector3.up * height);
            Vector3 look = generator.GroundPoint(target) + (Vector3.up * 2f);

            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
            Render(camera, 1280, 720, Path.Combine(output, $"{name}.png"));
        }

        private static void Render(Camera camera, int width, int height, string path)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
            }
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

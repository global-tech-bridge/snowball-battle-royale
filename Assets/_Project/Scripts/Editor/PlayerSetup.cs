using System.IO;
using System.Linq;
using SnowballBattleRoyale.Player;
using SnowballBattleRoyale.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// Builds the Phase 2 authoring assets: the tuning ScriptableObjects, the Player prefab and the
    /// <c>Sandbox_Player</c> test scene. Idempotent — re-running refreshes the prefab in place.
    /// </summary>
    public static class PlayerSetup
    {
        private const string ConfigDir = "Assets/_Project/Configs/Player";
        private const string StatsAssetPath = ConfigDir + "/PlayerStatsSO.asset";
        private const string LookAssetPath = ConfigDir + "/PlayerLookSettingsSO.asset";
        private const string MaterialDir = "Assets/_Project/Art/Materials";
        private const string PrefabDir = "Assets/_Project/Prefabs/Player";
        private const string PlayerPrefabPath = PrefabDir + "/Player.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Player.unity";
        private const string InputActionsPath = "Assets/_Project/Configs/Input/SBInputActions.inputactions";

        [MenuItem("Tools/Snowball/Build Player Prefab and Sandbox Scene (Phase 2)")]
        public static void BuildPlayerAssets()
        {
            EnsureFolder(ConfigDir);
            EnsureFolder(MaterialDir);
            EnsureFolder(PrefabDir);

            PlayerStatsSO stats = EnsureAsset<PlayerStatsSO>(StatsAssetPath);
            PlayerLookSettingsSO look = EnsureAsset<PlayerLookSettingsSO>(LookAssetPath);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (actions == null)
            {
                Debug.LogError($"[PlayerSetup] Input actions not found at {InputActionsPath}.");
                EditorApplication.Exit(1);
                return;
            }

            GameObject prefab = BuildPlayerPrefab(stats, look, actions);
            BuildSandboxScene(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerSetup] Player prefab and Sandbox_Player scene are up to date.");
        }

        private static GameObject BuildPlayerPrefab(
            PlayerStatsSO stats,
            PlayerLookSettingsSO look,
            InputActionAsset actions)
        {
            var root = new GameObject("Player");
            try
            {
                root.tag = "Player";
                root.layer = LayerMask.NameToLayer("Default");

                CharacterController controller = root.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.35f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.slopeLimit = 50f;
                controller.stepOffset = 0.35f;
                controller.skinWidth = 0.04f;

                var pivot = new GameObject("CameraPivot");
                pivot.transform.SetParent(root.transform, false);
                pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

                var cameraGo = new GameObject("PlayerCamera");
                cameraGo.transform.SetParent(pivot.transform, false);
                Camera camera = cameraGo.AddComponent<Camera>();
                camera.fieldOfView = 70f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 1000f;
                cameraGo.AddComponent<AudioListener>();
                cameraGo.tag = "MainCamera";

                PlayerInputReader input = root.AddComponent<PlayerInputReader>();
                PlayerMovement movement = root.AddComponent<PlayerMovement>();
                PlayerLook playerLook = root.AddComponent<PlayerLook>();
                DebugStatusHud hud = root.AddComponent<DebugStatusHud>();

                SetReferences(input, ("_actions", actions));
                SetReferences(movement, ("_statsConfig", stats), ("_input", input));
                SetReferences(
                    playerLook,
                    ("_settings", look),
                    ("_input", input),
                    ("_cameraPivot", pivot.transform),
                    ("_movement", movement));
                SetReferences(hud, ("_player", movement));

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log($"[PlayerSetup] Wrote {PlayerPrefabPath}");
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildSandboxScene(GameObject playerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material groundMaterial = EnsureMaterial("Sandbox_Ground", new Color(0.82f, 0.85f, 0.88f));
            Material obstacleMaterial = EnsureMaterial("Sandbox_Obstacle", new Color(0.35f, 0.42f, 0.5f));

            var light = new GameObject("Directional Light");
            Light lightComponent = light.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.1f;
            lightComponent.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
            SceneManager.MoveGameObjectToScene(light, scene);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);   // 100 m x 100 m
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            SceneManager.MoveGameObjectToScene(ground, scene);

            var obstacles = new GameObject("Obstacles");
            SceneManager.MoveGameObjectToScene(obstacles, scene);

            // Reference boxes: spacing lets you eyeball the 8 / 14 / 4 / 2 m/s speeds.
            CreateBox(obstacles.transform, "Wall_A", new Vector3(8f, 1f, 12f), new Vector3(6f, 2f, 0.5f), obstacleMaterial);
            CreateBox(obstacles.transform, "Wall_B", new Vector3(-10f, 1.5f, 6f), new Vector3(0.5f, 3f, 8f), obstacleMaterial);
            CreateBox(obstacles.transform, "Crate", new Vector3(3f, 0.5f, 5f), new Vector3(1f, 1f, 1f), obstacleMaterial);
            CreateBox(obstacles.transform, "LowBar_Crouch", new Vector3(-4f, 1.25f, 4f), new Vector3(4f, 0.2f, 1f), obstacleMaterial);

            // 25-degree ramp for slope handling.
            GameObject ramp = CreateBox(obstacles.transform, "Ramp", new Vector3(14f, 1f, -6f), new Vector3(6f, 0.4f, 10f), obstacleMaterial);
            ramp.transform.rotation = Quaternion.Euler(-25f, 0f, 0f);

            // Distance markers every 10 m along +Z, to sanity-check movement speed.
            var markers = new GameObject("DistanceMarkers");
            SceneManager.MoveGameObjectToScene(markers, scene);
            for (int distance = 10; distance <= 40; distance += 10)
            {
                CreateBox(
                    markers.transform,
                    $"Marker_{distance}m",
                    new Vector3(0f, 0.05f, distance),
                    new Vector3(2f, 0.1f, 0.2f),
                    obstacleMaterial);
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            Debug.Log($"[PlayerSetup] Wrote {ScenePath}");
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static Material EnsureMaterial(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[PlayerSetup] URP Lit shader not found; falling back to the default shader.");
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void RegisterSceneInBuildSettings(string path)
        {
            if (EditorBuildSettings.scenes.Any(s => s.path == path))
            {
                return;
            }

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Append(new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        /// <summary>
        /// Assigns inspector-only fields. The components keep them private with
        /// <c>[SerializeField]</c>, so they are written through the serialization API.
        /// </summary>
        private static void SetReferences(Object target, params (string Field, Object Value)[] fields)
        {
            var serialized = new SerializedObject(target);

            foreach ((string field, Object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[PlayerSetup] {target.GetType().Name} has no serialized field '{field}'.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            Debug.Log($"[PlayerSetup] Created {path}");
            return created;
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

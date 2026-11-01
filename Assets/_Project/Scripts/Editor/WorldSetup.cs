using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnowballBattleRoyale.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// Builds the Phase 3 placeholder art and wiring: terrain textures and layers, low-poly tree and
    /// rock meshes, cabin / well / fence / campfire prefabs, the world ScriptableObjects and the
    /// <c>Sandbox_World</c> scene.
    ///
    /// Idempotent. Existing generated assets are overwritten in place (keeping their GUIDs, so
    /// references survive); tuning ScriptableObjects are only created when missing.
    /// </summary>
    public static class WorldSetup
    {
        private const string TextureDir = "Assets/_Project/Art/Terrain/Textures";
        private const string LayerDir = "Assets/_Project/Art/Terrain/Layers";
        private const string MaterialDir = "Assets/_Project/Art/Materials/World";
        private const string MeshDir = "Assets/_Project/Art/Meshes";
        private const string PrefabDir = "Assets/_Project/Prefabs/World";
        private const string ConfigDir = "Assets/_Project/Configs/World";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_World.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string SettingsPath = ConfigDir + "/WorldGenSettingsSO.asset";
        private const string PrefabsPath = ConfigDir + "/WorldPrefabsSO.asset";

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        private const string TerrainShader = "Universal Render Pipeline/Terrain/Lit";

        [MenuItem("Tools/Snowball/Build World Assets and Sandbox Scene (Phase 3)")]
        public static void BuildWorldAssets()
        {
            foreach (string dir in new[] { TextureDir, LayerDir, MaterialDir, MeshDir, PrefabDir, ConfigDir })
            {
                EnsureFolder(dir);
            }

            Palette palette = BuildMaterials();
            TerrainLayer[] layers = BuildTerrainLayers();
            Material terrainMaterial = SaveAsset(
                new Material(FindShader(TerrainShader)) { name = "World_Terrain" },
                $"{MaterialDir}/World_Terrain.mat");

            var prefabs = new Dictionary<string, GameObject>
            {
                ["Pine"] = BuildPine(palette),
                ["Birch"] = BuildBirch(palette),
                ["DeadTree"] = BuildDeadTree(palette),
                ["Rock"] = BuildRock("Rock", 0.6f, 11, 0.3f, 0.5f, palette.Rock),
                ["Boulder"] = BuildRock("Boulder", 1.3f, 23, 0.35f, 0.45f, palette.Rock),
                ["Cabin"] = BuildCabin(palette),
                ["Well"] = BuildWell(palette),
                ["Fence"] = BuildFence(palette),
                ["Campfire"] = BuildCampfire(palette),
            };

            BiomeSettingsSO biomes = EnsureAsset<BiomeSettingsSO>($"{ConfigDir}/BiomeSettingsSO.asset");
            WorldGenSettingsSO settings = EnsureAsset<WorldGenSettingsSO>(SettingsPath);
            SetIfMissing(settings, "_biomes", biomes);

            WorldPrefabsSO worldPrefabs = EnsureAsset<WorldPrefabsSO>(PrefabsPath);
            WirePrefabs(worldPrefabs, prefabs, layers, terrainMaterial);

            AssetDatabase.SaveAssets();
            BuildSandboxScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WorldSetup] World assets and Sandbox_World scene are up to date.");
        }

        // ================================================================== materials & textures

        private sealed class Palette
        {
            public Material Bark;
            public Material Needles;
            public Material BirchBark;
            public Material BirchLeaves;
            public Material DeadWood;
            public Material Rock;
            public Material Wood;
            public Material Roof;
            public Material Stone;
            public Material Water;
            public Material Beacon;
            public Material Fire;
            public Material Embers;
        }

        private static Palette BuildMaterials()
        {
            Texture2D softDot = SaveAsset(SoftDotTexture(), $"{TextureDir}/Particle_SoftDot.asset");

            return new Palette
            {
                Bark = Lit("World_Bark", new Color(0.30f, 0.22f, 0.16f)),
                Needles = Lit("World_PineNeedles", new Color(0.13f, 0.27f, 0.22f)),
                BirchBark = Lit("World_BirchBark", new Color(0.88f, 0.87f, 0.82f)),
                BirchLeaves = Lit("World_BirchLeaves", new Color(0.62f, 0.60f, 0.36f)),
                DeadWood = Lit("World_DeadWood", new Color(0.36f, 0.33f, 0.30f)),
                Rock = Lit("World_Rock", new Color(0.46f, 0.47f, 0.50f), 0.2f),
                Wood = Lit("World_CabinWood", new Color(0.42f, 0.28f, 0.18f)),
                Roof = Lit("World_SnowRoof", new Color(0.90f, 0.92f, 0.95f), 0.3f),
                Stone = Lit("World_Stone", new Color(0.52f, 0.52f, 0.54f), 0.15f),
                Water = Lit("World_DarkWater", new Color(0.05f, 0.08f, 0.12f), 0.8f),
                Beacon = TransparentUnlit("World_Beacon", new Color(0.25f, 1f, 0.45f, 0.35f)),
                Fire = AdditiveParticle("World_Fire", new Color(1f, 0.6f, 0.25f, 1f), softDot),
                Embers = Emissive("World_Embers", new Color(0.25f, 0.08f, 0.02f), new Color(2.2f, 0.7f, 0.15f)),
            };
        }

        private static Material Lit(string name, Color color, float smoothness = 0.1f)
        {
            var material = new Material(FindShader(LitShader)) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            return SaveAsset(material, $"{MaterialDir}/{name}.mat");
        }

        private static Material Emissive(string name, Color color, Color emission)
        {
            var material = new Material(FindShader(LitShader)) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return SaveAsset(material, $"{MaterialDir}/{name}.mat");
        }

        private static Material TransparentUnlit(string name, Color color)
        {
            var material = new Material(FindShader(UnlitShader)) { name = name };
            material.SetColor("_BaseColor", color);
            MakeTransparent(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, blendModeId: 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            return SaveAsset(material, $"{MaterialDir}/{name}.mat");
        }

        private static Material AdditiveParticle(string name, Color color, Texture2D texture)
        {
            var material = new Material(FindShader(ParticleShader)) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            MakeTransparent(material, BlendMode.SrcAlpha, BlendMode.One, blendModeId: 2);
            return SaveAsset(material, $"{MaterialDir}/{name}.mat");
        }

        /// <summary>
        /// Sets the properties URP's material inspector would set for a transparent surface.
        /// (The inspector helper lives in an editor-only URP assembly we do not reference.)
        /// </summary>
        private static void MakeTransparent(Material material, BlendMode src, BlendMode dst, int blendModeId)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", blendModeId);
            material.SetFloat("_SrcBlend", (float)src);
            material.SetFloat("_DstBlend", (float)dst);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)dst);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static TerrainLayer[] BuildTerrainLayers()
        {
            // Order must match TerrainLayerId.
            var specs = new (TerrainLayerId Id, Color Base, Color Accent, float Accent01, float NoiseScale, float Tile, float Smoothness)[]
            {
                (TerrainLayerId.Snow, new Color(0.90f, 0.93f, 0.97f), new Color(0.78f, 0.84f, 0.93f), 0.45f, 6f, 7f, 0.35f),
                (TerrainLayerId.ForestSnow, new Color(0.76f, 0.80f, 0.79f), new Color(0.32f, 0.40f, 0.34f), 0.30f, 9f, 5f, 0.2f),
                // Kept dark on purpose: a pale mountain dissolves into the fog at range and leaves its
                // trees looking like they float.
                (TerrainLayerId.Rock, new Color(0.31f, 0.32f, 0.35f), new Color(0.78f, 0.81f, 0.86f), 0.2f, 5f, 6f, 0.15f),
                (TerrainLayerId.PowderSnow, new Color(0.97f, 0.98f, 1.00f), new Color(0.86f, 0.91f, 0.99f), 0.35f, 4f, 9f, 0.5f),
                (TerrainLayerId.Ice, new Color(0.66f, 0.82f, 0.92f), new Color(0.88f, 0.95f, 1.00f), 0.40f, 3f, 10f, 0.85f),
                (TerrainLayerId.Dirt, new Color(0.40f, 0.30f, 0.21f), new Color(0.30f, 0.22f, 0.15f), 0.50f, 8f, 3f, 0.05f),
                (TerrainLayerId.DeepDirt, new Color(0.22f, 0.15f, 0.10f), new Color(0.15f, 0.10f, 0.07f), 0.50f, 8f, 3f, 0.05f),
            };

            var layers = new TerrainLayer[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                string name = $"Terrain_{spec.Id}";
                Texture2D texture = SaveAsset(
                    NoiseTexture(name, spec.Base, spec.Accent, spec.Accent01, spec.NoiseScale, 100 + i),
                    $"{TextureDir}/{name}.asset");

                var layer = new TerrainLayer
                {
                    name = name,
                    diffuseTexture = texture,
                    tileSize = new Vector2(spec.Tile, spec.Tile),
                    smoothness = spec.Smoothness,
                    metallic = 0f,
                };

                layers[i] = SaveAsset(layer, $"{LayerDir}/{name}.terrainlayer");
            }

            return layers;
        }

        /// <summary>
        /// 64 px tileable noise texture. Stored as a .asset, not a .png: binary images are routed
        /// through Git LFS, which is not set up yet.
        /// </summary>
        private static Texture2D NoiseTexture(string name, Color baseColor, Color accent, float accentAmount, float scale, int seed)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };

            var noise = new SeededNoise(seed);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = (TileableNoise(noise, x, y, size, scale) * 0.5f) + 0.5f;
                    float speckle = Mathf.Clamp01((v - (1f - accentAmount)) / Mathf.Max(0.01f, accentAmount));
                    Color c = Color.Lerp(baseColor, accent, speckle * speckle);
                    float shade = 0.94f + (0.12f * v);
                    pixels[(y * size) + x] = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            return texture;
        }

        /// <summary>Blends four offset samples so the texture wraps seamlessly at its edges.</summary>
        private static float TileableNoise(SeededNoise noise, int x, int y, int size, float scale)
        {
            float N(float px, float py) => noise.Fbm(px / size * scale, py / size * scale, 3);

            float tx = x / (float)size;
            float ty = y / (float)size;
            float a = Mathf.Lerp(N(x, y), N(x - size, y), tx);
            float b = Mathf.Lerp(N(x, y - size), N(x - size, y - size), tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static Texture2D SoftDotTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true)
            {
                name = "Particle_SoftDot",
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size * 2f) - 1f;
                    float dy = ((y + 0.5f) / size * 2f) - 1f;
                    float falloff = Mathf.Clamp01(1f - Mathf.Sqrt((dx * dx) + (dy * dy)));
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, falloff * falloff);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            return texture;
        }

        // ================================================================== trees & rocks

        private static GameObject BuildPine(Palette palette)
        {
            var parts = new List<ProceduralMeshes.Part>
            {
                Part(ProceduralMeshes.Frustum(0.25f, 0.16f, 2.2f, 7), Vector3.zero, 0f, 0),
                Part(ProceduralMeshes.Frustum(2.1f, 0f, 3.2f, 8), new Vector3(0f, 1.3f, 0f), 0f, 1),
                Part(ProceduralMeshes.Frustum(1.6f, 0f, 2.8f, 8), new Vector3(0f, 2.7f, 0f), 22f, 1),
                Part(ProceduralMeshes.Frustum(1.05f, 0f, 2.3f, 8), new Vector3(0f, 4.0f, 0f), 11f, 1),
            };

            Mesh mesh = SaveAsset(ProceduralMeshes.Combine("Tree_Pine", parts, 2), $"{MeshDir}/Tree_Pine.asset");
            return SaveTreePrefab("Tree_Pine", mesh, new[] { palette.Bark, palette.Needles }, 0.3f, 2.2f);
        }

        private static GameObject BuildBirch(Palette palette)
        {
            var parts = new List<ProceduralMeshes.Part>
            {
                Part(ProceduralMeshes.Frustum(0.16f, 0.09f, 6f, 7), Vector3.zero, 0f, 0),
                Part(ProceduralMeshes.Blob(1f, new Vector3(1.3f, 1f, 1.3f)), new Vector3(0.2f, 4.6f, 0.1f), 0f, 1),
                Part(ProceduralMeshes.Blob(0.95f, new Vector3(1.1f, 1f, 1.2f)), new Vector3(-0.4f, 5.4f, -0.2f), 40f, 1),
                Part(ProceduralMeshes.Blob(0.8f, new Vector3(1f, 1.1f, 1f)), new Vector3(0.1f, 6.1f, 0.3f), 80f, 1),
            };

            Mesh mesh = SaveAsset(ProceduralMeshes.Combine("Tree_Birch", parts, 2), $"{MeshDir}/Tree_Birch.asset");
            return SaveTreePrefab("Tree_Birch", mesh, new[] { palette.BirchBark, palette.BirchLeaves }, 0.2f, 5f);
        }

        private static GameObject BuildDeadTree(Palette palette)
        {
            Mesh branch = ProceduralMeshes.Frustum(0.08f, 0.02f, 1.8f, 5);
            var parts = new List<ProceduralMeshes.Part>
            {
                Part(ProceduralMeshes.Frustum(0.22f, 0.06f, 4.8f, 6), Vector3.zero, 0f, 0),
                new ProceduralMeshes.Part(branch, Matrix4x4.TRS(new Vector3(0f, 2.2f, 0f), Quaternion.Euler(0f, 0f, 50f), Vector3.one), 0),
                new ProceduralMeshes.Part(branch, Matrix4x4.TRS(new Vector3(0f, 3.0f, 0f), Quaternion.Euler(0f, 130f, 0f) * Quaternion.Euler(0f, 0f, 45f), Vector3.one), 0),
                new ProceduralMeshes.Part(branch, Matrix4x4.TRS(new Vector3(0f, 3.7f, 0f), Quaternion.Euler(0f, 250f, 0f) * Quaternion.Euler(0f, 0f, 55f), Vector3.one), 0),
            };

            Mesh mesh = SaveAsset(ProceduralMeshes.Combine("Tree_Dead", parts, 1), $"{MeshDir}/Tree_Dead.asset");
            return SaveTreePrefab("Tree_Dead", mesh, new[] { palette.DeadWood }, 0.25f, 4.8f);
        }

        private static ProceduralMeshes.Part Part(Mesh mesh, Vector3 position, float yaw, int submesh)
        {
            return new ProceduralMeshes.Part(mesh, Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one), submesh);
        }

        /// <summary>
        /// Terrain tree prototypes need the renderer on the prefab root, and use a root capsule
        /// collider for tree collisions.
        /// </summary>
        private static GameObject SaveTreePrefab(string name, Mesh mesh, Material[] materials, float trunkRadius, float trunkHeight)
        {
            var root = new GameObject(name);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;

                // A LODGroup makes the terrain treat this as a LOD tree: it culls distant trees, and
                // skips the legacy "must use Nature/Soft Occlusion" check, which URP cannot satisfy.
                var lod = root.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(0.015f, new Renderer[] { renderer }) });
                lod.RecalculateBounds();

                var collider = root.AddComponent<CapsuleCollider>();
                collider.radius = trunkRadius;
                collider.height = trunkHeight;
                collider.center = new Vector3(0f, trunkHeight * 0.5f, 0f);

                return SavePrefab(root, name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildRock(string name, float radius, int seed, float lumpiness, float flatten, Material material)
        {
            Mesh mesh = SaveAsset(ProceduralMeshes.Rock(radius, 2, seed, lumpiness, flatten), $"{MeshDir}/{name}.asset");

            var root = new GameObject(name);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                root.AddComponent<MeshRenderer>().sharedMaterial = material;
                var collider = root.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
                return SavePrefab(root, name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ================================================================== sites

        /// <summary>5 x 4 m cabin with a doorway on its +Z side, a snow-covered gable roof and a beacon.</summary>
        private static GameObject BuildCabin(Palette palette)
        {
            var root = new GameObject("Cabin");
            try
            {
                Transform t = root.transform;
                Box(t, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(5f, 0.2f, 4f), palette.Wood);
                Box(t, "Wall_Back", new Vector3(0f, 1.4f, -1.9f), new Vector3(5f, 2.8f, 0.2f), palette.Wood);
                Box(t, "Wall_Left", new Vector3(-2.4f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 4f), palette.Wood);
                Box(t, "Wall_Right", new Vector3(2.4f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 4f), palette.Wood);
                Box(t, "Wall_FrontLeft", new Vector3(-1.55f, 1.4f, 1.9f), new Vector3(1.9f, 2.8f, 0.2f), palette.Wood);
                Box(t, "Wall_FrontRight", new Vector3(1.55f, 1.4f, 1.9f), new Vector3(1.9f, 2.8f, 0.2f), palette.Wood);
                Box(t, "Lintel", new Vector3(0f, 2.45f, 1.9f), new Vector3(1.2f, 0.7f, 0.2f), palette.Wood);

                // Gable roof, ridge along X: 30 degree pitch with a 0.3 m overhang.
                Box(t, "Roof_Front", new Vector3(0f, 3.46f, 1.15f), new Vector3(5.6f, 0.15f, 2.66f), palette.Roof, euler: new Vector3(30f, 0f, 0f));
                Box(t, "Roof_Back", new Vector3(0f, 3.46f, -1.15f), new Vector3(5.6f, 0.15f, 2.66f), palette.Roof, euler: new Vector3(-30f, 0f, 0f));
                Gable(t, -2.4f, palette.Wood);
                Gable(t, 2.4f, palette.Wood);
                Box(t, "Chimney", new Vector3(1.5f, 3.9f, -0.8f), new Vector3(0.5f, 1.6f, 0.5f), palette.Stone);

                var interaction = new GameObject("InteractionPoint").transform;
                interaction.SetParent(t, false);
                interaction.localPosition = new Vector3(0f, 0.2f, 0f);

                var beaconRoot = new GameObject("Beacon");
                beaconRoot.transform.SetParent(t, false);
                beaconRoot.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                CabinBeacon beacon = beaconRoot.AddComponent<CabinBeacon>();

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Pillar";
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(beaconRoot.transform, false);
                pillar.transform.localPosition = new Vector3(0f, 30f, 0f);
                pillar.transform.localScale = new Vector3(0.6f, 30f, 0.6f);
                var pillarRenderer = pillar.GetComponent<MeshRenderer>();
                pillarRenderer.sharedMaterial = palette.Beacon;
                pillarRenderer.shadowCastingMode = ShadowCastingMode.Off;

                SetReferences(beacon, ("_pillar", pillar));
                Cabin cabin = root.AddComponent<Cabin>();
                SetReferences(cabin, ("_beacon", beacon), ("_interactionPoint", interaction));

                return SavePrefab(root, "Cabin");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Triangular gable end: a 45-degree diamond under a parent squashed to the roof pitch. The
        /// lower half hides inside the side wall.
        /// </summary>
        private static void Gable(Transform parent, float x, Material material)
        {
            var holder = new GameObject(x < 0f ? "Gable_Left" : "Gable_Right").transform;
            holder.SetParent(parent, false);
            holder.localPosition = new Vector3(x, 2.8f, 0f);
            holder.localScale = new Vector3(1f, 1.33f / 2.3f, 1f);

            Box(holder, "Diamond", Vector3.zero, new Vector3(0.16f, 3.25f, 3.25f), material, collider: false, euler: new Vector3(45f, 0f, 0f));
        }

        private static GameObject BuildWell(Palette palette)
        {
            var root = new GameObject("Well");
            try
            {
                Transform t = root.transform;
                Primitive(t, PrimitiveType.Cylinder, "Ring", new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.45f, 1.6f), palette.Stone);
                Primitive(t, PrimitiveType.Cylinder, "Water", new Vector3(0f, 0.905f, 0f), new Vector3(1.25f, 0.012f, 1.25f), palette.Water, collider: false);
                Box(t, "Post_Left", new Vector3(-0.7f, 1.7f, 0f), new Vector3(0.14f, 1.6f, 0.14f), palette.Wood);
                Box(t, "Post_Right", new Vector3(0.7f, 1.7f, 0f), new Vector3(0.14f, 1.6f, 0.14f), palette.Wood);
                Box(t, "Beam", new Vector3(0f, 2.45f, 0f), new Vector3(1.6f, 0.12f, 0.12f), palette.Wood, collider: false);
                Box(t, "Roof_Front", new Vector3(0f, 2.75f, 0.35f), new Vector3(1.9f, 0.08f, 0.9f), palette.Roof, collider: false, euler: new Vector3(35f, 0f, 0f));
                Box(t, "Roof_Back", new Vector3(0f, 2.75f, -0.35f), new Vector3(1.9f, 0.08f, 0.9f), palette.Roof, collider: false, euler: new Vector3(-35f, 0f, 0f));
                return SavePrefab(root, "Well");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>3 m fence segment along local X, with one root collider instead of one per plank.</summary>
        private static GameObject BuildFence(Palette palette)
        {
            var root = new GameObject("Fence");
            try
            {
                Transform t = root.transform;
                Box(t, "Post_A", new Vector3(-1.45f, 0.6f, 0f), new Vector3(0.14f, 1.2f, 0.14f), palette.Wood, collider: false);
                Box(t, "Post_B", new Vector3(1.45f, 0.6f, 0f), new Vector3(0.14f, 1.2f, 0.14f), palette.Wood, collider: false);
                Box(t, "Rail_Low", new Vector3(0f, 0.45f, 0f), new Vector3(3f, 0.08f, 0.06f), palette.Wood, collider: false);
                Box(t, "Rail_High", new Vector3(0f, 0.95f, 0f), new Vector3(3f, 0.08f, 0.06f), palette.Wood, collider: false);

                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.6f, 0f);
                collider.size = new Vector3(3f, 1.2f, 0.15f);

                return SavePrefab(root, "Fence");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildCampfire(Palette palette)
        {
            var root = new GameObject("Campfire");
            try
            {
                Transform t = root.transform;

                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI * 2f / 8f;
                    var position = new Vector3(Mathf.Cos(angle) * 0.55f, 0.06f, Mathf.Sin(angle) * 0.55f);
                    Primitive(t, PrimitiveType.Sphere, $"Stone_{i}", position, new Vector3(0.32f, 0.22f, 0.32f), palette.Stone, collider: false);
                }

                for (int i = 0; i < 3; i++)
                {
                    float yaw = i * 120f;
                    Vector3 outward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    Primitive(
                        t,
                        PrimitiveType.Cylinder,
                        $"Log_{i}",
                        (outward * 0.18f) + (Vector3.up * 0.28f),
                        new Vector3(0.12f, 0.45f, 0.12f),
                        palette.Wood,
                        collider: false,
                        euler: new Vector3(-35f, yaw, 0f));
                }

                Primitive(t, PrimitiveType.Sphere, "Embers", new Vector3(0f, 0.03f, 0f), new Vector3(0.6f, 0.06f, 0.6f), palette.Embers, collider: false);

                var lightObject = new GameObject("Light");
                lightObject.transform.SetParent(t, false);
                lightObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.55f, 0.2f);
                light.range = 10f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;

                ParticleSystem flames = BuildFlames(t, palette.Fire);

                Campfire campfire = root.AddComponent<Campfire>();
                SetReferences(campfire, ("_light", light), ("_flames", flames));

                return SavePrefab(root, "Campfire");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static ParticleSystem BuildFlames(Transform parent, Material material)
        {
            var go = new GameObject("Flames");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            var particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.65f, 0.25f), new Color(1f, 0.35f, 0.1f));
            main.gravityModifier = -0.05f;
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 35f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.22f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.75f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.08f), 0.6f),
                    new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return particles;
        }

        // ================================================================== config & scene

        private static void WirePrefabs(
            WorldPrefabsSO target,
            IReadOnlyDictionary<string, GameObject> prefabs,
            TerrainLayer[] layers,
            Material terrainMaterial)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty("_pine").objectReferenceValue = prefabs["Pine"];
            serialized.FindProperty("_birch").objectReferenceValue = prefabs["Birch"];
            serialized.FindProperty("_deadTree").objectReferenceValue = prefabs["DeadTree"];
            serialized.FindProperty("_rock").objectReferenceValue = prefabs["Rock"];
            serialized.FindProperty("_boulder").objectReferenceValue = prefabs["Boulder"];
            serialized.FindProperty("_cabin").objectReferenceValue = prefabs["Cabin"];
            serialized.FindProperty("_well").objectReferenceValue = prefabs["Well"];
            serialized.FindProperty("_fence").objectReferenceValue = prefabs["Fence"];
            serialized.FindProperty("_campfire").objectReferenceValue = prefabs["Campfire"];
            serialized.FindProperty("_terrainMaterial").objectReferenceValue = terrainMaterial;

            SerializedProperty layerArray = serialized.FindProperty("_terrainLayers");
            layerArray.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
            {
                layerArray.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void BuildSandboxScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Load only after NewScene: opening a scene in Single mode unloads unused assets, and a
            // reference held from before can then serialize as null without any error.
            var settings = AssetDatabase.LoadAssetAtPath<WorldGenSettingsSO>(SettingsPath);
            var prefabs = AssetDatabase.LoadAssetAtPath<WorldPrefabsSO>(PrefabsPath);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>() != null)
                {
                    // The Player prefab brings its own camera.
                    Object.DestroyImmediate(root);
                }
                else if (root.TryGetComponent(out Light sun) && sun.type == LightType.Directional)
                {
                    sun.transform.rotation = Quaternion.Euler(32f, -35f, 0f);
                    sun.color = new Color(0.94f, 0.96f, 1f);
                    sun.intensity = 1.1f;
                    sun.shadows = LightShadows.Soft;
                }
            }

            // Cold, hazy winter air. Phase 8 animates fog density with the weather; this is the
            // clear early-game value.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.002f;
            RenderSettings.fogColor = new Color(0.80f, 0.86f, 0.92f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.74f, 0.81f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.64f, 0.69f, 0.77f);
            RenderSettings.ambientGroundColor = new Color(0.52f, 0.55f, 0.60f);
            RenderSettings.skybox = BuildSkybox();

            var world = new GameObject("World");
            WorldGenerator generator = world.AddComponent<WorldGenerator>();
            SetReferences(generator, ("_settings", settings), ("_prefabs", prefabs));
            VerifyReferences(generator, "_settings", "_prefabs");

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab != null)
            {
                // Placed high; WorldGenerator moves it onto the generated spawn point on Awake.
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                player.transform.position = new Vector3(0f, 60f, 0f);
            }
            else
            {
                Debug.LogWarning($"[WorldSetup] {PlayerPrefabPath} not found; Sandbox_World has no player.");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            if (EditorBuildSettings.scenes.All(s => s.path != ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(ScenePath, true))
                    .ToArray();
            }

            Debug.Log($"[WorldSetup] Wrote {ScenePath}");
        }

        /// <summary>
        /// Procedural sky with a snow-coloured ground half. The default skybox's brown ground shows
        /// as a band past the map edge, which looks wrong on a snowfield.
        /// </summary>
        private static Material BuildSkybox()
        {
            var material = new Material(FindShader("Skybox/Procedural")) { name = "World_Skybox" };
            material.SetColor("_GroundColor", new Color(0.82f, 0.86f, 0.90f));
            material.SetColor("_SkyTint", new Color(0.55f, 0.62f, 0.72f));
            material.SetFloat("_AtmosphereThickness", 0.9f);
            material.SetFloat("_Exposure", 1.15f);
            material.SetFloat("_SunSize", 0.03f);
            return SaveAsset(material, $"{MaterialDir}/World_Skybox.mat");
        }

        // ================================================================== helpers

        private static GameObject Box(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Material material,
            bool collider = true,
            Vector3? euler = null)
        {
            return Primitive(parent, PrimitiveType.Cube, name, position, size, material, collider, euler);
        }

        private static GameObject Primitive(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = true,
            Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (!collider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            return go;
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
        }

        /// <summary>
        /// Creates the asset, or overwrites an existing one in place so its GUID — and every
        /// reference to it — survives a rebuild.
        /// </summary>
        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            if (asset is Texture2D source && existing is Texture2D target)
            {
                // Copy pixels explicitly: serialized-copying a texture does not reliably carry
                // CPU-side pixel data created at edit time.
                target.Reinitialize(source.width, source.height, source.format, source.mipmapCount > 1);
                target.wrapMode = source.wrapMode;
                target.filterMode = source.filterMode;
                target.anisoLevel = source.anisoLevel;
                target.SetPixels(source.GetPixels());
                target.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            }
            else
            {
                EditorUtility.CopySerialized(asset, existing);
            }

            existing.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
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
            Debug.Log($"[WorldSetup] Created {path}");
            return created;
        }

        private static void SetIfMissing(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetReferences(Object target, params (string Field, Object Value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[WorldSetup] {target.GetType().Name} has no serialized field '{field}'.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fails loudly if a reference we just assigned did not stick.</summary>
        private static void VerifyReferences(Object target, params string[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (string field in fields)
            {
                if (serialized.FindProperty(field)?.objectReferenceValue == null)
                {
                    throw new System.InvalidOperationException(
                        $"[WorldSetup] {target.GetType().Name}.{field} is null after assignment.");
                }
            }
        }

        private static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader == null)
            {
                Debug.LogError($"[WorldSetup] Shader '{name}' not found; is URP installed?");
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            return shader;
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

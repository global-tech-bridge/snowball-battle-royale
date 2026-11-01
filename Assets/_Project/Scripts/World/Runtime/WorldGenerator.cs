using System;
using System.Collections.Generic;
using System.Diagnostics;
using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Builds the playable world at runtime from a seed: terrain, trees, rocks, settlements, campfires
    /// and map boundary, then hands the terrain to a <see cref="TerrainDigSystem"/>.
    ///
    /// The terrain is generated rather than saved as an asset on purpose: a 700 m TerrainData asset is
    /// tens of megabytes of binary in git, and digging in Play Mode would write into the asset and
    /// persist. A seed is all a client (or, in Phase 12, a server) needs to rebuild the same map.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class WorldGenerator : MonoBehaviour
    {
        private const string LogCategory = "World";
        private const string GeneratedRootName = "Generated";
        private const float BoundaryHeight = 300f;
        private const float BoundaryThickness = 4f;

        [SerializeField]
        private WorldGenSettingsSO _settings;

        [SerializeField]
        private WorldPrefabsSO _prefabs;

        [SerializeField]
        private bool _generateOnAwake = true;

        [Tooltip("Move every object tagged Player to the generated spawn point.")]
        [SerializeField]
        private bool _placePlayersAtSpawn = true;

        private Transform _root;

        /// <summary>Raised once generation has finished and the world is playable.</summary>
        public event Action<WorldGenerator> Generated;

        public WorldBlueprint Blueprint { get; private set; }

        public Terrain Terrain { get; private set; }

        public TerrainDigSystem DigSystem { get; private set; }

        public bool IsGenerated => Terrain != null;

        /// <summary>Last generation time in milliseconds.</summary>
        public long GenerationMilliseconds { get; private set; }

        private void Awake()
        {
            if (_generateOnAwake)
            {
                Generate();
            }
        }

        /// <summary>Replaces the settings at runtime, e.g. to start a match on a new seed.</summary>
        public void Configure(WorldGenSettingsSO settings, WorldPrefabsSO prefabs)
        {
            _settings = settings;
            _prefabs = prefabs;
        }

        public void Generate()
        {
            Clear();

            if (_settings == null || _prefabs == null)
            {
                SBLog.Error("WorldGenerator needs both WorldGenSettingsSO and WorldPrefabsSO.", this, LogCategory);
                return;
            }

            var timer = Stopwatch.StartNew();
            WorldGenConfig config = _settings.ToConfig();
            Blueprint = WorldBlueprint.Create(config);

            _root = new GameObject(GeneratedRootName).transform;
            _root.SetParent(transform, false);
            if (!Application.isPlaying)
            {
                // Edit-mode previews must never end up in a saved scene: the runtime TerrainData is
                // not an asset, so the scene would reference nothing.
                _root.gameObject.hideFlags = HideFlags.DontSave;
            }

            Terrain = BuildTerrain(config);
            BuildRocks();
            BuildSites();
            BuildBoundary(config.Layout.Size);

            DigSystem = Terrain.gameObject.AddComponent<TerrainDigSystem>();
            DigSystem.Initialize(Terrain, Blueprint.Biomes, config.Dig, config.Soil, DigLocks());

            if (_placePlayersAtSpawn && Application.isPlaying)
            {
                PlacePlayersAtSpawn();
            }

            timer.Stop();
            GenerationMilliseconds = timer.ElapsedMilliseconds;
            SBLog.Info(
                $"World generated from seed {config.Seed} in {GenerationMilliseconds} ms: " +
                $"{Blueprint.Layout.Regions.Count} biome regions, {Blueprint.Props.Count} props, " +
                $"{Blueprint.Layout.Cabins.Count} cabins, {Blueprint.Layout.Campfires.Count} campfires.",
                this,
                LogCategory);

            Generated?.Invoke(this);
        }

        public void Clear()
        {
            Transform existing = _root != null ? _root : transform.Find(GeneratedRootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }

            _root = null;
            Terrain = null;
            DigSystem = null;
            Blueprint = null;
        }

        /// <summary>World position on the terrain surface at map XZ <paramref name="xz"/>.</summary>
        public Vector3 GroundPoint(Vector2 xz)
        {
            var point = new Vector3(xz.x, 0f, xz.y);
            if (Terrain != null)
            {
                point.y = Terrain.SampleHeight(point) + Terrain.transform.position.y;
            }

            return point;
        }

        // ------------------------------------------------------------------ terrain

        private Terrain BuildTerrain(WorldGenConfig config)
        {
            WorldLayout layout = Blueprint.Layout;
            int heightRes = _settings.HeightmapResolution;
            int alphaRes = _settings.AlphamapResolution;

            var data = new TerrainData { name = "GeneratedTerrain", heightmapResolution = heightRes };

            // Resolution must be set before size: changing it rescales the existing size.
            data.size = new Vector3(layout.Size, config.Shape.TerrainHeight, layout.Size);
            data.SetHeights(0, 0, Blueprint.Heights.Sample(heightRes));

            data.terrainLayers = _prefabs.TerrainLayers;
            data.alphamapResolution = alphaRes;
            data.SetAlphamaps(0, 0, PaintAlphamap(alphaRes, data.alphamapLayers, config.Soil));

            data.treePrototypes = new[]
            {
                new TreePrototype { prefab = _prefabs.PrefabFor(PropKind.Pine) },
                new TreePrototype { prefab = _prefabs.PrefabFor(PropKind.Birch) },
                new TreePrototype { prefab = _prefabs.PrefabFor(PropKind.DeadTree) },
            };
            data.SetTreeInstances(BuildTreeInstances(layout), snapToHeightmap: true);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Terrain";
            terrainObject.transform.SetParent(_root, false);
            terrainObject.transform.position = new Vector3(-layout.HalfSize, 0f, -layout.HalfSize);

            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.materialTemplate = _prefabs.TerrainMaterial;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;

            // Shade the whole map with the full terrain shader. Past basemapDistance the terrain
            // switches to a pre-baked basemap, which is not built for terrain generated at runtime:
            // distant mountains rendered sky-white and their trees appeared to float.
            terrain.basemapDistance = layout.Size * 1.5f;
            terrain.treeDistance = 400f;

            // Mesh trees have no billboards; keep them as meshes out to the tree distance.
            terrain.treeBillboardDistance = 2000f;
            terrain.treeCrossFadeLength = 0f;

            return terrain;
        }

        private float[,,] PaintAlphamap(int resolution, int layers, SoilSettings soil)
        {
            var alphas = new float[resolution, resolution, layers];
            var weights = new float[TerrainPaintRules.LayerCount];
            float size = Blueprint.Layout.Size;
            float half = Blueprint.Layout.HalfSize;
            float texel = size / resolution;
            int paintable = Mathf.Min(layers, TerrainPaintRules.LayerCount);

            if (paintable < TerrainPaintRules.LayerCount)
            {
                SBLog.Warn(
                    $"WorldPrefabsSO provides {layers} terrain layers; {TerrainPaintRules.LayerCount} are expected.",
                    this,
                    LogCategory);
            }

            for (int z = 0; z < resolution; z++)
            {
                float worldZ = -half + ((z + 0.5f) * texel);
                for (int x = 0; x < resolution; x++)
                {
                    var p = new Vector2(-half + ((x + 0.5f) * texel), worldZ);
                    TerrainPaintRules.Evaluate(Blueprint.Biomes, p, 0f, soil, weights);

                    for (int l = 0; l < paintable; l++)
                    {
                        alphas[z, x, l] = weights[l];
                    }
                }
            }

            return alphas;
        }

        private TreeInstance[] BuildTreeInstances(WorldLayout layout)
        {
            var instances = new List<TreeInstance>();
            foreach (PropPlacement prop in Blueprint.Props)
            {
                if (!prop.Kind.IsTree())
                {
                    continue;
                }

                instances.Add(new TreeInstance
                {
                    position = new Vector3(
                        (prop.Position.x + layout.HalfSize) / layout.Size,
                        0f,
                        (prop.Position.y + layout.HalfSize) / layout.Size),
                    prototypeIndex = prop.Kind switch
                    {
                        PropKind.Birch => 1,
                        PropKind.DeadTree => 2,
                        _ => 0,
                    },
                    widthScale = prop.Scale,
                    heightScale = prop.Scale,
                    rotation = prop.RotationDegrees * Mathf.Deg2Rad,

                    // Terrain trees are tinted by these; leaving them default renders black trees.
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }

            return instances.ToArray();
        }

        // ------------------------------------------------------------------ objects

        private void BuildRocks()
        {
            Transform parent = CreateGroup("Rocks");

            foreach (PropPlacement prop in Blueprint.Props)
            {
                if (!prop.Kind.IsRock())
                {
                    continue;
                }

                GameObject prefab = _prefabs.PrefabFor(prop.Kind);
                if (prefab == null)
                {
                    continue;
                }

                // Sink rocks slightly so they read as embedded rather than resting on the snow.
                Vector3 position = GroundPoint(prop.Position) - (Vector3.up * (0.2f * prop.Scale));
                GameObject rock = Instantiate(prefab, position, Quaternion.Euler(0f, prop.RotationDegrees, 0f), parent);
                rock.transform.localScale = Vector3.one * prop.Scale;
            }

            if (Application.isPlaying)
            {
                StaticBatchingUtility.Combine(parent.gameObject);
            }
        }

        private void BuildSites()
        {
            WorldLayout layout = Blueprint.Layout;

            Spawn(_prefabs.Cabin, layout.Cabins, CreateGroup("Cabins"));
            Spawn(_prefabs.Well, layout.Wells, CreateGroup("Wells"));
            Spawn(_prefabs.Fence, layout.Fences, CreateGroup("Fences"));

            Transform fires = CreateGroup("Campfires");
            foreach (WorldSite site in layout.Campfires)
            {
                GameObject fire = SpawnOne(_prefabs.Campfire, site, fires);
                if (fire != null && fire.TryGetComponent(out Campfire campfire))
                {
                    campfire.Initialize(site.Lifetime);
                }
            }
        }

        private void Spawn(GameObject prefab, IReadOnlyList<WorldSite> sites, Transform parent)
        {
            foreach (WorldSite site in sites)
            {
                SpawnOne(prefab, site, parent);
            }
        }

        private GameObject SpawnOne(GameObject prefab, WorldSite site, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            return Instantiate(
                prefab,
                GroundPoint(site.Position),
                Quaternion.Euler(0f, site.RotationDegrees, 0f),
                parent);
        }

        private void BuildBoundary(float size)
        {
            Transform parent = CreateGroup("Boundary");
            float half = size * 0.5f;
            float offset = half + (BoundaryThickness * 0.5f);
            float length = size + (BoundaryThickness * 2f);

            CreateWall(parent, "North", new Vector3(0f, 0f, offset), new Vector3(length, BoundaryHeight, BoundaryThickness));
            CreateWall(parent, "South", new Vector3(0f, 0f, -offset), new Vector3(length, BoundaryHeight, BoundaryThickness));
            CreateWall(parent, "East", new Vector3(offset, 0f, 0f), new Vector3(BoundaryThickness, BoundaryHeight, length));
            CreateWall(parent, "West", new Vector3(-offset, 0f, 0f), new Vector3(BoundaryThickness, BoundaryHeight, length));
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var wall = new GameObject($"Wall_{name}");
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position + (Vector3.up * (size.y * 0.5f));
            wall.AddComponent<BoxCollider>().size = size;
        }

        private Transform CreateGroup(string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(_root, false);
            return group;
        }

        private IEnumerable<(Vector2 Center, float Radius)> DigLocks()
        {
            foreach (WorldSite cabin in Blueprint.Layout.Cabins)
            {
                yield return (cabin.Position, cabin.FootprintRadius);
            }

            foreach (WorldSite well in Blueprint.Layout.Wells)
            {
                yield return (well.Position, well.FootprintRadius);
            }
        }

        private void PlacePlayersAtSpawn()
        {
            Vector3 spawn = GroundPoint(Blueprint.Layout.SpawnPoint) + (Vector3.up * 0.1f);

            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                // A CharacterController overrides transform writes while enabled.
                var controller = player.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }

                player.transform.position = spawn;

                if (controller != null)
                {
                    controller.enabled = true;
                }
            }

            Physics.SyncTransforms();
        }
    }
}

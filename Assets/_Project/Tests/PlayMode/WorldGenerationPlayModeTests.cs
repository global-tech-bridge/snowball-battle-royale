using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SnowballBattleRoyale.Tests.PlayMode
{
    /// <summary>
    /// Generates the real default world once and checks what actually ends up in the scene: terrain
    /// size, tree / rock / site counts, biome colouring, levelled sites and the dig system.
    /// </summary>
    public sealed class WorldGenerationPlayModeTests
    {
        private const string SettingsPath = "Assets/_Project/Configs/World/WorldGenSettingsSO.asset";
        private const string PrefabsPath = "Assets/_Project/Configs/World/WorldPrefabsSO.asset";

        private GameObject _worldObject;
        private WorldGenerator _world;

        private WorldBlueprint Blueprint => _world.Blueprint;

        private Terrain Terrain => _world.Terrain;

        [OneTimeSetUp]
        public void GenerateWorld()
        {
            WorldGenSettingsSO settings = Load<WorldGenSettingsSO>(SettingsPath);
            WorldPrefabsSO prefabs = Load<WorldPrefabsSO>(PrefabsPath);
            Assert.That(settings, Is.Not.Null, $"Missing {SettingsPath}");
            Assert.That(prefabs, Is.Not.Null, $"Missing {PrefabsPath}");

            // Configure before activation so Awake generates with these assets.
            _worldObject = new GameObject("TestWorld");
            _worldObject.SetActive(false);
            _world = _worldObject.AddComponent<WorldGenerator>();
            _world.Configure(settings, prefabs);
            _worldObject.SetActive(true);

            Assert.That(_world.IsGenerated, Is.True, "World did not generate.");
            TestContext.WriteLine($"World generated in {_world.GenerationMilliseconds} ms.");
        }

        [OneTimeTearDown]
        public void DestroyWorld()
        {
            if (_worldObject != null)
            {
                Object.DestroyImmediate(_worldObject);
            }
        }

        // ---------- terrain ----------

        [Test]
        public void Terrain_Is700By700MetresCentredOnTheOrigin()
        {
            Assert.That(Terrain.terrainData.size.x, Is.EqualTo(700f));
            Assert.That(Terrain.terrainData.size.z, Is.EqualTo(700f));
            Assert.That(Terrain.transform.position, Is.EqualTo(new Vector3(-350f, 0f, -350f)));
        }

        [Test]
        public void Terrain_HasAllSevenPaintLayers()
        {
            Assert.That(Terrain.terrainData.alphamapLayers, Is.EqualTo(TerrainPaintRules.LayerCount));
        }

        [Test]
        public void WholeMapUsesFullTerrainShading()
        {
            // Beyond basemapDistance the terrain draws a pre-baked basemap that runtime-generated
            // terrain does not have; distant mountains then vanish and leave their trees floating.
            float diagonal = 700f * Mathf.Sqrt(2f);

            Assert.That(Terrain.basemapDistance, Is.GreaterThanOrEqualTo(diagonal));
        }

        [Test]
        public void MountainsRiseAboveTheSpawnArea()
        {
            float spawn = SampleHeight(Blueprint.Layout.SpawnPoint);

            foreach (BiomeRegion mountain in Blueprint.Layout.Regions.Where(r => r.Type == BiomeType.Mountain))
            {
                Assert.That(SampleHeight(mountain.Center), Is.GreaterThan(spawn + 25f), mountain.ToString());
            }
        }

        [TestCase(BiomeType.Forest, TerrainLayerId.ForestSnow)]
        [TestCase(BiomeType.Mountain, TerrainLayerId.Rock)]
        [TestCase(BiomeType.DeepSnow, TerrainLayerId.PowderSnow)]
        [TestCase(BiomeType.FrozenLake, TerrainLayerId.Ice)]
        public void EachBiomeIsPaintedWithItsOwnLayer(BiomeType biome, TerrainLayerId expected)
        {
            foreach (BiomeRegion region in Blueprint.Layout.Regions.Where(r => r.Type == biome))
            {
                Assert.That(DominantLayerAt(region.Center), Is.EqualTo(expected), region.ToString());
            }
        }

        [Test]
        public void SpawnAreaIsPlainSnow()
        {
            Assert.That(DominantLayerAt(Blueprint.Layout.SpawnPoint), Is.EqualTo(TerrainLayerId.Snow));
        }

        [Test]
        public void MapShowsAtLeastFiveDistinctGroundTypes()
        {
            var seen = new HashSet<TerrainLayerId>();
            for (float x = -340f; x <= 340f; x += 10f)
            {
                for (float z = -340f; z <= 340f; z += 10f)
                {
                    seen.Add(DominantLayerAt(new Vector2(x, z)));
                }
            }

            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(5), string.Join(", ", seen));
        }

        // ---------- props ----------

        [Test]
        public void AllTreesArePlantedAsTerrainTrees()
        {
            Assert.That(Terrain.terrainData.treeInstanceCount, Is.EqualTo(1100));
            Assert.That(Terrain.terrainData.treePrototypes.Length, Is.EqualTo(3));
            Assert.That(Terrain.terrainData.treePrototypes.All(p => p.prefab != null), Is.True);
        }

        [Test]
        public void AllRocksAreSpawned()
        {
            Assert.That(Group("Rocks").childCount, Is.EqualTo(250));
        }

        // ---------- sites ----------

        [Test]
        public void TwentyCabinsSpawnWithVisibleBeacons()
        {
            List<Cabin> cabins = Group("Cabins").GetComponentsInChildren<Cabin>().ToList();

            Assert.That(cabins.Count, Is.EqualTo(20));
            Assert.That(cabins.All(c => c.Beacon != null && c.Beacon.IsVisible), Is.True);
        }

        [Test]
        public void EverySettlementHasAWellAndFences()
        {
            Assert.That(Group("Wells").childCount, Is.EqualTo(4));
            Assert.That(Group("Fences").childCount, Is.EqualTo(Blueprint.Layout.Fences.Count));
            Assert.That(Group("Fences").childCount, Is.GreaterThan(4 * 20));
        }

        [Test]
        public void TwentyCampfiresAreBurning()
        {
            List<Campfire> fires = Group("Campfires").GetComponentsInChildren<Campfire>().ToList();

            Assert.That(fires.Count, Is.EqualTo(20));
            Assert.That(fires.All(f => f.IsLit), Is.True);
        }

        [Test]
        public void CabinsStandOnLevelGround()
        {
            foreach (Cabin cabin in Group("Cabins").GetComponentsInChildren<Cabin>())
            {
                Vector3 p = cabin.transform.position;
                foreach (Vector3 corner in new[] { new Vector3(2.5f, 0f, 2f), new Vector3(-2.5f, 0f, 2f), new Vector3(2.5f, 0f, -2f), new Vector3(-2.5f, 0f, -2f) })
                {
                    Vector3 world = cabin.transform.TransformPoint(corner);
                    Assert.That(Terrain.SampleHeight(world), Is.EqualTo(p.y).Within(0.15f), cabin.name);
                }
            }
        }

        [Test]
        public void MapEdgesAreWalledOff()
        {
            Assert.That(Group("Boundary").GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo(4));
        }

        [Test]
        public void DigSystemIsReadyForThisTerrain()
        {
            Assert.That(_world.DigSystem, Is.Not.Null);
            Assert.That(_world.DigSystem.IsReady, Is.True);
            Assert.That(TerrainDigSystem.Active, Is.SameAs(_world.DigSystem));
        }

        [Test]
        public void GroundUnderCabinsCannotBeDug()
        {
            Cabin cabin = Group("Cabins").GetComponentInChildren<Cabin>();
            Vector3 floor = cabin.transform.position;

            DigResult result = _world.DigSystem.Dig(floor);

            Assert.That(_world.DigSystem.DepthAt(floor), Is.EqualTo(0f));
            Assert.That(result.Applied, Is.False);
        }

        // ---------- campfire lifetime ----------

        [UnityTest]
        public IEnumerator CampfireGoesOutWhenItsTimeRunsOut()
        {
            Campfire fire = Group("Campfires").GetComponentInChildren<Campfire>();
            var light = fire.GetComponentInChildren<Light>();

            fire.Initialize(0.1f);
            Assume.That(fire.IsLit, Is.True);

            yield return new WaitForSeconds(0.3f);

            Assert.That(fire.IsLit, Is.False);
            Assert.That(light.enabled, Is.False);
            Assert.That(Campfire.Active, Has.No.Member(fire));

            // Relight so the other tests see the generated state.
            fire.Initialize(CampfireState.Endless);
        }

        // ---------- helpers ----------

        private Transform Group(string name)
        {
            Transform group = _worldObject.transform.Find($"Generated/{name}");
            Assert.That(group, Is.Not.Null, $"Generated/{name} missing");
            return group;
        }

        private float SampleHeight(Vector2 xz) => Terrain.SampleHeight(new Vector3(xz.x, 0f, xz.y));

        private TerrainLayerId DominantLayerAt(Vector2 xz)
        {
            TerrainData data = Terrain.terrainData;
            int resolution = data.alphamapResolution;
            int ax = Mathf.Clamp(Mathf.FloorToInt((xz.x + 350f) / 700f * resolution), 0, resolution - 1);
            int az = Mathf.Clamp(Mathf.FloorToInt((xz.y + 350f) / 700f * resolution), 0, resolution - 1);
            float[,,] texel = data.GetAlphamaps(ax, az, 1, 1);

            int best = 0;
            for (int l = 1; l < data.alphamapLayers; l++)
            {
                if (texel[0, 0, l] > texel[0, 0, best])
                {
                    best = l;
                }
            }

            return (TerrainLayerId)best;
        }

        private static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }
    }
}

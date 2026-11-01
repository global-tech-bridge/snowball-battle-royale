using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SnowballBattleRoyale.Player;
using SnowballBattleRoyale.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SnowballBattleRoyale.Tests.PlayMode
{
    /// <summary>
    /// Phase 3 digging acceptance, driven through the real Player prefab with a synthetic mouse:
    /// right-click dents the ground once, holding keeps digging into a pit deep enough to hide in,
    /// the collider follows the new surface, and the ground changes colour with depth.
    ///
    /// Uses a small flat terrain instead of the full world so each test starts from untouched snow.
    /// </summary>
    public sealed class DigPlayModeTests : InputTestFixture
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string WorldPrefabsPath = "Assets/_Project/Configs/World/WorldPrefabsSO.asset";

        private const float FrameTime = 1f / 60f;
        private const float GroundHeight = 20f;

        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameObject _terrainObject;
        private TerrainDigSystem _digSystem;
        private GameObject _player;
        private PlayerDigger _digger;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;

            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();

            BuildFlatTerrain();

            GameObject prefab = Load<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing {PlayerPrefabPath}");
            _player = Object.Instantiate(prefab, new Vector3(0f, GroundHeight + 0.05f, 0f), Quaternion.identity);
            _digger = _player.GetComponent<PlayerDigger>();
            Assert.That(_digger, Is.Not.Null, "Player prefab has no PlayerDigger.");
        }

        public override void TearDown()
        {
            if (_player != null)
            {
                Object.DestroyImmediate(_player);
            }

            if (_terrainObject != null)
            {
                Object.DestroyImmediate(_terrainObject);
            }

            Time.captureDeltaTime = 0f;
            base.TearDown();
        }

        // ---------- acceptance ----------

        [UnityTest]
        public IEnumerator RightClick_DentsTheGroundOnce()
        {
            yield return LookDownAtTheGround();
            Assert.That(_digger.TryGetDigTarget(out RaycastHit target), Is.True, "Nothing diggable in reach.");

            yield return Click(_mouse.rightButton);
            yield return Frames(10);

            Assert.That(_digger.DigCount, Is.EqualTo(1));
            Assert.That(_digSystem.DepthAt(target.point), Is.EqualTo(_digSystem.Settings.Amount).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator HoldingRightClick_KeepsDigging()
        {
            yield return LookDownAtTheGround();

            Press(_mouse.rightButton);
            yield return Seconds(1f);
            Release(_mouse.rightButton);

            // One dig on press, then one every repeat interval.
            int expected = 1 + Mathf.FloorToInt(1f / _digger.RepeatInterval);
            Assert.That(_digger.DigCount, Is.InRange(expected - 1, expected + 1));
        }

        [UnityTest]
        public IEnumerator HoldingRightClick_DigsAPitDeepEnoughToHideIn()
        {
            yield return LookDownAtTheGround();

            Press(_mouse.rightButton);
            yield return Seconds(2.5f);
            Release(_mouse.rightButton);

            Assert.That(_digSystem.Field.MaxDepthInField(), Is.GreaterThanOrEqualTo(_digSystem.Settings.HideDepth));
        }

        [UnityTest]
        public IEnumerator DiggingLowersTheColliderNotJustTheVisuals()
        {
            yield return LookDownAtTheGround();
            _digger.TryGetDigTarget(out RaycastHit target);

            Press(_mouse.rightButton);
            yield return Seconds(1f);
            Release(_mouse.rightButton);
            yield return null;

            float depth = _digSystem.DepthAt(target.point);
            Assume.That(depth, Is.GreaterThan(0.5f));

            Vector3 above = new Vector3(target.point.x, GroundHeight + 5f, target.point.z);
            Assert.That(Physics.Raycast(above, Vector3.down, out RaycastHit ground, 20f), Is.True);
            Assert.That(ground.point.y, Is.EqualTo(GroundHeight - depth).Within(0.15f));
        }

        [UnityTest]
        public IEnumerator GroundColourFollowsDigDepth()
        {
            yield return LookDownAtTheGround();
            Assume.That(DominantLayerAt(Vector2.zero + new Vector2(0f, 10f)), Is.EqualTo(TerrainLayerId.Snow));

            Press(_mouse.rightButton);
            yield return Seconds(2.5f);
            Release(_mouse.rightButton);

            Vector2 deepest = DeepestPoint(out float maxDepth);
            Assume.That(maxDepth, Is.GreaterThan(1.2f));

            Assert.That(DominantLayerAt(deepest), Is.EqualTo(TerrainLayerId.DeepDirt), "Deep pit floor should be deep dirt.");
            Assert.That(DominantLayerAt(new Vector2(-20f, -20f)), Is.EqualTo(TerrainLayerId.Snow), "Untouched ground stays snow.");
            Assert.That(PathCrossesLayer(TerrainLayerId.Dirt), Is.True, "The pit wall should show dirt between snow and deep dirt.");
        }

        [UnityTest]
        public IEnumerator LookingAtTheSky_DigsNothing()
        {
            for (int i = 0; i < 5; i++)
            {
                Set(_mouse.delta, new Vector2(0f, 500f));
                yield return null;
            }

            yield return Click(_mouse.rightButton);
            yield return Frames(5);

            Assert.That(_digger.DigCount, Is.EqualTo(0));
            Assert.That(_digSystem.Field.MaxDepthInField(), Is.EqualTo(0f));
        }

        // ---------- setup helpers ----------

        private void BuildFlatTerrain()
        {
            var prefabs = Load<WorldPrefabsSO>(WorldPrefabsPath);
            Assert.That(prefabs, Is.Not.Null, $"Missing {WorldPrefabsPath}");

            const int resolution = 129;
            const float size = 64f;
            const float height = 40f;

            var data = new TerrainData { heightmapResolution = resolution };
            data.size = new Vector3(size, height, size);

            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights[z, x] = GroundHeight / height;
                }
            }

            data.SetHeights(0, 0, heights);
            data.terrainLayers = prefabs.TerrainLayers;
            data.alphamapResolution = 128;

            var alphas = new float[128, 128, data.alphamapLayers];
            for (int z = 0; z < 128; z++)
            {
                for (int x = 0; x < 128; x++)
                {
                    alphas[z, x, (int)TerrainLayerId.Snow] = 1f;
                }
            }

            data.SetAlphamaps(0, 0, alphas);

            _terrainObject = Terrain.CreateTerrainGameObject(data);
            _terrainObject.transform.position = new Vector3(-size * 0.5f, 0f, -size * 0.5f);

            _digSystem = _terrainObject.AddComponent<TerrainDigSystem>();
            var openPlain = new BiomeMap(new List<BiomeRegion>(), BiomeDefinition.CreateDefaults(), seed: 1);
            _digSystem.Initialize(_terrainObject.GetComponent<Terrain>(), openPlain, new DigSettings(), new SoilSettings());
        }

        /// <summary>Pitches the view about 45 degrees down so the ground ahead is within reach.</summary>
        private IEnumerator LookDownAtTheGround()
        {
            yield return Frames(5);
            Set(_mouse.delta, new Vector2(0f, -380f));
            yield return null;
            yield return Frames(2);
        }

        private IEnumerator Click(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private static IEnumerator Seconds(float seconds)
        {
            return Frames(Mathf.CeilToInt(seconds / FrameTime));
        }

        private Vector2 DeepestPoint(out float maxDepth)
        {
            DigField field = _digSystem.Field;
            maxDepth = 0f;
            Vector2 best = Vector2.zero;

            for (int z = 0; z < field.Height; z++)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    if (field.DepthAt(x, z) > maxDepth)
                    {
                        maxDepth = field.DepthAt(x, z);
                        best = field.CellToWorld(x, z);
                    }
                }
            }

            return best;
        }

        /// <summary>Walks outward from the deepest point and reports whether any texel is mostly dirt.</summary>
        private bool PathCrossesLayer(TerrainLayerId layer)
        {
            Vector2 start = DeepestPoint(out _);
            foreach (Vector2 direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                for (float d = 0f; d < 5f; d += 0.25f)
                {
                    if (DominantLayerAt(start + (direction * d)) == layer)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private TerrainLayerId DominantLayerAt(Vector2 xz)
        {
            TerrainData data = _terrainObject.GetComponent<Terrain>().terrainData;
            int resolution = data.alphamapResolution;
            float size = data.size.x;
            Vector3 origin = _terrainObject.transform.position;

            int ax = Mathf.Clamp(Mathf.FloorToInt((xz.x - origin.x) / size * resolution), 0, resolution - 1);
            int az = Mathf.Clamp(Mathf.FloorToInt((xz.y - origin.z) / size * resolution), 0, resolution - 1);
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

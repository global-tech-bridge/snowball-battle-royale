using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Tuning for world generation. Defaults match the Phase 3 spec (700 m map, 1100 trees,
    /// 250 rocks, 4 settlements + 8 scattered cabins, 20 campfires).
    /// </summary>
    [CreateAssetMenu(fileName = "WorldGenSettingsSO", menuName = "Snowball/World/World Gen Settings", order = 0)]
    public sealed class WorldGenSettingsSO : ScriptableObject
    {
        [Tooltip("Same seed, same map. Phase 12 shares the seed instead of the terrain.")]
        [SerializeField]
        private int _seed = WorldGenConfig.DefaultSeed;

        [Tooltip("Biome boundary definitions. Falls back to built-in defaults when empty.")]
        [SerializeField]
        private BiomeSettingsSO _biomes;

        [SerializeField]
        private WorldLayoutSettings _layout = new WorldLayoutSettings();

        [SerializeField]
        private TerrainShapeSettings _shape = new TerrainShapeSettings();

        [SerializeField]
        private ScatterSettings _scatter = new ScatterSettings();

        [SerializeField]
        private SoilSettings _soil = new SoilSettings();

        [SerializeField]
        private DigSettings _dig = new DigSettings();

        [Header("Terrain resolution")]
        [Tooltip("Heightmap samples per side. Must be 2^n + 1. 1025 gives ~0.68 m cells on a 700 m map.")]
        [SerializeField]
        private int _heightmapResolution = 1025;

        [Tooltip("Splat map texels per side. Matches the heightmap so dug patches recolour cleanly.")]
        [SerializeField]
        private int _alphamapResolution = 1024;

        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        public int HeightmapResolution => _heightmapResolution;

        public int AlphamapResolution => _alphamapResolution;

        public WorldGenConfig ToConfig()
        {
            return new WorldGenConfig
            {
                Seed = _seed,
                Layout = _layout,
                Biomes = _biomes != null && _biomes.Definitions.Count > 0
                    ? new List<BiomeDefinition>(_biomes.Definitions)
                    : BiomeDefinition.CreateDefaults(),
                Shape = _shape,
                Scatter = _scatter,
                Soil = _soil,
                Dig = _dig,
            };
        }

        private void OnValidate()
        {
            // Unity only accepts heightmap sizes of 2^n + 1.
            int n = Mathf.Max(5, Mathf.RoundToInt(Mathf.Log(Mathf.Max(33, _heightmapResolution) - 1, 2)));
            _heightmapResolution = Mathf.Clamp((1 << n) + 1, 33, 4097);
            _alphamapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(Mathf.Max(16, _alphamapResolution)), 16, 4096);
        }
    }
}

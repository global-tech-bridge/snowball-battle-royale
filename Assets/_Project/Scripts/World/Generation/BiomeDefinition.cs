using System;
using System.Collections.Generic;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// How one biome is laid out and how its boundary behaves. A list of these is the "biome boundary
    /// definition" edited through <c>BiomeSettingsSO</c>.
    /// </summary>
    [Serializable]
    public sealed class BiomeDefinition
    {
        public BiomeType Type;

        /// <summary>Number of regions to place. Plain is the background and uses 0.</summary>
        public int RegionCount;

        /// <summary>Region radius range (m). For the valley this is the half-width of the channel.</summary>
        public float MinRadius;

        public float MaxRadius;

        /// <summary>Fraction of the radius over which membership fades from 1 to 0 at the edge.</summary>
        public float BoundaryBlend = 0.25f;

        /// <summary>Edge wobble as a fraction of the radius, so regions are not perfect circles.</summary>
        public float BoundaryNoise = 0.15f;

        /// <summary>Where regions overlap, the higher priority biome wins.</summary>
        public int Priority;

        /// <summary>Relative tree density. Only ratios between biomes matter.</summary>
        public float TreeDensity;

        /// <summary>Relative rock density. Only ratios between biomes matter.</summary>
        public float RockDensity;

        public float PineWeight;

        public float BirchWeight;

        public float DeadTreeWeight;

        /// <summary>Share of rocks placed as large boulders (0-1).</summary>
        public float BoulderChance;

        public static List<BiomeDefinition> CreateDefaults()
        {
            return new List<BiomeDefinition>
            {
                new BiomeDefinition
                {
                    Type = BiomeType.Plain, RegionCount = 0, Priority = 0,
                    TreeDensity = 0.15f, RockDensity = 0.3f,
                    PineWeight = 0.3f, BirchWeight = 0.6f, DeadTreeWeight = 0.1f, BoulderChance = 0.2f,
                },
                new BiomeDefinition
                {
                    Type = BiomeType.Forest, RegionCount = 5, MinRadius = 45f, MaxRadius = 65f, Priority = 2,
                    TreeDensity = 4f, RockDensity = 0.4f,
                    PineWeight = 0.7f, BirchWeight = 0.25f, DeadTreeWeight = 0.05f, BoulderChance = 0.2f,
                },
                new BiomeDefinition
                {
                    Type = BiomeType.Mountain, RegionCount = 4, MinRadius = 60f, MaxRadius = 85f, Priority = 5,
                    BoundaryNoise = 0.1f,
                    TreeDensity = 0.6f, RockDensity = 4f,
                    PineWeight = 0.5f, BirchWeight = 0f, DeadTreeWeight = 0.5f, BoulderChance = 0.5f,
                },
                new BiomeDefinition
                {
                    Type = BiomeType.Valley, RegionCount = 1, MinRadius = 26f, MaxRadius = 32f, Priority = 4,
                    BoundaryNoise = 0.1f,
                    TreeDensity = 0.5f, RockDensity = 1.5f,
                    PineWeight = 0.3f, BirchWeight = 0.5f, DeadTreeWeight = 0.2f, BoulderChance = 0.3f,
                },
                new BiomeDefinition
                {
                    Type = BiomeType.DeepSnow, RegionCount = 3, MinRadius = 40f, MaxRadius = 60f, Priority = 3,
                    TreeDensity = 0.35f, RockDensity = 0.2f,
                    PineWeight = 0.2f, BirchWeight = 0.1f, DeadTreeWeight = 0.7f, BoulderChance = 0.1f,
                },
                new BiomeDefinition
                {
                    Type = BiomeType.FrozenLake, RegionCount = 2, MinRadius = 30f, MaxRadius = 45f, Priority = 6,
                    BoundaryNoise = 0.08f,
                    TreeDensity = 0f, RockDensity = 0f,
                },
            };
        }
    }
}

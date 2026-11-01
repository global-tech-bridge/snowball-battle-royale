using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>Map size and man-made site layout.</summary>
    [Serializable]
    public sealed class WorldLayoutSettings
    {
        [Tooltip("Map edge length (m). The map is centred on the world origin.")]
        public float Size = 700f;

        [Tooltip("Biome region centres keep at least this far from the map edge (m).")]
        public float RegionEdgeMargin = 40f;

        [Tooltip("Cabins, wells and campfires keep at least this far from the map edge (m).")]
        public float SiteEdgeMargin = 30f;

        [Tooltip("Regions keep centre distance >= (r1 + r2) * this factor. < 1 lets edges blend.")]
        public float RegionSeparation = 0.85f;

        [Header("Settlements")]
        public int SettlementCount = 4;

        public int CabinsPerSettlement = 3;

        [Tooltip("Distance from settlement centre to each cabin (m).")]
        public float SettlementCabinRing = 14f;

        [Tooltip("Ground inside this radius is levelled to the settlement height (m).")]
        public float SettlementFlattenRadius = 26f;

        [Tooltip("Radius of the fence ring around a settlement (m).")]
        public float SettlementFenceRadius = 24f;

        public float FenceSegmentLength = 3f;

        [Tooltip("Openings in the fence ring, spread evenly.")]
        public int FenceGapCount = 2;

        [Tooltip("Fence segments removed per opening.")]
        public int FenceGapSegments = 2;

        public float MinSettlementSpacing = 140f;

        [Header("Scattered cabins")]
        public int ScatteredCabinCount = 8;

        public float MinScatteredCabinSpacing = 50f;

        [Header("Footprints (m)")]
        public float CabinFootprintRadius = 4.5f;

        public float WellFootprintRadius = 1.5f;

        public float CampfireFootprintRadius = 1.5f;

        [Header("Campfires")]
        public int CampfireCount = 20;

        public float MinCampfireSpacing = 30f;

        public float MinCampfireCabinDistance = 10f;

        [Tooltip("Burn times are spread evenly across this range (s). Phase 8 replaces this schedule.")]
        public float CampfireMinLifetime = 90f;

        public float CampfireMaxLifetime = 360f;

        [Header("Spawn")]
        [Tooltip("Spawn keeps this much clear ground beyond any site footprint (m).")]
        public float SpawnClearance = 12f;

        [Header("Placement")]
        public int MaxPlacementAttempts = 6000;
    }

    /// <summary>Terrain elevation shaping. All values in metres.</summary>
    [Serializable]
    public sealed class TerrainShapeSettings
    {
        [Tooltip("Vertical extent of the terrain volume.")]
        public float TerrainHeight = 120f;

        [Tooltip("Base height of the open snowfield. Leaves room below for valleys, lakes and digging.")]
        public float PlainElevation = 30f;

        public float PlainNoiseAmplitude = 3f;

        public float PlainNoiseScale = 180f;

        public float DetailNoiseAmplitude = 0.6f;

        public float DetailNoiseScale = 25f;

        public float ForestRise = 2f;

        public float MountainPeak = 60f;

        [Tooltip("Mountains vary their peak by +/- this fraction.")]
        public float MountainPeakVariation = 0.15f;

        public float MountainRidgeScale = 60f;

        public float ValleyDepth = 12f;

        public float DeepSnowRise = 2.5f;

        [Tooltip("Lake ice sits this far below the plain.")]
        public float LakeDepth = 4f;

        [Tooltip("Width of the blend from a levelled site to the natural ground (m).")]
        public float SiteFlattenBlend = 6f;
    }

    /// <summary>Tree and rock scattering.</summary>
    [Serializable]
    public sealed class ScatterSettings
    {
        public int TreeCount = 1100;

        public int RockCount = 250;

        public float MinTreeSpacing = 3f;

        public float MinRockSpacing = 3f;

        [Tooltip("Minimum distance between any tree and any rock (m).")]
        public float MinTreeRockSpacing = 2f;

        [Tooltip("Props keep this far beyond any site footprint (m).")]
        public float SiteClearance = 2f;

        [Tooltip("Props keep this far from the player spawn point (m).")]
        public float SpawnClearance = 6f;

        public int MaxAttemptsPerProp = 400;
    }

    /// <summary>Everything that determines a generated world. Same config, same world.</summary>
    [Serializable]
    public sealed class WorldGenConfig
    {
        public const int DefaultSeed = 12345;

        public int Seed = DefaultSeed;

        public WorldLayoutSettings Layout = new WorldLayoutSettings();

        public List<BiomeDefinition> Biomes = BiomeDefinition.CreateDefaults();

        public TerrainShapeSettings Shape = new TerrainShapeSettings();

        public ScatterSettings Scatter = new ScatterSettings();

        public SoilSettings Soil = new SoilSettings();

        public DigSettings Dig = new DigSettings();

        public static WorldGenConfig CreateDefault(int seed = DefaultSeed)
        {
            return new WorldGenConfig { Seed = seed };
        }

        public BiomeDefinition DefinitionFor(BiomeType type)
        {
            foreach (BiomeDefinition definition in Biomes)
            {
                if (definition.Type == type)
                {
                    return definition;
                }
            }

            throw new ArgumentException($"No biome definition for {type}.", nameof(type));
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Analytic terrain elevation: <see cref="HeightAt"/> gives the height in metres for any XZ point.
    ///
    /// Built in layers — rolling plain, then per-biome shaping (mountains rise, the valley cuts down,
    /// forests and deep snow undulate), then frozen lakes are levelled to ice, and finally every
    /// settlement, cabin and campfire site is levelled so buildings sit flat.
    /// </summary>
    public sealed class TerrainHeightField
    {
        private const float ForestNoiseScale = 45f;
        private const float DeepSnowNoiseScale = 30f;

        private readonly WorldLayout _layout;
        private readonly TerrainShapeSettings _shape;
        private readonly SeededNoise _plainNoise;
        private readonly SeededNoise _detailNoise;
        private readonly SeededNoise _ridgeNoise;
        private readonly SeededNoise _hummockNoise;
        private readonly List<FlattenZone> _zones = new List<FlattenZone>();

        public TerrainHeightField(WorldLayout layout, WorldLayoutSettings layoutSettings, TerrainShapeSettings shape, int seed)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));
            if (layoutSettings == null)
            {
                throw new ArgumentNullException(nameof(layoutSettings));
            }

            _plainNoise = new SeededNoise(DeterministicRandom.Derive(seed, 0x4E01));
            _detailNoise = new SeededNoise(DeterministicRandom.Derive(seed, 0x4E02));
            _ridgeNoise = new SeededNoise(DeterministicRandom.Derive(seed, 0x4E03));
            _hummockNoise = new SeededNoise(DeterministicRandom.Derive(seed, 0x4E04));

            BuildFlattenZones(layoutSettings);
        }

        public float TerrainHeight => _shape.TerrainHeight;

        /// <summary>Ground height in metres at world XZ <paramref name="p"/>, within [0, TerrainHeight].</summary>
        public float HeightAt(Vector2 p)
        {
            return ApplyFlattening(p, NaturalHeightAt(p), _zones.Count);
        }

        /// <summary>
        /// Samples the field on a <paramref name="resolution"/>² grid spanning the map, normalized to
        /// 0-1 by <see cref="TerrainHeight"/> and indexed [z, x] — the layout
        /// <c>TerrainData.SetHeights</c> expects.
        /// </summary>
        public float[,] Sample(int resolution)
        {
            if (resolution < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), "Resolution must be at least 2.");
            }

            var heights = new float[resolution, resolution];
            float step = _layout.Size / (resolution - 1);
            float origin = -_layout.HalfSize;
            float inverseHeight = 1f / _shape.TerrainHeight;

            for (int z = 0; z < resolution; z++)
            {
                float worldZ = origin + (z * step);
                for (int x = 0; x < resolution; x++)
                {
                    float worldX = origin + (x * step);
                    heights[z, x] = Mathf.Clamp01(HeightAt(new Vector2(worldX, worldZ)) * inverseHeight);
                }
            }

            return heights;
        }

        private float NaturalHeightAt(Vector2 p)
        {
            float h = _shape.PlainElevation
                      + (_plainNoise.Fbm(p.x / _shape.PlainNoiseScale, p.y / _shape.PlainNoiseScale, 3) * _shape.PlainNoiseAmplitude)
                      + (_detailNoise.Fbm(p.x / _shape.DetailNoiseScale, p.y / _shape.DetailNoiseScale, 2) * _shape.DetailNoiseAmplitude);

            IReadOnlyList<BiomeRegion> regions = _layout.Regions;

            for (int i = 0; i < regions.Count; i++)
            {
                BiomeRegion region = regions[i];
                float t = region.DistanceTo(p) / region.Radius;
                if (t >= 1f)
                {
                    continue;
                }

                // Raised-cosine profile: 1 at the core, 0 at the rim, with zero slope at both ends so
                // shaped regions meet the plain without a crease.
                float profile = 0.5f * (1f + Mathf.Cos(Mathf.PI * t));

                switch (region.Type)
                {
                    case BiomeType.Mountain:
                        float ridge = 0.8f + (0.2f * _ridgeNoise.Ridged(
                            p.x / _shape.MountainRidgeScale, p.y / _shape.MountainRidgeScale, 4));
                        h += _shape.MountainPeak * region.Strength * profile * ridge;
                        break;

                    case BiomeType.Valley:
                        h -= _shape.ValleyDepth * profile;
                        break;

                    case BiomeType.Forest:
                        float rolling = 0.5f + (0.5f * _hummockNoise.Fbm(p.x / ForestNoiseScale, p.y / ForestNoiseScale, 2));
                        h += _shape.ForestRise * profile * rolling;
                        break;

                    case BiomeType.DeepSnow:
                        float drifts = 0.6f + (0.4f * _hummockNoise.Fbm(p.x / DeepSnowNoiseScale, p.y / DeepSnowNoiseScale, 3));
                        h += _shape.DeepSnowRise * profile * drifts;
                        break;
                }
            }

            // Lakes last: ice is flat, whatever the ground around it does.
            float lakeLevel = _shape.PlainElevation - _shape.LakeDepth;
            for (int i = 0; i < regions.Count; i++)
            {
                BiomeRegion region = regions[i];
                if (region.Type != BiomeType.FrozenLake)
                {
                    continue;
                }

                float t = region.DistanceTo(p) / region.Radius;
                if (t >= 1f)
                {
                    continue;
                }

                float ice = 1f - BiomeMap.SmoothStep(0.75f, 1f, t);
                h = Mathf.Lerp(h, lakeLevel, ice);
            }

            return Mathf.Clamp(h, 0f, _shape.TerrainHeight);
        }

        /// <summary>
        /// Applies the first <paramref name="zoneCount"/> flatten zones. Each zone levels to the height
        /// its centre has after the zones before it, so a campfire inside a settlement inherits the
        /// settlement's level instead of punching its own step into it.
        /// </summary>
        private float ApplyFlattening(Vector2 p, float height, int zoneCount)
        {
            for (int i = 0; i < zoneCount; i++)
            {
                FlattenZone zone = _zones[i];
                float distance = Vector2.Distance(p, zone.Center);
                float outer = zone.Radius + zone.Blend;
                if (distance >= outer)
                {
                    continue;
                }

                float mask = 1f - BiomeMap.SmoothStep(zone.Radius, outer, distance);
                height = Mathf.Lerp(height, zone.Level, mask);
            }

            return height;
        }

        private void BuildFlattenZones(WorldLayoutSettings settings)
        {
            float blend = _shape.SiteFlattenBlend;

            foreach (Vector2 center in _layout.SettlementCenters)
            {
                AddZone(center, settings.SettlementFlattenRadius, blend);
            }

            foreach (WorldSite cabin in _layout.Cabins)
            {
                if (cabin.SettlementIndex < 0)
                {
                    AddZone(cabin.Position, cabin.FootprintRadius + 1f, blend);
                }
            }

            foreach (WorldSite fire in _layout.Campfires)
            {
                AddZone(fire.Position, fire.FootprintRadius + 0.5f, blend * 0.5f);
            }
        }

        private void AddZone(Vector2 center, float radius, float blend)
        {
            float level = ApplyFlattening(center, NaturalHeightAt(center), _zones.Count);
            _zones.Add(new FlattenZone(center, radius, blend, level));
        }

        private readonly struct FlattenZone
        {
            public FlattenZone(Vector2 center, float radius, float blend, float level)
            {
                Center = center;
                Radius = radius;
                Blend = blend;
                Level = level;
            }

            public Vector2 Center { get; }

            public float Radius { get; }

            public float Blend { get; }

            public float Level { get; }
        }
    }
}

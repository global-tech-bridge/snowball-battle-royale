using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Places biome regions, settlements, scattered cabins, campfires and the spawn point.
    ///
    /// Everything is rejection-sampled from a <see cref="DeterministicRandom"/>, so a seed always yields
    /// the same layout. When a constraint cannot be met, spacing is relaxed gradually down to a floor;
    /// if even that fails the generator throws rather than silently returning a short layout.
    /// </summary>
    public static class WorldLayoutGenerator
    {
        private const int RegionSalt = 1;
        private const int SiteSalt = 2;

        // Spacing is relaxed by this factor each time a batch of attempts fails.
        private const float RelaxStep = 0.97f;
        private const int AttemptsPerRelax = 300;

        // Floors keep relaxed spacing above what the rest of the design relies on.
        private const float RegionRelaxFloor = 0.3f;
        private const float SettlementRelaxFloor = 0.86f;
        private const float ScatteredCabinRelaxFloor = 0.5f;
        private const float CampfireRelaxFloor = 0.84f;

        private static readonly Vector2[] QuadrantSigns =
        {
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f),
        };

        public static WorldLayout Generate(WorldGenConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var regionRandom = new DeterministicRandom(DeterministicRandom.Derive(config.Seed, RegionSalt));
            List<BiomeRegion> regions = PlaceRegions(config, regionRandom);

            var biomes = new BiomeMap(regions, config.Biomes, config.Seed);
            var siteRandom = new DeterministicRandom(DeterministicRandom.Derive(config.Seed, SiteSalt));
            var builder = new SiteBuilder(config.Layout, biomes, siteRandom);

            builder.PlaceSettlements();
            builder.PlaceScatteredCabins();
            builder.PlaceScatteredCampfires();
            builder.AssignCampfireLifetimes();
            Vector2 spawn = builder.FindSpawnPoint();

            return new WorldLayout(
                config.Layout.Size,
                regions,
                builder.SettlementCenters,
                builder.Cabins,
                builder.Wells,
                builder.Fences,
                builder.Campfires,
                spawn);
        }

        // ------------------------------------------------------------------ regions

        private static List<BiomeRegion> PlaceRegions(WorldGenConfig config, DeterministicRandom random)
        {
            var regions = new List<BiomeRegion>();

            PlaceMountains(config, random, regions);

            // Larger and higher-priority features first, while there is still room for them.
            foreach (BiomeType type in new[] { BiomeType.FrozenLake, BiomeType.Valley, BiomeType.DeepSnow, BiomeType.Forest })
            {
                BiomeDefinition definition = config.DefinitionFor(type);
                for (int i = 0; i < definition.RegionCount; i++)
                {
                    regions.Add(type == BiomeType.Valley
                        ? PlaceValley(config, definition, random, regions)
                        : PlaceDisc(config, definition, random, regions));
                }
            }

            return regions;
        }

        /// <summary>One mountain per quadrant, pushed toward the map edge to frame an open centre.</summary>
        private static void PlaceMountains(WorldGenConfig config, DeterministicRandom random, List<BiomeRegion> regions)
        {
            BiomeDefinition definition = config.DefinitionFor(BiomeType.Mountain);
            WorldLayoutSettings layout = config.Layout;
            float half = layout.Size * 0.5f;

            for (int i = 0; i < definition.RegionCount; i++)
            {
                float radius = random.Range(definition.MinRadius, definition.MaxRadius);
                float strength = 1f + random.Range(-config.Shape.MountainPeakVariation, config.Shape.MountainPeakVariation);

                if (i >= QuadrantSigns.Length)
                {
                    regions.Add(PlaceDisc(config, definition, random, regions, strength));
                    continue;
                }

                Vector2 sign = QuadrantSigns[i];
                float limit = half - layout.RegionEdgeMargin - (radius * 0.3f);
                var center = new Vector2(
                    sign.x * Mathf.Min(limit, random.Range(0.2f, 0.3f) * layout.Size),
                    sign.y * Mathf.Min(limit, random.Range(0.2f, 0.3f) * layout.Size));

                regions.Add(new BiomeRegion(BiomeType.Mountain, center, radius, strength));
            }
        }

        private static BiomeRegion PlaceDisc(
            WorldGenConfig config,
            BiomeDefinition definition,
            DeterministicRandom random,
            List<BiomeRegion> existing,
            float strength = 1f)
        {
            WorldLayoutSettings layout = config.Layout;
            float half = layout.Size * 0.5f;
            float separation = layout.RegionSeparation;

            for (int attempt = 1; attempt <= layout.MaxPlacementAttempts; attempt++)
            {
                float radius = random.Range(definition.MinRadius, definition.MaxRadius);
                float extent = half - layout.RegionEdgeMargin - radius;
                if (extent <= 0f)
                {
                    break;
                }

                Vector2 center = random.InsideSquare(extent);
                var candidate = new BiomeRegion(definition.Type, center, radius, strength);

                if (IsSeparated(candidate, existing, separation))
                {
                    return candidate;
                }

                separation = Relax(separation, attempt, layout.RegionSeparation * RegionRelaxFloor);
            }

            throw new InvalidOperationException($"Could not place a {definition.Type} region; the map is too crowded.");
        }

        /// <summary>The valley is a long capsule-shaped channel cutting across the map.</summary>
        private static BiomeRegion PlaceValley(
            WorldGenConfig config,
            BiomeDefinition definition,
            DeterministicRandom random,
            List<BiomeRegion> existing)
        {
            WorldLayoutSettings layout = config.Layout;
            float half = layout.Size * 0.5f;
            float limit = half - layout.RegionEdgeMargin;
            float separation = layout.RegionSeparation;

            for (int attempt = 1; attempt <= layout.MaxPlacementAttempts; attempt++)
            {
                float radius = random.Range(definition.MinRadius, definition.MaxRadius);
                float halfLength = random.Range(110f, 150f);
                float angle = random.Range(0f, Mathf.PI);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 center = random.InsideSquare(limit - radius);

                Vector2 start = center - (direction * halfLength);
                Vector2 end = center + (direction * halfLength);
                if (!Inside(start, limit - radius) || !Inside(end, limit - radius))
                {
                    continue;
                }

                var candidate = new BiomeRegion(BiomeType.Valley, start, end, radius);
                if (IsSeparated(candidate, existing, separation))
                {
                    return candidate;
                }

                separation = Relax(separation, attempt, layout.RegionSeparation * RegionRelaxFloor);
            }

            throw new InvalidOperationException("Could not place the valley; the map is too crowded.");
        }

        private static bool IsSeparated(BiomeRegion candidate, List<BiomeRegion> existing, float separation)
        {
            foreach (BiomeRegion other in existing)
            {
                // Measure core-to-core so capsules (the valley) are handled on either side.
                float distance = Mathf.Min(candidate.DistanceTo(other.Center), other.DistanceTo(candidate.Center));
                if (other.IsCapsule || candidate.IsCapsule)
                {
                    distance = Mathf.Min(
                        distance,
                        Mathf.Min(candidate.DistanceTo(other.Start), candidate.DistanceTo(other.End)));
                }

                if (distance < (candidate.Radius + other.Radius) * separation)
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------------ shared helpers

        private static float Relax(float value, int attempt, float floor)
        {
            return attempt % AttemptsPerRelax == 0 ? Mathf.Max(floor, value * RelaxStep) : value;
        }

        private static bool Inside(Vector2 p, float halfExtent)
        {
            return Mathf.Abs(p.x) <= halfExtent && Mathf.Abs(p.y) <= halfExtent;
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>Yaw that turns an object's forward (+Z) to face along <paramref name="direction"/>.</summary>
        private static float YawFacing(Vector2 direction)
        {
            return Normalize360(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
        }

        private static float Normalize360(float degrees)
        {
            float wrapped = degrees % 360f;
            return wrapped < 0f ? wrapped + 360f : wrapped;
        }

        // ------------------------------------------------------------------ sites

        private sealed class SiteBuilder
        {
            private const float SettlementCampfireOffset = 7f;

            private readonly WorldLayoutSettings _settings;
            private readonly BiomeMap _biomes;
            private readonly DeterministicRandom _random;
            private readonly float _half;

            public SiteBuilder(WorldLayoutSettings settings, BiomeMap biomes, DeterministicRandom random)
            {
                _settings = settings;
                _biomes = biomes;
                _random = random;
                _half = settings.Size * 0.5f;
            }

            public List<Vector2> SettlementCenters { get; } = new List<Vector2>();

            public List<WorldSite> Cabins { get; } = new List<WorldSite>();

            public List<WorldSite> Wells { get; } = new List<WorldSite>();

            public List<WorldSite> Fences { get; } = new List<WorldSite>();

            public List<WorldSite> Campfires { get; } = new List<WorldSite>();

            public void PlaceSettlements()
            {
                float extent = _half - _settings.SiteEdgeMargin - _settings.SettlementFenceRadius;
                float spacing = _settings.MinSettlementSpacing;

                for (int i = 0; i < _settings.SettlementCount; i++)
                {
                    Vector2 center = Sample(
                        extent,
                        p => IsAreaBuildable(p, _settings.SettlementFenceRadius + 2f)
                             && FarFrom(SettlementCenters, p, spacing),
                        attempt => spacing = Relax(spacing, attempt, _settings.MinSettlementSpacing * SettlementRelaxFloor),
                        "settlement");

                    SettlementCenters.Add(center);
                    BuildSettlement(i, center);
                }
            }

            private void BuildSettlement(int index, Vector2 center)
            {
                Wells.Add(new WorldSite(SiteKind.Well, center, _random.Range(0f, 360f), _settings.WellFootprintRadius, index));

                int cabinCount = Mathf.Max(1, _settings.CabinsPerSettlement);
                float baseAngle = _random.Range(0f, 360f);
                float step = 360f / cabinCount;

                for (int k = 0; k < cabinCount; k++)
                {
                    Vector2 outward = Direction(baseAngle + (k * step));
                    Vector2 position = center + (outward * _settings.SettlementCabinRing);

                    // Doors face the well.
                    Cabins.Add(new WorldSite(
                        SiteKind.Cabin,
                        position,
                        YawFacing(-outward),
                        _settings.CabinFootprintRadius,
                        index));
                }

                // One campfire per settlement, in the gap between the first two cabins.
                Vector2 firePosition = center + (Direction(baseAngle + (step * 0.5f)) * SettlementCampfireOffset);
                Campfires.Add(new WorldSite(SiteKind.Campfire, firePosition, 0f, _settings.CampfireFootprintRadius, index));

                BuildFenceRing(index, center, baseAngle + (step * 0.5f));
            }

            private void BuildFenceRing(int index, Vector2 center, float gapAngle)
            {
                float radius = _settings.SettlementFenceRadius;
                int segments = Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * radius / _settings.FenceSegmentLength));
                float step = 360f / segments;

                var skipped = new HashSet<int>();
                int gapCount = Mathf.Max(0, _settings.FenceGapCount);
                for (int g = 0; g < gapCount; g++)
                {
                    float angle = gapAngle + (g * 360f / gapCount);
                    int middle = Mathf.RoundToInt(Normalize360(angle) / step);
                    for (int s = 0; s < _settings.FenceGapSegments; s++)
                    {
                        int offset = s - (_settings.FenceGapSegments / 2);
                        skipped.Add(((middle + offset) % segments + segments) % segments);
                    }
                }

                for (int k = 0; k < segments; k++)
                {
                    if (skipped.Contains(k))
                    {
                        continue;
                    }

                    float angle = k * step;
                    Vector2 position = center + (Direction(angle) * radius);

                    // Fence rails run along local +X; turn +X onto the ring's tangent.
                    float yaw = Normalize360(-(angle + 90f));
                    Fences.Add(new WorldSite(SiteKind.Fence, position, yaw, _settings.FenceSegmentLength * 0.5f, index));
                }
            }

            public void PlaceScatteredCabins()
            {
                float extent = _half - _settings.SiteEdgeMargin - _settings.CabinFootprintRadius;
                float spacing = _settings.MinScatteredCabinSpacing;
                float settlementClearance = _settings.SettlementFenceRadius + 20f;

                for (int i = 0; i < _settings.ScatteredCabinCount; i++)
                {
                    Vector2 position = Sample(
                        extent,
                        p => IsAreaBuildable(p, _settings.CabinFootprintRadius + 2f)
                             && FarFrom(SettlementCenters, p, Mathf.Max(spacing, settlementClearance))
                             && FarFromSites(Cabins, p, spacing),
                        attempt => spacing = Relax(spacing, attempt, _settings.MinScatteredCabinSpacing * ScatteredCabinRelaxFloor),
                        "scattered cabin");

                    Cabins.Add(new WorldSite(SiteKind.Cabin, position, _random.Range(0f, 360f), _settings.CabinFootprintRadius));
                }
            }

            public void PlaceScatteredCampfires()
            {
                float extent = _half - _settings.SiteEdgeMargin;
                float spacing = _settings.MinCampfireSpacing;
                float settlementClearance = _settings.SettlementFenceRadius + 3f;

                while (Campfires.Count < _settings.CampfireCount)
                {
                    Vector2 position = Sample(
                        extent,
                        p => IsAreaBuildable(p, _settings.CampfireFootprintRadius + 1f)
                             && FarFromSites(Campfires, p, spacing)
                             && FarFromSites(Cabins, p, _settings.MinCampfireCabinDistance)
                             && FarFrom(SettlementCenters, p, settlementClearance),
                        attempt => spacing = Relax(spacing, attempt, _settings.MinCampfireSpacing * CampfireRelaxFloor),
                        "campfire");

                    Campfires.Add(new WorldSite(SiteKind.Campfire, position, 0f, _settings.CampfireFootprintRadius));
                }
            }

            /// <summary>
            /// Spreads burn times evenly over the configured range and deals them out at random, so
            /// fires go out one by one across the match instead of all at once.
            /// </summary>
            public void AssignCampfireLifetimes()
            {
                int count = Campfires.Count;
                var lifetimes = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = count > 1 ? i / (float)(count - 1) : 1f;
                    lifetimes[i] = Mathf.Lerp(_settings.CampfireMinLifetime, _settings.CampfireMaxLifetime, t);
                }

                _random.Shuffle(lifetimes);

                for (int i = 0; i < count; i++)
                {
                    WorldSite fire = Campfires[i];
                    Campfires[i] = new WorldSite(
                        fire.Kind, fire.Position, fire.RotationDegrees, fire.FootprintRadius, fire.SettlementIndex, lifetimes[i]);
                }
            }

            /// <summary>
            /// Nearest clear ground to the map centre, spiralling outward. Open plain is preferred so
            /// the player does not start boxed in by forest; any buildable ground is the fallback.
            /// </summary>
            public Vector2 FindSpawnPoint()
            {
                if (TrySpiral(requirePlain: true, out Vector2 spawn) || TrySpiral(requirePlain: false, out spawn))
                {
                    return spawn;
                }

                throw new InvalidOperationException("No valid spawn point on the map.");
            }

            private bool TrySpiral(bool requirePlain, out Vector2 spawn)
            {
                const float ringStep = 4f;
                float limit = _half - _settings.SiteEdgeMargin;

                for (float radius = 0f; radius < limit; radius += ringStep)
                {
                    int samples = Mathf.Max(1, Mathf.CeilToInt(2f * Mathf.PI * radius / ringStep));
                    for (int i = 0; i < samples; i++)
                    {
                        Vector2 p = Direction(i * 360f / samples) * radius;
                        if (IsGoodSpawn(p, requirePlain))
                        {
                            spawn = p;
                            return true;
                        }
                    }
                }

                spawn = default;
                return false;
            }

            private bool IsGoodSpawn(Vector2 p, bool requirePlain)
            {
                BiomeType biome = _biomes.Classify(p);
                if (requirePlain ? biome != BiomeType.Plain : !biome.IsBuildable())
                {
                    return false;
                }

                if (!FarFrom(SettlementCenters, p, _settings.SettlementFenceRadius + 4f))
                {
                    return false;
                }

                foreach (WorldSite site in AllPointSites())
                {
                    if (Vector2.Distance(site.Position, p) < site.FootprintRadius + _settings.SpawnClearance)
                    {
                        return false;
                    }
                }

                return true;
            }

            private IEnumerable<WorldSite> AllPointSites()
            {
                foreach (WorldSite site in Cabins)
                {
                    yield return site;
                }

                foreach (WorldSite site in Wells)
                {
                    yield return site;
                }

                foreach (WorldSite site in Campfires)
                {
                    yield return site;
                }
            }

            private Vector2 Sample(float extent, Func<Vector2, bool> accept, Action<int> onReject, string what)
            {
                for (int attempt = 1; attempt <= _settings.MaxPlacementAttempts; attempt++)
                {
                    Vector2 p = _random.InsideSquare(extent);
                    if (accept(p))
                    {
                        return p;
                    }

                    onReject(attempt);
                }

                throw new InvalidOperationException($"Could not place a {what}; constraints are too tight for this map.");
            }

            /// <summary>The point and a ring around it all lie on buildable ground.</summary>
            private bool IsAreaBuildable(Vector2 center, float radius)
            {
                if (!_biomes.Classify(center).IsBuildable())
                {
                    return false;
                }

                const int ringSamples = 12;
                for (int i = 0; i < ringSamples; i++)
                {
                    if (!_biomes.Classify(center + (Direction(i * 360f / ringSamples) * radius)).IsBuildable())
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool FarFrom(List<Vector2> points, Vector2 p, float distance)
            {
                foreach (Vector2 other in points)
                {
                    if (Vector2.Distance(other, p) < distance)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool FarFromSites(List<WorldSite> sites, Vector2 p, float distance)
            {
                foreach (WorldSite site in sites)
                {
                    if (Vector2.Distance(site.Position, p) < distance)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

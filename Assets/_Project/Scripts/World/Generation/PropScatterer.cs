using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Scatters trees and rocks to exact target counts.
    ///
    /// Candidate points are drawn uniformly and accepted with probability proportional to the local
    /// biome's density, so dense biomes fill up while open plain stays sparse. Spacing is enforced with
    /// a spatial hash, and nothing grows on lakes, on sites, or at the spawn point.
    /// </summary>
    public static class PropScatterer
    {
        private const int ScatterSalt = 3;

        public static IReadOnlyList<PropPlacement> Scatter(WorldLayout layout, BiomeMap biomes, WorldGenConfig config)
        {
            if (layout == null || biomes == null || config == null)
            {
                throw new ArgumentNullException(layout == null ? nameof(layout) : biomes == null ? nameof(biomes) : nameof(config));
            }

            var random = new DeterministicRandom(DeterministicRandom.Derive(config.Seed, ScatterSalt));
            var job = new ScatterJob(layout, biomes, config, random);

            job.Place(config.Scatter.TreeCount, isTree: true);
            job.Place(config.Scatter.RockCount, isTree: false);

            return job.Results;
        }

        private sealed class ScatterJob
        {
            private readonly WorldLayout _layout;
            private readonly BiomeMap _biomes;
            private readonly WorldGenConfig _config;
            private readonly ScatterSettings _settings;
            private readonly DeterministicRandom _random;
            private readonly SpatialHash _trees;
            private readonly SpatialHash _rocks;
            private readonly List<WorldSite> _sites = new List<WorldSite>();
            private readonly float _settlementExclusion;

            public ScatterJob(WorldLayout layout, BiomeMap biomes, WorldGenConfig config, DeterministicRandom random)
            {
                _layout = layout;
                _biomes = biomes;
                _config = config;
                _settings = config.Scatter;
                _random = random;

                float cell = Mathf.Max(_settings.MinTreeSpacing, Mathf.Max(_settings.MinRockSpacing, _settings.MinTreeRockSpacing));
                _trees = new SpatialHash(cell);
                _rocks = new SpatialHash(cell);

                _sites.AddRange(layout.Cabins);
                _sites.AddRange(layout.Wells);
                _sites.AddRange(layout.Campfires);

                // Keep the whole fenced settlement clear, not just the individual buildings.
                _settlementExclusion = config.Layout.SettlementFenceRadius + 3f;
            }

            public List<PropPlacement> Results { get; } = new List<PropPlacement>();

            public void Place(int count, bool isTree)
            {
                float maxDensity = 0f;
                foreach (BiomeDefinition definition in _config.Biomes)
                {
                    maxDensity = Mathf.Max(maxDensity, isTree ? definition.TreeDensity : definition.RockDensity);
                }

                if (count <= 0 || maxDensity <= 0f)
                {
                    return;
                }

                float extent = _layout.HalfSize - 5f;
                int maxAttempts = count * _settings.MaxAttemptsPerProp;
                int placed = 0;

                for (int attempt = 0; attempt < maxAttempts && placed < count; attempt++)
                {
                    Vector2 p = _random.InsideSquare(extent);
                    BiomeType biome = _biomes.Classify(p);
                    BiomeDefinition definition = _config.DefinitionFor(biome);

                    float density = isTree ? definition.TreeDensity : definition.RockDensity;
                    if (_random.NextFloat() * maxDensity >= density)
                    {
                        continue;
                    }

                    if (biome == BiomeType.FrozenLake || !IsClearOfSites(p) || !IsSpaced(p, isTree))
                    {
                        continue;
                    }

                    PropPlacement prop = isTree ? MakeTree(p, definition) : MakeRock(p, definition);
                    Results.Add(prop);
                    (isTree ? _trees : _rocks).Add(p);
                    placed++;
                }

                if (placed < count)
                {
                    throw new InvalidOperationException(
                        $"Placed only {placed}/{count} {(isTree ? "trees" : "rocks")}; densities or spacing leave too little room.");
                }
            }

            private PropPlacement MakeTree(Vector2 p, BiomeDefinition definition)
            {
                int species = _random.PickWeighted(definition.PineWeight, definition.BirchWeight, definition.DeadTreeWeight);
                PropKind kind = species switch
                {
                    1 => PropKind.Birch,
                    2 => PropKind.DeadTree,
                    _ => PropKind.Pine,
                };

                float scale = kind == PropKind.DeadTree ? _random.Range(0.7f, 1.1f) : _random.Range(0.8f, 1.3f);
                return new PropPlacement(kind, p, _random.Range(0f, 360f), scale);
            }

            private PropPlacement MakeRock(Vector2 p, BiomeDefinition definition)
            {
                bool boulder = _random.NextFloat() < definition.BoulderChance;
                float scale = boulder ? _random.Range(0.9f, 1.6f) : _random.Range(0.6f, 1.4f);
                return new PropPlacement(boulder ? PropKind.Boulder : PropKind.Rock, p, _random.Range(0f, 360f), scale);
            }

            private bool IsClearOfSites(Vector2 p)
            {
                if (Vector2.Distance(p, _layout.SpawnPoint) < _settings.SpawnClearance)
                {
                    return false;
                }

                foreach (Vector2 settlement in _layout.SettlementCenters)
                {
                    if (Vector2.Distance(p, settlement) < _settlementExclusion)
                    {
                        return false;
                    }
                }

                foreach (WorldSite site in _sites)
                {
                    if (Vector2.Distance(p, site.Position) < site.FootprintRadius + _settings.SiteClearance)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool IsSpaced(Vector2 p, bool isTree)
            {
                if (isTree)
                {
                    return !_trees.AnyWithin(p, _settings.MinTreeSpacing)
                           && !_rocks.AnyWithin(p, _settings.MinTreeRockSpacing);
                }

                return !_rocks.AnyWithin(p, _settings.MinRockSpacing)
                       && !_trees.AnyWithin(p, _settings.MinTreeRockSpacing);
            }
        }

        /// <summary>Uniform grid bucket lookup for "is anything within r of p" queries.</summary>
        private sealed class SpatialHash
        {
            private readonly float _cellSize;
            private readonly Dictionary<long, List<Vector2>> _cells = new Dictionary<long, List<Vector2>>();

            public SpatialHash(float cellSize)
            {
                _cellSize = cellSize;
            }

            public void Add(Vector2 p)
            {
                long key = Key(Cell(p.x), Cell(p.y));
                if (!_cells.TryGetValue(key, out List<Vector2> bucket))
                {
                    bucket = new List<Vector2>();
                    _cells[key] = bucket;
                }

                bucket.Add(p);
            }

            public bool AnyWithin(Vector2 p, float radius)
            {
                float radiusSq = radius * radius;
                int reach = Mathf.CeilToInt(radius / _cellSize);
                int cx = Cell(p.x);
                int cz = Cell(p.y);

                for (int dz = -reach; dz <= reach; dz++)
                {
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        if (!_cells.TryGetValue(Key(cx + dx, cz + dz), out List<Vector2> bucket))
                        {
                            continue;
                        }

                        foreach (Vector2 other in bucket)
                        {
                            if ((other - p).sqrMagnitude < radiusSq)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            private int Cell(float v) => Mathf.FloorToInt(v / _cellSize);

            private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
        }
    }
}

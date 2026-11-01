using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Answers "which biome is this point in, and how strongly" for a set of placed regions.
    ///
    /// Membership fades from 1 to 0 over the outer <see cref="BiomeDefinition.BoundaryBlend"/> of each
    /// region, and the edge is wobbled with noise so regions are not perfect circles.
    /// </summary>
    public sealed class BiomeMap
    {
        private const float BoundaryNoiseScale = 40f;

        private readonly IReadOnlyList<BiomeRegion> _regions;
        private readonly BiomeDefinition[] _definitionsByRegion;
        private readonly int[] _byPriority;
        private readonly SeededNoise _boundaryNoise;

        public BiomeMap(IReadOnlyList<BiomeRegion> regions, IReadOnlyList<BiomeDefinition> definitions, int seed)
        {
            _regions = regions ?? throw new ArgumentNullException(nameof(regions));
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitionsByRegion = new BiomeDefinition[regions.Count];
            for (int i = 0; i < regions.Count; i++)
            {
                _definitionsByRegion[i] = Find(definitions, regions[i].Type);
            }

            _byPriority = new int[regions.Count];
            for (int i = 0; i < _byPriority.Length; i++)
            {
                _byPriority[i] = i;
            }

            // Stable sort: equal priorities keep placement order, so results never depend on sort quirks.
            Array.Sort(_byPriority, (a, b) =>
            {
                int byPriority = _definitionsByRegion[a].Priority.CompareTo(_definitionsByRegion[b].Priority);
                return byPriority != 0 ? byPriority : a.CompareTo(b);
            });

            _boundaryNoise = new SeededNoise(DeterministicRandom.Derive(seed, 0xB10E));
        }

        public IReadOnlyList<BiomeRegion> Regions => _regions;

        /// <summary>Region indices from lowest to highest priority (paint order).</summary>
        public IReadOnlyList<int> RegionsByPriority => _byPriority;

        /// <summary>Membership of <paramref name="p"/> in region <paramref name="index"/>, 0-1.</summary>
        public float Weight(int index, Vector2 p)
        {
            BiomeRegion region = _regions[index];
            BiomeDefinition definition = _definitionsByRegion[index];

            float distance = region.DistanceTo(p);
            float outer = region.Radius * (1f + definition.BoundaryNoise);
            if (distance >= outer)
            {
                return 0f;
            }

            float wobble = _boundaryNoise.Fbm(p.x / BoundaryNoiseScale, p.y / BoundaryNoiseScale, 2)
                           * definition.BoundaryNoise * region.Radius;
            float t = (distance + wobble) / region.Radius;

            float fadeStart = 1f - Mathf.Clamp01(definition.BoundaryBlend);
            return 1f - SmoothStep(fadeStart, 1f, t);
        }

        /// <summary>Strongest membership of any region at <paramref name="p"/>; 0 on open plain.</summary>
        public float MaxWeight(Vector2 p)
        {
            float max = 0f;
            for (int i = 0; i < _regions.Count; i++)
            {
                max = Mathf.Max(max, Weight(i, p));
            }

            return max;
        }

        /// <summary>
        /// The biome at <paramref name="p"/>: the highest-priority region the point is at least half
        /// inside, otherwise <see cref="BiomeType.Plain"/>.
        /// </summary>
        public BiomeType Classify(Vector2 p)
        {
            BiomeType best = BiomeType.Plain;
            int bestPriority = int.MinValue;

            for (int i = 0; i < _regions.Count; i++)
            {
                int priority = _definitionsByRegion[i].Priority;
                if (priority <= bestPriority)
                {
                    continue;
                }

                if (Weight(i, p) >= 0.5f)
                {
                    best = _regions[i].Type;
                    bestPriority = priority;
                }
            }

            return best;
        }

        internal static float SmoothStep(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0)
            {
                return x < edge0 ? 0f : 1f;
            }

            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - (2f * t));
        }

        private static BiomeDefinition Find(IReadOnlyList<BiomeDefinition> definitions, BiomeType type)
        {
            foreach (BiomeDefinition definition in definitions)
            {
                if (definition.Type == type)
                {
                    return definition;
                }
            }

            throw new ArgumentException($"No biome definition for {type}.");
        }
    }
}

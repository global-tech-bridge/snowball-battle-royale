using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// The placed regions and sites of one world. Produced by <see cref="WorldLayoutGenerator"/>;
    /// everything here is in world XZ metres with the map centred on the origin.
    /// </summary>
    public sealed class WorldLayout
    {
        public WorldLayout(
            float size,
            IReadOnlyList<BiomeRegion> regions,
            IReadOnlyList<Vector2> settlementCenters,
            IReadOnlyList<WorldSite> cabins,
            IReadOnlyList<WorldSite> wells,
            IReadOnlyList<WorldSite> fences,
            IReadOnlyList<WorldSite> campfires,
            Vector2 spawnPoint)
        {
            Size = size;
            Regions = regions;
            SettlementCenters = settlementCenters;
            Cabins = cabins;
            Wells = wells;
            Fences = fences;
            Campfires = campfires;
            SpawnPoint = spawnPoint;
        }

        public float Size { get; }

        public float HalfSize => Size * 0.5f;

        public IReadOnlyList<BiomeRegion> Regions { get; }

        public IReadOnlyList<Vector2> SettlementCenters { get; }

        public IReadOnlyList<WorldSite> Cabins { get; }

        public IReadOnlyList<WorldSite> Wells { get; }

        public IReadOnlyList<WorldSite> Fences { get; }

        public IReadOnlyList<WorldSite> Campfires { get; }

        public Vector2 SpawnPoint { get; }
    }
}

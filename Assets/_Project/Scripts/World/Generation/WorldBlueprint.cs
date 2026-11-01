using System;
using System.Collections.Generic;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Everything generated for one world, independent of Unity scene objects: layout, biome map,
    /// height field and scattered props. <see cref="WorldGenerator"/> turns a blueprint into a terrain
    /// and GameObjects; tests inspect it directly.
    /// </summary>
    public sealed class WorldBlueprint
    {
        private WorldBlueprint(
            WorldGenConfig config,
            WorldLayout layout,
            BiomeMap biomes,
            TerrainHeightField heights,
            IReadOnlyList<PropPlacement> props)
        {
            Config = config;
            Layout = layout;
            Biomes = biomes;
            Heights = heights;
            Props = props;
        }

        public WorldGenConfig Config { get; }

        public WorldLayout Layout { get; }

        public BiomeMap Biomes { get; }

        public TerrainHeightField Heights { get; }

        public IReadOnlyList<PropPlacement> Props { get; }

        public static WorldBlueprint Create(WorldGenConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            WorldLayout layout = WorldLayoutGenerator.Generate(config);
            var biomes = new BiomeMap(layout.Regions, config.Biomes, config.Seed);
            var heights = new TerrainHeightField(layout, config.Layout, config.Shape, config.Seed);
            IReadOnlyList<PropPlacement> props = PropScatterer.Scatter(layout, biomes, config);

            return new WorldBlueprint(config, layout, biomes, heights, props);
        }
    }
}

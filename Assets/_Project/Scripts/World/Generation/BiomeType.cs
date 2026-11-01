namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Terrain biome. <see cref="Plain"/> is the background: every point not claimed by a region is
    /// plain snowfield.
    /// </summary>
    public enum BiomeType
    {
        Plain,
        Forest,
        Mountain,
        Valley,
        DeepSnow,
        FrozenLake,
    }

    public static class BiomeTypeExtensions
    {
        /// <summary>Ground where cabins, campfires and the player spawn may be placed.</summary>
        public static bool IsBuildable(this BiomeType biome)
        {
            return biome != BiomeType.Mountain && biome != BiomeType.FrozenLake;
        }
    }
}

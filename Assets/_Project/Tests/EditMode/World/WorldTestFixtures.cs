using SnowballBattleRoyale.World;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    /// <summary>
    /// Generating the default 700 m world is the expensive part of these tests, so the blueprint is
    /// built once and shared across fixtures. Everything it exposes is read-only.
    /// </summary>
    internal static class WorldTestFixtures
    {
        public const int DefaultSeed = 12345;

        private static WorldBlueprint _default;

        public static WorldGenConfig DefaultConfig(int seed = DefaultSeed)
        {
            return WorldGenConfig.CreateDefault(seed);
        }

        public static WorldBlueprint DefaultBlueprint => _default ??= WorldBlueprint.Create(DefaultConfig());
    }
}

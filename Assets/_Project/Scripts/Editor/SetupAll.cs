using UnityEditor;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>Regenerates every generated asset in dependency order (the world scene uses the Player prefab).</summary>
    public static class SetupAll
    {
        [MenuItem("Tools/Snowball/Rebuild All Generated Assets")]
        public static void RebuildAll()
        {
            PlayerSetup.BuildPlayerAssets();
            WorldSetup.BuildWorldAssets();
        }
    }
}

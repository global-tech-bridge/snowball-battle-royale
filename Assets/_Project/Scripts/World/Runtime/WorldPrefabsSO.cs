using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>Art used by <see cref="WorldGenerator"/>. Swap these for final assets without touching code.</summary>
    [CreateAssetMenu(fileName = "WorldPrefabsSO", menuName = "Snowball/World/World Prefabs", order = 2)]
    public sealed class WorldPrefabsSO : ScriptableObject
    {
        [Header("Trees (terrain tree prototypes)")]
        [SerializeField]
        private GameObject _pine;

        [SerializeField]
        private GameObject _birch;

        [SerializeField]
        private GameObject _deadTree;

        [Header("Rocks")]
        [SerializeField]
        private GameObject _rock;

        [SerializeField]
        private GameObject _boulder;

        [Header("Sites")]
        [SerializeField]
        private GameObject _cabin;

        [SerializeField]
        private GameObject _well;

        [SerializeField]
        private GameObject _fence;

        [SerializeField]
        private GameObject _campfire;

        [Header("Terrain")]
        [Tooltip("One layer per TerrainLayerId, in enum order.")]
        [SerializeField]
        private TerrainLayer[] _terrainLayers = new TerrainLayer[TerrainPaintRules.LayerCount];

        [Tooltip("URP terrain material (Universal Render Pipeline/Terrain/Lit).")]
        [SerializeField]
        private Material _terrainMaterial;

        public GameObject Cabin => _cabin;

        public GameObject Well => _well;

        public GameObject Fence => _fence;

        public GameObject Campfire => _campfire;

        public TerrainLayer[] TerrainLayers => _terrainLayers;

        public Material TerrainMaterial => _terrainMaterial;

        public GameObject PrefabFor(PropKind kind)
        {
            return kind switch
            {
                PropKind.Pine => _pine,
                PropKind.Birch => _birch,
                PropKind.DeadTree => _deadTree,
                PropKind.Rock => _rock,
                PropKind.Boulder => _boulder,
                _ => null,
            };
        }
    }
}

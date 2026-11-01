using System.Collections.Generic;
using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Biome boundary definitions: how many regions of each biome, how large, how soft their edges
    /// are, which wins where they overlap, and what grows there.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeSettingsSO", menuName = "Snowball/World/Biome Settings", order = 1)]
    public sealed class BiomeSettingsSO : ScriptableObject
    {
        [SerializeField]
        private List<BiomeDefinition> _definitions = BiomeDefinition.CreateDefaults();

        public IReadOnlyList<BiomeDefinition> Definitions => _definitions;

        private void OnValidate()
        {
            var seen = new HashSet<BiomeType>();
            foreach (BiomeDefinition definition in _definitions)
            {
                if (definition != null && !seen.Add(definition.Type))
                {
                    SBLog.Warn($"Biome {definition.Type} is defined more than once; the first entry wins.", this, "World");
                }
            }

            foreach (BiomeType type in System.Enum.GetValues(typeof(BiomeType)))
            {
                if (!seen.Contains(type))
                {
                    SBLog.Warn($"Biome {type} has no definition; generation will fail.", this, "World");
                }
            }
        }
    }
}

using System;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>Terrain layer slots. The order must match <c>WorldPrefabsSO.TerrainLayers</c>.</summary>
    public enum TerrainLayerId
    {
        Snow = 0,
        ForestSnow = 1,
        Rock = 2,
        PowderSnow = 3,
        Ice = 4,
        Dirt = 5,
        DeepDirt = 6,
    }

    /// <summary>What digging has exposed at a point.</summary>
    public enum SoilLayer
    {
        Snow,
        Dirt,
        DeepDirt,
    }

    /// <summary>Depths (m) at which digging exposes each soil layer.</summary>
    [Serializable]
    public sealed class SoilSettings
    {
        [Tooltip("Dug deeper than this, the ground shows dirt.")]
        public float DirtDepth = 0.25f;

        [Tooltip("Dug deeper than this, the ground shows dark deep soil.")]
        public float DeepDirtDepth = 1f;

        [Tooltip("Colour transition width just above each threshold (m).")]
        public float Blend = 0.1f;
    }

    /// <summary>
    /// Terrain colour as a function of biome membership and dig depth. Pure, so the same rule paints
    /// the initial alphamap and repaints dug patches.
    /// </summary>
    public static class TerrainPaintRules
    {
        public const int LayerCount = 7;

        /// <summary>Valley floors are mostly snow with exposed stone.</summary>
        private const float ValleyRockShare = 0.35f;

        /// <summary>
        /// Fills <paramref name="weights"/> (length <see cref="LayerCount"/>) with layer weights summing
        /// to 1 for world point <paramref name="p"/> dug to <paramref name="digDepth"/> metres.
        /// </summary>
        public static void Evaluate(BiomeMap biomes, Vector2 p, float digDepth, SoilSettings soil, float[] weights)
        {
            if (weights == null || weights.Length < LayerCount)
            {
                throw new ArgumentException($"Need a buffer of at least {LayerCount} weights.", nameof(weights));
            }

            Array.Clear(weights, 0, LayerCount);
            weights[(int)TerrainLayerId.Snow] = 1f;

            if (biomes != null)
            {
                // Lowest priority first, so stronger biomes paint over weaker ones where they overlap.
                foreach (int index in biomes.RegionsByPriority)
                {
                    float w = biomes.Weight(index, p);
                    if (w > 0f)
                    {
                        BlendToward(weights, biomes.Regions[index].Type, w);
                    }
                }
            }

            ApplySoil(weights, digDepth, soil);
        }

        public static SoilLayer SoilFor(float digDepth, SoilSettings soil)
        {
            if (digDepth >= soil.DeepDirtDepth)
            {
                return SoilLayer.DeepDirt;
            }

            return digDepth >= soil.DirtDepth ? SoilLayer.Dirt : SoilLayer.Snow;
        }

        private static void BlendToward(float[] weights, BiomeType biome, float amount)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    LerpToward(weights, TerrainLayerId.ForestSnow, amount);
                    break;
                case BiomeType.Mountain:
                    LerpToward(weights, TerrainLayerId.Rock, amount);
                    break;
                case BiomeType.DeepSnow:
                    LerpToward(weights, TerrainLayerId.PowderSnow, amount);
                    break;
                case BiomeType.FrozenLake:
                    LerpToward(weights, TerrainLayerId.Ice, amount);
                    break;
                case BiomeType.Valley:
                    LerpTowardMix(weights, TerrainLayerId.Snow, TerrainLayerId.Rock, ValleyRockShare, amount);
                    break;
            }
        }

        private static void ApplySoil(float[] weights, float digDepth, SoilSettings soil)
        {
            if (soil == null || digDepth <= 0f)
            {
                return;
            }

            float dirt = BiomeMap.SmoothStep(soil.DirtDepth - soil.Blend, soil.DirtDepth, digDepth);
            float deep = BiomeMap.SmoothStep(soil.DeepDirtDepth - soil.Blend, soil.DeepDirtDepth, digDepth);

            LerpToward(weights, TerrainLayerId.Dirt, dirt);
            LerpToward(weights, TerrainLayerId.DeepDirt, deep);
        }

        /// <summary>weights = lerp(weights, one-hot(layer), amount). Keeps the sum at 1.</summary>
        private static void LerpToward(float[] weights, TerrainLayerId layer, float amount)
        {
            LerpTowardMix(weights, layer, layer, 0f, amount);
        }

        /// <summary>
        /// weights = lerp(weights, mix, amount) where mix is <paramref name="secondShare"/> of
        /// <paramref name="second"/> and the rest <paramref name="first"/>. Keeps the sum at 1.
        /// </summary>
        private static void LerpTowardMix(
            float[] weights,
            TerrainLayerId first,
            TerrainLayerId second,
            float secondShare,
            float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (amount <= 0f)
            {
                return;
            }

            for (int i = 0; i < LayerCount; i++)
            {
                weights[i] *= 1f - amount;
            }

            weights[(int)first] += amount * (1f - secondShare);
            weights[(int)second] += amount * secondShare;
        }
    }
}

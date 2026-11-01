using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class TerrainPaintRulesTests
    {
        private static WorldBlueprint World => WorldTestFixtures.DefaultBlueprint;

        private static SoilSettings Soil => World.Config.Soil;

        private static float[] WeightsAt(Vector2 p, float digDepth = 0f)
        {
            var weights = new float[TerrainPaintRules.LayerCount];
            TerrainPaintRules.Evaluate(World.Biomes, p, digDepth, Soil, weights);
            return weights;
        }

        private static TerrainLayerId Dominant(float[] weights)
        {
            int best = 0;
            for (int i = 1; i < weights.Length; i++)
            {
                if (weights[i] > weights[best])
                {
                    best = i;
                }
            }

            return (TerrainLayerId)best;
        }

        private static Vector2 CenterOf(BiomeType type)
        {
            return World.Layout.Regions.First(r => r.Type == type).Center;
        }

        // ---------- biome colouring ----------

        [TestCase(BiomeType.Forest, TerrainLayerId.ForestSnow)]
        [TestCase(BiomeType.Mountain, TerrainLayerId.Rock)]
        [TestCase(BiomeType.DeepSnow, TerrainLayerId.PowderSnow)]
        [TestCase(BiomeType.FrozenLake, TerrainLayerId.Ice)]
        public void RegionCenters_ArePaintedWithTheirBiomeLayer(BiomeType biome, TerrainLayerId expected)
        {
            Assert.That(Dominant(WeightsAt(CenterOf(biome))), Is.EqualTo(expected));
        }

        [Test]
        public void ValleyIsPaintedDifferentlyFromOpenPlain()
        {
            float[] valley = WeightsAt(CenterOf(BiomeType.Valley));

            Assert.That(valley[(int)TerrainLayerId.Rock], Is.GreaterThan(0.2f), "Valley floor should be stony.");
        }

        [Test]
        public void OpenPlainIsPlainSnow()
        {
            Vector2 open = Vector2.zero;
            float best = float.MinValue;
            for (float x = -300f; x <= 300f; x += 15f)
            {
                for (float z = -300f; z <= 300f; z += 15f)
                {
                    var p = new Vector2(x, z);
                    float clearance = World.Layout.Regions.Min(r => r.DistanceTo(p) - r.Radius);
                    if (clearance > best)
                    {
                        best = clearance;
                        open = p;
                    }
                }
            }

            Assert.That(WeightsAt(open)[(int)TerrainLayerId.Snow], Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void WeightsAlwaysSumToOne()
        {
            float half = World.Layout.Size * 0.5f;
            float[] depths = { 0f, 0.2f, 0.5f, 1.2f, 2f };

            for (float x = -half; x <= half; x += 31f)
            {
                for (float z = -half; z <= half; z += 31f)
                {
                    foreach (float depth in depths)
                    {
                        float sum = WeightsAt(new Vector2(x, z), depth).Sum();
                        Assert.That(sum, Is.EqualTo(1f).Within(1e-4f), $"({x}, {z}) depth {depth}");
                    }
                }
            }
        }

        [Test]
        public void WeightsAreNeverNegative()
        {
            float[] weights = WeightsAt(CenterOf(BiomeType.Forest), 0.6f);

            Assert.That(weights, Is.All.GreaterThanOrEqualTo(0f));
        }

        // ---------- soil exposed by digging ----------

        [Test]
        public void SoilFor_ClassifiesDepthIntoSnowDirtAndDeepDirt()
        {
            Assert.That(TerrainPaintRules.SoilFor(0f, Soil), Is.EqualTo(SoilLayer.Snow));
            Assert.That(TerrainPaintRules.SoilFor(Soil.DirtDepth - 0.01f, Soil), Is.EqualTo(SoilLayer.Snow));
            Assert.That(TerrainPaintRules.SoilFor(Soil.DirtDepth, Soil), Is.EqualTo(SoilLayer.Dirt));
            Assert.That(TerrainPaintRules.SoilFor(Soil.DeepDirtDepth - 0.01f, Soil), Is.EqualTo(SoilLayer.Dirt));
            Assert.That(TerrainPaintRules.SoilFor(Soil.DeepDirtDepth, Soil), Is.EqualTo(SoilLayer.DeepDirt));
        }

        [Test]
        public void ShallowDentsKeepTheSurfaceColour()
        {
            Vector2 p = CenterOf(BiomeType.Forest);

            Assert.That(WeightsAt(p, 0.05f), Is.EqualTo(WeightsAt(p, 0f)).Within(1e-4f));
        }

        [Test]
        public void DiggingToDirtDepth_ExposesDirt()
        {
            float depth = (Soil.DirtDepth + Soil.DeepDirtDepth) * 0.5f;

            Assert.That(Dominant(WeightsAt(CenterOf(BiomeType.Forest), depth)), Is.EqualTo(TerrainLayerId.Dirt));
        }

        [Test]
        public void DiggingPastDeepDirtDepth_ExposesDeepDirt()
        {
            float depth = Soil.DeepDirtDepth + 0.3f;

            Assert.That(Dominant(WeightsAt(CenterOf(BiomeType.Forest), depth)), Is.EqualTo(TerrainLayerId.DeepDirt));
        }

        [Test]
        public void SoilAppliesInEveryBiome()
        {
            float depth = Soil.DeepDirtDepth + 0.3f;

            foreach (BiomeType biome in new[] { BiomeType.Forest, BiomeType.Mountain, BiomeType.DeepSnow, BiomeType.Valley })
            {
                Assert.That(Dominant(WeightsAt(CenterOf(biome), depth)), Is.EqualTo(TerrainLayerId.DeepDirt), biome.ToString());
            }
        }
    }
}

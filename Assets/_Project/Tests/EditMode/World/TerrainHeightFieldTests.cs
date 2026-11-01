using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class TerrainHeightFieldTests
    {
        private static WorldBlueprint World => WorldTestFixtures.DefaultBlueprint;

        private static TerrainHeightField Heights => World.Heights;

        private static TerrainShapeSettings Shape => World.Config.Shape;

        [Test]
        public void HeightsStayWithinTheTerrainVolume()
        {
            float half = World.Layout.Size * 0.5f;

            for (float x = -half; x <= half; x += 17f)
            {
                for (float z = -half; z <= half; z += 17f)
                {
                    float h = Heights.HeightAt(new Vector2(x, z));
                    Assert.That(h, Is.InRange(0f, Shape.TerrainHeight), $"({x}, {z})");
                }
            }
        }

        [Test]
        public void MountainCentersRiseWellAboveThePlain()
        {
            foreach (BiomeRegion mountain in World.Layout.Regions.Where(r => r.Type == BiomeType.Mountain))
            {
                Assert.That(
                    Heights.HeightAt(mountain.Center),
                    Is.GreaterThan(Shape.PlainElevation + 30f),
                    mountain.ToString());
            }
        }

        [Test]
        public void ValleyFloorSitsBelowThePlain()
        {
            BiomeRegion valley = World.Layout.Regions.Single(r => r.Type == BiomeType.Valley);

            Assert.That(Heights.HeightAt(valley.Center), Is.LessThan(Shape.PlainElevation - 6f));
        }

        [Test]
        public void FrozenLakesAreFlatAndBelowThePlain()
        {
            foreach (BiomeRegion lake in World.Layout.Regions.Where(r => r.Type == BiomeType.FrozenLake))
            {
                float min = float.MaxValue;
                float max = float.MinValue;

                for (int i = 0; i < 64; i++)
                {
                    float angle = i * Mathf.PI * 2f / 64f;
                    float radius = lake.Radius * 0.5f * (i % 4) / 3f;
                    var p = lake.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    float h = Heights.HeightAt(p);
                    min = Mathf.Min(min, h);
                    max = Mathf.Max(max, h);
                }

                Assert.That(max - min, Is.LessThan(0.3f), $"{lake} surface should be flat ice.");
                Assert.That(max, Is.LessThan(Shape.PlainElevation), lake.ToString());
            }
        }

        [Test]
        public void CabinFootprintsAreFlattened()
        {
            foreach (WorldSite cabin in World.Layout.Cabins)
            {
                float center = Heights.HeightAt(cabin.Position);

                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8f;
                    var p = cabin.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cabin.FootprintRadius;
                    Assert.That(Heights.HeightAt(p), Is.EqualTo(center).Within(0.05f), cabin.ToString());
                }
            }
        }

        [Test]
        public void OpenPlainIsGentleEnoughToWalk()
        {
            // The player controller's slope limit is 50 degrees; open plain should be far gentler.
            float maxSlope = Mathf.Tan(30f * Mathf.Deg2Rad);
            const float step = 2f;
            int checkedPoints = 0;

            float half = World.Layout.Size * 0.5f - 20f;
            for (float x = -half; x <= half; x += 23f)
            {
                for (float z = -half; z <= half; z += 23f)
                {
                    var p = new Vector2(x, z);
                    if (World.Biomes.Classify(p) != BiomeType.Plain || World.Biomes.MaxWeight(p) > 0f)
                    {
                        continue;
                    }

                    float dx = (Heights.HeightAt(p + new Vector2(step, 0f)) - Heights.HeightAt(p)) / step;
                    float dz = (Heights.HeightAt(p + new Vector2(0f, step)) - Heights.HeightAt(p)) / step;
                    Assert.That(Mathf.Sqrt(dx * dx + dz * dz), Is.LessThan(maxSlope), $"({x}, {z})");
                    checkedPoints++;
                }
            }

            Assert.That(checkedPoints, Is.GreaterThan(20), "Too few open plain samples to judge.");
        }

        [Test]
        public void HeightAt_IsDeterministic()
        {
            TerrainHeightField other = WorldBlueprint.Create(WorldTestFixtures.DefaultConfig()).Heights;
            var p = new Vector2(123.4f, -56.7f);

            Assert.That(other.HeightAt(p), Is.EqualTo(Heights.HeightAt(p)));
        }

        [Test]
        public void Sample_ProducesANormalizedSquareGrid()
        {
            const int resolution = 65;

            float[,] grid = Heights.Sample(resolution);

            Assert.That(grid.GetLength(0), Is.EqualTo(resolution));
            Assert.That(grid.GetLength(1), Is.EqualTo(resolution));
            foreach (float v in grid)
            {
                Assert.That(v, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Sample_IsIndexedRowByZThenColumnByX()
        {
            // Unity's TerrainData.SetHeights expects [z, x]. Pick the map's highest region to make
            // the orientation observable.
            const int resolution = 65;
            float[,] grid = Heights.Sample(resolution);
            BiomeRegion mountain = World.Layout.Regions.First(r => r.Type == BiomeType.Mountain);

            float size = World.Layout.Size;
            int ix = Mathf.RoundToInt((mountain.Center.x / size + 0.5f) * (resolution - 1));
            int iz = Mathf.RoundToInt((mountain.Center.y / size + 0.5f) * (resolution - 1));

            float expected = Heights.HeightAt(mountain.Center) / Shape.TerrainHeight;
            Assert.That(grid[iz, ix], Is.EqualTo(expected).Within(0.08f));
        }
    }
}

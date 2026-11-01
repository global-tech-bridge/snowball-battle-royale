using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class PropScatterTests
    {
        private static WorldBlueprint World => WorldTestFixtures.DefaultBlueprint;

        private static IReadOnlyList<PropPlacement> Props => World.Props;

        private static IEnumerable<PropPlacement> Trees => Props.Where(p => p.Kind.IsTree());

        private static IEnumerable<PropPlacement> Rocks => Props.Where(p => p.Kind.IsRock());

        [Test]
        public void PlacesAbout1100Trees()
        {
            Assert.That(Trees.Count(), Is.EqualTo(World.Config.Scatter.TreeCount));
            Assert.That(World.Config.Scatter.TreeCount, Is.EqualTo(1100));
        }

        [Test]
        public void PlacesAbout250Rocks()
        {
            Assert.That(Rocks.Count(), Is.EqualTo(World.Config.Scatter.RockCount));
            Assert.That(World.Config.Scatter.RockCount, Is.EqualTo(250));
        }

        [TestCase(3)]
        [TestCase(777)]
        public void OtherSeeds_ReachTheTargetCounts(int seed)
        {
            WorldBlueprint other = WorldBlueprint.Create(WorldTestFixtures.DefaultConfig(seed));

            Assert.That(other.Props.Count(p => p.Kind.IsTree()), Is.EqualTo(1100));
            Assert.That(other.Props.Count(p => p.Kind.IsRock()), Is.EqualTo(250));
        }

        [Test]
        public void AllThreeTreeSpeciesAppear()
        {
            Assert.That(Trees.Any(t => t.Kind == PropKind.Pine), Is.True);
            Assert.That(Trees.Any(t => t.Kind == PropKind.Birch), Is.True);
            Assert.That(Trees.Any(t => t.Kind == PropKind.DeadTree), Is.True);
        }

        [Test]
        public void BothRockSizesAppear()
        {
            Assert.That(Rocks.Any(r => r.Kind == PropKind.Rock), Is.True);
            Assert.That(Rocks.Any(r => r.Kind == PropKind.Boulder), Is.True);
        }

        [Test]
        public void TreesRespectMinimumSpacing()
        {
            AssertMinimumSpacing(Trees.ToList(), World.Config.Scatter.MinTreeSpacing);
        }

        [Test]
        public void RocksRespectMinimumSpacing()
        {
            AssertMinimumSpacing(Rocks.ToList(), World.Config.Scatter.MinRockSpacing);
        }

        [Test]
        public void NothingGrowsOnFrozenLakes()
        {
            foreach (PropPlacement prop in Props)
            {
                Assert.That(World.Biomes.Classify(prop.Position), Is.Not.EqualTo(BiomeType.FrozenLake), prop.ToString());
            }
        }

        [Test]
        public void PropsKeepClearOfSites()
        {
            float clearance = World.Config.Scatter.SiteClearance;
            var sites = World.Layout.Cabins
                .Concat(World.Layout.Campfires)
                .Concat(World.Layout.Wells)
                .ToList();

            foreach (PropPlacement prop in Props)
            {
                foreach (WorldSite site in sites)
                {
                    Assert.That(
                        Vector2.Distance(prop.Position, site.Position),
                        Is.GreaterThanOrEqualTo(site.FootprintRadius + clearance),
                        $"{prop} too close to {site}");
                }
            }
        }

        [Test]
        public void PropsKeepClearOfTheSpawnPoint()
        {
            foreach (PropPlacement prop in Props)
            {
                Assert.That(Vector2.Distance(prop.Position, World.Layout.SpawnPoint), Is.GreaterThan(4f), prop.ToString());
            }
        }

        [Test]
        public void ForestsAreDenserThanPlain()
        {
            float forest = TreeDensity(BiomeType.Forest);
            float plain = TreeDensity(BiomeType.Plain);

            Assert.That(forest, Is.GreaterThan(plain * 5f), $"forest {forest:F5} vs plain {plain:F5} trees/m2");
        }

        [Test]
        public void MountainsAreRockierThanPlain()
        {
            float mountain = RockDensity(BiomeType.Mountain);
            float plain = RockDensity(BiomeType.Plain);

            Assert.That(mountain, Is.GreaterThan(plain * 3f), $"mountain {mountain:F5} vs plain {plain:F5} rocks/m2");
        }

        [Test]
        public void DeepSnowFavoursDeadTreesMoreThanForestsDo()
        {
            Assert.That(DeadTreeShare(BiomeType.DeepSnow), Is.GreaterThan(DeadTreeShare(BiomeType.Forest)));
        }

        [Test]
        public void PropsHaveSensibleScaleAndRotation()
        {
            foreach (PropPlacement prop in Props)
            {
                Assert.That(prop.Scale, Is.InRange(0.5f, 2.5f), prop.ToString());
                Assert.That(prop.RotationDegrees, Is.InRange(0f, 360f), prop.ToString());
            }
        }

        [Test]
        public void ScatterIsDeterministic()
        {
            WorldBlueprint other = WorldBlueprint.Create(WorldTestFixtures.DefaultConfig());

            Assert.That(other.Props.Select(p => p.Position), Is.EqualTo(Props.Select(p => p.Position)));
            Assert.That(other.Props.Select(p => p.Kind), Is.EqualTo(Props.Select(p => p.Kind)));
        }

        private static void AssertMinimumSpacing(IReadOnlyList<PropPlacement> props, float spacing)
        {
            for (int i = 0; i < props.Count; i++)
            {
                for (int j = i + 1; j < props.Count; j++)
                {
                    float d = Vector2.Distance(props[i].Position, props[j].Position);
                    if (d < spacing)
                    {
                        Assert.Fail($"{props[i]} and {props[j]} are {d:F2} m apart (min {spacing}).");
                    }
                }
            }
        }

        /// <summary>Props per square metre within the given biome, estimated on a sample grid.</summary>
        private static float Density(BiomeType biome, System.Func<PropPlacement, bool> filter)
        {
            const float cell = 5f;
            float half = World.Layout.Size * 0.5f;
            int cells = 0;

            for (float x = -half + cell * 0.5f; x < half; x += cell)
            {
                for (float z = -half + cell * 0.5f; z < half; z += cell)
                {
                    if (World.Biomes.Classify(new Vector2(x, z)) == biome)
                    {
                        cells++;
                    }
                }
            }

            int count = Props.Count(p => filter(p) && World.Biomes.Classify(p.Position) == biome);
            return cells == 0 ? 0f : count / (cells * cell * cell);
        }

        private static float TreeDensity(BiomeType biome) => Density(biome, p => p.Kind.IsTree());

        private static float RockDensity(BiomeType biome) => Density(biome, p => p.Kind.IsRock());

        private static float DeadTreeShare(BiomeType biome)
        {
            var trees = Trees.Where(t => World.Biomes.Classify(t.Position) == biome).ToList();
            Assume.That(trees.Count, Is.GreaterThan(10), $"Too few trees in {biome} to compare.");
            return trees.Count(t => t.Kind == PropKind.DeadTree) / (float)trees.Count;
        }
    }
}

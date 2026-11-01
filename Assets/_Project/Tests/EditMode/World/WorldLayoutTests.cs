using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class WorldLayoutTests
    {
        private static WorldLayout Layout => WorldTestFixtures.DefaultBlueprint.Layout;

        private static BiomeMap Biomes => WorldTestFixtures.DefaultBlueprint.Biomes;

        // ---------- determinism ----------

        [Test]
        public void SameSeed_ProducesTheSameLayout()
        {
            WorldLayout a = WorldLayoutGenerator.Generate(WorldTestFixtures.DefaultConfig(99));
            WorldLayout b = WorldLayoutGenerator.Generate(WorldTestFixtures.DefaultConfig(99));

            Assert.That(a.Regions.Select(r => r.Center), Is.EqualTo(b.Regions.Select(r => r.Center)));
            Assert.That(a.Cabins.Select(c => c.Position), Is.EqualTo(b.Cabins.Select(c => c.Position)));
            Assert.That(a.Campfires.Select(c => c.Position), Is.EqualTo(b.Campfires.Select(c => c.Position)));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentLayouts()
        {
            WorldLayout a = WorldLayoutGenerator.Generate(WorldTestFixtures.DefaultConfig(1));
            WorldLayout b = WorldLayoutGenerator.Generate(WorldTestFixtures.DefaultConfig(2));

            Assert.That(a.Regions.Select(r => r.Center), Is.Not.EqualTo(b.Regions.Select(r => r.Center)));
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(2024)]
        [TestCase(99999)]
        public void OtherSeeds_StillProduceTheFullSiteCounts(int seed)
        {
            WorldLayout layout = WorldLayoutGenerator.Generate(WorldTestFixtures.DefaultConfig(seed));

            Assert.That(layout.SettlementCenters.Count, Is.EqualTo(4));
            Assert.That(layout.Cabins.Count, Is.EqualTo(20));
            Assert.That(layout.Campfires.Count, Is.EqualTo(20));
        }

        // ---------- biome regions ----------

        [Test]
        public void HasFourMountains()
        {
            Assert.That(Layout.Regions.Count(r => r.Type == BiomeType.Mountain), Is.EqualTo(4));
        }

        [Test]
        public void HasFiveForests()
        {
            Assert.That(Layout.Regions.Count(r => r.Type == BiomeType.Forest), Is.EqualTo(5));
        }

        [Test]
        public void HasOneValley()
        {
            Assert.That(Layout.Regions.Count(r => r.Type == BiomeType.Valley), Is.EqualTo(1));
        }

        [Test]
        public void HasDeepSnowAndFrozenLakeRegions()
        {
            Assert.That(Layout.Regions.Count(r => r.Type == BiomeType.DeepSnow), Is.EqualTo(3));
            Assert.That(Layout.Regions.Count(r => r.Type == BiomeType.FrozenLake), Is.EqualTo(2));
        }

        [Test]
        public void PlainIsTheBackgroundAndHasNoRegions()
        {
            Assert.That(Layout.Regions.Any(r => r.Type == BiomeType.Plain), Is.False);
        }

        [Test]
        public void MountainsOccupyFourDifferentQuadrants()
        {
            int quadrants = Layout.Regions
                .Where(r => r.Type == BiomeType.Mountain)
                .Select(r => (Mathf.Sign(r.Center.x), Mathf.Sign(r.Center.y)))
                .Distinct()
                .Count();

            Assert.That(quadrants, Is.EqualTo(4));
        }

        [Test]
        public void AllRegionCentersAreInsideTheMap()
        {
            float half = Layout.Size * 0.5f;

            foreach (BiomeRegion region in Layout.Regions)
            {
                Assert.That(Mathf.Abs(region.Center.x), Is.LessThan(half), region.ToString());
                Assert.That(Mathf.Abs(region.Center.y), Is.LessThan(half), region.ToString());
            }
        }

        [Test]
        public void RegionCentersClassifyAsTheirOwnBiome()
        {
            foreach (BiomeRegion region in Layout.Regions)
            {
                Assert.That(Biomes.Classify(region.Center), Is.EqualTo(region.Type), region.ToString());
            }
        }

        [Test]
        public void PointsFarFromEveryRegionAreOpenPlain()
        {
            Vector2 open = FindPointFarFromAllRegions();

            Assert.That(Biomes.Classify(open), Is.EqualTo(BiomeType.Plain));
        }

        // ---------- settlements, cabins, campfires ----------

        [Test]
        public void HasFourSettlements()
        {
            Assert.That(Layout.SettlementCenters.Count, Is.EqualTo(4));
        }

        [Test]
        public void HasTwentyCabins_ThreePerSettlementPlusEightScattered()
        {
            Assert.That(Layout.Cabins.Count(c => c.SettlementIndex >= 0), Is.EqualTo(12));
            Assert.That(Layout.Cabins.Count(c => c.SettlementIndex < 0), Is.EqualTo(8));
        }

        [Test]
        public void EverySettlementHasAWellAndFences()
        {
            for (int i = 0; i < Layout.SettlementCenters.Count; i++)
            {
                Assert.That(Layout.Wells.Count(w => w.SettlementIndex == i), Is.EqualTo(1), $"settlement {i}");
                Assert.That(Layout.Fences.Count(f => f.SettlementIndex == i), Is.GreaterThan(0), $"settlement {i}");
            }
        }

        [Test]
        public void HasTwentyCampfires()
        {
            Assert.That(Layout.Campfires.Count, Is.EqualTo(20));
        }

        [Test]
        public void NoCabinOrCampfireSitsOnALakeOrMountain()
        {
            foreach (WorldSite site in Layout.Cabins.Concat(Layout.Campfires))
            {
                BiomeType biome = Biomes.Classify(site.Position);
                Assert.That(biome, Is.Not.EqualTo(BiomeType.FrozenLake), site.ToString());
                Assert.That(biome, Is.Not.EqualTo(BiomeType.Mountain), site.ToString());
            }
        }

        [Test]
        public void SettlementsAreFarApart()
        {
            var centers = Layout.SettlementCenters;
            for (int i = 0; i < centers.Count; i++)
            {
                for (int j = i + 1; j < centers.Count; j++)
                {
                    Assert.That(Vector2.Distance(centers[i], centers[j]), Is.GreaterThanOrEqualTo(120f));
                }
            }
        }

        [Test]
        public void CabinsDoNotOverlap()
        {
            var cabins = Layout.Cabins;
            for (int i = 0; i < cabins.Count; i++)
            {
                for (int j = i + 1; j < cabins.Count; j++)
                {
                    float gap = Vector2.Distance(cabins[i].Position, cabins[j].Position);
                    Assert.That(
                        gap,
                        Is.GreaterThan(cabins[i].FootprintRadius + cabins[j].FootprintRadius),
                        $"{cabins[i]} vs {cabins[j]}");
                }
            }
        }

        [Test]
        public void CampfiresKeepTheirDistanceFromEachOtherAndFromCabins()
        {
            var fires = Layout.Campfires;
            for (int i = 0; i < fires.Count; i++)
            {
                for (int j = i + 1; j < fires.Count; j++)
                {
                    Assert.That(Vector2.Distance(fires[i].Position, fires[j].Position), Is.GreaterThanOrEqualTo(25f));
                }

                foreach (WorldSite cabin in Layout.Cabins)
                {
                    Assert.That(Vector2.Distance(fires[i].Position, cabin.Position), Is.GreaterThanOrEqualTo(8f));
                }
            }
        }

        [Test]
        public void AllSitesStayInsideThePlayableMargin()
        {
            float limit = Layout.Size * 0.5f - 20f;

            foreach (WorldSite site in Layout.Cabins.Concat(Layout.Campfires).Concat(Layout.Wells))
            {
                Assert.That(Mathf.Abs(site.Position.x), Is.LessThanOrEqualTo(limit), site.ToString());
                Assert.That(Mathf.Abs(site.Position.y), Is.LessThanOrEqualTo(limit), site.ToString());
            }
        }

        [Test]
        public void CampfiresHaveStaggeredBurnTimes()
        {
            var lifetimes = Layout.Campfires.Select(c => c.Lifetime).ToList();

            Assert.That(lifetimes.Distinct().Count(), Is.GreaterThan(10), "Campfires should go out at different times.");
            Assert.That(lifetimes, Is.All.GreaterThan(0f));
        }

        [Test]
        public void SomeCampfiresOutlastAFiveMinuteMatch()
        {
            // Late game should be scarce but not empty (Phase 8 refines this schedule).
            int survivors = Layout.Campfires.Count(c => c.Lifetime > 300f);

            Assert.That(survivors, Is.InRange(1, 8));
        }

        [Test]
        public void SpawnPointIsOnWalkableGroundAwayFromSites()
        {
            BiomeType biome = Biomes.Classify(Layout.SpawnPoint);
            Assert.That(biome, Is.Not.EqualTo(BiomeType.FrozenLake));
            Assert.That(biome, Is.Not.EqualTo(BiomeType.Mountain));

            foreach (WorldSite site in Layout.Cabins.Concat(Layout.Campfires).Concat(Layout.Wells))
            {
                Assert.That(
                    Vector2.Distance(site.Position, Layout.SpawnPoint),
                    Is.GreaterThan(site.FootprintRadius + 2f),
                    site.ToString());
            }
        }

        [TestCase(WorldTestFixtures.DefaultSeed)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(2024)]
        public void SpawnPointIsInOpenPlainRatherThanInsideABiome(int seed)
        {
            WorldBlueprint world = WorldBlueprint.Create(WorldTestFixtures.DefaultConfig(seed));

            Assert.That(world.Biomes.Classify(world.Layout.SpawnPoint), Is.EqualTo(BiomeType.Plain));
        }

        private static Vector2 FindPointFarFromAllRegions()
        {
            Vector2 best = Vector2.zero;
            float bestClearance = float.MinValue;
            float half = Layout.Size * 0.5f - 10f;

            for (float x = -half; x <= half; x += 10f)
            {
                for (float z = -half; z <= half; z += 10f)
                {
                    var p = new Vector2(x, z);
                    float clearance = Layout.Regions.Min(r => r.DistanceTo(p) - r.Radius);
                    if (clearance > bestClearance)
                    {
                        bestClearance = clearance;
                        best = p;
                    }
                }
            }

            Assume.That(bestClearance, Is.GreaterThan(10f), "Layout leaves no open plain to test against.");
            return best;
        }
    }
}

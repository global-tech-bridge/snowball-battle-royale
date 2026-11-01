using NUnit.Framework;
using SnowballBattleRoyale.World;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class DeterministicRandomTests
    {
        [Test]
        public void SameSeed_ProducesTheSameSequence()
        {
            var a = new DeterministicRandom(42);
            var b = new DeterministicRandom(42);

            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.NextFloat(), Is.EqualTo(b.NextFloat()));
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var a = new DeterministicRandom(1);
            var b = new DeterministicRandom(2);

            int differences = 0;
            for (int i = 0; i < 20; i++)
            {
                if (a.NextFloat() != b.NextFloat())
                {
                    differences++;
                }
            }

            Assert.That(differences, Is.GreaterThan(15));
        }

        [Test]
        public void NextFloat_StaysInTheHalfOpenUnitInterval()
        {
            var random = new DeterministicRandom(7);

            for (int i = 0; i < 10000; i++)
            {
                float value = random.NextFloat();
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void NextInt_StaysInRangeAndCoversIt()
        {
            var random = new DeterministicRandom(7);
            var seen = new bool[5];

            for (int i = 0; i < 1000; i++)
            {
                int value = random.NextInt(10, 15);
                Assert.That(value, Is.InRange(10, 14));
                seen[value - 10] = true;
            }

            Assert.That(seen, Is.All.True);
        }

        [Test]
        public void Range_StaysWithinBounds()
        {
            var random = new DeterministicRandom(9);

            for (int i = 0; i < 1000; i++)
            {
                Assert.That(random.Range(-3f, 5f), Is.InRange(-3f, 5f));
            }
        }

        [Test]
        public void ZeroSeed_IsStillUsable()
        {
            var random = new DeterministicRandom(0);

            float first = random.NextFloat();
            float second = random.NextFloat();

            Assert.That(first, Is.Not.EqualTo(second));
        }
    }
}

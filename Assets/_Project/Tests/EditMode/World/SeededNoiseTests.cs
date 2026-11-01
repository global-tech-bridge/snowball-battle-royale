using NUnit.Framework;
using SnowballBattleRoyale.World;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class SeededNoiseTests
    {
        [Test]
        public void SameSeed_GivesIdenticalValues()
        {
            var a = new SeededNoise(5);
            var b = new SeededNoise(5);

            Assert.That(a.Perlin(12.3f, 45.6f), Is.EqualTo(b.Perlin(12.3f, 45.6f)));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentFields()
        {
            var a = new SeededNoise(5);
            var b = new SeededNoise(6);

            int differences = 0;
            for (int i = 0; i < 50; i++)
            {
                if (System.Math.Abs(a.Perlin(i * 0.37f, i * 0.71f) - b.Perlin(i * 0.37f, i * 0.71f)) > 1e-4f)
                {
                    differences++;
                }
            }

            Assert.That(differences, Is.GreaterThan(40));
        }

        [Test]
        public void Perlin_StaysWithinMinusOneToOne()
        {
            var noise = new SeededNoise(11);

            for (int i = 0; i < 5000; i++)
            {
                float v = noise.Perlin(i * 0.173f, i * 0.291f);
                Assert.That(v, Is.InRange(-1f, 1f));
            }
        }

        [Test]
        public void Perlin_IsZeroOnIntegerLatticePoints()
        {
            var noise = new SeededNoise(3);

            Assert.That(noise.Perlin(4f, 9f), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Perlin_IsContinuous()
        {
            var noise = new SeededNoise(3);

            float a = noise.Perlin(10.5f, 20.5f);
            float b = noise.Perlin(10.501f, 20.5f);

            Assert.That(System.Math.Abs(a - b), Is.LessThan(0.01f));
        }

        [Test]
        public void Fbm_StaysWithinMinusOneToOne()
        {
            var noise = new SeededNoise(13);

            for (int i = 0; i < 3000; i++)
            {
                Assert.That(noise.Fbm(i * 0.11f, i * 0.07f, 5), Is.InRange(-1f, 1f));
            }
        }

        [Test]
        public void Ridged_StaysWithinZeroToOne()
        {
            var noise = new SeededNoise(17);

            for (int i = 0; i < 3000; i++)
            {
                Assert.That(noise.Ridged(i * 0.13f, i * 0.05f, 4), Is.InRange(0f, 1f));
            }
        }
    }
}

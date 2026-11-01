using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Seeded PRNG (SplitMix64 seeding, xorshift64* output) with a fixed, documented algorithm.
    ///
    /// <see cref="System.Random"/> and <see cref="UnityEngine.Random"/> do not guarantee the same
    /// sequence across runtimes or Unity versions. World generation must, so that every client
    /// (and, later, a server) builds the identical map from the same seed.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(int seed)
        {
            ulong mixed = SplitMix64((ulong)(uint)seed + 0x9E3779B97F4A7C15UL);
            _state = mixed == 0 ? 0x2545F4914F6CDD1DUL : mixed;
        }

        /// <summary>Derives an independent seed for a sub-system, so adding draws to one stage never shifts another.</summary>
        public static int Derive(int seed, int salt)
        {
            ulong mixed = SplitMix64(((ulong)(uint)seed << 32) ^ (uint)salt);
            return (int)(mixed ^ (mixed >> 32));
        }

        public ulong NextULong()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat()
        {
            // Top 24 bits map exactly onto the float mantissa, so the result can never round up to 1.
            return (NextULong() >> 40) * (1f / (1 << 24));
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            ulong span = (ulong)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextULong() % span);
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (NextFloat() * (max - min));
        }

        public Vector2 InsideSquare(float halfExtent)
        {
            return new Vector2(Range(-halfExtent, halfExtent), Range(-halfExtent, halfExtent));
        }

        /// <summary>Picks an index with probability proportional to its weight. Returns -1 if all weights are zero.</summary>
        public int PickWeighted(params float[] weights)
        {
            float total = 0f;
            foreach (float w in weights)
            {
                total += Mathf.Max(0f, w);
            }

            if (total <= 0f)
            {
                return -1;
            }

            float roll = NextFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= Mathf.Max(0f, weights[i]);
                if (roll < 0f)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        public void Shuffle<T>(T[] items)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static ulong SplitMix64(ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }
}

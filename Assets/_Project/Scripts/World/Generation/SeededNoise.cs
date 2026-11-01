using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// 2D gradient (Perlin) noise with a seeded permutation table.
    ///
    /// <see cref="Mathf.PerlinNoise"/> has no seed, so two layers of it can only be decorrelated by
    /// offsetting coordinates. A seeded table keeps every noise layer independent and reproducible.
    /// </summary>
    public sealed class SeededNoise
    {
        // Unit-length gradients at 45-degree steps.
        private static readonly Vector2[] Gradients =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.70710678f, 0.70710678f), new Vector2(-0.70710678f, 0.70710678f),
            new Vector2(0.70710678f, -0.70710678f), new Vector2(-0.70710678f, -0.70710678f),
        };

        // With unit gradients, 2D Perlin peaks at sqrt(0.5); scale it to fill [-1, 1].
        private const float Normalization = 1.41421356f;

        private readonly int[] _permutation = new int[512];

        public SeededNoise(int seed)
        {
            var table = new int[256];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = i;
            }

            new DeterministicRandom(seed).Shuffle(table);

            for (int i = 0; i < 512; i++)
            {
                _permutation[i] = table[i & 255];
            }
        }

        /// <summary>Single-octave noise in [-1, 1]. Zero on integer lattice points.</summary>
        public float Perlin(float x, float y)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float xf = x - xi;
            float yf = y - yi;
            xi &= 255;
            yi &= 255;

            float n00 = Dot(Hash(xi, yi), xf, yf);
            float n10 = Dot(Hash(xi + 1, yi), xf - 1f, yf);
            float n01 = Dot(Hash(xi, yi + 1), xf, yf - 1f);
            float n11 = Dot(Hash(xi + 1, yi + 1), xf - 1f, yf - 1f);

            float u = Fade(xf);
            float v = Fade(yf);

            float value = Mathf.Lerp(Mathf.Lerp(n00, n10, u), Mathf.Lerp(n01, n11, u), v);
            return Mathf.Clamp(value * Normalization, -1f, 1f);
        }

        /// <summary>Fractal sum of octaves, normalized back to [-1, 1].</summary>
        public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float norm = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += Perlin(x * frequency, y * frequency) * amplitude;
                norm += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return norm > 0f ? Mathf.Clamp(sum / norm, -1f, 1f) : 0f;
        }

        /// <summary>Ridged multifractal in [0, 1]: sharp crests, used for mountain ridgelines.</summary>
        public float Ridged(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float norm = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float ridge = 1f - Mathf.Abs(Perlin(x * frequency, y * frequency));
                sum += ridge * ridge * amplitude;
                norm += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return norm > 0f ? Mathf.Clamp01(sum / norm) : 0f;
        }

        private Vector2 Hash(int x, int y)
        {
            return Gradients[_permutation[_permutation[x] + y] & 7];
        }

        private static float Dot(Vector2 g, float x, float y) => (g.x * x) + (g.y * y);

        private static float Fade(float t) => t * t * t * ((t * ((t * 6f) - 15f)) + 10f);
    }
}

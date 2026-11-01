using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// One placed biome area. Most regions are discs around <see cref="Center"/>; the valley is a
    /// capsule from <see cref="Start"/> to <see cref="End"/> with <see cref="Radius"/> as half-width.
    /// </summary>
    public readonly struct BiomeRegion
    {
        public BiomeRegion(BiomeType type, Vector2 center, float radius, float strength = 1f)
            : this(type, center, center, radius, strength)
        {
        }

        public BiomeRegion(BiomeType type, Vector2 start, Vector2 end, float radius, float strength = 1f)
        {
            Type = type;
            Start = start;
            End = end;
            Center = (start + end) * 0.5f;
            Radius = radius;
            Strength = strength;
        }

        public BiomeType Type { get; }

        public Vector2 Center { get; }

        public Vector2 Start { get; }

        public Vector2 End { get; }

        public float Radius { get; }

        /// <summary>Per-region variation multiplier (e.g. mountain peak height).</summary>
        public float Strength { get; }

        public bool IsCapsule => Start != End;

        /// <summary>Distance from <paramref name="p"/> to the region's core (point or segment).</summary>
        public float DistanceTo(Vector2 p)
        {
            if (!IsCapsule)
            {
                return Vector2.Distance(p, Center);
            }

            Vector2 axis = End - Start;
            float t = Mathf.Clamp01(Vector2.Dot(p - Start, axis) / axis.sqrMagnitude);
            return Vector2.Distance(p, Start + (axis * t));
        }

        public override string ToString()
        {
            return IsCapsule
                ? $"{Type} {Start}->{End} r={Radius:0.#}"
                : $"{Type} @{Center} r={Radius:0.#}";
        }
    }
}

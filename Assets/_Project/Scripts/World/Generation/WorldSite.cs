using UnityEngine;

namespace SnowballBattleRoyale.World
{
    public enum SiteKind
    {
        Cabin,
        Well,
        Fence,
        Campfire,
    }

    /// <summary>A placed man-made object: cabin, well, fence segment or campfire.</summary>
    public readonly struct WorldSite
    {
        public WorldSite(
            SiteKind kind,
            Vector2 position,
            float rotationDegrees,
            float footprintRadius,
            int settlementIndex = -1,
            float lifetime = 0f)
        {
            Kind = kind;
            Position = position;
            RotationDegrees = rotationDegrees;
            FootprintRadius = footprintRadius;
            SettlementIndex = settlementIndex;
            Lifetime = lifetime;
        }

        public SiteKind Kind { get; }

        /// <summary>World XZ position (Vector2.y is world Z).</summary>
        public Vector2 Position { get; }

        public float RotationDegrees { get; }

        /// <summary>Radius kept flat and clear of props.</summary>
        public float FootprintRadius { get; }

        /// <summary>Owning settlement, or -1 for scattered sites.</summary>
        public int SettlementIndex { get; }

        /// <summary>Campfires only: seconds until it burns out.</summary>
        public float Lifetime { get; }

        public override string ToString()
        {
            string owner = SettlementIndex >= 0 ? $" settlement {SettlementIndex}" : string.Empty;
            return $"{Kind} @{Position}{owner}";
        }
    }
}

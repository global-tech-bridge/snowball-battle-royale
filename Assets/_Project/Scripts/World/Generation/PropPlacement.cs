using UnityEngine;

namespace SnowballBattleRoyale.World
{
    public enum PropKind
    {
        Pine,
        Birch,
        DeadTree,
        Rock,
        Boulder,
    }

    public static class PropKindExtensions
    {
        public static bool IsTree(this PropKind kind)
        {
            return kind == PropKind.Pine || kind == PropKind.Birch || kind == PropKind.DeadTree;
        }

        public static bool IsRock(this PropKind kind)
        {
            return kind == PropKind.Rock || kind == PropKind.Boulder;
        }
    }

    /// <summary>One scattered tree or rock.</summary>
    public readonly struct PropPlacement
    {
        public PropPlacement(PropKind kind, Vector2 position, float rotationDegrees, float scale)
        {
            Kind = kind;
            Position = position;
            RotationDegrees = rotationDegrees;
            Scale = scale;
        }

        public PropKind Kind { get; }

        /// <summary>World XZ position (Vector2.y is world Z).</summary>
        public Vector2 Position { get; }

        public float RotationDegrees { get; }

        public float Scale { get; }

        public override string ToString() => $"{Kind} @{Position}";
    }
}

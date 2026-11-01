using System;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>Shape and limits of a single dig. All lengths in metres.</summary>
    [Serializable]
    public sealed class DigSettings
    {
        [Tooltip("Horizontal reach of one dig.")]
        public float Radius = 1.2f;

        [Tooltip("Depth removed at the centre of one dig.")]
        public float Amount = 0.3f;

        [Tooltip("Inner fraction of the radius that is dug at full depth.")]
        public float CoreFraction = 0.35f;

        [Tooltip("Deepest the ground can be dug below its original surface.")]
        public float MaxDepth = 2f;

        [Tooltip("Steepest allowed wall, as rise over run (1 = 45 deg). Keeps pits escapable.")]
        public float ReposeSlope = 1f;

        [Tooltip("Depth that hides a crouching player (crouch height is 1.1 m).")]
        public float HideDepth = 1.2f;
    }
}

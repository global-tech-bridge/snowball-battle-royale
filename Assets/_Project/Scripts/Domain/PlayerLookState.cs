namespace SnowballBattleRoyale.Domain
{
    /// <summary>
    /// Accumulated look orientation, kept free of Unity types so the sensitivity and clamping rules
    /// are unit testable.
    ///
    /// <see cref="Yaw"/> is wrapped into 0-360 and applied to the body; <see cref="Pitch"/> is a
    /// Unity Euler X angle (negative looks up) and is clamped to the configured range.
    /// </summary>
    public sealed class PlayerLookState
    {
        private readonly float _minPitch;
        private readonly float _maxPitch;

        public PlayerLookState(float minPitch = -85f, float maxPitch = 85f)
        {
            _minPitch = minPitch;
            _maxPitch = maxPitch;
        }

        public float Yaw { get; private set; }

        public float Pitch { get; private set; }

        /// <summary>
        /// Applies one frame of pointer delta. <paramref name="deltaY"/> is screen-space up, which
        /// maps to a decreasing pitch so that moving the mouse up looks up.
        /// </summary>
        public void Apply(float deltaX, float deltaY, float sensitivity)
        {
            if (sensitivity <= 0f)
            {
                return;
            }

            Yaw = Wrap360(Yaw + (deltaX * sensitivity));
            Pitch = ClampPitch(Pitch - (deltaY * sensitivity));
        }

        public void Reset(float yaw, float pitch)
        {
            Yaw = Wrap360(yaw);
            Pitch = ClampPitch(pitch);
        }

        private float ClampPitch(float value)
        {
            if (value < _minPitch)
            {
                return _minPitch;
            }

            return value > _maxPitch ? _maxPitch : value;
        }

        private static float Wrap360(float degrees)
        {
            float wrapped = degrees % 360f;
            return wrapped < 0f ? wrapped + 360f : wrapped;
        }
    }
}

using UnityEngine;

namespace SnowballBattleRoyale.Player
{
    /// <summary>
    /// Mouse look preferences. Read every frame by <see cref="PlayerLook"/>, so changing the asset
    /// while in Play Mode takes effect immediately.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerLookSettingsSO",
        menuName = "Snowball/Player/Look Settings",
        order = 1)]
    public sealed class PlayerLookSettingsSO : ScriptableObject
    {
        [Tooltip("Degrees of rotation per unit of pointer delta.")]
        [SerializeField, Range(0.01f, 2f)]
        private float _mouseSensitivity = 0.12f;

        [Tooltip("Separate multiplier for gamepad sticks, which report a different delta scale.")]
        [SerializeField, Range(0.1f, 20f)]
        private float _gamepadSensitivity = 4f;

        [SerializeField, Range(-89f, 0f)]
        private float _minPitch = -85f;

        [SerializeField, Range(0f, 89f)]
        private float _maxPitch = 85f;

        [SerializeField]
        private bool _invertY;

        public float MouseSensitivity
        {
            get => _mouseSensitivity;
            set => _mouseSensitivity = Mathf.Clamp(value, 0.01f, 2f);
        }

        public float GamepadSensitivity => _gamepadSensitivity;

        public float MinPitch => _minPitch;

        public float MaxPitch => _maxPitch;

        public bool InvertY => _invertY;
    }
}

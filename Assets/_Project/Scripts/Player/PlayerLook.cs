using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.Player
{
    /// <summary>
    /// First-person look. Yaw turns the body, pitch tilts the camera pivot.
    ///
    /// The angle maths (sensitivity, pitch clamping, yaw wrapping) lives in
    /// <see cref="PlayerLookState"/> so it can be unit tested.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLook : MonoBehaviour
    {
        private const string LogCategory = "Player";

        [SerializeField]
        private PlayerLookSettingsSO _settings;

        [SerializeField]
        private PlayerInputReader _input;

        [Tooltip("Transform the camera is parented to. Receives the pitch rotation.")]
        [SerializeField]
        private Transform _cameraPivot;

        [Tooltip("Camera pivot height for each stance (m).")]
        [SerializeField]
        private float _standingEyeHeight = 1.6f;

        [SerializeField]
        private float _crouchEyeHeight = 0.95f;

        [SerializeField]
        private float _proneEyeHeight = 0.45f;

        [SerializeField, Min(0.01f)]
        private float _eyeHeightTransitionTime = 0.15f;

        [SerializeField]
        private PlayerMovement _movement;

        private PlayerLookState _look;
        private float _eyeHeightVelocity;

        public float Yaw => _look?.Yaw ?? 0f;

        public float Pitch => _look?.Pitch ?? 0f;

        private void Awake()
        {
            if (_settings == null)
            {
                SBLog.Error("No PlayerLookSettingsSO assigned; look is disabled.", this, LogCategory);
                enabled = false;
                return;
            }

            if (_input == null)
            {
                _input = GetComponent<PlayerInputReader>();
            }

            if (_movement == null)
            {
                _movement = GetComponent<PlayerMovement>();
            }

            if (_cameraPivot == null)
            {
                SBLog.Error("No camera pivot assigned; look is disabled.", this, LogCategory);
                enabled = false;
                return;
            }

            _look = new PlayerLookState(_settings.MinPitch, _settings.MaxPitch);
            _look.Reset(transform.eulerAngles.y, 0f);
        }

        private void Update()
        {
            if (_look == null)
            {
                return;
            }

            Vector2 lookInput = _input != null ? _input.Look : Vector2.zero;

            // Read sensitivity every frame so settings changes apply without a restart.
            float sensitivity = _input != null && _input.IsUsingGamepadLook
                ? _settings.GamepadSensitivity * Time.deltaTime
                : _settings.MouseSensitivity;

            float deltaY = _settings.InvertY ? -lookInput.y : lookInput.y;
            _look.Apply(lookInput.x, deltaY, sensitivity);

            transform.localRotation = Quaternion.Euler(0f, _look.Yaw, 0f);
            _cameraPivot.localRotation = Quaternion.Euler(_look.Pitch, 0f, 0f);

            ApplyEyeHeight(Time.deltaTime);
        }

        private void ApplyEyeHeight(float deltaTime)
        {
            float target = _movement == null
                ? _standingEyeHeight
                : _movement.CurrentMode switch
                {
                    MovementMode.Crouch => _crouchEyeHeight,
                    MovementMode.Prone => _proneEyeHeight,
                    _ => _standingEyeHeight,
                };

            Vector3 position = _cameraPivot.localPosition;
            position.y = Mathf.SmoothDamp(
                position.y,
                target,
                ref _eyeHeightVelocity,
                _eyeHeightTransitionTime,
                Mathf.Infinity,
                deltaTime);
            _cameraPivot.localPosition = position;
        }
    }
}

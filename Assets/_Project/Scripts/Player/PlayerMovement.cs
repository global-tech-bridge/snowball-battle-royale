using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.Player
{
    /// <summary>
    /// Drives the <see cref="CharacterController"/> from <see cref="PlayerInputReader"/>.
    ///
    /// Speed selection and stamina live in <see cref="PlayerStats"/> / <see cref="MovementModeResolver"/>;
    /// this component only converts those decisions into motion and collider height.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const string LogCategory = "Player";

        [Header("Configuration")]
        [SerializeField]
        private PlayerStatsSO _statsConfig;

        [SerializeField]
        private PlayerInputReader _input;

        [Header("Collider heights (m)")]
        [SerializeField, Min(0.2f)]
        private float _standingHeight = 1.8f;

        [SerializeField, Min(0.2f)]
        private float _crouchHeight = 1.1f;

        [SerializeField, Min(0.2f)]
        private float _proneHeight = 0.6f;

        [Tooltip("Seconds to blend between stance heights.")]
        [SerializeField, Min(0.01f)]
        private float _stanceTransitionTime = 0.15f;

        [Header("Physics")]
        [SerializeField]
        private float _gravity = -19.62f;

        [Tooltip("Downward velocity kept while grounded, so the controller stays on slopes.")]
        [SerializeField]
        private float _groundedStickVelocity = -2f;

        [Tooltip("Seconds to reach the target speed. 0 is instant.")]
        [SerializeField, Min(0f)]
        private float _acceleration = 0.08f;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _heightVelocity;

        /// <summary>Health / stamina / cold for this player. Other systems read it from here.</summary>
        public PlayerStats Stats { get; private set; }

        public MovementMode CurrentMode { get; private set; } = MovementMode.Walk;

        public float CurrentSpeed => _horizontalVelocity.magnitude;

        public bool IsGrounded => _controller != null && _controller.isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (_statsConfig == null)
            {
                SBLog.Error("No PlayerStatsSO assigned; movement is disabled.", this, LogCategory);
                enabled = false;
                return;
            }

            if (_input == null)
            {
                _input = GetComponent<PlayerInputReader>();
            }

            Stats = new PlayerStats(_statsConfig);
            SetHeightImmediate(_standingHeight);
        }

        private void Update()
        {
            if (Stats == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            Vector2 moveInput = _input != null ? _input.Move : Vector2.zero;
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            bool wantsToMove = moveInput.sqrMagnitude > 0.0001f;

            var intent = new MovementIntent(
                sprintHeld: _input != null && _input.SprintHeld,
                crouch: _input != null && _input.CrouchToggled,
                prone: _input != null && _input.ProneToggled,
                isMoving: wantsToMove,
                canSprint: Stats.CanSprint);

            CurrentMode = MovementModeResolver.Resolve(intent);

            // Stamina is spent on the stance that actually applied, not the one that was requested.
            Stats.Tick(deltaTime, CurrentMode, wantsToMove);

            ApplyStanceHeight(deltaTime);
            ApplyMotion(moveInput, deltaTime);
        }

        private void ApplyMotion(Vector2 moveInput, float deltaTime)
        {
            Vector3 direction = (transform.right * moveInput.x) + (transform.forward * moveInput.y);
            Vector3 targetVelocity = direction * Stats.GetSpeed(CurrentMode);

            _horizontalVelocity = _acceleration <= 0f
                ? targetVelocity
                : Vector3.MoveTowards(
                    _horizontalVelocity,
                    targetVelocity,
                    (Stats.GetSpeed(CurrentMode) / _acceleration) * deltaTime);

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = _groundedStickVelocity;
            }
            else
            {
                _verticalVelocity += _gravity * deltaTime;
            }

            Vector3 motion = _horizontalVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * deltaTime);
        }

        private void ApplyStanceHeight(float deltaTime)
        {
            float target = CurrentMode switch
            {
                MovementMode.Crouch => _crouchHeight,
                MovementMode.Prone => _proneHeight,
                _ => _standingHeight,
            };

            if (Mathf.Approximately(_controller.height, target))
            {
                return;
            }

            float height = Mathf.SmoothDamp(
                _controller.height,
                target,
                ref _heightVelocity,
                _stanceTransitionTime,
                Mathf.Infinity,
                deltaTime);

            SetHeightImmediate(height);
        }

        private void SetHeightImmediate(float height)
        {
            _controller.height = height;

            // Keep the capsule standing on the same ground plane while its height changes.
            _controller.center = new Vector3(0f, height * 0.5f, 0f);
        }
    }
}

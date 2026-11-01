using SnowballBattleRoyale.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowballBattleRoyale.Player
{
    /// <summary>
    /// Single place that touches the Input System. Movement and look read plain values from here,
    /// which keeps the Input System out of the rest of the player code.
    ///
    /// Crouch and prone are toggles: pressing C toggles crouch, Z toggles prone, and entering one
    /// stance clears the other.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string LogCategory = "Input";
        private const string GameplayMapName = "Gameplay";

        [SerializeField]
        private InputActionAsset _actions;

        [Tooltip("Lock and hide the cursor while the gameplay map is active.")]
        [SerializeField]
        private bool _lockCursor = true;

        private InputActionMap _gameplayMap;
        private InputAction _move;
        private InputAction _look;
        private InputAction _sprint;
        private InputAction _crouch;
        private InputAction _prone;

        public Vector2 Move => _move?.ReadValue<Vector2>() ?? Vector2.zero;

        public Vector2 Look => _look?.ReadValue<Vector2>() ?? Vector2.zero;

        public bool SprintHeld => _sprint?.IsPressed() ?? false;

        public bool CrouchToggled { get; private set; }

        public bool ProneToggled { get; private set; }

        /// <summary>True when the most recent look input came from a gamepad stick.</summary>
        public bool IsUsingGamepadLook { get; private set; }

        private void Awake()
        {
            if (_actions == null)
            {
                SBLog.Error("No InputActionAsset assigned; player input is disabled.", this, LogCategory);
                enabled = false;
                return;
            }

            _gameplayMap = _actions.FindActionMap(GameplayMapName, throwIfNotFound: false);
            if (_gameplayMap == null)
            {
                SBLog.Error($"Action map '{GameplayMapName}' not found.", this, LogCategory);
                enabled = false;
                return;
            }

            _move = _gameplayMap.FindAction("Move", throwIfNotFound: false);
            _look = _gameplayMap.FindAction("Look", throwIfNotFound: false);
            _sprint = _gameplayMap.FindAction("Sprint", throwIfNotFound: false);
            _crouch = _gameplayMap.FindAction("Crouch", throwIfNotFound: false);
            _prone = _gameplayMap.FindAction("Prone", throwIfNotFound: false);
        }

        private void OnEnable()
        {
            if (_gameplayMap == null)
            {
                return;
            }

            _gameplayMap.Enable();

            if (_crouch != null)
            {
                _crouch.performed += OnCrouchPerformed;
            }

            if (_prone != null)
            {
                _prone.performed += OnPronePerformed;
            }

            if (_lockCursor)
            {
                SetCursorLocked(true);
            }
        }

        private void OnDisable()
        {
            if (_crouch != null)
            {
                _crouch.performed -= OnCrouchPerformed;
            }

            if (_prone != null)
            {
                _prone.performed -= OnPronePerformed;
            }

            _gameplayMap?.Disable();

            if (_lockCursor)
            {
                SetCursorLocked(false);
            }
        }

        private void Update()
        {
            if (_look == null)
            {
                return;
            }

            // Stick and mouse deltas are on very different scales, so look sensitivity has to know
            // which device produced the current value.
            IsUsingGamepadLook = _look.activeControl?.device is Gamepad;
        }

        private void OnCrouchPerformed(InputAction.CallbackContext _)
        {
            CrouchToggled = !CrouchToggled;
            if (CrouchToggled)
            {
                ProneToggled = false;
            }
        }

        private void OnPronePerformed(InputAction.CallbackContext _)
        {
            ProneToggled = !ProneToggled;
            if (ProneToggled)
            {
                CrouchToggled = false;
            }
        }

        /// <summary>Clears both stance toggles, e.g. on respawn.</summary>
        public void ResetStance()
        {
            CrouchToggled = false;
            ProneToggled = false;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

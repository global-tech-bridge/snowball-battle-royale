using System;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.Player
{
    /// <summary>
    /// Right-click digging. A click digs once where the player is looking; holding the button keeps
    /// digging at <see cref="RepeatInterval"/>, which is how trenches get dug.
    ///
    /// Only terrain can be dug. The actual terrain change lives in <see cref="TerrainDigSystem"/> so
    /// bots (Phase 7) dig through the same path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDigger : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader _input;

        [Tooltip("Ray origin and direction, normally the player camera.")]
        [SerializeField]
        private Transform _aim;

        [Tooltip("How far away the player can dig (m).")]
        [SerializeField, Min(0.5f)]
        private float _reach = 4f;

        [Tooltip("Seconds between digs while the button is held.")]
        [SerializeField, Min(0.05f)]
        private float _repeatInterval = 0.25f;

        [SerializeField]
        private LayerMask _mask = ~0;

        private float _cooldown;

        /// <summary>Raised after each dig that changed the ground.</summary>
        public event Action<DigResult, Vector3> Dug;

        public float Reach => _reach;

        public float RepeatInterval => _repeatInterval;

        public int DigCount { get; private set; }

        private void Awake()
        {
            if (_input == null)
            {
                _input = GetComponent<PlayerInputReader>();
            }

            if (_aim == null)
            {
                Camera playerCamera = GetComponentInChildren<Camera>();
                _aim = playerCamera != null ? playerCamera.transform : transform;
            }
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            if (_input.DigPressedThisFrame)
            {
                TryDig();
                _cooldown = _repeatInterval;
                return;
            }

            if (!_input.DigHeld)
            {
                _cooldown = 0f;
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                TryDig();
                _cooldown += _repeatInterval;
            }
        }

        /// <summary>The terrain point the player would dig right now, if any is in reach.</summary>
        public bool TryGetDigTarget(out RaycastHit hit)
        {
            return Physics.Raycast(_aim.position, _aim.forward, out hit, _reach, _mask, QueryTriggerInteraction.Ignore)
                   && hit.collider is TerrainCollider;
        }

        private void TryDig()
        {
            TerrainDigSystem digSystem = TerrainDigSystem.Active;
            if (digSystem == null || !TryGetDigTarget(out RaycastHit hit))
            {
                return;
            }

            DigResult result = digSystem.Dig(hit.point);
            if (!result.Applied)
            {
                return;
            }

            DigCount++;
            Dug?.Invoke(result, hit.point);
        }
    }
}

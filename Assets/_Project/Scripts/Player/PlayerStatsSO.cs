using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.Player
{
    /// <summary>
    /// Tuning asset behind <see cref="PlayerStats"/>. Defaults match the Phase 2 spec.
    /// Treated as read-only at runtime.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerStatsSO",
        menuName = "Snowball/Player/Player Stats",
        order = 0)]
    public sealed class PlayerStatsSO : ScriptableObject, IPlayerStatsConfig
    {
        [Header("Health")]
        [SerializeField, Min(1f)]
        private float _maxHealth = 100f;

        [Header("Stamina")]
        [SerializeField, Min(1f)]
        private float _maxStamina = 100f;

        [Tooltip("Stamina consumed per second while sprinting.")]
        [SerializeField, Min(0f)]
        private float _sprintStaminaDrainPerSecond = 10f;

        [Tooltip("Stamina restored per second while not sprinting.")]
        [SerializeField, Min(0f)]
        private float _staminaRegenPerSecond = 5f;

        [Tooltip("Stamina that must be recovered after hitting zero before sprinting is allowed again.")]
        [SerializeField, Min(0f)]
        private float _sprintRecoveryStamina = 15f;

        [Header("Cold penalty")]
        [Tooltip("Cold percentage at which the sprint drain penalty starts.")]
        [SerializeField, Range(0f, 100f)]
        private float _coldStaminaPenaltyThreshold = 75f;

        [Tooltip("Sprint drain multiplier once the cold threshold is reached.")]
        [SerializeField, Min(1f)]
        private float _coldStaminaDrainMultiplier = 2f;

        [Header("Movement speeds (m/s)")]
        [SerializeField, Min(0f)]
        private float _walkSpeed = 8f;

        [SerializeField, Min(0f)]
        private float _sprintSpeed = 14f;

        [SerializeField, Min(0f)]
        private float _crouchSpeed = 4f;

        [SerializeField, Min(0f)]
        private float _proneSpeed = 2f;

        public float MaxHealth => _maxHealth;

        public float MaxStamina => _maxStamina;

        public float SprintStaminaDrainPerSecond => _sprintStaminaDrainPerSecond;

        public float StaminaRegenPerSecond => _staminaRegenPerSecond;

        public float ColdStaminaPenaltyThreshold => _coldStaminaPenaltyThreshold;

        public float ColdStaminaDrainMultiplier => _coldStaminaDrainMultiplier;

        public float SprintRecoveryStamina => _sprintRecoveryStamina;

        public float WalkSpeed => _walkSpeed;

        public float SprintSpeed => _sprintSpeed;

        public float CrouchSpeed => _crouchSpeed;

        public float ProneSpeed => _proneSpeed;
    }
}

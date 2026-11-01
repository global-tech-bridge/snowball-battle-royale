using System;

namespace SnowballBattleRoyale.Domain
{
    /// <summary>
    /// Health, stamina and cold for one player. Pure C#: no Unity types, no frame loop — the caller
    /// drives it with <see cref="Tick"/>.
    ///
    /// Cold is stored here but only *consumed* in Phase 2 (it doubles the sprint drain). Phase 5
    /// owns the rules that raise and lower it.
    /// </summary>
    public sealed class PlayerStats
    {
        /// <summary>Cold is expressed as a percentage, so the scale is fixed at 0-100.</summary>
        public const float MaxCold = 100f;

        private readonly IPlayerStatsConfig _config;
        private bool _sprintLockedOut;

        public PlayerStats(IPlayerStatsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Health = _config.MaxHealth;
            Stamina = _config.MaxStamina;
            Cold = 0f;
        }

        public float Health { get; private set; }

        public float Stamina { get; private set; }

        /// <summary>Cold ("悴み") as a percentage, 0-100.</summary>
        public float Cold { get; private set; }

        public float MaxHealth => _config.MaxHealth;

        public float MaxStamina => _config.MaxStamina;

        public float HealthNormalized => Normalize(Health, _config.MaxHealth);

        public float StaminaNormalized => Normalize(Stamina, _config.MaxStamina);

        public float ColdNormalized => Normalize(Cold, MaxCold);

        public bool IsDead => Health <= 0f;

        /// <summary>True after stamina hit zero, until it recovers past the sprint threshold.</summary>
        public bool IsExhausted => _sprintLockedOut;

        public bool CanSprint => !IsDead && !_sprintLockedOut && Stamina > 0f;

        /// <summary>True once cold is high enough to double the sprint drain.</summary>
        public bool IsColdPenaltyActive => Cold >= _config.ColdStaminaPenaltyThreshold;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            Health = Math.Max(0f, Health - amount);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            Health = Math.Min(_config.MaxHealth, Health + amount);
        }

        public void SetCold(float value)
        {
            Cold = Clamp(value, 0f, MaxCold);
        }

        public void AddCold(float delta)
        {
            SetCold(Cold + delta);
        }

        public float GetSpeed(MovementMode mode)
        {
            return mode switch
            {
                MovementMode.Sprint => _config.SprintSpeed,
                MovementMode.Crouch => _config.CrouchSpeed,
                MovementMode.Prone => _config.ProneSpeed,
                _ => _config.WalkSpeed,
            };
        }

        /// <summary>
        /// Advances stamina by <paramref name="deltaTime"/> seconds.
        /// Sprinting drains only while actually moving; everything else regenerates.
        /// </summary>
        public void Tick(float deltaTime, MovementMode mode, bool isMoving)
        {
            if (deltaTime <= 0f || IsDead)
            {
                return;
            }

            bool isSprinting = mode == MovementMode.Sprint && isMoving;

            if (isSprinting)
            {
                float drain = _config.SprintStaminaDrainPerSecond;
                if (IsColdPenaltyActive)
                {
                    drain *= _config.ColdStaminaDrainMultiplier;
                }

                Stamina = Math.Max(0f, Stamina - (drain * deltaTime));

                if (Stamina <= 0f)
                {
                    _sprintLockedOut = true;
                }

                return;
            }

            Stamina = Math.Min(_config.MaxStamina, Stamina + (_config.StaminaRegenPerSecond * deltaTime));

            if (_sprintLockedOut && Stamina >= _config.SprintRecoveryStamina)
            {
                _sprintLockedOut = false;
            }
        }

        private static float Normalize(float value, float max)
        {
            return max <= 0f ? 0f : Clamp(value / max, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}

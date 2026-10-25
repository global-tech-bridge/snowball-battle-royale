namespace SnowballBattleRoyale.Domain
{
    /// <summary>
    /// Tunable values behind <see cref="PlayerStats"/>.
    ///
    /// Implemented by <c>PlayerStatsSO</c> at runtime, and by plain classes in tests, so the rules
    /// can be exercised without the Unity asset pipeline.
    /// </summary>
    public interface IPlayerStatsConfig
    {
        float MaxHealth { get; }

        float MaxStamina { get; }

        /// <summary>Stamina consumed per second while sprinting.</summary>
        float SprintStaminaDrainPerSecond { get; }

        /// <summary>Stamina restored per second while not sprinting.</summary>
        float StaminaRegenPerSecond { get; }

        /// <summary>Cold percentage (0-100) at which the sprint drain penalty kicks in.</summary>
        float ColdStaminaPenaltyThreshold { get; }

        /// <summary>Multiplier applied to the sprint drain once cold reaches the threshold.</summary>
        float ColdStaminaDrainMultiplier { get; }

        /// <summary>
        /// Stamina that must be recovered after hitting zero before sprinting is allowed again.
        /// Without this hysteresis, sprint would flicker on and off at zero stamina.
        /// </summary>
        float SprintRecoveryStamina { get; }

        float WalkSpeed { get; }

        float SprintSpeed { get; }

        float CrouchSpeed { get; }

        float ProneSpeed { get; }
    }
}

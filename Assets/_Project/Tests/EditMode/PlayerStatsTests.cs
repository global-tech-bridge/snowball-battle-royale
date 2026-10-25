using NUnit.Framework;
using SnowballBattleRoyale.Domain;

namespace SnowballBattleRoyale.Tests.EditMode
{
    public sealed class PlayerStatsTests
    {
        /// <summary>Config matching the Phase 2 spec; individual tests override what they care about.</summary>
        private sealed class TestConfig : IPlayerStatsConfig
        {
            public float MaxHealth { get; set; } = 100f;
            public float MaxStamina { get; set; } = 100f;
            public float SprintStaminaDrainPerSecond { get; set; } = 10f;
            public float StaminaRegenPerSecond { get; set; } = 5f;
            public float ColdStaminaPenaltyThreshold { get; set; } = 75f;
            public float ColdStaminaDrainMultiplier { get; set; } = 2f;
            public float SprintRecoveryStamina { get; set; } = 15f;
            public float WalkSpeed { get; set; } = 8f;
            public float SprintSpeed { get; set; } = 14f;
            public float CrouchSpeed { get; set; } = 4f;
            public float ProneSpeed { get; set; } = 2f;
        }

        private TestConfig _config;
        private PlayerStats _stats;

        [SetUp]
        public void SetUp()
        {
            _config = new TestConfig();
            _stats = new PlayerStats(_config);
        }

        // ---------- initial state ----------

        [Test]
        public void NewStats_StartAtFullHealthAndStamina()
        {
            Assert.That(_stats.Health, Is.EqualTo(100f));
            Assert.That(_stats.Stamina, Is.EqualTo(100f));
            Assert.That(_stats.Cold, Is.EqualTo(0f));
            Assert.That(_stats.IsDead, Is.False);
        }

        [Test]
        public void Constructor_ThrowsWhenConfigIsNull()
        {
            Assert.That(() => new PlayerStats(null), Throws.ArgumentNullException);
        }

        // ---------- health ----------

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            _stats.TakeDamage(30f);

            Assert.That(_stats.Health, Is.EqualTo(70f));
        }

        [Test]
        public void TakeDamage_ClampsAtZeroAndMarksDead()
        {
            _stats.TakeDamage(150f);

            Assert.That(_stats.Health, Is.EqualTo(0f));
            Assert.That(_stats.IsDead, Is.True);
        }

        [Test]
        public void TakeDamage_IgnoresNegativeAmounts()
        {
            _stats.TakeDamage(-10f);

            Assert.That(_stats.Health, Is.EqualTo(100f));
        }

        [Test]
        public void Heal_RestoresHealthButNotAboveMax()
        {
            _stats.TakeDamage(50f);

            _stats.Heal(80f);

            Assert.That(_stats.Health, Is.EqualTo(100f));
        }

        [Test]
        public void Heal_DoesNothingOnceDead()
        {
            _stats.TakeDamage(100f);

            _stats.Heal(50f);

            Assert.That(_stats.Health, Is.EqualTo(0f));
            Assert.That(_stats.IsDead, Is.True);
        }

        [Test]
        public void HealthNormalized_IsTheFractionOfMaxHealth()
        {
            _stats.TakeDamage(25f);

            Assert.That(_stats.HealthNormalized, Is.EqualTo(0.75f).Within(1e-4f));
        }

        // ---------- stamina: sprint drain ----------

        [Test]
        public void Sprinting_DrainsStaminaAtTheConfiguredRate()
        {
            _stats.Tick(1f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(90f).Within(1e-4f));
        }

        [Test]
        public void Sprinting_DrainScalesWithDeltaTime()
        {
            _stats.Tick(0.5f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(95f).Within(1e-4f));
        }

        [Test]
        public void SprintMode_WithoutMovingDoesNotDrainStamina()
        {
            _stats.Tick(1f, MovementMode.Sprint, isMoving: false);

            Assert.That(_stats.Stamina, Is.EqualTo(100f));
        }

        [Test]
        public void Stamina_NeverDropsBelowZero()
        {
            _stats.Tick(20f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(0f));
        }

        // ---------- stamina: regeneration ----------

        [Test]
        public void Walking_RegeneratesStamina()
        {
            _stats.Tick(5f, MovementMode.Sprint, isMoving: true);   // 100 -> 50

            _stats.Tick(2f, MovementMode.Walk, isMoving: true);     // +10

            Assert.That(_stats.Stamina, Is.EqualTo(60f).Within(1e-4f));
        }

        [Test]
        public void StandingStill_RegeneratesStamina()
        {
            _stats.Tick(5f, MovementMode.Sprint, isMoving: true);

            _stats.Tick(1f, MovementMode.Walk, isMoving: false);

            Assert.That(_stats.Stamina, Is.EqualTo(55f).Within(1e-4f));
        }

        [Test]
        public void Stamina_NeverExceedsMaximum()
        {
            _stats.Tick(100f, MovementMode.Walk, isMoving: false);

            Assert.That(_stats.Stamina, Is.EqualTo(100f));
        }

        // ---------- cold penalty ----------

        [Test]
        public void ColdAtOrAboveThreshold_DoublesSprintDrain()
        {
            _stats.SetCold(75f);

            _stats.Tick(1f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(80f).Within(1e-4f));
        }

        [Test]
        public void ColdJustBelowThreshold_UsesTheNormalDrain()
        {
            _stats.SetCold(74.9f);

            _stats.Tick(1f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void ColdPenalty_DoesNotAffectRegeneration()
        {
            _stats.SetCold(100f);
            _stats.Tick(5f, MovementMode.Sprint, isMoving: true);   // drains at 20/s -> 0
            Assume.That(_stats.Stamina, Is.EqualTo(0f));

            _stats.Tick(2f, MovementMode.Walk, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void SetCold_ClampsToZeroHundredRange()
        {
            _stats.SetCold(130f);
            Assert.That(_stats.Cold, Is.EqualTo(100f));

            _stats.SetCold(-20f);
            Assert.That(_stats.Cold, Is.EqualTo(0f));
        }

        [Test]
        public void AddCold_AccumulatesAndClamps()
        {
            _stats.AddCold(40f);
            _stats.AddCold(40f);

            Assert.That(_stats.Cold, Is.EqualTo(80f).Within(1e-4f));

            _stats.AddCold(50f);
            Assert.That(_stats.Cold, Is.EqualTo(100f));
        }

        [Test]
        public void ColdNormalized_IsTheFractionOfTheFullRange()
        {
            _stats.SetCold(40f);

            Assert.That(_stats.ColdNormalized, Is.EqualTo(0.4f).Within(1e-4f));
        }

        // ---------- sprint availability / exhaustion ----------

        [Test]
        public void CanSprint_IsTrueWhileStaminaRemains()
        {
            Assert.That(_stats.CanSprint, Is.True);
        }

        [Test]
        public void CanSprint_BecomesFalseWhenStaminaIsExhausted()
        {
            _stats.Tick(20f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.CanSprint, Is.False);
            Assert.That(_stats.IsExhausted, Is.True);
        }

        [Test]
        public void AfterExhaustion_SprintStaysBlockedUntilTheRecoveryThreshold()
        {
            _stats.Tick(20f, MovementMode.Sprint, isMoving: true);  // exhausted at 0

            _stats.Tick(1f, MovementMode.Walk, isMoving: false);    // +5 -> 5, still below 15

            Assert.That(_stats.Stamina, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(_stats.CanSprint, Is.False);
        }

        [Test]
        public void AfterExhaustion_SprintIsAllowedAgainAtTheRecoveryThreshold()
        {
            _stats.Tick(20f, MovementMode.Sprint, isMoving: true);

            _stats.Tick(3f, MovementMode.Walk, isMoving: false);    // +15 -> 15

            Assert.That(_stats.CanSprint, Is.True);
            Assert.That(_stats.IsExhausted, Is.False);
        }

        [Test]
        public void Exhaustion_DoesNotTriggerFromNormalDrainAboveZero()
        {
            _stats.Tick(1f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.IsExhausted, Is.False);
        }

        // ---------- speeds ----------

        [Test]
        public void GetSpeed_ReturnsTheConfiguredSpeedForEachMode()
        {
            Assert.That(_stats.GetSpeed(MovementMode.Walk), Is.EqualTo(8f));
            Assert.That(_stats.GetSpeed(MovementMode.Sprint), Is.EqualTo(14f));
            Assert.That(_stats.GetSpeed(MovementMode.Crouch), Is.EqualTo(4f));
            Assert.That(_stats.GetSpeed(MovementMode.Prone), Is.EqualTo(2f));
        }

        [Test]
        public void Tick_IgnoresNonPositiveDeltaTime()
        {
            _stats.Tick(0f, MovementMode.Sprint, isMoving: true);
            _stats.Tick(-1f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(100f));
        }

        [Test]
        public void Tick_DoesNothingOnceDead()
        {
            _stats.TakeDamage(100f);
            float staminaAtDeath = _stats.Stamina;

            _stats.Tick(5f, MovementMode.Sprint, isMoving: true);

            Assert.That(_stats.Stamina, Is.EqualTo(staminaAtDeath));
        }
    }
}

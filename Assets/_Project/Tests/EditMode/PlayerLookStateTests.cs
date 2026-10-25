using NUnit.Framework;
using SnowballBattleRoyale.Domain;

namespace SnowballBattleRoyale.Tests.EditMode
{
    public sealed class PlayerLookStateTests
    {
        private PlayerLookState _look;

        [SetUp]
        public void SetUp() => _look = new PlayerLookState(minPitch: -85f, maxPitch: 85f);

        [Test]
        public void NewState_StartsLookingStraightAhead()
        {
            Assert.That(_look.Yaw, Is.EqualTo(0f));
            Assert.That(_look.Pitch, Is.EqualTo(0f));
        }

        [Test]
        public void MouseDeltaX_TurnsYawRight()
        {
            _look.Apply(deltaX: 10f, deltaY: 0f, sensitivity: 1f);

            Assert.That(_look.Yaw, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void MouseDeltaY_PitchesUpWhenMovedUp()
        {
            _look.Apply(deltaX: 0f, deltaY: 10f, sensitivity: 1f);

            // Screen-space up must look up, so the stored pitch (a negative Euler X) goes down.
            Assert.That(_look.Pitch, Is.EqualTo(-10f).Within(1e-4f));
        }

        [Test]
        public void Sensitivity_ScalesBothAxes()
        {
            _look.Apply(deltaX: 10f, deltaY: 10f, sensitivity: 2.5f);

            Assert.That(_look.Yaw, Is.EqualTo(25f).Within(1e-4f));
            Assert.That(_look.Pitch, Is.EqualTo(-25f).Within(1e-4f));
        }

        [Test]
        public void Pitch_IsClampedLookingUp()
        {
            _look.Apply(deltaX: 0f, deltaY: 500f, sensitivity: 1f);

            Assert.That(_look.Pitch, Is.EqualTo(-85f).Within(1e-4f));
        }

        [Test]
        public void Pitch_IsClampedLookingDown()
        {
            _look.Apply(deltaX: 0f, deltaY: -500f, sensitivity: 1f);

            Assert.That(_look.Pitch, Is.EqualTo(85f).Within(1e-4f));
        }

        [Test]
        public void Yaw_IsNotClampedAndWrapsIntoZeroToThreeSixty()
        {
            _look.Apply(deltaX: 400f, deltaY: 0f, sensitivity: 1f);

            Assert.That(_look.Yaw, Is.EqualTo(40f).Within(1e-4f));
        }

        [Test]
        public void Yaw_WrapsWhenTurningPastZero()
        {
            _look.Apply(deltaX: -30f, deltaY: 0f, sensitivity: 1f);

            Assert.That(_look.Yaw, Is.EqualTo(330f).Within(1e-4f));
        }

        [Test]
        public void Apply_AccumulatesAcrossCalls()
        {
            _look.Apply(10f, 5f, 1f);
            _look.Apply(10f, 5f, 1f);

            Assert.That(_look.Yaw, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(_look.Pitch, Is.EqualTo(-10f).Within(1e-4f));
        }

        [Test]
        public void Apply_IgnoresNegativeSensitivity()
        {
            _look.Apply(10f, 10f, -3f);

            Assert.That(_look.Yaw, Is.EqualTo(0f));
            Assert.That(_look.Pitch, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_ReturnsToTheGivenOrientation()
        {
            _look.Apply(90f, 30f, 1f);

            _look.Reset(yaw: 180f, pitch: 0f);

            Assert.That(_look.Yaw, Is.EqualTo(180f).Within(1e-4f));
            Assert.That(_look.Pitch, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_ClampsThePitchItIsGiven()
        {
            _look.Reset(yaw: 0f, pitch: -300f);

            Assert.That(_look.Pitch, Is.EqualTo(-85f).Within(1e-4f));
        }
    }
}

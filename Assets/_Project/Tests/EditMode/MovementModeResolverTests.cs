using NUnit.Framework;
using SnowballBattleRoyale.Domain;

namespace SnowballBattleRoyale.Tests.EditMode
{
    public sealed class MovementModeResolverTests
    {
        private static MovementIntent Intent(
            bool sprint = false,
            bool crouch = false,
            bool prone = false,
            bool moving = true,
            bool canSprint = true)
        {
            return new MovementIntent(sprint, crouch, prone, moving, canSprint);
        }

        [Test]
        public void NoModifiers_ResolvesToWalk()
        {
            Assert.That(MovementModeResolver.Resolve(Intent()), Is.EqualTo(MovementMode.Walk));
        }

        [Test]
        public void SprintHeldWhileMoving_ResolvesToSprint()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(sprint: true)),
                Is.EqualTo(MovementMode.Sprint));
        }

        [Test]
        public void SprintHeldWhileStandingStill_ResolvesToWalk()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(sprint: true, moving: false)),
                Is.EqualTo(MovementMode.Walk));
        }

        [Test]
        public void SprintWithoutStamina_ResolvesToWalk()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(sprint: true, canSprint: false)),
                Is.EqualTo(MovementMode.Walk));
        }

        [Test]
        public void Crouch_ResolvesToCrouch()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(crouch: true)),
                Is.EqualTo(MovementMode.Crouch));
        }

        [Test]
        public void Prone_ResolvesToProne()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(prone: true)),
                Is.EqualTo(MovementMode.Prone));
        }

        [Test]
        public void Prone_WinsOverCrouch()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(crouch: true, prone: true)),
                Is.EqualTo(MovementMode.Prone));
        }

        [Test]
        public void Prone_WinsOverSprint()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(sprint: true, prone: true)),
                Is.EqualTo(MovementMode.Prone));
        }

        [Test]
        public void Crouch_WinsOverSprint()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(sprint: true, crouch: true)),
                Is.EqualTo(MovementMode.Crouch));
        }

        [Test]
        public void Crouch_AppliesEvenWhenStandingStill()
        {
            Assert.That(
                MovementModeResolver.Resolve(Intent(crouch: true, moving: false)),
                Is.EqualTo(MovementMode.Crouch));
        }
    }
}

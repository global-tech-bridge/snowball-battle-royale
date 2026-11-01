using NUnit.Framework;
using SnowballBattleRoyale.World;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class CampfireStateTests
    {
        [Test]
        public void NewFire_IsLitWithItsFullLifetime()
        {
            var fire = new CampfireState(120f);

            Assert.That(fire.IsLit, Is.True);
            Assert.That(fire.Remaining, Is.EqualTo(120f));
            Assert.That(fire.Remaining01, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_BurnsDownTheRemainingTime()
        {
            var fire = new CampfireState(120f);

            fire.Tick(30f);

            Assert.That(fire.Remaining, Is.EqualTo(90f).Within(1e-4f));
            Assert.That(fire.Remaining01, Is.EqualTo(0.75f).Within(1e-4f));
        }

        [Test]
        public void Fire_GoesOutWhenItsTimeRunsOut()
        {
            var fire = new CampfireState(10f);

            fire.Tick(10f);

            Assert.That(fire.IsLit, Is.False);
            Assert.That(fire.Remaining, Is.EqualTo(0f));
        }

        [Test]
        public void BurnedOutFire_StaysOut()
        {
            var fire = new CampfireState(10f);
            fire.Tick(15f);

            fire.Tick(1f);

            Assert.That(fire.IsLit, Is.False);
            Assert.That(fire.Remaining, Is.EqualTo(0f));
        }

        [Test]
        public void Extinguish_PutsTheFireOutImmediately()
        {
            var fire = new CampfireState(120f);

            fire.Extinguish();

            Assert.That(fire.IsLit, Is.False);
        }

        [Test]
        public void WentOut_IsRaisedExactlyOnce()
        {
            var fire = new CampfireState(5f);
            int raised = 0;
            fire.WentOut += () => raised++;

            fire.Tick(3f);
            fire.Tick(3f);
            fire.Tick(3f);
            fire.Extinguish();

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void EndlessFire_NeverBurnsOut()
        {
            var fire = new CampfireState(CampfireState.Endless);

            fire.Tick(100000f);

            Assert.That(fire.IsLit, Is.True);
            Assert.That(fire.Remaining01, Is.EqualTo(1f));
        }

        [Test]
        public void NegativeDeltaTime_IsIgnored()
        {
            var fire = new CampfireState(60f);

            fire.Tick(-10f);

            Assert.That(fire.Remaining, Is.EqualTo(60f));
        }
    }
}

using System;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Burn-down state of one campfire. Pure, so the timing rules are unit tested; the
    /// <c>Campfire</c> component only mirrors it into light and particles.
    /// </summary>
    public sealed class CampfireState
    {
        /// <summary>Lifetime value for a fire that never burns out on its own.</summary>
        public const float Endless = float.PositiveInfinity;

        public CampfireState(float lifetime)
        {
            Lifetime = lifetime > 0f ? lifetime : Endless;
            Remaining = Lifetime;
            IsLit = true;
        }

        /// <summary>Raised once, when the fire burns out or is extinguished.</summary>
        public event Action WentOut;

        public float Lifetime { get; }

        public float Remaining { get; private set; }

        public bool IsLit { get; private set; }

        public bool IsEndless => float.IsPositiveInfinity(Lifetime);

        /// <summary>Fraction of fuel left, 0-1. Endless fires report 1 while lit.</summary>
        public float Remaining01
        {
            get
            {
                if (!IsLit)
                {
                    return 0f;
                }

                return IsEndless ? 1f : Remaining / Lifetime;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsLit || deltaTime <= 0f || IsEndless)
            {
                return;
            }

            Remaining = Math.Max(0f, Remaining - deltaTime);
            if (Remaining <= 0f)
            {
                GoOut();
            }
        }

        public void Extinguish()
        {
            if (IsLit)
            {
                GoOut();
            }
        }

        private void GoOut()
        {
            IsLit = false;
            Remaining = 0f;
            WentOut?.Invoke();
        }
    }
}

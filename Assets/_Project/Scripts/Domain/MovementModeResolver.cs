namespace SnowballBattleRoyale.Domain
{
    /// <summary>What the player is asking for this frame, before stance rules are applied.</summary>
    public readonly struct MovementIntent
    {
        public MovementIntent(bool sprintHeld, bool crouch, bool prone, bool isMoving, bool canSprint)
        {
            SprintHeld = sprintHeld;
            Crouch = crouch;
            Prone = prone;
            IsMoving = isMoving;
            CanSprint = canSprint;
        }

        public bool SprintHeld { get; }

        public bool Crouch { get; }

        public bool Prone { get; }

        public bool IsMoving { get; }

        /// <summary>False when stamina is exhausted; sprint requests fall back to walking.</summary>
        public bool CanSprint { get; }
    }

    /// <summary>
    /// Turns a <see cref="MovementIntent"/> into the stance that actually applies.
    ///
    /// Priority: Prone &gt; Crouch &gt; Sprint &gt; Walk. Stances win over sprint because you cannot
    /// sprint while crouched or prone.
    /// </summary>
    public static class MovementModeResolver
    {
        public static MovementMode Resolve(MovementIntent intent)
        {
            if (intent.Prone)
            {
                return MovementMode.Prone;
            }

            if (intent.Crouch)
            {
                return MovementMode.Crouch;
            }

            if (intent.SprintHeld && intent.IsMoving && intent.CanSprint)
            {
                return MovementMode.Sprint;
            }

            return MovementMode.Walk;
        }
    }
}

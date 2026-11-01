using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// A cabin players can enter. Phase 6 looks cabins up through <see cref="All"/> to offer item
    /// pickups at <see cref="InteractionPoint"/> and toggles the <see cref="Beacon"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Cabin : MonoBehaviour
    {
        private static readonly List<Cabin> Cabins = new List<Cabin>();

        [SerializeField]
        private CabinBeacon _beacon;

        [Tooltip("Where a player stands to loot the cabin.")]
        [SerializeField]
        private Transform _interactionPoint;

        public static IReadOnlyList<Cabin> All => Cabins;

        public CabinBeacon Beacon => _beacon;

        public Transform InteractionPoint => _interactionPoint != null ? _interactionPoint : transform;

        private void OnEnable() => Cabins.Add(this);

        private void OnDisable() => Cabins.Remove(this);
    }
}

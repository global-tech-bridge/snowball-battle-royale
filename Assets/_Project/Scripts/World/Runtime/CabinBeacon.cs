using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Green light pillar above a cabin, visible across the map. Phase 6 hides it once the cabin has
    /// been looted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CabinBeacon : MonoBehaviour
    {
        [SerializeField]
        private GameObject _pillar;

        public bool IsVisible => _pillar != null && _pillar.activeSelf;

        public void SetVisible(bool visible)
        {
            if (_pillar != null)
            {
                _pillar.SetActive(visible);
            }
        }
    }
}

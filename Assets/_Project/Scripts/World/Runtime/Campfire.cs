using System.Collections.Generic;
using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// A campfire in the world. Burns for a set time (see <see cref="CampfireState"/>), then goes out:
    /// the light and flames turn off and it leaves <see cref="Active"/>.
    ///
    /// Phase 5 reads <see cref="Active"/> and <see cref="WarmthRadius"/> for warming; Phase 8 drives
    /// the burn-out schedule from the weather phase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Campfire : MonoBehaviour
    {
        private const string LogCategory = "World";

        private static readonly List<Campfire> ActiveFires = new List<Campfire>();

        [SerializeField]
        private Light _light;

        [SerializeField]
        private ParticleSystem _flames;

        [Tooltip("Players within this distance are warmed (m).")]
        [SerializeField, Min(0f)]
        private float _warmthRadius = 8f;

        [Tooltip("Burn time when placed by hand rather than by the world generator. 0 burns forever.")]
        [SerializeField, Min(0f)]
        private float _defaultLifetime;

        [SerializeField, Min(0f)]
        private float _baseIntensity = 2.5f;

        private float _flickerSeed;

        /// <summary>Campfires that are currently burning.</summary>
        public static IReadOnlyList<Campfire> Active => ActiveFires;

        public CampfireState State { get; private set; }

        public bool IsLit => State != null && State.IsLit;

        public float WarmthRadius => _warmthRadius;

        private void Awake()
        {
            _flickerSeed = Random.value * 100f;
            if (State == null)
            {
                Initialize(_defaultLifetime);
            }
        }

        /// <summary>(Re)lights the fire with a fresh burn time. 0 or less burns forever.</summary>
        public void Initialize(float lifetime)
        {
            if (State != null)
            {
                State.WentOut -= OnWentOut;
            }

            State = new CampfireState(lifetime);
            State.WentOut += OnWentOut;
            SetVisualsLit(true);

            if (isActiveAndEnabled && !ActiveFires.Contains(this))
            {
                ActiveFires.Add(this);
            }
        }

        public void Extinguish() => State?.Extinguish();

        private void OnEnable()
        {
            if (IsLit && !ActiveFires.Contains(this))
            {
                ActiveFires.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveFires.Remove(this);
        }

        private void Update()
        {
            if (State == null || !State.IsLit)
            {
                return;
            }

            State.Tick(Time.deltaTime);

            if (_light != null && State.IsLit)
            {
                float flicker = Mathf.PerlinNoise(_flickerSeed, Time.time * 3f);
                float fuel = 0.4f + (0.6f * State.Remaining01);
                _light.intensity = _baseIntensity * (0.8f + (0.2f * flicker)) * fuel;
            }
        }

        private void OnWentOut()
        {
            ActiveFires.Remove(this);
            SetVisualsLit(false);
            SBLog.Info($"Campfire at {transform.position} went out.", this, LogCategory);
        }

        private void SetVisualsLit(bool lit)
        {
            if (_light != null)
            {
                _light.enabled = lit;
            }

            if (_flames == null)
            {
                return;
            }

            if (lit)
            {
                _flames.Play(true);
            }
            else
            {
                _flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}

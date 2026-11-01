using System;
using System.Collections.Generic;
using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Applies digs to a Unity <see cref="Terrain"/>: lowers the heightmap (which also updates the
    /// collider) and repaints the dug patch so deeper ground shows dirt, then deep dirt.
    ///
    /// Dig depth is tracked in a <see cref="DigField"/> on the heightmap grid and always applied as an
    /// offset from the generated base heights, so repeated digs never compound rounding errors.
    /// Any digger (player, and later bots) goes through <see cref="Dig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerrainDigSystem : MonoBehaviour
    {
        private const string LogCategory = "Dig";

        private TerrainData _data;
        private DigField _field;
        private BiomeMap _biomes;
        private SoilSettings _soil;
        private float[,] _baseHeights;
        private float _terrainHeight;
        private float _terrainSize;
        private Vector2 _origin;
        private readonly float[] _weights = new float[TerrainPaintRules.LayerCount];

        /// <summary>The dig system of the current world, or null if none is loaded.</summary>
        public static TerrainDigSystem Active { get; private set; }

        /// <summary>Raised after every dig that changed the ground, with the targeted world point.</summary>
        public event Action<DigResult, Vector3> Dug;

        public DigField Field => _field;

        public DigSettings Settings => _field?.Settings;

        public bool IsReady => _field != null;

        /// <param name="terrain">Terrain to dig. Its current heights become the undug surface.</param>
        /// <param name="biomes">Used to repaint dug ground; null paints plain snow.</param>
        /// <param name="locks">Areas that cannot be dug (e.g. under cabins).</param>
        public void Initialize(
            Terrain terrain,
            BiomeMap biomes,
            DigSettings digSettings,
            SoilSettings soilSettings,
            IEnumerable<(Vector2 Center, float Radius)> locks = null)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            _data = terrain.terrainData;
            _biomes = biomes;
            _soil = soilSettings ?? new SoilSettings();

            int resolution = _data.heightmapResolution;
            _terrainSize = _data.size.x;
            _terrainHeight = _data.size.y;
            _origin = new Vector2(terrain.transform.position.x, terrain.transform.position.z);

            float cellSize = _terrainSize / (resolution - 1);
            _field = new DigField(resolution, resolution, cellSize, _origin, digSettings ?? new DigSettings());
            _baseHeights = _data.GetHeights(0, 0, resolution, resolution);

            if (locks != null)
            {
                foreach ((Vector2 center, float radius) in locks)
                {
                    _field.Lock(center, radius);
                }
            }

            if (_data.alphamapLayers < TerrainPaintRules.LayerCount)
            {
                SBLog.Warn(
                    $"Terrain has {_data.alphamapLayers} layers but digging paints {TerrainPaintRules.LayerCount}; " +
                    "dug ground will not change colour.",
                    this,
                    LogCategory);
            }

            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        /// <summary>How deep the ground has been dug at a world point (m).</summary>
        public float DepthAt(Vector3 worldPoint)
        {
            return _field?.SampleDepth(new Vector2(worldPoint.x, worldPoint.z)) ?? 0f;
        }

        /// <summary>
        /// Digs at <paramref name="worldPoint"/>. Scales let tools such as the shovel (Phase 6) dig
        /// wider or deeper without changing the base settings.
        /// </summary>
        public DigResult Dig(Vector3 worldPoint, float radiusScale = 1f, float amountScale = 1f)
        {
            if (_field == null)
            {
                return DigResult.None;
            }

            DigSettings settings = _field.Settings;
            DigResult result = _field.Dig(
                new Vector2(worldPoint.x, worldPoint.z),
                settings.Radius * radiusScale,
                settings.Amount * amountScale);

            if (!result.Applied)
            {
                return result;
            }

            ApplyHeights(result);
            ApplyPaint(result);
            Dug?.Invoke(result, worldPoint);
            return result;
        }

        private void ApplyHeights(DigResult result)
        {
            var patch = new float[result.Depth, result.Width];
            float inverseHeight = 1f / _terrainHeight;

            for (int z = 0; z < result.Depth; z++)
            {
                for (int x = 0; x < result.Width; x++)
                {
                    int gx = result.MinX + x;
                    int gz = result.MinZ + z;
                    float lowered = _baseHeights[gz, gx] - (_field.DepthAt(gx, gz) * inverseHeight);
                    patch[z, x] = Mathf.Max(0f, lowered);
                }
            }

            // SetHeights (not the DelayLOD variant) so the collider updates now and the player
            // standing in the trench is not left floating on the old surface.
            _data.SetHeights(result.MinX, result.MinZ, patch);
        }

        private void ApplyPaint(DigResult result)
        {
            int layers = _data.alphamapLayers;
            if (layers < TerrainPaintRules.LayerCount)
            {
                return;
            }

            int resolution = _data.alphamapResolution;
            float cell = _field.CellSize;

            // Grow by one grid cell so the interpolated depth at the patch edge is repainted too.
            float worldMinX = _origin.x + ((result.MinX - 1) * cell);
            float worldMinZ = _origin.y + ((result.MinZ - 1) * cell);
            float worldMaxX = _origin.x + ((result.MaxX + 1) * cell);
            float worldMaxZ = _origin.y + ((result.MaxZ + 1) * cell);

            int ax0 = ToAlphamap(worldMinX - _origin.x, resolution, floor: true);
            int az0 = ToAlphamap(worldMinZ - _origin.y, resolution, floor: true);
            int ax1 = ToAlphamap(worldMaxX - _origin.x, resolution, floor: false);
            int az1 = ToAlphamap(worldMaxZ - _origin.y, resolution, floor: false);

            var patch = new float[az1 - az0 + 1, ax1 - ax0 + 1, layers];
            float texel = _terrainSize / resolution;

            for (int az = az0; az <= az1; az++)
            {
                for (int ax = ax0; ax <= ax1; ax++)
                {
                    var p = new Vector2(_origin.x + ((ax + 0.5f) * texel), _origin.y + ((az + 0.5f) * texel));
                    TerrainPaintRules.Evaluate(_biomes, p, _field.SampleDepth(p), _soil, _weights);

                    for (int l = 0; l < TerrainPaintRules.LayerCount; l++)
                    {
                        patch[az - az0, ax - ax0, l] = _weights[l];
                    }
                }
            }

            _data.SetAlphamaps(ax0, az0, patch);
        }

        private int ToAlphamap(float offset, int resolution, bool floor)
        {
            float t = offset / _terrainSize * resolution;
            int index = floor ? Mathf.FloorToInt(t) : Mathf.CeilToInt(t);
            return Mathf.Clamp(index, 0, resolution - 1);
        }
    }
}

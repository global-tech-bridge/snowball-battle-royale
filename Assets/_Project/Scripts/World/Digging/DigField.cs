using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// How deep the ground has been dug at each point of a regular grid (the terrain heightmap grid).
    ///
    /// A dig lowers a soft-edged disc. Afterwards an angle-of-repose pass deepens any neighbour that
    /// would leave a wall steeper than <see cref="DigSettings.ReposeSlope"/> — loose snow slumps — so
    /// trenches widen as they deepen and a player can always walk back out (the controller's slope
    /// limit is 50 degrees and there is no jump yet).
    /// </summary>
    public sealed class DigField
    {
        private const float Epsilon = 1e-5f;
        private static readonly float Sqrt2 = Mathf.Sqrt(2f);

        private readonly float[] _depth;
        private readonly bool[] _locked;
        private readonly DigSettings _settings;
        private readonly Queue<int> _pending = new Queue<int>();

        public DigField(int width, int height, float cellSize, Vector2 origin, DigSettings settings)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Field must be at least 1x1.");
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            }

            Width = width;
            Height = height;
            CellSize = cellSize;
            Origin = origin;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _depth = new float[width * height];
            _locked = new bool[width * height];
        }

        public int Width { get; }

        public int Height { get; }

        public float CellSize { get; }

        /// <summary>World XZ position of grid point (0, 0).</summary>
        public Vector2 Origin { get; }

        public DigSettings Settings => _settings;

        public float DepthAt(int x, int z) => _depth[Index(x, z)];

        public bool IsLocked(int x, int z) => _locked[Index(x, z)];

        public Vector2 CellToWorld(int x, int z) => Origin + (new Vector2(x, z) * CellSize);

        public Vector2Int WorldToCell(Vector2 world)
        {
            Vector2 g = (world - Origin) / CellSize;
            return new Vector2Int(Mathf.RoundToInt(g.x), Mathf.RoundToInt(g.y));
        }

        /// <summary>Bilinearly interpolated dig depth at a world point (clamped to the field).</summary>
        public float SampleDepth(Vector2 world)
        {
            float fx = Mathf.Clamp((world.x - Origin.x) / CellSize, 0f, Width - 1);
            float fz = Mathf.Clamp((world.y - Origin.y) / CellSize, 0f, Height - 1);

            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);
            int x1 = Mathf.Min(x0 + 1, Width - 1);
            int z1 = Mathf.Min(z0 + 1, Height - 1);
            float tx = fx - x0;
            float tz = fz - z0;

            float bottom = Mathf.Lerp(DepthAt(x0, z0), DepthAt(x1, z0), tx);
            float top = Mathf.Lerp(DepthAt(x0, z1), DepthAt(x1, z1), tx);
            return Mathf.Lerp(bottom, top, tz);
        }

        public float MaxDepthInField()
        {
            float max = 0f;
            foreach (float d in _depth)
            {
                max = Mathf.Max(max, d);
            }

            return max;
        }

        /// <summary>Makes every grid point within <paramref name="radius"/> undiggable (e.g. under a cabin).</summary>
        public void Lock(Vector2 center, float radius)
        {
            if (!TryGetRange(center, radius, out int minX, out int minZ, out int maxX, out int maxZ))
            {
                return;
            }

            float radiusSq = radius * radius;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if ((CellToWorld(x, z) - center).sqrMagnitude <= radiusSq)
                    {
                        _locked[Index(x, z)] = true;
                    }
                }
            }
        }

        /// <summary>
        /// Digs a disc of <paramref name="radius"/> metres around world XZ <paramref name="center"/>,
        /// removing up to <paramref name="amount"/> metres at its core.
        /// </summary>
        public DigResult Dig(Vector2 center, float radius, float amount)
        {
            if (amount <= 0f || radius <= 0f
                || !TryGetRange(center, radius, out int minX, out int minZ, out int maxX, out int maxZ))
            {
                return DigResult.None;
            }

            var dirty = new DirtyRect();
            float removed = 0f;
            float cellArea = CellSize * CellSize;
            float core = radius * Mathf.Clamp01(_settings.CoreFraction);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(CellToWorld(x, z), center);
                    int index = Index(x, z);
                    if (distance > radius || _locked[index])
                    {
                        continue;
                    }

                    float falloff = 1f - BiomeMap.SmoothStep(core, radius, distance);
                    float target = Mathf.Min(_settings.MaxDepth, _depth[index] + (amount * falloff));
                    if (target <= _depth[index] + Epsilon)
                    {
                        continue;
                    }

                    removed += (target - _depth[index]) * cellArea;
                    _depth[index] = target;
                    dirty.Include(x, z);
                    _pending.Enqueue(index);
                }
            }

            removed += RelaxSlopes(ref dirty, cellArea);

            return dirty.IsEmpty
                ? DigResult.None
                : new DigResult(true, dirty.MinX, dirty.MinZ, dirty.MaxX, dirty.MaxZ, removed);
        }

        /// <summary>
        /// Deepens neighbours until no wall exceeds the repose slope. Depths only ever increase, so the
        /// pass terminates, and its reach is bounded by MaxDepth / (slope * cell size) cells.
        /// </summary>
        private float RelaxSlopes(ref DirtyRect dirty, float cellArea)
        {
            float removed = 0f;
            float straightStep = _settings.ReposeSlope * CellSize;
            float diagonalStep = straightStep * Sqrt2;

            while (_pending.Count > 0)
            {
                int index = _pending.Dequeue();
                int cx = index % Width;
                int cz = index / Width;
                float depth = _depth[index];

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                        {
                            continue;
                        }

                        int nx = cx + dx;
                        int nz = cz + dz;
                        if (nx < 0 || nz < 0 || nx >= Width || nz >= Height)
                        {
                            continue;
                        }

                        int neighbour = Index(nx, nz);
                        if (_locked[neighbour])
                        {
                            continue;
                        }

                        float allowed = depth - (dx != 0 && dz != 0 ? diagonalStep : straightStep);
                        if (_depth[neighbour] + Epsilon >= allowed)
                        {
                            continue;
                        }

                        removed += (allowed - _depth[neighbour]) * cellArea;
                        _depth[neighbour] = allowed;
                        dirty.Include(nx, nz);
                        _pending.Enqueue(neighbour);
                    }
                }
            }

            return removed;
        }

        private bool TryGetRange(Vector2 center, float radius, out int minX, out int minZ, out int maxX, out int maxZ)
        {
            Vector2 g = (center - Origin) / CellSize;
            float reach = radius / CellSize;

            minX = Mathf.Max(0, Mathf.CeilToInt(g.x - reach));
            minZ = Mathf.Max(0, Mathf.CeilToInt(g.y - reach));
            maxX = Mathf.Min(Width - 1, Mathf.FloorToInt(g.x + reach));
            maxZ = Mathf.Min(Height - 1, Mathf.FloorToInt(g.y + reach));

            return minX <= maxX && minZ <= maxZ;
        }

        private int Index(int x, int z) => (z * Width) + x;

        private struct DirtyRect
        {
            private bool _any;

            public int MinX { get; private set; }

            public int MinZ { get; private set; }

            public int MaxX { get; private set; }

            public int MaxZ { get; private set; }

            public bool IsEmpty => !_any;

            public void Include(int x, int z)
            {
                if (!_any)
                {
                    MinX = MaxX = x;
                    MinZ = MaxZ = z;
                    _any = true;
                    return;
                }

                MinX = Math.Min(MinX, x);
                MinZ = Math.Min(MinZ, z);
                MaxX = Math.Max(MaxX, x);
                MaxZ = Math.Max(MaxZ, z);
            }
        }
    }
}

using NUnit.Framework;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode.World
{
    public sealed class DigFieldTests
    {
        // 50 m x 50 m field with 0.5 m cells, centred on the origin.
        private const int Cells = 101;
        private const float CellSize = 0.5f;
        private static readonly Vector2 Origin = new Vector2(-25f, -25f);

        private DigSettings _settings;
        private DigField _field;

        [SetUp]
        public void SetUp()
        {
            _settings = new DigSettings();
            _field = new DigField(Cells, Cells, CellSize, Origin, _settings);
        }

        private float DepthAtWorld(float x, float z) => _field.SampleDepth(new Vector2(x, z));

        // ---------- a single dig ----------

        [Test]
        public void NewField_IsUndug()
        {
            Assert.That(_field.DepthAt(50, 50), Is.EqualTo(0f));
            Assert.That(_field.MaxDepthInField(), Is.EqualTo(0f));
        }

        [Test]
        public void Dig_LowersTheGroundAtTheTargetByTheDigAmount()
        {
            _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);

            Assert.That(DepthAtWorld(0f, 0f), Is.EqualTo(_settings.Amount).Within(1e-4f));
        }

        [Test]
        public void Dig_FallsOffWithDistance()
        {
            _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);

            float center = DepthAtWorld(0f, 0f);
            float mid = DepthAtWorld(_settings.Radius * 0.7f, 0f);

            Assert.That(mid, Is.LessThan(center));
            Assert.That(mid, Is.GreaterThan(0f));
        }

        [Test]
        public void Dig_LeavesDistantGroundUntouched()
        {
            _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);

            Assert.That(DepthAtWorld(10f, 10f), Is.EqualTo(0f));
        }

        [Test]
        public void Dig_ReportsTheRemovedVolume()
        {
            DigResult result = _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);

            float expected = 0f;
            for (int z = 0; z < Cells; z++)
            {
                for (int x = 0; x < Cells; x++)
                {
                    expected += _field.DepthAt(x, z) * CellSize * CellSize;
                }
            }

            Assert.That(result.Applied, Is.True);
            Assert.That(result.RemovedVolume, Is.EqualTo(expected).Within(1e-3f));
            Assert.That(result.RemovedVolume, Is.GreaterThan(0f));
        }

        [Test]
        public void Dig_DirtyRegionCoversEveryChangedCell()
        {
            DigResult result = _field.Dig(new Vector2(3f, -4f), _settings.Radius, _settings.Amount);

            for (int z = 0; z < Cells; z++)
            {
                for (int x = 0; x < Cells; x++)
                {
                    if (_field.DepthAt(x, z) > 0f)
                    {
                        Assert.That(x, Is.InRange(result.MinX, result.MaxX), $"cell ({x}, {z})");
                        Assert.That(z, Is.InRange(result.MinZ, result.MaxZ), $"cell ({x}, {z})");
                    }
                }
            }
        }

        // ---------- repeated digging ----------

        [Test]
        public void RepeatedDigs_Accumulate()
        {
            _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);
            _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);

            Assert.That(DepthAtWorld(0f, 0f), Is.EqualTo(_settings.Amount * 2f).Within(1e-4f));
        }

        [Test]
        public void Depth_IsCappedAtTheMaximum()
        {
            for (int i = 0; i < 100; i++)
            {
                _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);
            }

            Assert.That(_field.MaxDepthInField(), Is.EqualTo(_settings.MaxDepth).Within(1e-4f));
        }

        [Test]
        public void DiggingAtMaxDepth_RemovesNothingMore()
        {
            for (int i = 0; i < 100; i++)
            {
                _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);
            }

            // The pit keeps widening through the slope rule, so dig a cell already at full depth
            // whose neighbours are also at full depth.
            DigResult result = _field.Dig(Vector2.zero, 0.01f, _settings.Amount);

            Assert.That(result.RemovedVolume, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void WallsNeverGetSteeperThanTheReposeSlope()
        {
            for (int i = 0; i < 60; i++)
            {
                _field.Dig(new Vector2(i * 0.1f, 0f), _settings.Radius, _settings.Amount);
            }

            float maxStep = _settings.ReposeSlope * CellSize + 1e-4f;
            for (int z = 0; z < Cells; z++)
            {
                for (int x = 0; x + 1 < Cells; x++)
                {
                    Assert.That(
                        Mathf.Abs(_field.DepthAt(x, z) - _field.DepthAt(x + 1, z)),
                        Is.LessThanOrEqualTo(maxStep),
                        $"between ({x}, {z}) and ({x + 1}, {z})");
                }
            }

            for (int z = 0; z + 1 < Cells; z++)
            {
                for (int x = 0; x < Cells; x++)
                {
                    Assert.That(
                        Mathf.Abs(_field.DepthAt(x, z) - _field.DepthAt(x, z + 1)),
                        Is.LessThanOrEqualTo(maxStep),
                        $"between ({x}, {z}) and ({x}, {z + 1})");
                }
            }
        }

        [Test]
        public void HoldingDigInOnePlace_MakesAPitDeepEnoughToHideIn()
        {
            // Crouched height is 1.1 m; a trench deeper than that hides a crouching player.
            int digs = Mathf.CeilToInt(_settings.HideDepth / _settings.Amount) + 1;
            for (int i = 0; i < digs; i++)
            {
                _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);
            }

            Assert.That(DepthAtWorld(0f, 0f), Is.GreaterThanOrEqualTo(_settings.HideDepth));
        }

        [Test]
        public void DiggingAlongALine_MakesAContinuousTrench()
        {
            // Walk 6 m while digging, a few digs per step.
            for (float x = -3f; x <= 3f; x += 0.25f)
            {
                for (int i = 0; i < 6; i++)
                {
                    _field.Dig(new Vector2(x, 0f), _settings.Radius, _settings.Amount);
                }
            }

            for (float x = -2.5f; x <= 2.5f; x += 0.5f)
            {
                Assert.That(DepthAtWorld(x, 0f), Is.GreaterThanOrEqualTo(_settings.HideDepth), $"trench floor at x={x}");
            }
        }

        // ---------- bounds and locks ----------

        [Test]
        public void DigOutsideTheField_IsIgnored()
        {
            DigResult result = _field.Dig(new Vector2(500f, 500f), _settings.Radius, _settings.Amount);

            Assert.That(result.Applied, Is.False);
            Assert.That(_field.MaxDepthInField(), Is.EqualTo(0f));
        }

        [Test]
        public void DigAtTheEdge_IsClampedToTheField()
        {
            DigResult result = _field.Dig(Origin, _settings.Radius, _settings.Amount);

            Assert.That(result.Applied, Is.True);
            Assert.That(result.MinX, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.MinZ, Is.GreaterThanOrEqualTo(0));
            Assert.That(_field.DepthAt(0, 0), Is.GreaterThan(0f));
        }

        [Test]
        public void LockedCells_CannotBeDug()
        {
            _field.Lock(Vector2.zero, 1.5f);

            for (int i = 0; i < 20; i++)
            {
                _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);
            }

            Assert.That(DepthAtWorld(0f, 0f), Is.EqualTo(0f));
            Assert.That(_field.IsLocked(50, 50), Is.True);
        }

        [Test]
        public void DiggingBesideALockedArea_StillWorks()
        {
            _field.Lock(Vector2.zero, 1.5f);

            _field.Dig(new Vector2(5f, 0f), _settings.Radius, _settings.Amount);

            Assert.That(DepthAtWorld(5f, 0f), Is.GreaterThan(0f));
            Assert.That(DepthAtWorld(0f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void ZeroOrNegativeAmount_DoesNothing()
        {
            Assert.That(_field.Dig(Vector2.zero, _settings.Radius, 0f).Applied, Is.False);
            Assert.That(_field.Dig(Vector2.zero, _settings.Radius, -1f).Applied, Is.False);
            Assert.That(_field.MaxDepthInField(), Is.EqualTo(0f));
        }

        [Test]
        public void SampleDepth_InterpolatesBetweenCells()
        {
            _field.Dig(Vector2.zero, _settings.Radius, _settings.Amount);

            // Halfway between two cell centres should sit between their depths.
            float a = _field.DepthAt(50, 50);
            float b = _field.DepthAt(51, 50);
            float half = DepthAtWorld(CellSize * 0.5f, 0f);

            Assert.That(half, Is.InRange(Mathf.Min(a, b), Mathf.Max(a, b)));
        }

        [Test]
        public void CellToWorld_RoundTripsThroughWorldToCell()
        {
            Vector2 world = _field.CellToWorld(37, 64);

            Assert.That(_field.WorldToCell(world), Is.EqualTo(new Vector2Int(37, 64)));
        }
    }
}

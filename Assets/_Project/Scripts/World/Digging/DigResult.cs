namespace SnowballBattleRoyale.World
{
    /// <summary>
    /// Outcome of one dig: whether anything changed, the inclusive grid rectangle that did, and how
    /// much snow was removed (later phases turn that into snowballs).
    /// </summary>
    public readonly struct DigResult
    {
        public static readonly DigResult None = new DigResult(false, 0, 0, -1, -1, 0f);

        public DigResult(bool applied, int minX, int minZ, int maxX, int maxZ, float removedVolume)
        {
            Applied = applied;
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
            RemovedVolume = removedVolume;
        }

        public bool Applied { get; }

        public int MinX { get; }

        public int MinZ { get; }

        public int MaxX { get; }

        public int MaxZ { get; }

        /// <summary>Cubic metres of snow removed by this dig.</summary>
        public float RemovedVolume { get; }

        public int Width => Applied ? MaxX - MinX + 1 : 0;

        public int Depth => Applied ? MaxZ - MinZ + 1 : 0;
    }
}

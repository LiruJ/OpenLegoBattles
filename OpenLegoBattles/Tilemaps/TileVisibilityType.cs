namespace OpenLegoBattles.Tilemaps
{
    public enum TileVisibilityType : byte
    {
        /// <summary>
        /// The tile has not been seen yet (thick fog).
        /// </summary>
        Unseen = 0,

        /// <summary>
        /// The tile has been seen (no fog).
        /// </summary>
        Seen,

        /// <summary>
        /// The tile has been seen before, but not currently (thin fog).
        /// </summary>
        PreviouslySeen,
    }
}

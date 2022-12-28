using System;

namespace GlobalShared.Tilemaps
{
    public struct TileData
    {
        #region Constants
        private const int treeShift = 12;

        private const int tileTypeShift = treeShift + 1;

        [Flags]
        private enum dataMasks : ushort
        {
            TileType    = 0b111 << tileTypeShift,
            Tree        = 0b1 << treeShift,
            Index       = 0xFFFF ^ (TileType | Tree),
        }
        #endregion

        #region Fields
        /// <summary>
        /// The raw integer value of the tile.
        /// </summary>
        private ushort rawData;
        #endregion

        #region Properties
        /// <inheritdoc cref="rawData"/>
        public ushort RawData => rawData;

        /// <summary>
        /// The type of tile this is.
        /// </summary>
        public TileType TileType
        {
            get => (TileType)((rawData & (ushort)dataMasks.TileType) >> tileTypeShift);
            set => rawData = (ushort)((rawData & (ushort)(dataMasks.Index | dataMasks.Tree)) | (ushort)((int)value << tileTypeShift));
        }

        /// <summary>
        /// Gets or sets the value determining if this tile has a tree.
        /// </summary>
        public bool HasTree
        {
            get => (rawData & (ushort)dataMasks.Tree) != 0;
            set => rawData = (ushort)(value ? rawData | (ushort)dataMasks.Tree : rawData & (ushort)~dataMasks.Tree);
        }

        /// <summary>
        /// The block palette index of this tile.
        /// </summary>
        public ushort Index
        {
            get => (ushort)(rawData & (ushort)dataMasks.Index);
            set => rawData = (ushort)((rawData & (ushort)(dataMasks.TileType | dataMasks.Tree)) | value);

        }
        #endregion

        #region Constructors
        public TileData(ushort rawData) => this.rawData = rawData;
        #endregion
    }
}

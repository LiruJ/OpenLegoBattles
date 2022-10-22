using System.Collections.Generic;

namespace OpenLegoBattles.TilemapSystem
{
    /// <summary>
    /// A 2D tilemap whose trees can be modified.
    /// </summary>
    internal class Tilemap
    {
        #region Fields
        /// <summary>
        /// The first data layer.
        /// </summary>
        private readonly byte[,] dataLayer1;

        /// <summary>
        /// The first data layer.
        /// </summary>
        private readonly byte[,] dataLayer2;

        /// <summary>
        /// The first data layer.
        /// </summary>
        private readonly byte[,] dataLayer3;

        /// <summary>
        /// The layer of graphical tile indices, where each index refers to a tile prset in the <see cref="TilePalette"/>.
        /// </summary>
        private readonly ushort[,] detailLayer;

        /// <summary>
        /// The tree layer of the map, where 0 means no tree, and other values handle the different graphical states of a tree tile.
        /// </summary>
        private readonly TreeLayer treeLayer;
        #endregion

        #region Properties
        /// <summary>
        /// The width of the map in tiles.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the map in tiles.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The tile palette used for the detail layer.
        /// </summary>
        public IReadOnlyList<TilePreset> TilePalette { get; }

        /// <summary>
        /// The name of the tilesheet used to draw this map.
        /// </summary>
        public string TilesheetName { get; }
        #endregion

        #region Constructors
        public Tilemap(string tilesheetName, int width, int height, IReadOnlyList<TilePreset> tilePalette, byte[,] dataLayer1, byte[,] dataLayer2, byte[,] dataLayer3, ushort[,] detailLayer, byte[] treeStrips)
        {
            this.dataLayer1 = dataLayer1;
            this.dataLayer2 = dataLayer2;
            this.dataLayer3 = dataLayer3;
            this.detailLayer = detailLayer;
            treeLayer = new TreeLayer(width, height, treeStrips);
            Width = width;
            Height = height;
            TilePalette = tilePalette;
            TilesheetName = tilesheetName;
        }
        #endregion

        #region Query Functions
        /// <summary>
        /// Gets the tile preset for the detail tile at the given position.
        /// </summary>
        /// <param name="x"> The x position. </param>
        /// <param name="y"> The y position. </param>
        /// <returns> The preset representing the detail tile at the given position. </returns>
        public TilePreset GetDetailTileAt(int x, int y) => TilePalette[detailLayer[x, y]];

        /// <inheritdoc cref="TreeLayer.HasTreeAtPosition"/>
        public bool HasTreeAtPosition(int x, int y) => treeLayer.HasTreeAtPosition(x, y);
        #endregion
    }
}

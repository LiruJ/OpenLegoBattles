using GlobalShared.Tilemaps;
using System.Collections.Generic;

namespace OpenLegoBattles.TilemapSystem
{
    /// <summary>
    /// A 2D tilemap whose trees can be modified.
    /// </summary>
    internal class Tilemap
    {
        #region Operators
        public TileData this[int x, int y] => mapData[x, y];
        #endregion

        #region Fields
        /// <summary>
        /// The layer of graphical tile indices, where each index refers to a tile preset in the <see cref="TilePalette"/>.
        /// </summary>
        private readonly TileData[,] mapData;
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
        public Tilemap(string tilesheetName, int width, int height, IReadOnlyList<TilePreset> tilePalette, TileData[,] mapData)
        {
            this.mapData = mapData;
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
        public TilePreset GetDetailTileAt(int x, int y) => TilePalette[mapData[x, y].Index];

        /// <inheritdoc cref="TreeLayer.HasTreeAtPosition"/>
        public bool HasTreeAtPosition(int x, int y) => mapData[x, y].HasTree;
        #endregion
    }
}

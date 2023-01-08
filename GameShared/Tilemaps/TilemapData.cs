using GlobalShared.Content;
using GlobalShared.Tilemaps;
using System.IO;

namespace OpenLegoBattles.TilemapSystem
{
    /// <summary>
    /// The underlying data of a tilemap.
    /// </summary>
    public class TilemapData
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
        public TilemapBlockPalette TilePalette { get; }

        /// <summary>
        /// The name of the tilesheet used to draw this map.
        /// </summary>
        public string TilesheetName { get; }
        #endregion

        #region Constructors
        public TilemapData(string tilesheetName, int width, int height, TilemapBlockPalette tilePalette)
        {
            mapData = new TileData[width, height];
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
        public TilemapPaletteBlock GetTileBlockAt(int x, int y) => TilePalette[mapData[x, y].Index];

        /// <summary>
        /// Gets if the tile at the given position has a tree.
        /// </summary>
        /// <param name="x"> The x position. </param>
        /// <param name="y"> The y position. </param>
        /// <returns> <c>true</c> if the given position has a tree; otherwise <c>false</c>. </returns>
        public bool HasTreeAtPosition(int x, int y) => IsPositionInRange(x, y) && mapData[x, y].HasTree;

        public bool IsPositionInRange(int x, int y) => x >= 0 || x < Width || y >= 0 || y < Height;
        #endregion

        #region Load Functions
        /// <summary>
        /// Loads the map file at the given path.
        /// </summary>
        /// <param name="filePath"> The path of the file, the extension is automatically appended. </param>
        /// <returns> The loaded tilemap data. </returns>
        public static TilemapData Load(string filePath)
        {
            // Create the reader.
            FileStream file = File.OpenRead(Path.ChangeExtension(filePath, ContentFileUtil.TilemapExtension));
            using BinaryReader reader = new(file);

            // Load the basic map info.
            string mapName = reader.ReadString();
            string tilesheetName = reader.ReadString();
            byte width = reader.ReadByte();
            byte height = reader.ReadByte();

            // Load the tile palette.
            TilemapBlockPalette tilePalette = TilemapBlockPalette.LoadFromFile(reader);

            // Create the tilemap data, set its underlying tile data, then return it.
            TilemapData tilemapData = new(tilesheetName, width, height, tilePalette);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tilemapData.mapData[x, y] = new(reader.ReadUInt16());
            return tilemapData;
        }
        #endregion
    }
}

using OpenLegoBattles.TilemapSystem;
using GlobalShared.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalShared.Tilemaps;

namespace OpenLegoBattles.RomContent.Loaders
{
    internal class TilemapLoader : RomContentLoader<Tilemap>
    {
        #region Constructors
        public TilemapLoader(RomContentManager romContentManager) : base(romContentManager)
        {
        }
        #endregion

        #region Load Functions
        public override Tilemap LoadFromPath(string path)
        {
            // Apply the root folder and extension to the path.
            path = ContentFileUtil.CreateFullFilePath(romContentManager.BaseGameDirectory, ContentFileUtil.TilemapDirectoryName, path, ContentFileUtil.TilemapExtension);

            // If the file does not exist, throw an exception.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException("File could not be found.", path);

            // Make a reader for the map file.
            using BinaryReader reader = new(File.OpenRead(path));

            // Load the basic map info.
            string mapName = reader.ReadString();
            string tilesheetName = reader.ReadString();
            byte width = reader.ReadByte();
            byte height = reader.ReadByte();

            // Load the tile palette.
            ushort tilePaletteCount = reader.ReadUInt16();
            List<TilePreset> tilePalette = loadTilePalette(reader, tilePaletteCount);

            // Load the data layer.
            TileData[,] mapData = loadMapData(reader, width, height);


            // Create and return the tilemap.
            return new Tilemap(tilesheetName, width, height, tilePalette, mapData);
        }

        private static List<TilePreset> loadTilePalette(BinaryReader reader, ushort tilePaletteCount)
        {
            // Load the presets.
            List<TilePreset> tilePalette = new List<TilePreset>(tilePaletteCount);
            for (int i = 0; i < tilePaletteCount; i++)
            {
                TilePreset tilePreset = new TilePreset((ushort)i, reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
                tilePalette.Add(tilePreset);
            }

            // Return the loaded palette.
            return tilePalette;
        }

        private static TileData[,] loadMapData(BinaryReader reader, byte width, byte height)
        {
            // Create the array.
            TileData[,] mapData = new TileData[width, height];

            // Load the data into the array.
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    mapData[x, y] = new TileData(reader.ReadUInt16());

            // Return the array.
            return mapData;
        }
        #endregion
    }
}

using OpenLegoBattles.TilemapSystem;
using Shared.Content;
using System.Collections.Generic;
using System.IO;

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

            // Load the data layers.
            byte[,] dataLayer1 = loadMapDataLayer(reader, width, height);
            byte[,] dataLayer2 = loadMapDataLayer(reader, width, height);
            byte[,] dataLayer3 = loadMapDataLayer(reader, width, height);

            // Load the detail layer.
            ushort[,] detailLayer = loadMapDetailLayer(reader, width, height);

            // Load the tree strips.
            byte[] treeStrips = loadTreeStrips(reader);

            // Create and return the tilemap.
            return new Tilemap(tilesheetName, width, height, tilePalette, dataLayer1, dataLayer2, dataLayer3, detailLayer, treeStrips);
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

        private static byte[,] loadMapDataLayer(BinaryReader reader, byte width, byte height)
        {
            // Create the array.
            byte[,] layerData = new byte[width, height];

            // Load the data into the array.
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    layerData[x, y] = reader.ReadByte();

            // Return the array.
            return layerData;
        }

        private static ushort[,] loadMapDetailLayer(BinaryReader reader, byte width, byte height)
        {
            // Create the array.
            ushort[,] layerData = new ushort[width, height];

            // Load the data into the array.
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    layerData[x, y] = reader.ReadUInt16();

            // Return the array.
            return layerData;
        }

        private static byte[] loadTreeStrips(BinaryReader reader)
        {
            // Read the strip length.
            ushort treeStripCount = reader.ReadUInt16();

            // Create an array to hold the tree strips.
            byte[] treeStrips = new byte[treeStripCount];

            // Read each byte as a strip length.
            for (int i = 0; i < treeStripCount; i++)
                treeStrips[i] = reader.ReadByte();

            // Return the tree strips.
            return treeStrips;
        }
        #endregion
    }
}

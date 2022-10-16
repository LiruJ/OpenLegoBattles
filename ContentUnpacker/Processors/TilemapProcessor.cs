using System.Text;

namespace ContentUnpacker.Processors
{
    internal class TilemapProcessor : ContentProcessor
    {
        #region Constants
        public const string MapDataRootFolder = "Tilemaps";

        public const string MapEntityDataRootFolder = "TilemapEntities";

        public const string MapDataFileExtension = "map";

        private const string tilePalettesPath = "TilePalettes";

        private const uint maptMagicNumber = 0x5450414D;
        private const ushort erMagicNumber = 0x5245;
        private const byte rMagicNumber = (byte)'R';

        private const int tilesetPaletteHardCount = 440;
        #endregion

        #region Process Functions
        public override async Task ProcessAsync(string inputPath, string outputRootPath)
        {
            // Make the reader for the map.
            using BinaryReader mapReader = new(File.OpenRead(inputPath));

            // Get the map name from the file.
            string mapName = Path.GetFileNameWithoutExtension(inputPath);

            // Ensure the data is valid.
            if (!ensureValidMagicWord(mapReader))
                throw new Exception($"Map file {mapName} does not begin with magic word and cannot be loaded.");

            // Read the size from the file.
            readMapSize(mapReader, out byte width, out byte height);

            // The next two bytes are for the tile size, which is always 3x2. This can be skipped.
            mapReader.BaseStream.Position += 2;

            // Read the tileset name from the file.
            string tilesetName = readTilesetName(mapReader);

            // The following few bytes are left empty, so can be skipped to get to the map data.
            mapReader.BaseStream.Position = 0x2B;

            // Create the directory for the tilemaps.
            Directory.CreateDirectory(Path.Combine(outputRootPath, MapDataRootFolder));

            // Make the output file path based on the map name.
            string outputFilePath = Path.ChangeExtension(Path.Combine(outputRootPath, MapDataRootFolder, mapName), MapDataFileExtension);
            
            // Make the output file and write the basic data to it.
            using BinaryWriter mapWriter = new(File.OpenWrite(outputFilePath));
            mapWriter.Write(mapName);
            mapWriter.Write(tilesetName);
            mapWriter.Write(width);
            mapWriter.Write(height);

            // Run the main bulk of the loading async.
            await Task.Run(() =>
            {
                // Write the tile palette to the file.
                transferTilePalette(mapWriter, tilesetName, mapName);

                // Transfer the basic layers as-is to the output file.
                transferMapDataLayer(mapReader, mapWriter, width, height);
                transferMapDataLayer(mapReader, mapWriter, width, height);
                transferMapDataLayer(mapReader, mapWriter, width, height);

                // Get the tree strip count.
                ushort treeStripCount = mapReader.ReadUInt16();

                // Save the position of the reader so the detail layer can be written to the file before the trees. Then skip the trees for now.
                long treeStripStartPosition = mapReader.BaseStream.Position;
                mapReader.BaseStream.Position += treeStripCount;

                // Transfer the detail layer so it is directly after the data layers.
                transferMapDetailLayer(mapReader, mapWriter, width, height);

                // Transfer the tree strips.
                mapReader.BaseStream.Position = treeStripStartPosition;
                transferTreeStrips(mapReader, mapWriter, treeStripCount);
            });
        }

        private static bool ensureValidMagicWord(BinaryReader mapReader)
        {
            if (mapReader.ReadInt32() != maptMagicNumber) return false;
            if (mapReader.ReadUInt16() != erMagicNumber) return false;
            if (mapReader.ReadByte() != rMagicNumber) return false;
            return true;
        }

        private static string readTilesetName(BinaryReader mapReader)
        {
            // Read the tilset name.
            bool stringContinue = true;
            StringBuilder tilesetNameStringBuilder = new();
            while (stringContinue)
            {
                char currentChar = mapReader.ReadChar();
                stringContinue = currentChar != 0;
                if (stringContinue)
                    tilesetNameStringBuilder.Append(currentChar);
            }

            // Return the tileset name.
            return tilesetNameStringBuilder.ToString();
        }

        private static void readMapSize(BinaryReader mapReader, out byte width, out byte height)
        {
            width = mapReader.ReadByte();
            height = mapReader.ReadByte();
        }

        private static void transferTilePalette(BinaryWriter mapWriter, string tilesetName, string mapName)
        {
            // Load the palette files.
            string tilesetPalettePath = Path.ChangeExtension(Path.Combine(RomUnpacker.UnpackedFolderName, tilePalettesPath, tilesetName), ".bin");
            using BinaryReader tilesetPaletteReader = new(File.OpenRead(tilesetPalettePath));
            string mapPalettePath = Path.ChangeExtension(Path.Combine(RomUnpacker.UnpackedFolderName, tilePalettesPath, mapName), ".bin");
            using BinaryReader mapPaletteReader = new(File.OpenRead(mapPalettePath));

            // Read the lengths.
            ushort tilesetPaletteCount = tilesetPaletteReader.ReadUInt16();
            ushort mapPaletteCount = mapPaletteReader.ReadUInt16();

            // Write the length of the tile palette.
            mapWriter.Write((ushort)(mapPaletteCount + tilesetPaletteHardCount));

            // Ignore the length of the tileset tile palette, only load 440 from it.
            for (int i = 0; i < tilesetPaletteHardCount; i++)
                for (int t = 0; t < 6; t++)
                    mapWriter.Write((ushort)(tilesetPaletteReader.ReadByte() + tilesetPaletteReader.ReadByte() * 256));

            // Load the full map palette.
            for (int i = 0; i < mapPaletteCount; i++)
                for (int t = 0; t < 6; t++)
                    mapWriter.Write((ushort)(mapPaletteReader.ReadByte() + mapPaletteReader.ReadByte() * 256));
        }

        private static void transferMapDataLayer(BinaryReader mapReader, BinaryWriter mapWriter, byte width, byte height)
        {
            for (int i = 0; i < width * height; i++)
                    mapWriter.Write(mapReader.ReadByte());
        }

        private static void transferMapDetailLayer(BinaryReader mapReader, BinaryWriter mapWriter, byte width, byte height)
        {
            for (int i = 0; i < width * height; i++)
                mapWriter.Write((ushort)(mapReader.ReadByte() + mapReader.ReadByte() * 256));
        }

        private static void transferTreeStrips(BinaryReader mapReader, BinaryWriter mapWriter, ushort treeStripCount)
        {
            mapWriter.Write(treeStripCount);
            for (int i = 0; i < treeStripCount; i++)
                mapWriter.Write(mapReader.ReadByte());
        }
        #endregion
    }
}

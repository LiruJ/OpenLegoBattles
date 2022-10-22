using ContentUnpacker.Decompressors;
using Shared.Content;
using System.Text;
using System.Xml;

namespace ContentUnpacker.Processors
{
    internal class TilemapProcessor : ContentProcessor
    {
        #region Constants
        /// <summary>
        /// The suffix added onto the map name to find the tile palette.
        /// </summary>
        private const string tilePaletteSuffix = "TilePalette";

        /// <summary>
        /// The magic word of this processor, "MAPT". Technically the full magic word is "MAPTERR" but the full string is not needed.
        /// </summary>
        public const uint MagicWord = 0x5450414D;

        /// <summary>
        /// The hard limit on the number of tiles to load from the faction's tileset.
        /// </summary>
        private const int tilesetPaletteHardCount = 440;
        #endregion

        #region Constructors
        public TilemapProcessor(RomUnpacker romUnpacker, BinaryReader reader, XmlNode contentNode) : base(romUnpacker, reader, contentNode)
        {
        }
        #endregion

        #region Process Functions
        public override void Process()
        {
            // Skip the reader 3 characters ahead to skip the end of the "MAPTERR" string.
            reader.BaseStream.Position += 3;

            // Create the writer and directory.
            createOutputDirectory();
            using BinaryWriter writer = createOutputWriter(ContentFileUtil.TilemapExtension);

            // Get the map name from the file.
            string mapName = Path.GetFileNameWithoutExtension(outputFilePath);

            // Read the size from the file.
            readMapSize(out byte width, out byte height);

            // The next two bytes are for the tile size, which is always 3x2. This can be skipped.
            reader.BaseStream.Position += 2;

            // Read the tileset name from the file.
            string tilesetName = readTilesetName();

            // The following few bytes are left empty, so can be skipped to get to the map data.
            reader.BaseStream.Position = 0x2B;

            // Write the basic data.
            writer.Write(mapName);
            writer.Write(tilesetName);
            writer.Write(width);
            writer.Write(height);

            // Get the name of the tile palette. This is simply the tileset name but with "TilePalette" rather than "Tileset" on the end.
            string tilePaletteName = string.Concat(tilesetName.AsSpan(0, tilesetName.Length - "Tileset".Length), tilePaletteSuffix);

            // Write the tile palette to the file.
            transferTilePalette(writer, tilePaletteName, mapName);

            // Transfer the basic layers as-is to the output file.
            transferMapDataLayer(writer, width, height);
            transferMapDataLayer(writer, width, height);
            transferMapDataLayer(writer, width, height);

            // Get the tree strip count.
            ushort treeStripCount = reader.ReadUInt16();

            // Save the position of the reader so the detail layer can be written to the file before the trees. Then skip the trees for now.
            long treeStripStartPosition = reader.BaseStream.Position;
            reader.BaseStream.Position += treeStripCount;

            // Transfer the detail layer so it is directly after the data layers.
            transferMapDetailLayer(writer, width, height);

            // Transfer the tree strips.
            reader.BaseStream.Position = treeStripStartPosition;
            transferTreeStrips(writer, treeStripCount);
        }

        private void readMapSize(out byte width, out byte height)
        {
            width = reader.ReadByte();
            height = reader.ReadByte();
        }

        private string readTilesetName()
        {
            // Read the tilset name.
            bool stringContinue = true;
            StringBuilder tilesetNameStringBuilder = new();
            while (stringContinue)
            {
                char currentChar = reader.ReadChar();
                stringContinue = currentChar != 0;
                if (stringContinue)
                    tilesetNameStringBuilder.Append(currentChar);
            }

            // Return the tileset name.
            return tilesetNameStringBuilder.ToString();
        }

        private void transferTilePalette(BinaryWriter writer, string tilePaletteName, string mapName)
        {
            // Load the palette files.
            string tilePalettePath = Path.ChangeExtension(Path.Combine(RomUnpacker.WorkingFolderName, LegoDecompressor.OutputFolderPath, tilePaletteName), ContentFileUtil.BinaryExtension);
            BinaryReader tilesetPaletteReader = romUnpacker.GetReaderForFilePath(tilePalettePath, out bool manualCloseTileset);
            string mapPalettePath = Path.ChangeExtension(Path.Combine(RomUnpacker.WorkingFolderName, LegoDecompressor.OutputFolderPath, mapName + tilePaletteSuffix), ContentFileUtil.BinaryExtension);
            BinaryReader mapPaletteReader = romUnpacker.GetReaderForFilePath(mapPalettePath, out bool manualCloseMap);

            // Read the lengths.
            ushort tilesetPaletteCount = tilesetPaletteReader.ReadUInt16();
            ushort mapPaletteCount = mapPaletteReader.ReadUInt16();

            // Write the length of the tile palette.
            writer.Write((ushort)(mapPaletteCount + tilesetPaletteHardCount));

            // Ignore the length of the tileset tile palette, only load 440 from it.
            for (int i = 0; i < tilesetPaletteHardCount; i++)
                for (int t = 0; t < 6; t++)
                    writer.Write((ushort)(tilesetPaletteReader.ReadByte() + tilesetPaletteReader.ReadByte() * 256));

            // Load the full map palette.
            for (int i = 0; i < mapPaletteCount; i++)
                for (int t = 0; t < 6; t++)
                    writer.Write((ushort)(mapPaletteReader.ReadByte() + mapPaletteReader.ReadByte() * 256));

            // Close or return the readers.
            if (manualCloseTileset) tilesetPaletteReader.Close();
            else romUnpacker.ReturnReader(tilesetPaletteReader);
            if (manualCloseMap) mapPaletteReader.Close();
            else romUnpacker.ReturnReader(mapPaletteReader);
        }

        private void transferMapDataLayer(BinaryWriter mapWriter, byte width, byte height)
        {
            for (int i = 0; i < width * height; i++)
                mapWriter.Write(reader.ReadByte());
        }

        private void transferMapDetailLayer(BinaryWriter mapWriter, byte width, byte height)
        {
            for (int i = 0; i < width * height; i++)
                mapWriter.Write((ushort)(reader.ReadByte() + reader.ReadByte() * 256));
        }

        private void transferTreeStrips(BinaryWriter mapWriter, ushort treeStripCount)
        {
            mapWriter.Write(treeStripCount);
            for (int i = 0; i < treeStripCount; i++)
                mapWriter.Write(reader.ReadByte());
        }
        #endregion
    }
}

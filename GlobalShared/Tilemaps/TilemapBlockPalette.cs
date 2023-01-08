using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace GlobalShared.Tilemaps
{
    public class TilemapBlockPalette : IEnumerable<TilemapPaletteBlock>
    {
        #region Constants
        private const string fileExtension = "tbp";
        #endregion

        #region Operators
        public TilemapPaletteBlock this[int i] => blocks[i];
        #endregion

        #region Fields
        private readonly List<TilemapPaletteBlock> blocks = new();
        #endregion

        #region Properties
        public IReadOnlyList<TilemapPaletteBlock> Blocks => blocks;

        public int Count => blocks.Count;
        #endregion

        #region Enumeration Functions
        public IEnumerator<TilemapPaletteBlock> GetEnumerator() => blocks.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Save Functions
        public void SaveToFile(string filePath, ushort? count = null, ushort? start = null)
        {
            // Create the writer.
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            FileStream file = File.OpenWrite(Path.ChangeExtension(filePath, fileExtension));
            using BinaryWriter writer = new(file);

            // Save the file
            SaveToFile(writer, count, start);
        }

        public void SaveToFile(BinaryWriter writer, ushort? count = null, ushort? start = null)
        {
            // Resolve the count and start.
            count ??= (ushort)Blocks.Count;
            start ??= 0;

            // Write the count.
            writer.Write(count.Value);

            // Write each block.
            for (int i = start.Value; i < count; i++)
                blocks[i].SaveToFile(writer);
        }
        #endregion

        #region Load Functions
        public static bool TryLoadFromFile(string filePath, out TilemapBlockPalette? palette, ushort? count = null, bool is16Bit = true, string? extension = fileExtension)
        {
            // If the file does not exist, return false with no palette.
            if (!File.Exists(Path.ChangeExtension(filePath, extension)))
            {
                palette = null;
                return false;
            }
            else
            {
                palette = LoadFromFile(filePath, count, is16Bit);
                return true;
            }
        }

        public static TilemapBlockPalette LoadFromFile(string filePath, ushort? count = null, bool is16Bit = true, string? extension = fileExtension)
        {
            // Create the reader and read the file.
            using BinaryReader reader = new(File.OpenRead(Path.ChangeExtension(filePath, extension)));
            return LoadFromFile(reader, count, is16Bit);
        }

        public static TilemapBlockPalette LoadFromFile(BinaryReader reader, ushort? count = null, bool is16Bit = true)
        {
            // Read the count if none was given, otherwise skip it.
            if (count == null) count = reader.ReadUInt16();
            else reader.BaseStream.Position += 2;

            // Create the palette and load from the file into it.
            TilemapBlockPalette palette = new();
            palette.loadBlocks(reader, count.Value, is16Bit);
            return palette;
        }

        private void loadBlocks(BinaryReader reader, ushort count, bool is16Bit)
        {
            for (int i = 0; i < count; i++)
            {
                TilemapPaletteBlock block = is16Bit ? TilemapPaletteBlock.LoadUInt16(reader) : TilemapPaletteBlock.LoadUInt8(reader);
                blocks.Add(block);
            }
        }
        #endregion
    }
}
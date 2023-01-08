using ContentUnpacker.NDSFS;
using System.Drawing;

namespace ContentUnpacker.Spritesheets
{
    internal class ColourPaletteLoader
    {
        #region Constants
        /// <summary>
        /// The magic number of a palette file.
        /// </summary>
        public const uint MagicWord = 0x4E434C52;

        /// <summary>
        /// The magic number of the palette section.
        /// </summary>
        private const uint paletteMagicWord = 0x504C5454;

        private const string fileExtension = "NCLR";
        #endregion

        #region Fields
        private readonly byte[] paletteData = Array.Empty<byte>();
        #endregion

        #region Properties
        /// <summary>
        /// The number of colours in this palette.
        /// </summary>
        public uint PaletteSize { get; }
        #endregion

        #region Get Functions
        public Color GetColourAtIndex(int index)
            => index == 0 ? Color.Transparent : Color.FromArgb(paletteData[index * 3], paletteData[index * 3 + 1], paletteData[index * 3 + 2]);
        #endregion

        #region Constructors
        private ColourPaletteLoader(uint paletteSize)
        {
            PaletteSize = paletteSize;
            paletteData = new byte[PaletteSize * 3];
        }
        #endregion

        #region Load Functions
        /// <summary>
        /// Loads the colour palette at the given path.
        /// </summary>
        /// <param name="filePath"> The path of the NCLR file. </param>
        /// <returns> The loaded palette. </returns>
        public static ColourPaletteLoader Load(string filePath)
        {
            // Create the reader.
            FileStream file = File.OpenRead(Path.ChangeExtension(filePath, fileExtension));
            using BinaryReader reader = new(file);

            // Load the header first and create the palette with the loaded size.
            NDSFileUtil.loadGenericHeader(reader, true);
            uint paletteSize = loadHeader(reader);
            ColourPaletteLoader palette = new(paletteSize);

            // Load and returns the palette data.
            palette.loadPalette(reader);
            return palette;
        }

        private static uint loadHeader(BinaryReader reader)
        {
            // Ensure the magic word matches.
            if (reader.ReadUInt32() != paletteMagicWord)
                throw new Exception("Invalid palette magic word");

            // Read the sizes and depth, skipping any unknowns.
            uint sectionSize = reader.ReadUInt32();
            ushort depth = reader.ReadUInt16();
            reader.BaseStream.Position += 6;
            uint paletteSize = reader.ReadUInt32();
            uint offset = reader.ReadUInt32();

            // Set the palette size divided by two. The size is normally in bytes, and there's 2 bytes per colour.
            return paletteSize / 2;
        }

        private void loadPalette(BinaryReader reader)
        {
            // Write all colours.
            for (int colourIndex = 0; colourIndex < PaletteSize; colourIndex++)
                readColour(reader, colourIndex);
        }

        private void readColour(BinaryReader reader, int colourIndex)
        {
            // Read the two byte BGR555 colour.
            ushort packedColour = reader.ReadUInt16();

            // Unpack each channel.
            byte r = (byte)((packedColour & 0b0_00000_00000_11111) * 8);
            r = (byte)Math.Min(r + Math.Floor(r / 31f), byte.MaxValue);
            byte g = (byte)(((packedColour & 0b0_00000_11111_00000) >> 5) * 8);
            g = (byte)Math.Min(g + Math.Floor(g / 31f), byte.MaxValue);
            byte b = (byte)(((packedColour & 0b0_11111_00000_00000) >> 10) * 8);
            b = (byte)Math.Min(b + Math.Floor(b / 31f), byte.MaxValue);

            // Save the colour.
            paletteData[colourIndex * 3] = r;
            paletteData[colourIndex * 3 + 1] = g;
            paletteData[colourIndex * 3 + 2] = b;
        }
        #endregion
    }
}
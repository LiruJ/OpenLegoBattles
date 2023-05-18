using ContentUnpacker.NDSFS;

namespace ContentUnpacker.Spritesheets
{
    /// <summary>
    /// Handles loading and holding an NCLR file full of colours.
    /// </summary>
    internal class NDSColourPalette
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

        /// <summary>
        /// The file extension of the NDS file.
        /// </summary>
        private const string fileExtension = "NCLR";
        #endregion

        #region Indexers
        /// <summary>
        /// Gets or sets the colour at the given index.
        /// </summary>
        /// <param name="index"> The palette index of the colour to get or set. </param>
        /// <returns> The colour at the given index. </returns>
        public Color this[int index]
        {
            get => GetColourAtIndex(index);
            set => SetColourAtIndex(index, value);
        }
        #endregion

        #region Fields
        /// <summary>
        /// The raw byte data of the palette.
        /// </summary>
        private readonly byte[] paletteData = Array.Empty<byte>();
        #endregion

        #region Properties
        /// <summary>
        /// The number of colours in this palette.
        /// </summary>
        public uint PaletteSize { get; }
        #endregion

        #region Colour Functions
        /// <summary>
        /// Gets the colour at the given index.
        /// </summary>
        /// <param name="index"> The palette index of the colour to get. </param>
        /// <returns> The colour at the given index. </returns>
        public Rgba32 GetColourAtIndex(int index)
            => index == 0 ? new Rgba32(0, 0, 0, 0) : new Rgba32(paletteData[index * 3], paletteData[(index * 3) + 1], paletteData[(index * 3) + 2]);

        /// <summary>
        /// Sets the colour at the given index.
        /// </summary>
        /// <param name="index"> The palette index of the colour to get. </param>
        /// <param name="colour"> The colour value to set. </param>
        public void SetColourAtIndex(int index, Rgb24 colour)
        {
            paletteData[index * 3] = colour.R;
            paletteData[(index * 3) + 1] = colour.G;
            paletteData[(index * 3) + 2] = colour.B;
        }

        /// <summary>
        /// Unpacks the given colour and sets the colour at the given index to it.
        /// </summary>
        /// <param name="index"> The index of the colour to set. </param>
        /// <param name="packedColour"> The packed RGB555 value. </param>
        public void SetColourAtIndexFromRGB555(int index, ushort packedColour)
        {
            // Unpack each channel.
            byte r = (byte)(((packedColour & 0b0_00000_00000_11111) / 31f) * byte.MaxValue);
            byte g = (byte)((((packedColour & 0b0_00000_11111_00000) >> 5) / 31f) * byte.MaxValue);
            byte b = (byte)((((packedColour & 0b0_11111_00000_00000) >> 10) / 31f) * byte.MaxValue);

            // Set the colour.
            paletteData[index * 3] = r;
            paletteData[(index * 3) + 1] = g;
            paletteData[(index * 3) + 2] = b;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an empty palette with the given size.
        /// </summary>
        /// <param name="paletteSize"> The size of the new palette. </param>
        private NDSColourPalette(uint paletteSize)
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
        public static NDSColourPalette Load(string filePath)
        {
            // Create the reader.
            FileStream file = File.OpenRead(Path.ChangeExtension(filePath, fileExtension));
            using BinaryReader reader = new(file);

            // Load the header first and create the palette with the loaded size.
            NDSFileUtil.loadGenericHeader(reader, true);
            uint paletteSize = loadHeader(reader);
            NDSColourPalette palette = new(paletteSize);

            // Load and returns the palette data.
            palette.loadPaletteColours(reader);
            return palette;
        }

        /// <summary>
        /// Loads the palette header from the given file.
        /// </summary>
        /// <param name="reader"> The file reader, where the header will be read from the current position. </param>
        /// <returns> The palette size of the read palette file. </returns>
        /// <exception cref="Exception"> Thrown when the magic word is incorrect. </exception>
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

        /// <summary>
        /// Loads the colours from the given file into this palette.
        /// </summary>
        /// <param name="reader"> The palette file from which to load. </param>
        private void loadPaletteColours(BinaryReader reader)
        {
            // Read the two byte RGB555 colour and set the value at the index based on the total number of colours in the palette.
            for (int colourIndex = 0; colourIndex < PaletteSize; colourIndex++)
            {
                ushort packedColour = reader.ReadUInt16();
                SetColourAtIndexFromRGB555(colourIndex, packedColour);
            }
        }
        #endregion
    }
}
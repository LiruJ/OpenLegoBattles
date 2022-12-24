using ContentUnpacker.Utils;
using System.Drawing;

namespace ContentUnpacker.Loaders
{
    internal class PaletteLoader : ContentLoader
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
        #endregion

        #region Fields
        private byte[] paletteData = Array.Empty<byte>();
        #endregion

        #region Properties
        public uint PaletteSize { get; private set; }
        #endregion

        #region Constructors
        public PaletteLoader(RomUnpacker romUnpacker) : base(romUnpacker)
        {
        }
        #endregion

        #region Get Functions
        public Color GetColourAtIndex(int index) 
            => index == 0 ? Color.Transparent : Color.FromArgb(paletteData[index * 3], paletteData[(index * 3) + 1], paletteData[(index * 3) + 2]);
        #endregion

        #region Load Functions
        public override void Load(BinaryReader reader)
        {
            // Load the header first.
            NDSFileUtil.loadGenericHeader(reader, true);
            loadHeader(reader);

            // Load the palette data.
            loadPalette(reader);
        }

        private void loadHeader(BinaryReader reader)
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
            PaletteSize = paletteSize / 2;
        }

        private void loadPalette(BinaryReader reader)
        {
            // Create the data array.
            paletteData = new byte[PaletteSize * 3];

            // Write all colours.
            for (int colourIndex = 0; colourIndex < PaletteSize; colourIndex++)
                ReadColour(reader, colourIndex);
        }

        private void ReadColour(BinaryReader reader, int colourIndex)
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
            paletteData[(colourIndex * 3) + 1] = g;
            paletteData[(colourIndex * 3) + 2] = b;
        }
        #endregion
    }
}
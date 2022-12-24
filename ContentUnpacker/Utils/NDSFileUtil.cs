﻿using System.Text;

namespace ContentUnpacker.Utils
{
    internal static class NDSFileUtil
    {
        #region Header Functions
        /// <summary>
        /// Reads the generic header of the chunk, skipping the magic word.
        /// </summary>
        /// <remarks>https://www.romhacking.net/documents/%5B469%5Dnds_formats.htm#Generic</remarks>
        /// <exception cref="Exception"> Thrown if the endianess is invalid. </exception>
        public static void loadGenericHeader(BinaryReader reader, bool readMagicWord = false)
        {
            // If the magic word should be read, do so.
            if (readMagicWord)
                reader.ReadUInt32();

            // Read the endianess.
            ushort endianess = reader.ReadUInt16();
            if (endianess != 0xFEFF)
                throw new Exception("Incorrect endianess.");

            // Read the constant value.
            ushort constant = reader.ReadUInt16();

            // Read sizes.
            uint fileSize = reader.ReadUInt32();
            ushort headerSize = reader.ReadUInt16();
            ushort sectionLength = reader.ReadUInt16();
        }
        #endregion

        #region String Functions
        public static string ReadString(BinaryReader reader, StringBuilder stringBuilder, int length)
        {
            stringBuilder.Clear();
            stringBuilder.Capacity = Math.Max(stringBuilder.Capacity, length);
            for (int i = 0; i < length; i++)
                stringBuilder.Append(reader.ReadChar());
            return stringBuilder.ToString();
        }
        #endregion
    }
}

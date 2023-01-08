using System.Text;

namespace ContentUnpacker.NDSFS
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
        /// <summary>
        /// Reads the string with the given length from the given reader and fills the given string builder.
        /// </summary>
        /// <param name="reader"> The reader to use to read the string. After reading the string, the reader's position is immediately after the string. </param>
        /// <param name="stringBuilder"> The string builder to fill. This is cleared first. </param>
        /// <param name="length"> The length of the string to read. </param>
        /// <returns> The read string. </returns>
        public static string ReadString(this BinaryReader reader, StringBuilder stringBuilder, int length)
        {
            stringBuilder.Clear();
            stringBuilder.Capacity = Math.Max(stringBuilder.Capacity, length);
            for (int i = 0; i < length; i++)
                stringBuilder.Append(reader.ReadChar());
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Reads characters from the given <paramref name="reader"/> until a null character (value of <c>0</c>) is found.
        /// </summary>
        /// <param name="reader"> The reader to use to read the string. After reading the string, the reader's position is immediately after the null character. </param>
        /// <returns> The read string without the null character. </returns>
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            // Read the string until a null character is found.
            bool stringContinue = true;
            StringBuilder stringBuilder = new();
            while (stringContinue)
            {
                char currentChar = reader.ReadChar();
                stringContinue = currentChar != 0;
                if (stringContinue)
                    stringBuilder.Append(currentChar);
            }

            // Return the string.
            return stringBuilder.ToString();
        }
        #endregion
    }
}

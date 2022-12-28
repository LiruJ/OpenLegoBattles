using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.Spritesheets
{
    internal class SpritesheetLoader : IDisposable
    {
        #region Constants
        /// <summary>
        /// The file extension of the raw file.
        /// </summary>
        private const string ncgrFileExtension = "NCGR";

        /// <summary>
        /// The magic number of the tile graphic section.
        /// </summary>
        public const uint MagicWord = 0x4E434752;

        /// <summary>
        /// The magic number of the tile graphic data section.
        /// </summary>
        private const uint tileGraphicMagicWord = 0x43484152;
        #endregion

        #region Fields
        private readonly BinaryReader reader;

        /// <summary>
        /// The number of tiles in this file.
        /// </summary>
        public ushort TileCount { get; }

        /// <summary>
        /// The starting position of the data in the file.
        /// </summary>
        public int StartPosition { get; }

        /// <summary>
        /// The current index of the tile that this loader is reading.
        /// </summary>
        public ushort CurrentTileIndex
        {
            get => (ushort)Math.Floor((reader.BaseStream.Position - StartPosition) / 64f);
            set => reader.BaseStream.Position = StartPosition + (value * 64);
        }
        #endregion

        #region Constructors
        public SpritesheetLoader(BinaryReader reader, ushort tileCount, int startPosition)
        {
            this.reader = reader;
            TileCount = tileCount;
            StartPosition = startPosition;
        }
        #endregion

        #region Read Functions
        public byte ReadNextByte() => reader.ReadByte();
        #endregion

        #region Load Functions
        public static SpritesheetLoader Load(string filePath)
        {
            // Load the file.
            BinaryReader reader = new(File.OpenRead(Path.ChangeExtension(filePath, ncgrFileExtension)));

            // Load the headers.
            NDSFileUtil.loadGenericHeader(reader, true);
            ushort tileCount = loadHeader(reader);
            int startPosition = (int)reader.BaseStream.Position;

            // Return the loaded file.
            return new(reader, tileCount, startPosition);
        }

        /// <summary>
        /// Loads the NCGR header.
        /// </summary>
        /// <remarks>
        /// https://problemkaputt.de/gbatek.htm
        /// </remarks>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static ushort loadHeader(BinaryReader reader)
        {
            // Ensure the magic string matches.
            if (tileGraphicMagicWord != reader.ReadUInt32())
                throw new Exception($"Graphical file has invalid CHAR magic word.");

            // Load the header.
            uint sectionSize = reader.ReadUInt32();
            ushort tileDataSizeKB = reader.ReadUInt16();
            reader.BaseStream.Position += 2;
            uint bitDepth = reader.ReadUInt32();
            reader.BaseStream.Position += 8;
            uint tileDataSizeBytes = reader.ReadUInt32();
            uint headerOffset = reader.ReadUInt32();

            // Return the tile count.
            return (ushort)(tileDataSizeBytes / 64);
        }
        #endregion

        #region Disposal Functions
        public void Dispose()
        {
            reader.Dispose();
        }
        #endregion
    }
}

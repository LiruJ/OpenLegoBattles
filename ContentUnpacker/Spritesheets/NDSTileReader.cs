using ContentUnpacker.NDSFS;

namespace ContentUnpacker.Spritesheets
{
    /// <summary>
    /// Handles loading a spritesheet (NCGR file) and indexing it.
    /// </summary>
    internal class NDSTileReader : IDisposable
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

        /// <summary>
        /// The default width and height of a single tile.
        /// </summary>
        public const byte TileSize = 8;
        #endregion

        #region Fields
        /// <summary>
        /// The wrapped binary reader.
        /// </summary>
        private readonly BinaryReader reader;
        #endregion

        #region Properties
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
            get => (ushort)Math.Floor((reader.BaseStream.Position - StartPosition) / (float)(TileSize * TileSize));
            set => reader.BaseStream.Position = StartPosition + (value * TileSize * TileSize);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new reader from the given binary reader and parameters.
        /// </summary>
        /// <param name="reader"> The wrapped reader. </param>
        /// <param name="tileCount"> The total number of tiles in the file. </param>
        /// <param name="startPosition"> The start position of the data in the file. </param>
        public NDSTileReader(BinaryReader reader, ushort tileCount, int startPosition)
        {
            this.reader = reader;
            TileCount = tileCount;
            StartPosition = startPosition;
        }
        #endregion

        #region Read Functions
        /// <summary>
        /// Reads the next byte of the reader.
        /// </summary>
        /// <returns> The read byte. </returns>
        public byte ReadNextByte() => reader.ReadByte();
        #endregion

        #region Load Functions
        /// <summary>
        /// Loads the file at the given path as a spritesheet.
        /// </summary>
        /// <param name="filePath"> The path of the file to load. </param>
        /// <returns> The loaded spritesheet. </returns>
        public static NDSTileReader Load(string filePath)
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
            return (ushort)(tileDataSizeBytes / (TileSize * TileSize));
        }
        #endregion

        #region Disposal Functions
        /// <summary>
        /// Disposes of the underlying reader.
        /// </summary>
        public void Dispose() => reader.Dispose();
        #endregion
    }
}
namespace ContentUnpacker.Loaders
{
    internal class BasicLoader : ChunkLoader
    {
        #region Constants
        /// <summary>
        /// The magic number of an uncompressed chunk.
        /// </summary>
        public const byte MagicByte = 0x0;
        #endregion

        #region Constructors
        public BasicLoader(BinaryReader reader, int startOffset, BinaryWriter writer, List<uint> segmentSizes, uint uncompressedSize) : base(reader, startOffset, writer, segmentSizes, uncompressedSize)
        {
        }
        #endregion

        #region Load Functions
        public override async Task LoadChunkAsync()
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < uncompressedSize; i++)
                    writer.Write(reader.ReadByte());
            });
        }
        #endregion
    }
}
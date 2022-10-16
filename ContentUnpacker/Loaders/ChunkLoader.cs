namespace ContentUnpacker.Loaders
{
    internal abstract class ChunkLoader
    {
        #region Fields
        protected readonly BinaryReader reader;

        protected readonly List<uint> segmentSizes;

        protected readonly int startOffset;

        protected readonly BinaryWriter writer;

        protected readonly uint uncompressedSize;
        #endregion

        #region Constructors
        public ChunkLoader(BinaryReader reader, int startOffset, BinaryWriter writer, List<uint> segmentSizes, uint uncompressedSize)
        {
            // Set the dependencies.
            this.reader = reader;
            this.segmentSizes = segmentSizes;
            this.startOffset = startOffset;
            this.writer = writer;
            this.uncompressedSize = uncompressedSize;
        }
        #endregion

        #region Load Functions
        public static void ReadChunkHeader(BinaryReader reader, out uint uncompressedSize, out List<uint> segmentSizes, out byte compressionType)
        {
            // Read the compression data.
            uncompressedSize = reader.ReadUInt32();
            uint compressedSegmentCount = reader.ReadUInt32();
            uint largestCompressedSize = reader.ReadUInt32();

            // Read the segment sizes.
            segmentSizes = new((int)compressedSegmentCount);
            for (uint i = 0; i < compressedSegmentCount; i++)
                segmentSizes.Add(reader.ReadUInt32());

            // Get the chunk processor for the type of chunk.
            compressionType = reader.ReadByte();
            reader.BaseStream.Position--;
        }

        public virtual async Task LoadChunkAsync() { }
        #endregion
    }
}
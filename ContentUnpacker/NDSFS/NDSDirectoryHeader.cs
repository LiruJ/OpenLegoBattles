namespace ContentUnpacker.NDSFS
{
    internal struct NDSDirectoryHeader
    {
        #region Properties
        /// <summary>
        /// The offset of this directory's header data in the file.
        /// </summary>
        public int HeaderOffset { get; }

        /// <summary>
        /// The ID of the first file in this directory.
        /// </summary>
        public ushort FirstFileID { get; }

        /// <summary>
        /// The ID of this directory's parent. Note that for the root directory, this is actually how many total directories exist in the system.
        /// </summary>
        public ushort ParentDirectoryID { get; }
        #endregion

        #region Constructors
        public NDSDirectoryHeader(int headerOffset, ushort firstFileID, ushort parentDirectoryID)
        {
            HeaderOffset = headerOffset;
            FirstFileID = firstFileID;
            ParentDirectoryID = parentDirectoryID;
        }
        #endregion

        #region Load Functions
        public static NDSDirectoryHeader Load(BinaryReader reader, int fileNameTablePosition)
        {
            int relativeHeaderOffset = reader.ReadInt32();
            ushort firstFileID = reader.ReadUInt16();
            ushort parentDirectoryID = reader.ReadUInt16();
            return new(fileNameTablePosition + relativeHeaderOffset, firstFileID, parentDirectoryID);
        }
        #endregion
    }
}

using ContentUnpacker.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.NDSFS
{
    [DebuggerDisplay("{Name,nq} ({ID,nq,h})")]
    internal class NDSDirectory
    {
        #region Fields
        private string? path = null;

        private NDSDirectory? parent = null;

        private readonly List<NDSDirectory> subDirectories = new();

        private readonly List<NDSFile> files = new();
        #endregion

        #region Properties
        /// <summary>
        /// The relative path of this directory.
        /// </summary>
        public string? Path
        {
            get => path;
            set
            {
                // Ensure the state is correct.
                if (value == null || path != null)
                    throw new Exception("Path has already been set or is being set to invalid value.");

                // Set the path.
                path = value;
            }
        }

        /// <summary>
        /// The name of the directory.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The ID of the directory.
        /// </summary>
        public ushort ID { get; private set; }

        /// <summary>
        /// The ID of this directory's parent directory.
        /// </summary>
        public ushort ParentID { get; }

        /// <summary>
        /// The parent directory of this directory. This is null if this directory is the root.
        /// </summary>
        public NDSDirectory? Parent
        {
            get => parent;
            set
            {
                // Ensure the state is correct.
                if (parent != null)
                    throw new Exception("Directory's parent has already been set.");
                if (value == null || value.ID != ParentID)
                    throw new Exception("Cannot set parent of directory to null or directory with different ID.");

                // Set the parent directory.
                parent = value;
            }
        }

        /// <summary>
        /// The collection of sub-directories under this directory.
        /// </summary>
        public IReadOnlyList<NDSDirectory> SubDirectories => subDirectories;

        /// <summary>
        /// The collection of files in this directory.
        /// </summary>
        public IReadOnlyList<NDSFile> Files => files;
        #endregion

        #region Constructors
        public NDSDirectory(string name, ushort id, ushort parentID)
        {
            Name = name;
            ID = id;
            ParentID = parentID;
        }
        #endregion

        #region Load Functions
        public void LoadContents(BinaryReader reader, StringBuilder nameBuilder, int fileAllocationTablePosition, IReadOnlyList<NDSDirectoryHeader> directoryHeaders, ref int headerIndex)
        {
            // Get the header for this directory.
            NDSDirectoryHeader directoryInfo = directoryHeaders[headerIndex];

            // Save the original position and set the reader to the start position.
            int originalPosition = (int)reader.BaseStream.Position;
            reader.BaseStream.Position = directoryInfo.HeaderOffset;

            // Keep reading directory contents until the end byte is found.
            bool isEnd;
            ushort currentFileId = directoryInfo.FirstFileID;
            do
            {
                // Read the type/length byte and determine if it is a directory or file.
                byte typeLength = reader.ReadByte();
                int nameLength = typeLength & 0b0111_1111;
                bool isDirectory = (typeLength & 0b1000_0000) >= 1;
                isEnd = typeLength == 0;

                // If the type is not the end, load the file/directory.
                if (!isEnd)
                {
                    // Read the name of the file/directory.
                    string name = NDSFileUtil.ReadString(reader, nameBuilder, nameLength);

                    // If the content is a directory, load its ID, load it, and add it as such.
                    if (isDirectory)
                    {
                        // Create the sub-directory and add it to this directory.
                        ushort directoryId = reader.ReadUInt16();
                        ushort parentID = directoryHeaders[headerIndex + 1].ParentDirectoryID;
                        NDSDirectory subDirectory = new(name, directoryId, parentID);
                        subDirectories.Add(subDirectory);

                        // Increment the header index and load the sub-directory.
                        headerIndex++;
                        subDirectory.LoadContents(reader, nameBuilder, fileAllocationTablePosition, directoryHeaders, ref headerIndex);
                    }
                    // Otherwise if the content is a file, load it and add it as such, and increment the file id.
                    else
                    {
                        // Read the data offset of the file.
                        long savedPosition = reader.BaseStream.Position;
                        reader.BaseStream.Position = fileAllocationTablePosition + (currentFileId * 8);
                        int fileDataOffset = reader.ReadInt32();
                        int fileDataEndOffset = reader.ReadInt32();
                        reader.BaseStream.Position = savedPosition;

                        // Create the file with its name, id, and offsets.
                        NDSFile file = new(name, currentFileId, fileDataOffset, fileDataEndOffset - fileDataOffset);
                        files.Add(file);
                        currentFileId++;
                    }
                }
            } while (!isEnd);

            // Reset the reader to its original position.
            reader.BaseStream.Position = originalPosition;
        }

        public static NDSDirectory CreateRoot() =>
            new("Root", 0xF000, 0)
            {
                Path = string.Empty
            };
        #endregion
    }
}

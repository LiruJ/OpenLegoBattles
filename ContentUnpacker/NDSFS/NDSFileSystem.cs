using System.Text;

namespace ContentUnpacker.NDSFS
{
    internal class NDSFileSystem
    {
        #region Constants
        /// <summary>
        /// What the rom file should start with.
        /// </summary>
        private const string fileMagicString = "LEGO BATTLES";

        /// <summary>
        /// The size of the expected file.
        /// </summary>
        private const long expectedFileSize = 0x8000000;
        #endregion

        #region Fields
        private readonly Dictionary<ushort, NDSDirectory> directoriesByID = new();

        private readonly Dictionary<string, NDSDirectory> directoriesByPath = new();

        private readonly Dictionary<ushort, NDSFile> filesById = new();

        private readonly Dictionary<string, NDSFile> filesByPath = new();
        #endregion

        #region Properties
        /// <summary>
        /// The root directory of the file system. 
        /// </summary>
        public NDSDirectory RootDirectory { get; }

        /// <summary>
        /// The collection of directories keyed by ID.
        /// </summary>
        public IReadOnlyDictionary<ushort, NDSDirectory> DirectoriesByID => directoriesByID;

        /// <summary>
        /// The collection of directories keyed by relative path, where the root folder is <see cref="string.Empty"/>.
        /// </summary>
        public IReadOnlyDictionary<string, NDSDirectory> DirectoriesByPath => directoriesByPath;

        /// <summary>
        /// The collection of files keyed by ID.
        /// </summary>
        public IReadOnlyDictionary<ushort, NDSFile> FilesById => filesById;

        /// <summary>
        /// The collection of files keyed by relative path.
        /// </summary>
        public IReadOnlyDictionary<string, NDSFile> FilesByPath => filesByPath;
        #endregion

        #region Constructors
        private NDSFileSystem()
        {
            RootDirectory = NDSDirectory.CreateRoot();
        }
        #endregion

        #region File System Functions
        private void registerAllFilesAndDirectories(NDSDirectory directory)
        {
            // Register the directory itself with its ID.
            directoriesByID.TryAdd(directory.ID, directory);

            // Register each of the directory's files by ID. This will throw an exception if the file is being registered twice.
            foreach (NDSFile file in directory.Files)
                filesById.Add(file.ID, file);

            // Recursively register all sub directories.
            foreach (NDSDirectory subDirectory in directory.SubDirectories)
                registerAllFilesAndDirectories(subDirectory);
        }

        private void finaliseDirectoryStructure()
        {
            // Set the parent of each directory.
            foreach (NDSDirectory directory in DirectoriesByID.Values)
            {
                // Skip the root directory as it has no parent.
                if (directory == RootDirectory) continue;

                // Set the parent of the directory based on its parent id.
                directory.Parent = DirectoriesByID[directory.ParentID];
            }
        }

        private void registerAllFileAndDirectoryPaths(NDSDirectory directory, StringBuilder pathBuilder)
        {
            // Handle the path of non-root directories.
            if (directory != RootDirectory)
            {
                // Append the name of the directory. If the directory is the root, append nothing.
                pathBuilder.Append(directory.Name);
                pathBuilder.Append('/');

                // Set the directory's path.
                directory.Path = pathBuilder.ToString();
            }

            // Register the directory.
            directoriesByPath.Add(directory.Path ?? throw new Exception("Directory path was somehow null."), directory);

            // Register all files in this directory.
            int directoryPathLength = pathBuilder.Length;
            foreach (NDSFile file in directory.Files)
            {
                // Construct the filepath then immediately reset the string builder to its directory length.
                pathBuilder.Append(file.Name);
                string filepath = pathBuilder.ToString();
                pathBuilder.Length = directoryPathLength;

                // Register the file by its path.
                filesByPath.Add(filepath, file);
                file.Path = filepath;
            }

            // Recursively register all sub-directories.
            foreach (NDSDirectory subDirectory in directory.SubDirectories)
            {
                registerAllFileAndDirectoryPaths(subDirectory, pathBuilder);
                pathBuilder.Length = directoryPathLength;
            }
        }
        #endregion

        #region Load Functions
        public static NDSFileSystem LoadFromRom(string filePath)
        {
            // Create the binary reader for the file.
            using BinaryReader reader = new(File.OpenRead(filePath));
            if (!assertRomValidity(reader))
                throw new Exception("Invalid ROM file");

            // Write the starting string.
            Console.WriteLine("Reading filesystem");

            // Create the file system.
            NDSFileSystem fileSystem = new();

            // Read the file table info from the cart header.
            reader.BaseStream.Position = 0x40;
            int fileNameTablePosition = reader.ReadInt32();
            int fileNameTableSize = reader.ReadInt32();
            int fileAllocationTablePosition = reader.ReadInt32();
            int fileAllocationTableSize = reader.ReadInt32();

            // Start reading the file name table.
            reader.BaseStream.Position = fileNameTablePosition;

            // Create a string builder for more efficient reading of names.
            StringBuilder nameBuilder = new();

            // Read the headers first.
            NDSDirectoryHeader rootHeader = NDSDirectoryHeader.Load(reader, fileNameTablePosition);
            ushort subDirectoryCount = rootHeader.ParentDirectoryID;
            NDSDirectoryHeader[] directoryHeaders = new NDSDirectoryHeader[subDirectoryCount];
            directoryHeaders[0] = rootHeader;
            for (int i = 1; i < subDirectoryCount; i++)
                directoryHeaders[i] = NDSDirectoryHeader.Load(reader, fileNameTablePosition);

            // Load the root directory contents, which recursively builds the whole structure.
            int headerIndex = 0;
            fileSystem.RootDirectory.LoadContents(reader, nameBuilder, fileAllocationTablePosition, directoryHeaders, ref headerIndex);

            // Register all files and directories into the file system, then finalise the structure.
            fileSystem.registerAllFilesAndDirectories(fileSystem.RootDirectory);
            fileSystem.finaliseDirectoryStructure();

            // Register the file and directory paths so everything can be indexed by path.
            nameBuilder.Clear();
            fileSystem.registerAllFileAndDirectoryPaths(fileSystem.RootDirectory, nameBuilder);

            // Write the stopping string.
            Console.WriteLine($"Finished reading filesystem of {fileSystem.FilesById.Count} files and {fileSystem.DirectoriesByID.Count} directories");

            // Return the created file system.
            return fileSystem;
        }

        private static bool assertRomValidity(BinaryReader reader)
        {
            // Ensure the file is valid.
            if (reader.BaseStream.Length != expectedFileSize)
                throw new Exception($"Invalid filesize, expected: 0x{expectedFileSize:X8}, got: 0x{reader.BaseStream.Length:X8}");

            // Ensure the file starts with "LEGO BATTLES".
            for (int i = 0; i < fileMagicString.Length; i++)
            {
                // Get the current character.
                char currentCharacter = reader.ReadChar();

                // If the character is unexpected, stop.
                if (currentCharacter != fileMagicString[i])
                    throw new Exception($"Invalid character in magic string at 0x{reader.BaseStream.Position:X8} expected: {fileMagicString[i]}, got: {currentCharacter}");
            }

            // Return true, as the rom is valid.
            return true;
        }
        #endregion
    }
}

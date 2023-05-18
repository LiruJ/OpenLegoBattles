using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;
using ContentUnpacker.Tilemaps;
using System.Collections.Concurrent;

namespace ContentUnpacker
{
    /// <summary>
    /// Unpacks and decompresses all required data from the rom file and saves it to an output.
    /// </summary>
    internal static class RomUnpacker
    {
        #region Constants
        /// <summary>
        /// The name of the folder used to store any temporary files during the decompression/loading step.
        /// </summary>
        public const string WorkingFolderName = "Working";

        public const byte MainVersion = 0;

        public const byte SubVersion = 1;

        public const byte PatchVersion = 1;
        #endregion

        #region XML Constants
        /// <summary>
        /// The name of the main node.
        /// </summary>
        private const string mainNodeName = "Content";
        #endregion

        #region Load Functions
        public static async Task UnpackFileAsync(CommandLineOptions options)
        {
            // Write the starting string.
            Console.WriteLine($"Rom Unpacker {MainVersion}.{SubVersion}.{PatchVersion} started");
            
            // Check that the parameters are correct.
            if (string.IsNullOrWhiteSpace(options.InputFile) || !File.Exists(options.InputFile))
                throw new FileNotFoundException("Input file is invalid or does not exist.", options.InputFile);
            if (string.IsNullOrWhiteSpace(options.OutputFolder))
                throw new ArgumentException($"'{nameof(options.OutputFolder)}' cannot be null or whitespace.", nameof(options.OutputFolder));

            // Create the file system from the rom.
            NDSFileSystem fileSystem = NDSFileSystem.LoadFromRom(options.InputFile);
            
            // Load the file's nodes.
            await loadMainNodeAsync(options, fileSystem);

            // Write the stopping string.
            Console.WriteLine($"Rom Unpacker has successfully unpacked rom. Enjoy!");
        }

        private static async Task loadMainNodeAsync(CommandLineOptions options, NDSFileSystem fileSystem)
        {
            // TODO: Implement logging rather than silent errors or exceptions.


            // Create a collection to hold pooled binary readers.
            ConcurrentQueue<BinaryReader> pooledBinaryReaders = new();

#if !CONTENTTEST
            // Create the unpacked files directory.
            if (Directory.Exists(WorkingFolderName))
                Directory.Delete(WorkingFolderName, true);
            Directory.CreateDirectory(WorkingFolderName);

            // Begin each stage in sequence.
            await DecompressionStage.BeginAsync(options, fileSystem);
#endif
            await TilemapOptimiserStage.BeginAsync(options);

#if RELEASE
            // Delete the unpacked folder.
            Directory.Delete(WorkingFolderName, true);
#endif

            // Close all binary readers.
            foreach (BinaryReader reader in pooledBinaryReaders)
                reader.Close();
            pooledBinaryReaders.Clear();
        }
#endregion
    }
}
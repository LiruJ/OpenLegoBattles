using ContentUnpacker.NDSFS;
using System.Collections.Concurrent;

namespace ContentUnpacker.Decompressors
{
    internal static class DecompressionStage
    {
        private class progressTracker
        {
            /// <summary>
            /// The total number of tasks to complete.
            /// </summary>
            public int TotalCount { get; set; }

            /// <summary>
            /// The current number of completed tasks.
            /// </summary>
            public int DoneCount { get; set; } = 0;

            /// <summary>
            /// The percentage of tasks completed, from 0 to 1.
            /// </summary>
            public float ProgressPercentage => (float)DoneCount / TotalCount;
        }

        #region Constants
        /// <summary>
        /// The path of the folder where the decompressed files are stored, relative to the working folder.
        /// </summary>
        public const string OutputFolderPath = "Decompressed";
        #endregion

        #region Stage Functions
        public static async Task BeginAsync(CommandLineOptions options, NDSFileSystem fileSystem)
        {
            // Write the starting strings.
            await Console.Out.WriteLineAsync("Extracting files");
            await Console.Out.WriteLineAsync("Thanks to CUE for the decompressors!");

            // Create the required directories.
            Directory.CreateDirectory(Path.Combine(RomUnpacker.WorkingFolderName, OutputFolderPath, LegoDecompressor.TemporaryFolderPath));

            // Create the reader pool.
            ConcurrentQueue<BinaryReader> pooledReaders = new();

            // Go over each content node in the main node.
            progressTracker progressTracker = new();
            Progress<string> progress = new((filePath) => reportProgress(filePath, progressTracker));

            // Decompress all files.
            progressTracker.TotalCount = fileSystem.FilesByPath.Count;
            await Parallel.ForEachAsync(fileSystem.FilesByPath.Values, async (file, _) => await decompressFile(options, file, pooledReaders, progress));

            // Close the readers.
            foreach (BinaryReader reader in pooledReaders)
                reader.Dispose();

            // Write the ending string.
            await Console.Out.WriteLineAsync("Finished extracting files");
        }

        private static void reportProgress(string filePath, progressTracker progressTracker)
        {
            progressTracker.DoneCount++;
            Console.WriteLine($"{progressTracker.ProgressPercentage:P0} - {filePath}");
        }
        #endregion

        #region Decompression Functions
        private static async Task decompressFile(CommandLineOptions options, NDSFile file, ConcurrentQueue<BinaryReader> pooledReaders, IProgress<string> progress)
        {
            // Get or create a pooled binary reader and prepare it for the file.
            if (!pooledReaders.TryDequeue(out BinaryReader? reader))
                reader = new BinaryReader(File.OpenRead(options.InputFile));
            reader.BaseStream.Position = file.Offset;

            // Decompress the file and report the progress.
            await LegoDecompressor.DecompressFileAsync(reader, file);
            progress.Report(file.Path ?? "Missing");

            // Add the reader back to the queue.
            pooledReaders.Enqueue(reader);
        }
        #endregion
    }
}
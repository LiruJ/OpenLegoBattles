using ContentUnpacker.NDSFS;
using ContentUnpacker.Utils;
using System.Collections.Concurrent;
using System.Xml;

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
        public static async Task BeginAsync(CommandLineOptions options, NDSFileSystem fileSystem, XmlNode mainNode)
        {
            // Write the starting string.
            Console.WriteLine("Extracting files");

            // Create the required directories.
            Directory.CreateDirectory(Path.Combine(RomUnpacker.WorkingFolderName, OutputFolderPath, LegoDecompressor.TemporaryFolderPath));

            // Create the reader pool.
            ConcurrentQueue<BinaryReader> pooledReaders = new();

            // Go over each content node in the main node.
            progressTracker progressTracker = new();
            Progress<string> progress = new((filename) => reportProgress(filename, progressTracker));
            List<Task> outputTasks = new();
            foreach (XmlNode contentNode in mainNode)
            {
                // Ignore comments and load each node.
                if (contentNode.NodeType != XmlNodeType.Element) continue;
                decompressContentNode(options, fileSystem, contentNode, pooledReaders, outputTasks, progress);
            }

            // Handle setting up the progress reporter.
            progressTracker.TotalCount = outputTasks.Count;

            // Start decompressing the files.
            await Task.WhenAll(outputTasks);

            // Close the readers.
            foreach (BinaryReader reader in pooledReaders)
                reader.Dispose();

            // Write the ending string.
            Console.WriteLine("Finished extracting files");
        }

        private static void reportProgress(string filename, progressTracker progressTracker)
        {
            progressTracker.DoneCount++;
            Console.WriteLine($"{progressTracker.ProgressPercentage:P0} - {filename}");
        }
        #endregion

        #region Decompression Functions
        private static void decompressContentNode(CommandLineOptions options, NDSFileSystem fileSystem, XmlNode contentNode, ConcurrentQueue<BinaryReader> pooledReaders, List<Task> outputTasks, IProgress<string> progress)
        {
            // Get the file/directory attributes.
            bool hasFile = contentNode.TryGetTextAttribute("RomFile", out string romFilePath);
            bool hasDirectory = contentNode.TryGetTextAttribute("RomDirectory", out string romDirectoryPath);

            // Ensure the node doesn't have both a file and a directory.
            if (hasDirectory && hasFile)
            {
                Console.WriteLine($"Node {contentNode.Name} contains both path and file, only one is needed. Skipping.");
                return;
            }

            // Ensure the node has at least one input defined.
            if (!hasDirectory && !hasFile)
            {
                Console.WriteLine($"Node {contentNode.Name} contains neither path nor file, one is needed. Skipping.");
                return;
            }

            // Handle decompressing the content.
            if (hasDirectory)
            {
                IEnumerator<Task> directoryTasks = decompressDirectory(options, fileSystem, contentNode, romDirectoryPath, pooledReaders, progress);
                while (directoryTasks.MoveNext())
                    outputTasks.Add(directoryTasks.Current);
            }
            else if (hasFile)
                outputTasks.Add(decompressFile(options, fileSystem, contentNode, romFilePath, pooledReaders, progress));
        }

        private static IEnumerator<Task> decompressDirectory(CommandLineOptions options, NDSFileSystem fileSystem, XmlNode contentNode, string romDirectoryPath, ConcurrentQueue<BinaryReader> pooledReaders, IProgress<string> progress)
        {
            // Ensure the directory exists.
            if (!fileSystem.DirectoriesByPath.TryGetValue(romDirectoryPath, out NDSDirectory? directory))
            {
                Console.WriteLine($"Node {contentNode.Name} has an invalid rom directory path. Skipping.");
                yield break;
            }

            // Decompress each file.
            foreach (NDSFile contentFile in directory.Files)
                yield return decompressFile(options, contentFile, pooledReaders, progress);
        }

        private static Task decompressFile(CommandLineOptions options, NDSFileSystem fileSystem, XmlNode contentNode, string romFilePath, ConcurrentQueue<BinaryReader> pooledReaders, IProgress<string> progress)
        {
            // Ensure the file exists.
            if (!fileSystem.FilesByPath.TryGetValue(romFilePath, out NDSFile? file))
            {
                Console.WriteLine($"Node {contentNode.Name} has an invalid rom file path. Skipping.");
                return Task.CompletedTask;
            }

            // Decompress the file.
            return decompressFile(options, file, pooledReaders, progress);
        }

        private static Task decompressFile(CommandLineOptions options, NDSFile file, ConcurrentQueue<BinaryReader> pooledReaders, IProgress<string> progress)
        {
            // Decompress the file.
            Task decompressionTask = Task.Run(async () =>
            {
                // Get or create a pooled binary reader and prepare it for the file.
                if (!pooledReaders.TryDequeue(out BinaryReader? reader))
                    reader = new BinaryReader(File.OpenRead(options.InputFile));
                reader.BaseStream.Position = file.Offset;

                // Decompress the file and report the progress.
                await LegoDecompressor.DecompressFileAsync(reader, file);
                progress.Report(file.Name);

                // Add the reader back to the queue.
                pooledReaders.Enqueue(reader);
            });

            // Return the task.
            return decompressionTask;
        }
        #endregion
    }
}
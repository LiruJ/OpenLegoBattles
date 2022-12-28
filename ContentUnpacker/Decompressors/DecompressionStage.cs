using ContentUnpacker.NDSFS;
using ContentUnpacker.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ContentUnpacker.Decompressors
{
    internal class DecompressionStage
    {
        #region Constants
        /// <summary>
        /// The path of the folder where the decompressed files are stored, relative to the working folder.
        /// </summary>
        public const string OutputFolderPath = "Decompressed";
        #endregion

        #region Dependencies
        private readonly RomUnpacker romUnpacker;
        private readonly NDSFileSystem fileSystem;
        #endregion

        #region Fields
        private readonly ConcurrentQueue<BinaryReader> pooledReaders = new();
        #endregion

        #region Constructors
        public DecompressionStage(RomUnpacker romUnpacker, NDSFileSystem fileSystem)
        {
            this.romUnpacker = romUnpacker;
            this.fileSystem = fileSystem;
        }
        #endregion

        #region Stage Functions
        public async Task BeginAsync(XmlNode mainNode)
        {
            // Create the required directories.
            Directory.CreateDirectory(Path.Combine(RomUnpacker.WorkingFolderName, OutputFolderPath, LegoDecompressor.TemporaryFolderPath));

            // Save the decompression tasks.
            List<Task> decompressionTasks = new();

            // Go over each content node in the main node.
            foreach (XmlNode contentNode in mainNode)
            {
                // Ignore comments.
                if (contentNode.NodeType != XmlNodeType.Element) continue;

                decompressContentNode(contentNode, decompressionTasks);
            }

            // Wait for the tasks to be done.
            await Task.WhenAll(decompressionTasks);
        }
        #endregion

        #region Decompression Functions
        private void decompressContentNode(XmlNode contentNode, List<Task> decompressionTasks)
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
            if (hasDirectory) decompressDirectory(contentNode, romDirectoryPath, decompressionTasks);
            else if (hasFile) decompressFile(contentNode, romFilePath, decompressionTasks);
        }

        private void decompressDirectory(XmlNode contentNode, string romDirectoryPath, List<Task> decompressionTasks)
        {
            // Ensure the directory exists.
            if (!fileSystem.DirectoriesByPath.TryGetValue(romDirectoryPath, out NDSDirectory? directory))
            {
                Console.WriteLine($"Node {contentNode.Name} has an invalid rom directory path. Skipping.");
                return;
            }

            // Decompress each file.
            foreach (NDSFile contentFile in directory.Files)
                decompressionTasks.Add(decompressFileAsync(contentFile));
        }

        private void decompressFile(XmlNode contentNode, string romFilePath, List<Task> decompressionTasks)
        {
            // Ensure the file exists.
            if (!fileSystem.FilesByPath.TryGetValue(romFilePath, out NDSFile? file))
            {
                Console.WriteLine($"Node {contentNode.Name} has an invalid rom file path. Skipping.");
                return;
            }

            // Decompress the file.
            decompressionTasks.Add(decompressFileAsync(file));
        }

        private async Task decompressFileAsync(NDSFile file)
        {
            // Get or create a pooled binary reader and prepare it for the file.
            if (!pooledReaders.TryDequeue(out BinaryReader? reader))
                reader = new BinaryReader(File.OpenRead(romUnpacker.Options.InputFile));
            reader.BaseStream.Position = file.Offset;

            // Decompress the file.
            await LegoDecompressor.DecompressFileAsync(reader, file);

            // Add the reader back to the queue.
            pooledReaders.Enqueue(reader);
        }
        #endregion
    }
}
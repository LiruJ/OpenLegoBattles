using ContentUnpacker.Loaders;
using ContentUnpacker.Processors;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Xml;

namespace ContentUnpacker
{
    /// <summary>
    /// Unpacks and decompresses all required data from the rom file and saves it to an output.
    /// </summary>
    internal static class RomUnpacker
    {
        #region Constants
        /// <summary>
        /// The name of the folder that holds all of the unpacked files while they are being processed.
        /// </summary>
        public const string UnpackedFolderName = "Unpacked";

        /// <summary>
        /// What the rom file should start with.
        /// </summary>
        private const string fileMagicString = "LEGO BATTLES";

        /// <summary>
        /// The size of the expected file.
        /// </summary>
        private const long expectedFileSize = 0x8000000;

        /// <summary>
        /// What a compressed chunk should start with, "PMOC".
        /// </summary>
        private const uint compressedMagicString = 0x434F4D50;
        #endregion

        #region XML Constants
        /// <summary>
        /// The name of the main node.
        /// </summary>
        private const string mainNodeName = "Content";

        /// <summary>
        /// The name of the xml attribute responsible for specifying the offset within the file.
        /// </summary>
        private const string offsetAttributeName = "Offset";

        /// <summary>
        /// The name of the xml attribute responsible for specifying the output file.
        /// </summary>
        private const string outputAttributeName = "Output";

        /// <summary>
        /// The name of the xml attribute responsible for specifying the processor.
        /// </summary>
        private const string processorAttributeName = "Processor";
        #endregion

        #region Fields
        private static readonly Dictionary<string, Type> contentProcessorsByName = new();
        #endregion

        #region Properties
        public static string TemporaryDirectoryPath { get; private set; } = "";
        #endregion

        #region Constructors
        static RomUnpacker()
        {
            contentProcessorsByName.Add(nameof(TilemapProcessor), typeof(TilemapProcessor));
        }
        #endregion

        #region Load Functions
        public static async Task UnpackFileAsync(string romInputPath, string outputDirectoryPath, ILogger logger)
        {
            // Check that the parameters are correct.
            if (string.IsNullOrWhiteSpace(romInputPath) || !File.Exists(romInputPath))
            {
                logger.LogCritical("Input file is invalid or does not exist: {inputPath}", romInputPath);
                throw new FileNotFoundException("Input file is invalid or does not exist.", romInputPath);
            }
            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                logger.LogCritical("'{outputDirectoryPath}' cannot be null or whitespace.", nameof(outputDirectoryPath));
                throw new ArgumentException($"'{nameof(outputDirectoryPath)}' cannot be null or whitespace.", nameof(outputDirectoryPath));
            }

            // Create the binary reader for the file.
            using BinaryReader binaryReader = new(File.OpenRead(romInputPath));

            // Ensure the file is valid.
            if (binaryReader.BaseStream.Length != expectedFileSize)
                throw new Exception($"Invalid filesize, expected: 0x{expectedFileSize:X8}, got: 0x{binaryReader.BaseStream.Length:X8}");

            // Ensure the file starts with "LEGO BATTLES".
            for (int i = 0; i < fileMagicString.Length; i++)
            {
                // Get the current character.
                char currentCharacter = binaryReader.ReadChar();

                // If the character is unexpected, stop.
                if (currentCharacter != fileMagicString[i])
                    throw new Exception($"Invalid character in magic string at 0x{binaryReader.BaseStream.Position:X8} expected: {fileMagicString[i]}, got: {currentCharacter}");
            }

            // Load the xml file.
            XmlDocument contentDescription = new();
            contentDescription.Load("OffsetOutputs.xml");

            // Load the file's nodes.
            await loadMainNodeAsync(contentDescription, romInputPath, outputDirectoryPath);
        }

        private static async Task loadMainNodeAsync(XmlDocument contentDescription, string romInputPath, string outputRootPath)
        {
            // TODO: Implement logging rather than silent errors or exceptions.

            // Ensure the main node exists.
            XmlNode? mainNode = contentDescription.SelectSingleNode(mainNodeName);
            if (mainNode == null)
                throw new Exception($"Content description xml is missing main node named {mainNode}");

            // Create a collection to hold pooled binary readers.
            ConcurrentQueue<BinaryReader> pooledBinaryReaders = new();

            // Save the loading tasks of each chunk.
            List<Task> loadingTasks = new(mainNode.ChildNodes.Count);

            // Create the unpacked files directory.
            if (Directory.Exists(UnpackedFolderName))
                Directory.Delete(UnpackedFolderName, true);
            Directory.CreateDirectory(UnpackedFolderName);

            // Create the temporary files directory.
            TemporaryDirectoryPath = Path.Combine(UnpackedFolderName, "temp");
            Directory.CreateDirectory(TemporaryDirectoryPath);

            // Go over each node in the xml; unpacking, decompressing, and saving the content.
            foreach (XmlNode contentNode in mainNode)
            {
                // Ensure both attributes exist.
                XmlAttribute? offsetAttribute = contentNode.Attributes?[offsetAttributeName];
                XmlAttribute? outputAttribute = contentNode.Attributes?[outputAttributeName];
                if (offsetAttribute == null || outputAttribute == null)
                    continue;

                // Get the filename, ensuring it has an extension.
                string unpackedOutputPath = Path.GetFullPath(Path.Combine(UnpackedFolderName, outputAttribute.Value));
                unpackedOutputPath = Path.ChangeExtension(unpackedOutputPath, ".bin");

                string? unpackedOutputDirectoryPath = Path.GetDirectoryName(unpackedOutputPath);
                if (unpackedOutputDirectoryPath == null)
                {
                    Console.WriteLine($"Content node named \"{contentNode.Name}\" has invalid file path \"{unpackedOutputDirectoryPath}\", skipping.");
                    continue;
                }

                // Parse the offset.
                if (!int.TryParse(offsetAttribute.Value, System.Globalization.NumberStyles.HexNumber, null, out int offsetPosition))
                {
                    Console.WriteLine($"Content node named \"{contentNode.Name}\" has invalid offset \"{offsetAttribute.Value}\", should be a hex number WITHOUT the \"0x\" prefix, skipping.");
                    continue;
                }

                // Get or create a pooled binary reader.
                if (!pooledBinaryReaders.TryDequeue(out var reader))
                    reader = new BinaryReader(File.OpenRead(romInputPath));

                // Move the binary reader to the offset.
                reader.BaseStream.Position = offsetPosition;

                // Ensure the chunk starts with the magic string "PMOC" (COMP backwards, meaning compressed).
                uint testPMOC = reader.ReadUInt32();
                //if (testPMOC != compressedMagicString)
                //{
                //    Console.WriteLine($"Content node named \"{contentNode.Name}\" has invalid magic string character at offset: {reader.BaseStream.Position:X8}, expected: {compressedMagicString}, got: {testPMOC}, skipping.");
                //    continue;
                //}

                // Load the chunk.
                loadingTasks.Add(loadChunkAsync(reader, offsetPosition, unpackedOutputPath, unpackedOutputDirectoryPath, pooledBinaryReaders));



                Console.WriteLine($"Successfully loaded \"{contentNode.Name}\" node to \"{unpackedOutputPath}\"");
            }

            // Wait for all chunks to be processed.
            await Task.WhenAll(loadingTasks);

            // The list of processing tasks to await.
            List<Task> processingTasks = new();

            // Go over the content nodes again, this time processing them.
            foreach (XmlNode contentNode in mainNode)
            {
                // Get the processor type of the content.
                string? processorName = contentNode.Attributes?[processorAttributeName]?.Value;

                // If a processor name was given, process it.
                if (!string.IsNullOrWhiteSpace(processorName))
                {
                    // Try get the processor associated with the name.
                    if (!contentProcessorsByName.TryGetValue(processorName + "Processor", out var processorType))
                    {
                        Console.WriteLine($"Content node named \"{contentNode.Name}\" has invalid content processor name \"{processorName}\".");
                        continue;
                    }

                    // Get the name of the input file. This is the output path of the loading step.
                    string inputPath = contentNode.Attributes?[outputAttributeName]?.Value ?? throw new Exception("Somehow, the output path was null during the processing phase.");
                    inputPath = Path.ChangeExtension(Path.Combine(UnpackedFolderName, inputPath), ".bin");

                    // Create the processor instance.
                    ContentProcessor processor = (ContentProcessor?)Activator.CreateInstance(processorType) ?? throw new Exception("Processor name was valid, yet processor failed to create.");

                    // Process the data.
                    processingTasks.Add(processor.ProcessAsync(inputPath, outputRootPath));
                }
            }

            // Wait for all the processing tasks.
            await Task.WhenAll(processingTasks);

            // Delete the unpacked folder.
            //Directory.Delete(UnpackedFolderName, true);

            // Close all binary readers.
            foreach (BinaryReader reader in pooledBinaryReaders)
                reader.Dispose();
        }

        private static async Task loadChunkAsync(BinaryReader reader, int offsetPosition, string unpackedOutputPath, string unpackedOutputDirectoryPath, ConcurrentQueue<BinaryReader> pooledBinaryReaders)
        {
            // Read the chunk header.
            ChunkLoader.ReadChunkHeader(reader, out uint uncompressedSize, out List<uint> segmentSizes, out byte compressionType);

            // Create the output directories, if they do not exist.
            Directory.CreateDirectory(unpackedOutputDirectoryPath);

            // Create the output file.
            using BinaryWriter writer = new(File.Create(unpackedOutputPath));

            // Handle the loader.
            ChunkLoader loader;
            switch (compressionType)
            {
                case LZXLoader.MagicByte:
                    loader = new LZXLoader(reader, offsetPosition, writer, segmentSizes, uncompressedSize);
                    break;
                case BasicLoader.MagicByte:
                    loader = new BasicLoader(reader, offsetPosition, writer, segmentSizes, uncompressedSize);
                    break;
                default:
                    return;
            }

            // Load the chunk.
            await loader.LoadChunkAsync();

            // Return the reader to the pool.
            pooledBinaryReaders.Enqueue(reader);
        }
        #endregion
    }
}
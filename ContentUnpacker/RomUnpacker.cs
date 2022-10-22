using ContentUnpacker.Data;
using ContentUnpacker.Decompressors;
using ContentUnpacker.Processors;
using ContentUnpacker.Utils;
using Microsoft.Extensions.Logging;
using Shared.Content;
using System.Collections.Concurrent;
using System.Xml;

namespace ContentUnpacker
{
    /// <summary>
    /// Unpacks and decompresses all required data from the rom file and saves it to an output.
    /// </summary>
    internal class RomUnpacker
    {
        #region Constants
        /// <summary>
        /// The name of the folder used to store any temporary files during the decompression/loading step.
        /// </summary>
        public const string WorkingFolderName = "Working";

        /// <summary>
        /// What the rom file should start with.
        /// </summary>
        private const string fileMagicString = "LEGO BATTLES";

        /// <summary>
        /// The size of the expected file.
        /// </summary>
        private const long expectedFileSize = 0x8000000;
        #endregion

        #region XML Constants
        /// <summary>
        /// The name of the main node.
        /// </summary>
        private const string mainNodeName = "Content";
        #endregion

        #region Fields
        private readonly Dictionary<uint, Type> contentProcessorsByName = new();

        private readonly ConcurrentDictionary<string, int> outputOffsets = new();

        private readonly ConcurrentQueue<BinaryReader> pooledBinaryReaders = new();

        #endregion

        #region Properties
        public CommandLineOptions Options { get; }

        public ILogger Logger { get; }

        public DataCache DataCache { get; }
        #endregion

        #region Constructors
        public RomUnpacker(CommandLineOptions options, ILogger logger)
        {
            // Set dependencies.
            Options = options;
            Logger = logger;

            DataCache = new(this);

            // Register the processors.
            registerProcessors();
        }
        #endregion

        #region Initialisation Functions
        private void registerProcessors()
        {
            contentProcessorsByName.Add(TileGraphicProcessor.MagicWord, typeof(TileGraphicProcessor));
            contentProcessorsByName.Add(TilemapProcessor.MagicWord, typeof(TilemapProcessor));
        }
        #endregion

        #region Reader Functions
        public BinaryReader GetReaderForFilePath(string filePath, out bool manualClose)
        {
            string rawFilename = Path.GetFileNameWithoutExtension(filePath);

            // If the node name is associated with an offset in the main file, get a pooled reader.
            BinaryReader? reader;
            manualClose = !outputOffsets.TryGetValue(rawFilename, out int offsetPosition);
            if (!manualClose)
            {
                // Get or create a pooled binary reader.
                if (!pooledBinaryReaders.TryDequeue(out reader))
                    reader = new BinaryReader(File.OpenRead(Options.InputFile));

                // Move the binary reader to the offset.
                reader.BaseStream.Position = offsetPosition;
            }
            // Otherwise, a new reader has to be made to read the specific decompressed file.
            else
            {
                // Get the full path of the file.
                string fullPath = Path.ChangeExtension(Path.IsPathFullyQualified(filePath) ? filePath : Path.GetFullPath(filePath), ContentFileUtil.BinaryExtension);

                // Open the file in a new reader.
                reader = new(File.OpenRead(fullPath));
            }

            // Return the reader.
            return reader;
        }

        public void ReturnReader(BinaryReader reader) => pooledBinaryReaders.Enqueue(reader);
        #endregion

        #region Load Functions
        public async Task UnpackFileAsync()
        {
            // Check that the parameters are correct.
            if (string.IsNullOrWhiteSpace(Options.InputFile) || !File.Exists(Options.InputFile))
            {
                Logger.LogCritical("Input file is invalid or does not exist: {inputPath}", Options.InputFile);
                throw new FileNotFoundException("Input file is invalid or does not exist.", Options.InputFile);
            }
            if (string.IsNullOrWhiteSpace(Options.OutputFolder))
            {
                Logger.LogCritical("'{outputDirectoryPath}' cannot be null or whitespace.", nameof(Options.OutputFolder));
                throw new ArgumentException($"'{nameof(Options.OutputFolder)}' cannot be null or whitespace.", nameof(Options.OutputFolder));
            }

            // Create the binary reader for the file.
            BinaryReader reader = new(File.OpenRead(Options.InputFile));

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

            // Pool the reader.
            pooledBinaryReaders.Enqueue(reader);

            // Load the xml file.
            XmlDocument contentDescription = new();
            contentDescription.Load("OffsetOutputs.xml");

            // Load the file's nodes.
            await loadMainNodeAsync(contentDescription);
        }

        private async Task loadMainNodeAsync(XmlDocument contentDescription)
        {
            // TODO: Implement logging rather than silent errors or exceptions.

            // Ensure the main node exists.
            XmlNode? mainNode = contentDescription.SelectSingleNode(mainNodeName);
            if (mainNode == null)
                throw new Exception($"Content description xml is missing main node named {mainNode}");

            // Create a collection to hold pooled binary readers.
            ConcurrentQueue<BinaryReader> pooledBinaryReaders = new();

            // Create the unpacked files directory.
            if (Directory.Exists(WorkingFolderName))
                Directory.Delete(WorkingFolderName, true);
            Directory.CreateDirectory(WorkingFolderName);

            // Decompress chunks and save the offsets of any uncompressed chunks.
            await decompressChunksAsync(mainNode);

            // Process chunks.
            processChunks(mainNode);

#if RELEASE
            // Delete the unpacked folder.
            Directory.Delete(WorkingFolderName, true);
#endif

            // Close all binary readers.
            foreach (BinaryReader reader in pooledBinaryReaders)
                reader.Close();
        }
        #endregion

        #region Decompression Functions
        private async Task decompressChunksAsync(XmlNode mainNode)
        {
            // Create the required directories.
            Directory.CreateDirectory(Path.Combine(WorkingFolderName, LegoDecompressor.OutputFolderPath, LegoDecompressor.TemporaryFolderPath));

            // Save the decompression tasks.
            List<Task> decompressionTasks = new();

            // Decompress any compressed chunks.
            foreach (XmlNode contentNode in mainNode)
            {
                // Ignore comments.
                if (contentNode.NodeType != XmlNodeType.Element) continue;

                // Parse the offset position.
                if (!contentNode.TryParseOffsetAttribute(out int offsetPosition))
                {
                    Console.WriteLine($"Content node named \"{contentNode.Name}\" has invalid offset, should be a hex number WITHOUT the \"0x\" prefix, skipping.");
                    continue;
                }

                // Decompress the chunk.
                decompressionTasks.Add(decompressChunkAsync(offsetPosition, contentNode.Name));
            }

            // Wait for all decompression tasks to finish.
            await Task.WhenAll(decompressionTasks);
        }

        private async Task decompressChunkAsync(int offsetPosition, string filename)
        {
            // Get or create a pooled binary reader.
            if (!pooledBinaryReaders.TryDequeue(out BinaryReader? reader))
                reader = new BinaryReader(File.OpenRead(Options.InputFile));

            // Move the binary reader to the offset.
            reader.BaseStream.Position = offsetPosition;

            // Peek the magic word.
            uint magicWord = reader.ReadUInt32();
            reader.BaseStream.Position -= 4;

            // If the magic word says that the chunk is compressed, decompress it. Otherwise; save the offset to the dictionary of offsets.
            if (magicWord == LegoDecompressor.MagicWord)
                await LegoDecompressor.DecompressChunkAsync(reader, filename);
            else
                outputOffsets.TryAdd(Path.GetFileNameWithoutExtension(filename), offsetPosition);

            // Return the reader to the pool.
            pooledBinaryReaders.Enqueue(reader);
        }
        #endregion

        #region Process Functions
        private void processChunks(XmlNode mainNode)
        {
            // Go over each node again, but this time take the outputs from decompression and run them through the processors.
            foreach (XmlNode contentNode in mainNode)
            {
                // Ignore comments.
                if (contentNode.NodeType != XmlNodeType.Element) continue;

                // Get a reader for this file.
                string filePath = Path.ChangeExtension(Path.Combine(WorkingFolderName, LegoDecompressor.OutputFolderPath, contentNode.Name), ContentFileUtil.BinaryExtension);
                BinaryReader reader = GetReaderForFilePath(filePath, out bool manualClose);

                // Process the chunk.
                processChunk(reader, contentNode);

                // If the chunk was read from a file, the reader must be closed.
                if (manualClose) reader.Close();
            }
        }

        private void processChunk(BinaryReader reader, XmlNode contentNode)
        {
            // Get the loader type from the magic 4 chars.
            uint magicWord = reader.ReadUInt32();

            // Try get the processor associated with the name.
            if (!contentProcessorsByName.TryGetValue(magicWord, out Type? processorType))
            {
                Console.WriteLine($"Content node's {contentNode.Name}'s file's magic word {magicWord} is not associated with any processor; skipping processing step.");
                return;
            }

            // Create the processor instance.
            ContentProcessor processor = (ContentProcessor?)Activator.CreateInstance(processorType, this, reader, contentNode) ?? throw new Exception("Processor name was valid, yet processor failed to create.");

            // Process the data.
            processor.Process();
        }
        #endregion
    }
}
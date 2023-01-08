using ContentUnpacker.NDSFS;
using GlobalShared.Content;
using System.Diagnostics;

namespace ContentUnpacker.Decompressors
{
    /// <summary>
    /// Decompressor for Lego Battles' custom compression format.
    /// </summary>
    internal static class LegoDecompressor
    {
        #region Constants
        /// <summary>
        /// The name of the tool used to decode.
        /// </summary>
        private const string decoderToolName = "lzx.exe";

        /// <summary>
        /// The magic number of an uncompressed chunk.
        /// </summary>
        private const byte uncompressedMagicByte = 0x0;

        /// <summary>
        /// The magic byte that the lzx decompresser looks for.
        /// </summary>
        private const byte lzxMagicByte = 0x11;

        /// <summary>
        /// The magic byte that this loader looks for. "PMOC".
        /// </summary>
        public const uint MagicWord = 0x434F4D50;

        /// <summary>
        /// The folder path relative to the <see cref="OutputFolderPath"/> that stores the temporary files used to communicate with the decompression tools.
        /// </summary>
        public const string TemporaryFolderPath = "Temporary";
        #endregion

        #region Load Functions
        public static async Task DecompressFileAsync(BinaryReader reader, NDSFile file)
        {
            // Ensure the file is valid.
            if (file.Path == null)
                throw new ArgumentException("File's path was somehow null.");

            // Create the output path so that it mirrors the NDS structure.
            string outputPath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using BinaryWriter writer = new(File.Create(outputPath));

            // Peek the magic word.
            uint magicWord = reader.ReadUInt32();
            reader.BaseStream.Position -= 4;

            // If the file is compressed, decompress it.
            if (magicWord == MagicWord)
            {
                // Read the chunk header.
                readChunkHeader(reader, out uint uncompressedSize, out List<uint> segmentSizes, out byte compressionType);

                // Handle the compression type. Note that for whatever reason, it can be a compressed header with an uncompressed body.
                switch (compressionType)
                {
                    case lzxMagicByte:
                        await loadLZXAsync(reader, writer, segmentSizes, file);
                        break;
                    case uncompressedMagicByte:
                        await loadUncompressedAsync(reader, writer, uncompressedSize);
                        break;
                    default:
                        Console.WriteLine($"Unhandled compression type: {compressionType}.");
                        break;
                }
            }
            // Otherwise; just copy it directly.
            else
                await loadUncompressedAsync(reader, writer, (uint)file.Size);
        }

        private static void readChunkHeader(BinaryReader reader, out uint uncompressedSize, out List<uint> segmentSizes, out byte compressionType)
        {
            // Skip the magic word.
            reader.BaseStream.Position += 4;

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

        private static async Task loadLZXAsync(BinaryReader reader, BinaryWriter writer, List<uint> segmentSizes, NDSFile file)
        {
            // Save the filenames and tasks for each segment.
            List<string> segmentFilenames = new(segmentSizes.Count);

            // Go over each compressed segment.
            for (int segmentIndex = 0; segmentIndex < segmentSizes.Count; segmentIndex++)
            {
                // Get the size of the segment.
                uint segmentSize = segmentSizes[segmentIndex];

                // Create the filename so that this thread and segment get a unique filename.
                string segmentTempFilename = Path.ChangeExtension($"{Path.GetFileName(file.Path).Replace('.', '_')}-{Environment.CurrentManagedThreadId}-{segmentIndex}", ContentFileUtil.TemporaryExtension);
                string segmentTempFilePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, TemporaryFolderPath, segmentTempFilename);
                segmentFilenames.Add(segmentTempFilePath);

                // Read the segment from the file into a new temporary file.
                using BinaryWriter tempFileWriter = new(File.OpenWrite(segmentTempFilePath));
                for (uint byteIndex = 0; byteIndex < segmentSize; byteIndex++)
                    tempFileWriter.Write(reader.ReadByte());
                tempFileWriter.Close();

                // Run the decoder on the file.
                ProcessStartInfo processStartInfo = new(decoderToolName, $"-d \"{segmentTempFilePath}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };
                Process? process = new();
                process.StartInfo = processStartInfo;
                process.Start();
                if (process == null) throw new Exception("LZX processor exe could not be opened.");

                // Add the done task to the list.
                await process.WaitForExitAsync();
            }

            // Go over each temporary file and write to the main file, deleting temporary files as they are done.
            for (int segmentIndex = 0; segmentIndex < segmentFilenames.Count; segmentIndex++)
            {
                string segmentFilename = segmentFilenames[segmentIndex];

                // Get the size of the segment.
                uint segmentSize = segmentSizes[segmentIndex];

                // Write the bytes to the file.
                using BinaryReader tempFileReader = new(File.OpenRead(segmentFilename));
                for (int byteIndex = 0; byteIndex < tempFileReader.BaseStream.Length; byteIndex++)
                    writer.Write(tempFileReader.ReadByte());
                tempFileReader.Close();

                // Delete the file.
                File.Delete(segmentFilename);
            }
        }

        private static async Task loadUncompressedAsync(BinaryReader reader, BinaryWriter writer, uint uncompressedSize)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < uncompressedSize; i++)
                    writer.Write(reader.ReadByte());
            });
        }
        #endregion
    }
}
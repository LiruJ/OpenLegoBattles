using Shared.Content;
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

        /// <summary>
        /// The path of the folder where the decompressed files are stored, relative to the working folder.
        /// </summary>
        public const string OutputFolderPath = "Decompressed";
        #endregion

        #region Load Functions
        public static async Task DecompressChunkAsync(BinaryReader reader, string filename)
        {
            // Read the chunk header.
            readChunkHeader(reader, out uint uncompressedSize, out List<uint> segmentSizes, out byte compressionType);

            // Make the file writer.
            string outputPath = Path.ChangeExtension(Path.Combine(RomUnpacker.WorkingFolderName, OutputFolderPath, filename), ContentFileUtil.BinaryExtension);
            using BinaryWriter writer = new(File.Create(outputPath));

            switch (compressionType)
            {
                case lzxMagicByte:
                    await loadLZXAsync(reader, writer, segmentSizes, filename);
                    break;
                case uncompressedMagicByte:
                    await loadUncompressed(reader, writer, uncompressedSize);
                    break;
                default:
                    break;
            }
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

        private static async Task loadLZXAsync(BinaryReader reader, BinaryWriter writer, List<uint> segmentSizes, string filename)
        {
            // Save the filenames and tasks for each segment.
            List<string> segmentFilenames = new(segmentSizes.Count);

            // Go over each compressed segment.
            for (int segmentIndex = 0; segmentIndex < segmentSizes.Count; segmentIndex++)
            {
                // Get the size of the segment.
                uint segmentSize = segmentSizes[segmentIndex];

                // Create the filename so that this thread and segment get a unique filename.
                string segmentTempFilename = Path.ChangeExtension(Path.Combine(RomUnpacker.WorkingFolderName, OutputFolderPath, TemporaryFolderPath, $"{filename}-{segmentIndex}"), ContentFileUtil.TemporaryExtension);
                segmentFilenames.Add(segmentTempFilename);

                // Read the segment from the file into a new temporary file.
                using BinaryWriter tempFileWriter = new(File.OpenWrite(segmentTempFilename));
                for (uint byteIndex = 0; byteIndex < segmentSize; byteIndex++)
                    tempFileWriter.Write(reader.ReadByte());
                tempFileWriter.Close();

                // Run the decoder on the file.
                ProcessStartInfo processStartInfo = new(decoderToolName, $"-d \"{segmentTempFilename}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };
                Process? process = new();
                process.StartInfo = processStartInfo;
                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.Start();
                process.BeginOutputReadLine();
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

        private static async Task loadUncompressed(BinaryReader reader, BinaryWriter writer, uint uncompressedSize)
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
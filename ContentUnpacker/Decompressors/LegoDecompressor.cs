using ContentUnpacker.NDSFS;
using GlobalShared.Content;
using System.Runtime.InteropServices;

namespace ContentUnpacker.Decompressors
{
    /// <summary>
    /// Decompressor for Lego Battles' custom compression format.
    /// </summary>
    internal static class LegoDecompressor
    {
        #region External Functions
        [DllImport("NDSDecompressors.dll", EntryPoint = "LZX_Decode", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void DecodeLZXFile(string filename);
        #endregion

        #region Constants
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
            if (string.IsNullOrWhiteSpace(file.Path))
                throw new ArgumentException("File's path was somehow null.");

            // Create the output path so that it mirrors the NDS structure.
            string outputPath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using BinaryWriter writer = new(File.Create(outputPath));

            // Peek the magic word.
            uint magicWord = reader.ReadUInt32();
            reader.BaseStream.Position -= 4;

            // If the file has no compression, just copy it over and do nothing more.
            if (magicWord != MagicWord)
            {
                await loadUncompressedAsync(reader, writer, (uint)file.Size);
                return;
            }

            // Read the compressed chunk header.
            readChunkHeader(reader, out uint uncompressedSize, out List<uint> segmentSizes);

            // Go over each segment.
            foreach (uint segmentSize in segmentSizes)
            {
                // Get the chunk processor for the type of chunk.
                byte compressionType = reader.ReadByte();

                // Handle the compression type.
                switch (compressionType)
                {
                    case lzxMagicByte:
                        reader.BaseStream.Position--;
                        await loadLZXAsync(reader, writer, segmentSize, file);
                        break;
                    case uncompressedMagicByte:
                        await loadUncompressedAsync(reader, writer, segmentSize - 1);
                        break;
                    default:
                        await Console.Out.WriteLineAsync($"Unhandled compression type: {compressionType} in file {file.Path}. Skipping");
                        writer.Close();
                        return;
                }
            }
        }

        private static void readChunkHeader(BinaryReader reader, out uint uncompressedSize, out List<uint> segmentSizes)
        {
            // Skip the magic word.
            reader.BaseStream.Position += 4;

            // Read the compression data.
            uncompressedSize = reader.ReadUInt32();
            uint compressedSegmentCount = reader.ReadUInt32();
            uint largestCompressedSize = (uint)Math.Abs(reader.ReadInt32());

            // Read the segment sizes.
            segmentSizes = new((int)compressedSegmentCount);
            for (uint i = 0; i < compressedSegmentCount; i++)
                segmentSizes.Add((uint)Math.Abs(reader.ReadInt32()));
        }

        private static async Task loadLZXAsync(BinaryReader reader, BinaryWriter writer, uint segmentSize, NDSFile file)
        {
            // Create the filename and path for the temporary file.
            string segmentFilename = Path.ChangeExtension(file.Path.Replace('.', '_').Replace('/', '_'), ContentFileUtil.TemporaryExtension);
            string segmentFilePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, TemporaryFolderPath, segmentFilename);

            // Read the segment from the file into a new temporary file.
            using BinaryWriter tempFileWriter = new(File.OpenWrite(segmentFilePath));
            for (uint byteIndex = 0; byteIndex < segmentSize; byteIndex++)
                tempFileWriter.Write(reader.ReadByte());
            tempFileWriter.Close();
            
            // Decompress the file.
            await Task.Run(() => DecodeLZXFile(segmentFilePath));

            // Write the bytes to the main file.
            using BinaryReader tempFileReader = new(File.OpenRead(segmentFilePath));
            for (int byteIndex = 0; byteIndex < tempFileReader.BaseStream.Length; byteIndex++)
                writer.Write(tempFileReader.ReadByte());
            tempFileReader.Close();
            
            // Delete the temporary file.
            File.Delete(segmentFilePath);
        }

        private static async Task loadUncompressedAsync(BinaryReader reader, BinaryWriter writer, uint size)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < size; i++)
                    writer.Write(reader.ReadByte());
            });
        }
        #endregion
    }
}
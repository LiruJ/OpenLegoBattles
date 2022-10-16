using System.Diagnostics;

namespace ContentUnpacker.Loaders
{
    internal class LZXLoader : ChunkLoader
    {
        #region Constants
        /// <summary>
        /// The name of the tool used to decode.
        /// </summary>
        private const string decoderToolName = "lzx.exe";

        /// <summary>
        /// The name of the temporary file used to communicate with the decoder.
        /// </summary>
        private const string tempFilename = "TempData";

        /// <summary>
        /// The magic byte that this processor looks for. 
        /// </summary>
        public const byte MagicByte = 0x11;
        #endregion

        #region Constructors
        public LZXLoader(BinaryReader reader, int startOffset, BinaryWriter writer, List<uint> segmentSizes, uint uncompressedSize) : base(reader, startOffset, writer, segmentSizes, uncompressedSize)
        {
        }
        #endregion

        #region Load Functions
        public override async Task LoadChunkAsync()
        {
            // Save the filenames and tasks for each segment.
            List<string> segmentFilenames = new(segmentSizes.Count);
            List<Task> segmentTasks = new(segmentSizes.Count);

            // Go over each compressed segment.
            for (int segmentIndex = 0; segmentIndex < segmentSizes.Count; segmentIndex++)
            {
                // Get the size of the segment.
                uint segmentSize = segmentSizes[segmentIndex];

                // Create the filename so that this thread and segment get a unique filename.
                string segmentTempFilename = Path.Combine(RomUnpacker.TemporaryDirectoryPath, $"{tempFilename}-{Environment.CurrentManagedThreadId}-{segmentIndex}.tmp");
                segmentFilenames.Add(segmentTempFilename);

                // Read the segment from the file into a new temporary file.
                using BinaryWriter tempFileWriter = new(File.OpenWrite(segmentTempFilename));
                for (uint byteIndex = 0; byteIndex < segmentSize; byteIndex++)
                    tempFileWriter.Write(reader.ReadByte());
                tempFileWriter.Dispose();

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
                //segmentTasks.Add(process.WaitForExitAsync());

                process.WaitForExit();
            }

            // Wait for each task to be done.
            //await Task.WhenAll(segmentTasks);

            // Go over each temporary file and write to the main file, deleting temporary files as they are done.
            for (int segmentIndex = 0; segmentIndex < segmentFilenames.Count; segmentIndex++)
            {
                string filename = segmentFilenames[segmentIndex];

                // Get the size of the segment.
                uint segmentSize = segmentSizes[segmentIndex];

                // Write the bytes to the file.
                using BinaryReader tempFileReader = new(File.OpenRead(filename));
                for (int byteIndex = 0; byteIndex < tempFileReader.BaseStream.Length; byteIndex++)
                    writer.Write(tempFileReader.ReadByte());
                tempFileReader.Dispose();
                
                // Delete the file.
                File.Delete(filename);
            }
        }
        #endregion
    }
}
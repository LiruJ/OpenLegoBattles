﻿using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OpenLegoBattles.RomContent
{
    /// <summary>
    /// Handles communication with the rom unpacking program.
    /// </summary>
    internal class RomUnpacker
    {
        #region Constants
        private const string unpackerFolderName = "RomUnpacker";

        private const string unpackerProgramName = "ContentUnpacker.exe";
        #endregion

        #region Properties
        public string BaseGameDirectory { get; }
        
        public string LastUnpackerMessage { get; private set; } = string.Empty;

        public bool HasUnpacked => FindIfHasUnpacked(BaseGameDirectory);
        #endregion

        #region Constructors
        public RomUnpacker(string baseGameDirectory)
        {
            BaseGameDirectory = baseGameDirectory;
        }
        #endregion

        #region Unpack Functions
        public static bool FindIfHasUnpacked(string baseGameDirectory) => Directory.Exists(baseGameDirectory);

        public async Task UnpackRomAsync(string romPath)
        {
            // Ensure the file exists.
            if (string.IsNullOrEmpty(romPath) || !File.Exists(romPath))
                throw new FileNotFoundException("Rom path could not be found.", romPath);

#if DEBUG
            // Copy the unpacker tool over for debug builds.
            if (Directory.Exists(unpackerFolderName))
                Directory.Delete(unpackerFolderName, true);
            string path = Path.GetFullPath("../../../../ContentUnpacker/bin/Release/net6.0");
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(path, unpackerFolderName);
#endif

#if CONTENTTEST
            if (Directory.Exists(BaseGameDirectory))
                Directory.Delete(BaseGameDirectory, true);

            // When testing content, run the content test content tool, which skips the decompression stage. This is faster than running the whole process.
            string contentTestPath = "../../../../ContentUnpacker/bin/ContentTest/net6.0/";
            ProcessStartInfo processStartInfo = new(Path.Combine(contentTestPath, unpackerProgramName), $"-i \"{romPath}\" -o \"{Path.GetFullPath(BaseGameDirectory)}\"")
            {
                WorkingDirectory = Path.GetFullPath(contentTestPath),
#else
            // In debug or release, just use the regular tool that's bundled in the bin's folder.
            ProcessStartInfo processStartInfo = new(Path.Combine(unpackerFolderName, unpackerProgramName), $"-i \"{romPath}\" -o \"{Path.GetFullPath(BaseGameDirectory)}\"")
            {
                WorkingDirectory = Path.GetFullPath(unpackerFolderName),
#endif

#if RELEASE
                // Hide the window in release builds.
                CreateNoWindow = true,
#endif
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Process unpackerProcess = new()
            {
                StartInfo = processStartInfo,
            };
            unpackerProcess.OutputDataReceived += (_, eventArgs) => LastUnpackerMessage = eventArgs.Data;
            unpackerProcess.Start();
            unpackerProcess.BeginOutputReadLine();

            // Wait for the process to stop.
            await unpackerProcess.WaitForExitAsync();
        }
        #endregion
    }
}
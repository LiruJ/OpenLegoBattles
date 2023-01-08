using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using OpenLegoBattles.RomContent.Loaders;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OpenLegoBattles.RomContent
{
    internal class RomContentManager
    {
        #region Constants
        public const string BaseContentFolderName = "BaseGame";

        private const string unpackerFolderName = "RomUnpacker";

        private const string unpackerProgramName = "ContentUnpacker.exe";
        #endregion

        #region Dependencies
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Fields
        private readonly Dictionary<Type, IRomContentLoader> contentLoadersByType = new();
        #endregion

        #region Properties
        public string RootDirectory { get; }

        public string BaseGameDirectory => Path.Combine(RootDirectory, BaseContentFolderName);

        public bool HasUnpacked => Directory.Exists(Path.Combine(RootDirectory, BaseContentFolderName));
        #endregion

        #region Constructors
        public RomContentManager(GraphicsDevice graphicsDevice, string rootDirectory)
        {
            // Set dependencies.
            this.graphicsDevice = graphicsDevice;

            // Set the root directory.
            RootDirectory = rootDirectory;

            // Load all default loaders.
            registerDefaultLoaders();
        }
        #endregion

        #region Loader Functions
        public RomContentLoader<T> GetLoaderForType<T>() where T : class => (RomContentLoader<T>)contentLoadersByType[typeof(T)];
        #endregion

        #region Registration Functions
        public void RegisterLoader<T>(IRomContentLoader contentLoader) => contentLoadersByType.Add(typeof(T), contentLoader);

        private void registerDefaultLoaders()
        {
            RegisterLoader<Texture2D>(new TextureLoader(this, graphicsDevice));
            RegisterLoader<TilemapData>(new TilemapLoader(this, graphicsDevice));
            RegisterLoader<Spritesheet>(new TilesetLoader(this));
        }
        #endregion

        #region Load Functions
        public T Load<T>(string path) where T : class
        {
           
            // Get the reader for the given type.
            if (!contentLoadersByType.TryGetValue(typeof(T), out IRomContentLoader loader))
                throw new ArgumentException("Given type has no associated loader.");

            // Load the file using the reader and loader.
            object content = loader.LoadObjectFromPath(path);

            // Ensure the object is valid.
            if (content == null || content is not T castContent)
                throw new Exception("Loaded content was null or not of the correct type.");

            // Return the content.
            return castContent;
        }
        #endregion

        #region Unpack Functions
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

            ProcessStartInfo processStartInfo = new(Path.Combine(unpackerFolderName, unpackerProgramName), $"-i \"{romPath}\" -o \"{Path.GetFullPath(Path.Combine(RootDirectory, BaseContentFolderName))}\"")
            {
                WorkingDirectory = Path.GetFullPath(unpackerFolderName),
#if DEBUG
                CreateNoWindow = false,
#elif RELEASE
                CreateNoWindow = true,
#endif
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Process unpackerProcess = new()
            {
                StartInfo = processStartInfo,
            };
            unpackerProcess.OutputDataReceived += UnpackerProcess_OutputDataReceived;
            unpackerProcess.Start();
            unpackerProcess.BeginOutputReadLine();


            await unpackerProcess.WaitForExitAsync();
        }

        private void UnpackerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
        #endregion
    }
}

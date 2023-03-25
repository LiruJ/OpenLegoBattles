using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using OpenLegoBattles.RomContent.Loaders;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenLegoBattles.RomContent
{
    public class RomContentManager
    {
        #region Constants
        public const string BaseContentFolderName = "BaseGame";
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

        /// <summary>
        /// Unloads all cached content tracked by this manager.
        /// </summary>
        public void Unload()
        {
            foreach (IRomContentLoader contentLoader in contentLoadersByType.Values)
                contentLoader.Unload();
        }
        #endregion
    }
}

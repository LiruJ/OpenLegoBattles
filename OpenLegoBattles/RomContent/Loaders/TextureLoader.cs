using Microsoft.Xna.Framework.Graphics;
using GlobalShared.Content;
using System;
using System.Collections.Generic;

namespace OpenLegoBattles.RomContent.Loaders
{
    internal class TextureLoader : RomContentLoader<Texture2D>, IDisposable
    {
        #region Dependencies
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Fields
        /// <summary>
        /// The cache of loaded textures, keyed by full path (no extension).
        /// </summary>
        private readonly Dictionary<string, Texture2D> cachedTextures = new();
        #endregion

        #region Constructors
        public TextureLoader(RomContentManager romContentManager, GraphicsDevice graphicsDevice) : base(romContentManager)
        {
            this.graphicsDevice = graphicsDevice;
        }
        #endregion

        #region Load Functions
        public override Texture2D LoadFromPath(string path)
        {
            // Convert the path into a full path with the .png extension.
            path = ContentFileUtil.CreateFullFilePath(romContentManager.BaseGameDirectory, ContentFileUtil.SpriteDirectoryName, path, ContentFileUtil.SpriteExtension);

            // If the texture has not yet been loaded, load it.
            if (!cachedTextures.TryGetValue(path, out Texture2D texture))
            {
                texture = Texture2D.FromFile(graphicsDevice, path);
                cachedTextures.Add(path, texture);
            }

            // Return the cached or loaded texture.
            return texture;
        }

        public void Unload()
        {
            foreach (Texture2D texture in cachedTextures.Values)
                if (!texture.IsDisposed) texture.Dispose();
            cachedTextures.Clear();
        }

        public void Dispose() => Unload();
        #endregion
    }
}

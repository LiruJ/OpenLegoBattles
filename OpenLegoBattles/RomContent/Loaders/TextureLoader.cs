using Microsoft.Xna.Framework.Graphics;
using GlobalShared.Content;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// The collection of textures that are disposed of when the content manager is unloaded.
        /// </summary>
        private readonly HashSet<Texture2D> managedTextures = new();
        #endregion

        #region Constructors
        public TextureLoader(RomContentManager romContentManager, GraphicsDevice graphicsDevice) : base(romContentManager)
        {
            this.graphicsDevice = graphicsDevice;
        }
        #endregion

        #region Cache Functions
        /// <summary>
        /// Adds a texture to the managed textures collection so that when <see cref="Unload"/> is called, the texture is disposed.
        /// </summary>
        /// <param name="texture"> The texture to manage. </param>
        public void AddManagedTexture(Texture2D texture) => managedTextures.Add(texture);

        /// <summary>
        /// Removes a texture from the managed textures collection.
        /// </summary>
        /// <param name="texture"> The texture to remove. </param>
        /// <seealso cref="AddManagedTexture(Texture2D)"/>
        public void RemoveManagedTexture(Texture2D texture) => managedTextures.Remove(texture);
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
                AddManagedTexture(texture);
            }
            
            // Return the cached or loaded texture.
            return texture;
        }

        public void Unload(Texture2D texture)
        {
            // Dispose of the texture and remove it from the collections.
            if (!texture.IsDisposed) texture.Dispose();
            managedTextures.Remove(texture);
            // TODO: Make this better.
            foreach (KeyValuePair<string, Texture2D> otherTexture in cachedTextures)
                if (texture == otherTexture.Value)
                {
                    cachedTextures.Remove(otherTexture.Key);
                    break;
                }
        }

        public void Unload()
        {
            foreach (Texture2D texture in managedTextures)
                if (!texture.IsDisposed) texture.Dispose();
            cachedTextures.Clear();
            managedTextures.Clear();
        }

        public void Dispose() => Unload();
        #endregion
    }
}

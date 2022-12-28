using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using GlobalShared.Content;
using System.IO;

namespace OpenLegoBattles.RomContent.Loaders
{
    /// <summary>
    /// Handles loading sprite tileset (tst) files.
    /// </summary>
    internal class TilesetLoader : RomContentLoader<Spritesheet>
    {
        #region Constructors
        public TilesetLoader(RomContentManager romContentManager) : base(romContentManager)
        {       
        }
        #endregion

        #region Load Functions
        public override Spritesheet LoadFromPath(string path)
        {
            // Get the full path.
            path = ContentFileUtil.CreateFullFilePath(romContentManager.BaseGameDirectory, ContentFileUtil.TilesetDirectoryName, path, ContentFileUtil.TilesetExtension);

            // Create the reader.
            using BinaryReader reader = new(File.OpenRead(path));

            // Read the total tile count.
            ushort tileCount = reader.ReadUInt16();

            // Read the size of the sprite in tiles.
            byte width = reader.ReadByte();
            byte height = reader.ReadByte();

            // Read the name of the texture.
            string spriteName = reader.ReadString();

            // Load the texture.
            Texture2D texture = romContentManager.Load<Texture2D>(spriteName);

            // Return the created tilesheet.
            return new Spritesheet(texture, width, height);
        }
        #endregion
    }
}

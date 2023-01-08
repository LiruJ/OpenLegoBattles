using GlobalShared.Content;
using GlobalShared.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using OpenLegoBattles.TilemapSystem;
using System.IO;

namespace OpenLegoBattles.RomContent.Loaders
{
    internal class TilemapLoader : RomContentLoader<TilemapData>
    {
        #region Dependencies
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Constructors
        public TilemapLoader(RomContentManager romContentManager, GraphicsDevice graphicsDevice) : base(romContentManager)
        {
            this.graphicsDevice = graphicsDevice;
        }
        #endregion

        #region Load Functions
        public override TilemapData LoadFromPath(string path)
        {
            // Apply the root folder and extension to the path.
            path = ContentFileUtil.CreateFullFilePath(romContentManager.BaseGameDirectory, ContentFileUtil.TilemapDirectoryName, path, ContentFileUtil.TilemapExtension);

            // If the file does not exist, throw an exception.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException("File could not be found.", path);

            return TilemapData.Load(path);
        }
        #endregion

        #region Helper Functions
        public Spritesheet CreateTilePaletteTexture(TilemapData tilemapData, TilemapBlockPalette treePalette, TilemapBlockPalette fogPalette)
        {
            // Load the spritesheet for the tilemap.
            Spritesheet spritesheet = romContentManager.Load<Spritesheet>(tilemapData.TilesheetName);

            // Get the texture loader.
            TextureLoader textureLoader = (TextureLoader)romContentManager.GetLoaderForType<Texture2D>();

            // Make a new texture and add it to the texture loader.
            const int textureDimensionBlocks = 30;
            int textureWidth = spritesheet.TileSize.X * 3 * textureDimensionBlocks;
            int textureHeight = spritesheet.TileSize.Y * 2 * textureDimensionBlocks;
            RenderTarget2D texture = new(graphicsDevice, textureWidth, textureHeight);
            textureLoader.AddManagedTexture(texture);
            Spritesheet packedSpritesheet = new(texture, textureDimensionBlocks, textureDimensionBlocks);

            // Create a spritebatch and prepare to draw to the texture.
            using SpriteBatch creatorSpriteBatch = new(graphicsDevice);
            graphicsDevice.SetRenderTarget(texture);
            graphicsDevice.Clear(Color.Transparent);
            creatorSpriteBatch.Begin();

            // Write the trees, then the fog.
            int destinationIndex = 0;
            for (int i = 0; i < treePalette.Count; i++, destinationIndex++)
                writeTileBlock(treePalette[i], destinationIndex, spritesheet, packedSpritesheet, creatorSpriteBatch);
            for (int i = 0; i < fogPalette.Count; i++, destinationIndex++)
                writeTileBlock(fogPalette[i], destinationIndex, spritesheet, packedSpritesheet, creatorSpriteBatch);

            // Write the terrain last.
            for (int i = 0; i < tilemapData.TilePalette.Count; i++, destinationIndex++)
                writeTileBlock(tilemapData.TilePalette[i], destinationIndex, spritesheet, packedSpritesheet, creatorSpriteBatch);

            creatorSpriteBatch.End();
            graphicsDevice.SetRenderTarget(null);

            // Unload the original texture.
            textureLoader.Unload(spritesheet.Texture);

            return packedSpritesheet;
        }

        private void writeTileBlock(TilemapPaletteBlock block, int index, Spritesheet sourceSpritesheet, Spritesheet destinationSpritesheet, SpriteBatch creatorSpriteBatch)
        {
            Point blockPosition = destinationSpritesheet.CalculateXYFromIndex(index);
            int screenX = blockPosition.X * sourceSpritesheet.TileSize.X * 3;
            int screenY = blockPosition.Y * sourceSpritesheet.TileSize.Y * 2;
            int bottomRowScreenY = screenY + sourceSpritesheet.TileSize.Y;

            Rectangle source = sourceSpritesheet.CalculateSourceRectangle(block.TopLeft);
            creatorSpriteBatch.Draw(sourceSpritesheet.Texture, new Rectangle(screenX, screenY, sourceSpritesheet.TileSize.X, sourceSpritesheet.TileSize.Y), source, Color.White);

            source = sourceSpritesheet.CalculateSourceRectangle(block.TopMiddle);
            creatorSpriteBatch.Draw(sourceSpritesheet.Texture, new Rectangle(screenX + sourceSpritesheet.TileSize.X, screenY, sourceSpritesheet.TileSize.X, sourceSpritesheet.TileSize.Y), source, Color.White);

            source = sourceSpritesheet.CalculateSourceRectangle(block.TopRight);
            creatorSpriteBatch.Draw(sourceSpritesheet.Texture, new Rectangle(screenX + (sourceSpritesheet.TileSize.X * 2), screenY, sourceSpritesheet.TileSize.X, sourceSpritesheet.TileSize.Y), source, Color.White);

            source = sourceSpritesheet.CalculateSourceRectangle(block.BottomLeft);
            creatorSpriteBatch.Draw(sourceSpritesheet.Texture, new Rectangle(screenX, bottomRowScreenY, sourceSpritesheet.TileSize.X, sourceSpritesheet.TileSize.Y), source, Color.White);

            source = sourceSpritesheet.CalculateSourceRectangle(block.BottomMiddle);
            creatorSpriteBatch.Draw(sourceSpritesheet.Texture, new Rectangle(screenX + sourceSpritesheet.TileSize.X, bottomRowScreenY, sourceSpritesheet.TileSize.X, sourceSpritesheet.TileSize.Y), source, Color.White);

            source = sourceSpritesheet.CalculateSourceRectangle(block.BottomRight);
            creatorSpriteBatch.Draw(sourceSpritesheet.Texture, new Rectangle(screenX + (sourceSpritesheet.TileSize.X * 2), bottomRowScreenY, sourceSpritesheet.TileSize.X, sourceSpritesheet.TileSize.Y), source, Color.White);
        }
        #endregion
    }
}

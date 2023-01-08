using GlobalShared.Content;
using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.RomContent.Loaders;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLegoBattles.Rendering
{
    internal class TileGraphicsManager
    {
        #region Dependencies
        private readonly RomContentManager romContentManager;
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Properties
        /// <summary>
        /// The currently loaded tilesheet for maps.
        /// </summary>
        public Spritesheet Tilesheet { get; private set; }

        /// <summary>
        /// The first index of the first terrain block in the tilesheet.
        /// </summary>
        public ushort TerrainBlockOffset => (ushort)(TreeRuleSet.PaletteCount + FogRuleSet.PaletteCount);

        /// <summary>
        /// The connection rule set for drawing trees.
        /// </summary>
        public ConnectionRuleSet TreeRuleSet { get; private set; }


        /// <summary>
        /// The connection rule set for drawing fog.
        /// </summary>
        public ConnectionRuleSet FogRuleSet { get; private set; }
        #endregion

        #region Constructors
        private TileGraphicsManager(RomContentManager romContentManager, GraphicsDevice graphicsDevice, ConnectionRuleSet treeRuleSet, ConnectionRuleSet fogRuleSet)
        {
            this.romContentManager = romContentManager;
            this.graphicsDevice = graphicsDevice;
            TreeRuleSet = treeRuleSet;
            FogRuleSet = fogRuleSet;
        }
        #endregion

        #region Tile Functions
        public ushort GetTerrainBlockIndex(ushort index) => (ushort)(TerrainBlockOffset + index);

        public Rectangle GetTerrainBlockSource(ushort index) => Tilesheet.CalculateSourceRectangle(GetTerrainBlockIndex(index));

        public DirectionMask CreateTreeMask(TilemapData tilemap, int x, int y)
        {
            DirectionMask mask = 0;
            if (tilemap.HasTreeAtPosition(x, y - 1)) mask |= DirectionMask.Top;
            if (tilemap.HasTreeAtPosition(x + 1, y - 1)) mask |= DirectionMask.TopRight;
            if (tilemap.HasTreeAtPosition(x + 1, y)) mask |= DirectionMask.Right;
            if (tilemap.HasTreeAtPosition(x + 1, y + 1)) mask |= DirectionMask.BottomRight;
            if (tilemap.HasTreeAtPosition(x, y + 1)) mask |= DirectionMask.Bottom;
            if (tilemap.HasTreeAtPosition(x - 1, y + 1)) mask |= DirectionMask.BottomLeft;
            if (tilemap.HasTreeAtPosition(x - 1, y)) mask |= DirectionMask.Left;
            if (tilemap.HasTreeAtPosition(x - 1, y - 1)) mask |= DirectionMask.TopLeft;
            return mask;
        }
        #endregion

        #region Data Functions
        public void LoadDataForMap(TilemapData tilemapData)
        {
            // Unload first.
            Unload();

            // Create the tilesheet.
            Tilesheet = createTilePaletteTexture(tilemapData);
        }

        public void Unload()
        {
            // Unload the tilesheet.
            if (Tilesheet != null)
            {
                Tilesheet.Texture.Dispose();
                Tilesheet = null;
            }
        }
        #endregion

        #region Graphical Functions
        private Spritesheet createTilePaletteTexture(TilemapData tilemapData)
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
            for (int i = 0; i < TreeRuleSet.PaletteCount; i++, destinationIndex++)
                writeTileBlock(TreeRuleSet.BlockPalette[i], destinationIndex, spritesheet, packedSpritesheet, creatorSpriteBatch);
            for (int i = 0; i < FogRuleSet.PaletteCount; i++, destinationIndex++)
                writeTileBlock(FogRuleSet.BlockPalette[i], destinationIndex, spritesheet, packedSpritesheet, creatorSpriteBatch);

            // Write the terrain last.
            for (int i = 0; i < tilemapData.TilePalette.Count; i++, destinationIndex++)
                writeTileBlock(tilemapData.TilePalette[i], destinationIndex, spritesheet, packedSpritesheet, creatorSpriteBatch);

            creatorSpriteBatch.End();
            graphicsDevice.SetRenderTarget(null);

            // Unload the original texture.
            textureLoader.Unload(spritesheet.Texture);

            return packedSpritesheet;
        }

        private static void writeTileBlock(TilemapPaletteBlock block, int index, Spritesheet sourceSpritesheet, Spritesheet destinationSpritesheet, SpriteBatch creatorSpriteBatch)
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

        #region Load Functions
        public static TileGraphicsManager Load(RomContentManager romContentManager, GraphicsDevice graphicsDevice)
        {
            // TODO: Make actual tree/fog rules.
            string treeRulesPath = Path.ChangeExtension(Path.Combine(romContentManager.BaseGameDirectory, "Sprites", "TreeRules"), ContentFileUtil.TileRuleSetExtension);
            ConnectionRuleSet treeRuleSet = ConnectionRuleSet.LoadFromFile(treeRulesPath, 0);

            string fogRulesPath = Path.ChangeExtension(Path.Combine(romContentManager.BaseGameDirectory, "Sprites", "FogRules"), ContentFileUtil.TileRuleSetExtension);
            ConnectionRuleSet fogRuleSet = ConnectionRuleSet.LoadFromFile(fogRulesPath, treeRuleSet.PaletteCount);

            // Create and return the new manager.
            return new(romContentManager, graphicsDevice, treeRuleSet, fogRuleSet);
        }
        #endregion
    }
}

using GameShared.Scenes;
using GlobalShared.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.TilemapSystem;
using System;
using System.IO;
using System.Reflection;

namespace OpenLegoBattles.GameStates
{
    internal class FogTestState : IGameState
    {
        #region Fields
        private Spritesheet tiles;
        #endregion

        #region Properties
        public bool UpdateUnder => false;

        public bool DrawUnder => false;

        public BattleScene Scene { get; }

        public SceneRenderManager RenderManager { get; }
        #endregion

        #region Constructors
        public FogTestState(RomContentManager romContentManager, TileGraphicsManager tileGraphicsManager, GameWindow window, GraphicsDevice graphicsDevice)
        {
            TilemapData tilemap = romContentManager.Load<TilemapData>("mp01");
            Scene = new(tilemap);

            tileGraphicsManager.LoadDataForMap(tilemap);

            Scene.VisibilityView.RevealCircle(3, 3, 3);
            Scene.VisibilityView.ClearCurrentVisibility();
            Scene.VisibilityView.RevealCircle(7, 3, 3);

            RenderManager = SceneRenderManager.CreateFromScene(Scene, tileGraphicsManager, window, graphicsDevice);
            RenderManager.CalculateAllFogTiles();

            tiles = romContentManager.Load<Spritesheet>("KingTileset");

            window.AllowUserResizing = true;
        }
        #endregion

        #region Update Functions
        public void Update(GameTime gameTime)
        {

        }
        #endregion

        #region Draw Functions
        public void Draw(GameTime gameTime)
        {

            RenderManager.Draw();
            //RenderManager.Camera.Begin();
            //for (int i = 0; i < RenderManager.TileGraphicsManager.FogRuleSet.PaletteCount; i++)
            //{
            //    TilemapPaletteBlock block = RenderManager.TileGraphicsManager.FogRuleSet.BlockPalette[i];
            //    Point blockPosition = RenderManager.TileGraphicsManager.Tilesheet.CalculateXYFromIndex(i);
            //    int screenX = blockPosition.X * tiles.TileSize.X * 3;
            //    int screenY = blockPosition.Y * tiles.TileSize.Y * 2;
            //    int bottomRowScreenY = screenY + tiles.TileSize.Y;

            //    Rectangle source = tiles.CalculateSourceRectangle(block.TopLeft);
            //    RenderManager.Camera.SpriteBatch.Draw(tiles.Texture, new Rectangle(screenX, screenY, tiles.TileSize.X, tiles.TileSize.Y), source, Color.White);

            //    source = tiles.CalculateSourceRectangle(block.TopMiddle);
            //    RenderManager.Camera.SpriteBatch.Draw(tiles.Texture, new Rectangle(screenX + tiles.TileSize.X, screenY, tiles.TileSize.X, tiles.TileSize.Y), source, Color.White);

            //    source = tiles.CalculateSourceRectangle(block.TopRight);
            //    RenderManager.Camera.SpriteBatch.Draw(tiles.Texture, new Rectangle(screenX + (tiles.TileSize.X * 2), screenY, tiles.TileSize.X, tiles.TileSize.Y), source, Color.White);

            //    source = tiles.CalculateSourceRectangle(block.BottomLeft);
            //    RenderManager.Camera.SpriteBatch.Draw(tiles.Texture, new Rectangle(screenX, bottomRowScreenY, tiles.TileSize.X, tiles.TileSize.Y), source, Color.White);

            //    source = tiles.CalculateSourceRectangle(block.BottomMiddle);
            //    RenderManager.Camera.SpriteBatch.Draw(tiles.Texture, new Rectangle(screenX + tiles.TileSize.X, bottomRowScreenY, tiles.TileSize.X, tiles.TileSize.Y), source, Color.White);

            //    source = tiles.CalculateSourceRectangle(block.BottomRight);
            //    RenderManager.Camera.SpriteBatch.Draw(tiles.Texture, new Rectangle(screenX + (tiles.TileSize.X * 2), bottomRowScreenY, tiles.TileSize.X, tiles.TileSize.Y), source, Color.White);
            //}
            //RenderManager.Camera.End();
        }
        #endregion
    }
}
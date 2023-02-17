using GlobalShared.Content;
using GlobalShared.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLegoBattles.GameStates
{
    /// <summary>
    /// State for viewing and rendering maps to textures.
    /// </summary>
    internal class MapRenderState : IGameState
    {
        // TODO: Turn this into a general map viewer as well as having the option to save all map renders.
        #region Dependencies
        private readonly RomContentManager romContentManager;
        private readonly TileGraphicsManager tileGraphicsManager;
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Properties
        public bool UpdateUnder => false;

        public bool DrawUnder => false;
        #endregion

        #region Constructors
        public MapRenderState(RomContentManager romContentManager, TileGraphicsManager tileGraphicsManager, GraphicsDevice graphicsDevice)
        {
            this.romContentManager = romContentManager;
            this.tileGraphicsManager = tileGraphicsManager;
            this.graphicsDevice = graphicsDevice;
            renderAllMapsToFiles();
        }
        #endregion

        #region Render Functions
        private void renderAllMapsToFiles()
        {
            using SpriteBatch spriteBatch = new(graphicsDevice);

            string rendersFolderPath = "MapRenders";
            Directory.CreateDirectory(rendersFolderPath);

            IEnumerable<string> mapFilePaths = Directory.EnumerateFiles(Path.Combine(romContentManager.BaseGameDirectory, ContentFileUtil.TilemapDirectoryName), "*.map");
            foreach (string mapFilePath in mapFilePaths)
            {
                TilemapData map = TilemapData.Load(mapFilePath);

                tileGraphicsManager.LoadDataForMap(map);

                using RenderTarget2D renderTarget = new(graphicsDevice, tileGraphicsManager.Tilesheet.TileSize.X * map.Width, tileGraphicsManager.Tilesheet.TileSize.Y * map.Height);
                graphicsDevice.SetRenderTarget(renderTarget);
                graphicsDevice.Clear(Color.Transparent);

                spriteBatch.Begin();
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        Rectangle source = tileGraphicsManager.GetTerrainBlockSource(map[x, y].Index);
                        spriteBatch.Draw(tileGraphicsManager.Tilesheet.Texture, new Rectangle(x * source.Width, y * source.Height, source.Width, source.Height), source, Color.White);

                        if (map.HasTreeAtPosition(x, y))
                        {
                            uint treeMask = SceneRenderManager.CreateTreeMask(map, x, y);
                            ushort treeIndex = tileGraphicsManager.TreeRuleSet.GetBlockForTileHash(treeMask);
                            Rectangle treeSource = tileGraphicsManager.Tilesheet.CalculateSourceRectangle(treeIndex);
                            spriteBatch.Draw(tileGraphicsManager.Tilesheet.Texture, new Rectangle(x * source.Width, y * source.Height, source.Width, source.Height), treeSource, Color.White);
                        }
                    }
                spriteBatch.End();

                graphicsDevice.SetRenderTarget(null);
                tileGraphicsManager.Unload();

                string renderPath = Path.ChangeExtension(Path.Combine(rendersFolderPath, Path.GetFileNameWithoutExtension(mapFilePath)), ContentFileUtil.SpriteExtension);
                using FileStream renderFile = File.Create(renderPath);
                renderTarget.SaveAsPng(renderFile, renderTarget.Width, renderTarget.Height);
            }
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

        }
        #endregion
    }
}

using GameShared.Scenes;
using GlobalShared.DataTypes;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLegoBattles.Rendering
{
    /// <summary>
    /// Handles the drawing of a player's scene.
    /// </summary>
    internal class PlayerRenderManager
    {
        #region Fields
        private readonly byte[,] treeTiles;
        #endregion

        #region Properties
        /// <summary>
        /// The scene to render.
        /// </summary>
        public BattleScene Scene { get; }

        /// <summary>
        /// The tile graphics for the current map.
        /// </summary>
        public TileGraphicsManager TileGraphicsManager { get; }
        #endregion

        #region Constructors
        public PlayerRenderManager(BattleScene scene, TileGraphicsManager tileGraphicsManager)
        {
            Scene = scene;
            TileGraphicsManager = tileGraphicsManager;

            treeTiles = new byte[scene.Tilemap.Width, scene.Tilemap.Height];
            calculateAllTreeTiles();
        }
        #endregion

        #region Tree Functions
        private void calculateAllTreeTiles()
        {
            for (int y = 0; y < Scene.Tilemap.Height; y++)
                for (int x = 0; x < Scene.Tilemap.Width; x++)
                    if (Scene.Tilemap.HasTreeAtPosition(x, y))
                    {
                        DirectionMask treeMask = TileGraphicsManager.CreateTreeMask(Scene.Tilemap, x, y);
                        treeTiles[x, y] = (byte)TileGraphicsManager.TreeRuleSet.GetBlockForTileHash(treeMask);
                    }
                    else treeTiles[x, y] = byte.MaxValue;
        }
        #endregion

        #region Draw Functions
        public void DrawScene(TileCamera camera)
        {
            DrawTilemap(camera, Scene.Tilemap);
            DrawCachedTrees(camera);
        }

        public void DrawTilemap(TileCamera camera, TilemapData tilemap)
        {
            for (int y = 0; y < tilemap.Height; y++)
                for (int x = 0; x < tilemap.Width; x++)
                    camera.DrawTileAtTilePosition(TileGraphicsManager.Tilesheet, TileGraphicsManager.GetTerrainBlockIndex(tilemap[x, y].Index), x, y);
        }

        public void DrawCachedTrees(TileCamera camera)
        {
            for (int y = 0; y < Scene.Tilemap.Height; y++)
                for (int x = 0; x < Scene.Tilemap.Width; x++)
                    if (Scene.Tilemap.HasTreeAtPosition(x, y))
                        camera.DrawTileAtTilePosition(TileGraphicsManager.Tilesheet, treeTiles[x, y], x, y);
        }
        #endregion
    }
}

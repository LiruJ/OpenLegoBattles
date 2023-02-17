﻿using GameShared.DataTypes;
using GameShared.Scenes;
using GlobalShared.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Tilemaps;
using OpenLegoBattles.TilemapSystem;

namespace OpenLegoBattles.Rendering
{
    /// <summary>
    /// Handles the drawing of a player's scene.
    /// </summary>
    public class SceneRenderManager
    {
        #region Fields
        private readonly byte[,] treeTiles;

        private readonly byte[,] fogTiles;
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

        /// <summary>
        /// The default camera used to draw.
        /// </summary>
        public TileCamera Camera { get; }
        #endregion

        #region Constructors
        public SceneRenderManager(BattleScene scene, TileGraphicsManager tileGraphicsManager, TileCamera camera)
        {
            Scene = scene;
            TileGraphicsManager = tileGraphicsManager;
            Camera = camera;
            treeTiles = new byte[scene.Tilemap.Width, scene.Tilemap.Height];
            fogTiles = new byte[scene.Tilemap.Width, scene.Tilemap.Height];
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
                        uint treeMask = CreateTreeMask(Scene.Tilemap, x, y);
                        treeTiles[x, y] = (byte)TileGraphicsManager.TreeRuleSet.GetBlockForTileHash(treeMask);
                    }
                    else treeTiles[x, y] = byte.MaxValue;
        }

        public void CalculateAllFogTiles()
        {
            for (int y = 0; y < Scene.Tilemap.Height; y++)
                for (int x = 0; x < Scene.Tilemap.Width; x++)
                    if (Scene.VisibilityView[x, y] != TileVisibilityType.Seen)
                    {
                        uint fogMask = CreateFogMask(Scene.VisibilityView, x, y);
                        fogTiles[x, y] = (byte)TileGraphicsManager.FogRuleSet.GetBlockForTileHash(fogMask);
                    }
                    else fogTiles[x, y] = byte.MaxValue;
        }

        public static uint CreateTreeMask(TilemapData tilemap, int x, int y)
        {
            // Calculate the mask using the surrounding tiles.
            DirectionMask mask = 0;
            foreach (Direction direction in Direction.GetSurroundingDirectionsEnumator())
                if (tilemap.HasTreeAtPosition(x + direction.TileNormal.X, y + direction.TileNormal.Y)) mask |= direction.ToMask();
            return (uint)mask;
        }

        public static uint CreateFogMask(TilemapVisibilityView visibilityView, int x, int y)
        {
            // Calculate the mask using the surrounding tiles.
            uint mask = 0;
            int i = 7;
            foreach (Direction direction in Direction.GetSurroundingDirectionsEnumator())
            {
                uint visibilityValue = (uint)visibilityView.GetTileVisibility(x + direction.TileNormal.X, y + direction.TileNormal.Y);
                mask |= visibilityValue << i;
                i--;
            }

            return mask;
        }
        #endregion

        #region Draw Functions
        public void Draw()
        {
            // Begin, draw, and end with the default camera.
            Camera.Begin();
            DrawScene();
            Camera.End();
        }

        public void DrawScene(TileCamera camera = null)
        {
            // Default to this manager's camera if none was given.
            camera ??= Camera;

            const bool drawUnseen = false;
            DrawTilemap(camera, Scene.Tilemap, drawUnseen);
            DrawCachedTrees(camera, drawUnseen);
            if (!drawUnseen)
                DrawCachedFog(camera);
        }

        public void DrawTilemap(TileCamera camera, TilemapData tilemap, bool drawUnseen = false)
        {
            // TODO: Draw only what's in the camera view.

            for (int y = 0; y < tilemap.Height; y++)
                for (int x = 0; x < tilemap.Width; x++)
                {
                    if (drawUnseen || Scene.VisibilityView.IsTileVisible(x, y))
                        camera.DrawTileAtTilePosition(TileGraphicsManager.Tilesheet, TileGraphicsManager.GetTerrainBlockIndex(tilemap[x, y].Index), x, y);
                }
        }

        public void DrawCachedTrees(TileCamera camera, bool drawUnseen = false)
        {
            for (int y = 0; y < Scene.Tilemap.Height; y++)
                for (int x = 0; x < Scene.Tilemap.Width; x++)
                    if (Scene.Tilemap.HasTreeAtPosition(x, y) && (drawUnseen || Scene.VisibilityView.IsTileVisible(x, y)))
                        camera.DrawTileAtTilePosition(TileGraphicsManager.Tilesheet, treeTiles[x, y], x, y);
        }

        public void DrawCachedFog(TileCamera camera)
        {
            for (int y = 0; y < Scene.Tilemap.Height; y++)
                for (int x = 0; x < Scene.Tilemap.Width; x++)
                    if (Scene.VisibilityView[x, y] != TileVisibilityType.Seen)
                        camera.DrawTileAtTilePosition(TileGraphicsManager.Tilesheet, fogTiles[x, y], x, y);
        }
        #endregion

        #region Creation Functions
        public static SceneRenderManager CreateFromScene(BattleScene scene, TileGraphicsManager tileGraphicsManager, GameWindow window, GraphicsDevice graphicsDevice)
        {
            // Create the dependencies, use them to create the manager, and return it.
            TileCamera camera = new(window, new(graphicsDevice));
            tileGraphicsManager.LoadDataForMap(scene.Tilemap);
            return new(scene, tileGraphicsManager, camera);
        }
        #endregion
    }
}

using GameShared.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLegoBattles.GameStates
{
    internal class FogTestState : IGameState
    {
        #region Fields
        private BattleScene scene;

        private TileCamera camera;

        private TileGraphicsManager tileGraphicsManager;

        private PlayerRenderManager renderManager;
        #endregion

        #region Properties
        public bool UpdateUnder => false;

        public bool DrawUnder => false;
        #endregion

        #region Constructors
        public FogTestState(RomContentManager romContentManager, GameWindow window, GraphicsDevice graphicsDevice)
        {
            TilemapData tilemap = romContentManager.Load<TilemapData>("mp01");
            scene = new(tilemap);
            camera = new(window, new(graphicsDevice));

            tileGraphicsManager = TileGraphicsManager.Load(romContentManager, graphicsDevice);
            tileGraphicsManager.LoadDataForMap(tilemap);

            renderManager = new(scene, tileGraphicsManager);

            camera.CentrePosition += new Point(5, 2);
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
            camera.Begin();

            renderManager.DrawScene(camera);

            camera.End();
        }
        #endregion
    }
}

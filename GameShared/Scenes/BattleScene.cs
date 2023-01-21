using GlobalShared.Tilemaps;
using OpenLegoBattles.TilemapSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameShared.Scenes
{
    /// <summary>
    /// Represents a battle scene from a single perspective (server, a player, etc.).
    /// </summary>
    public class BattleScene
    {
        #region Properties
        /// <summary>
        /// The tilemap data of the scene.
        /// </summary>
        public TilemapData Tilemap { get; }
        #endregion

        #region Constructor
        public BattleScene(TilemapData tilemap)
        {
            Tilemap = tilemap;
        }
        #endregion
    }
}

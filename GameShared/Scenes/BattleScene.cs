using OpenLegoBattles.Players;
using OpenLegoBattles.Tilemaps;
using OpenLegoBattles.TilemapSystem;

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

        /// <summary>
        /// The viewable area of the tilemap.
        /// </summary>
        public TilemapVisibilityView VisibilityView { get; }

        /// <summary>
        /// The player whose perspective this scene is from.
        /// </summary>
        public BattlePlayer Owner { get; }

        // TODO: NetworkManager (local, client), PlayerManager, BuildingManager, UnitManager, PickupManager
        #endregion

        #region Constructor
        public BattleScene(TilemapData tilemap)
        {
            Tilemap = tilemap;
            VisibilityView = new(tilemap);
        }
        #endregion
    }
}

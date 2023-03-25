using OpenLegoBattles.Rendering;
using OpenLegoBattles.Scenes;

namespace OpenLegoBattles.Players
{
    /// <summary>
    /// A player as they exist in a battle.
    /// </summary>
    public class BattlePlayer
    {
        #region Properties
        /// <summary>
        /// The player's version of the game's scene. In multiplayer, this will only hold data for the data that have been discovered by the player.
        /// </summary>
        public BattleScene Scene { get; }

        /// <summary>
        /// The player's camera.
        /// </summary>
        public TileCamera Camera { get; }

        // TODO: PlayerController (local, client, server), index, PlayerManager
        #endregion

        #region Constructors
        public BattlePlayer(BattleScene scene, TileCamera camera)
        {
            Scene = scene;
            Camera = camera;
        }
        #endregion
    }
}

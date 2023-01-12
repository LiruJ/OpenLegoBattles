using Microsoft.Xna.Framework;

namespace OpenLegoBattles.GameStates
{
    public interface IGameState
    {
        /// <summary> Represents a value which is <c>true</c> when the <see cref="IGameState"/> under this one in the list should be updated. </summary>
        bool UpdateUnder { get; }

        /// <summary> Represents a value which is <c>true</c> when the <see cref="IGameState"/> under this one in the list should be drawn. </summary>
        bool DrawUnder { get; }

        void Draw(GameTime gameTime);

        void Update(GameTime gameTime);
    }
}

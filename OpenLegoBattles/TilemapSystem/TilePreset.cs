using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;

namespace OpenLegoBattles.TilemapSystem
{
    public class TilePreset
    {
        #region Properties
        public ushort Index { get; }

        public ushort TopLeft { get; }

        public ushort TopMiddle { get; }

        public ushort TopRight { get; }

        public ushort BottomLeft { get; }

        public ushort BottomMiddle { get; }

        public ushort BottomRight { get; }
        #endregion

        #region Constructors
        public TilePreset(ushort index, ushort topLeft, ushort topMiddle, ushort topRight, ushort bottomLeft, ushort bottomMiddle, ushort bottomRight)
        {
            Index = index;
            TopLeft = topLeft;
            TopMiddle = topMiddle;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomMiddle = bottomMiddle;
            BottomRight = bottomRight;
        }
        #endregion

        #region Draw Functions
        public void Draw(SpriteBatch spriteBatch, Spritesheet spritesheet, int screenX, int screenY)
        {
            int bottomRowScreenY = screenY + spritesheet.TileSize.Y;

            Rectangle source = spritesheet.CalculateSourceRectangle(TopLeft);
            spriteBatch.Draw(spritesheet.Texture, new Rectangle(screenX, screenY, spritesheet.TileSize.X, spritesheet.TileSize.Y), source, Color.White);

            source = spritesheet.CalculateSourceRectangle(TopMiddle);
            spriteBatch.Draw(spritesheet.Texture, new Rectangle(screenX + spritesheet.TileSize.X, screenY, spritesheet.TileSize.X, spritesheet.TileSize.Y), source, Color.White);

            source = spritesheet.CalculateSourceRectangle(TopRight);
            spriteBatch.Draw(spritesheet.Texture, new Rectangle(screenX + (spritesheet.TileSize.X * 2), screenY, spritesheet.TileSize.X, spritesheet.TileSize.Y), source, Color.White);

            source = spritesheet.CalculateSourceRectangle(BottomLeft);
            spriteBatch.Draw(spritesheet.Texture, new Rectangle(screenX, bottomRowScreenY, spritesheet.TileSize.X, spritesheet.TileSize.Y), source, Color.White);

            source = spritesheet.CalculateSourceRectangle(BottomMiddle);
            spriteBatch.Draw(spritesheet.Texture, new Rectangle(screenX + spritesheet.TileSize.X, bottomRowScreenY, spritesheet.TileSize.X, spritesheet.TileSize.Y), source, Color.White);

            source = spritesheet.CalculateSourceRectangle(BottomRight);
            spriteBatch.Draw(spritesheet.Texture, new Rectangle(screenX + (spritesheet.TileSize.X * 2), bottomRowScreenY, spritesheet.TileSize.X, spritesheet.TileSize.Y), source, Color.White);
        }
        #endregion
    }
}
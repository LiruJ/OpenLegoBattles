using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;

namespace OpenLegoBattles.TilemapSystem
{
    public class TilePreset
    {
        #region Properties
        public ushort TopLeft { get; }

        public ushort TopMiddle { get; }

        public ushort TopRight { get; }

        public ushort BottomLeft { get; }

        public ushort BottomMiddle { get; }

        public ushort BottomRight { get; }
        #endregion

        #region Constructors
        public TilePreset(ushort topLeft, ushort topMiddle, ushort topRight, ushort bottomLeft, ushort bottomMiddle, ushort bottomRight)
        {
            TopLeft = topLeft;
            TopMiddle = topMiddle;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomMiddle = bottomMiddle;
            BottomRight = bottomRight;
        }
        #endregion

        #region Draw Functions
        public void Draw(SpriteBatch spriteBatch, Spritesheet spritesheet, int x, int y)
        {
            Point presetTilePosition = new Point(x, y) * spritesheet.PresetTileSize;

            int currentSectionIndex = 0;
            for (int sectionY = 0; sectionY < 2; sectionY++)
                for (int sectionX = 0; sectionX < 3; sectionX++)
                {
                    //Point tileOffset = new Point(sectionX, sectionY) * spritesheet.TileSize;
                    //Rectangle source = spritesheet.CalculateSourceRectangle(tileIndices[currentSectionIndex]);
                    //spriteBatch.Draw(spritesheet.Texture, new Rectangle(presetTilePosition + tileOffset, spritesheet.TileSize), source, Color.White);
                    //currentSectionIndex++;
                }
        }
        #endregion
    }
}
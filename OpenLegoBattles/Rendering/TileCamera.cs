using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLegoBattles.Rendering
{
    /// <summary>
    /// A 2D camera used for viewing tilemaps, tile objects, and 3D objects composited onto the map.
    /// </summary>
    internal class TileCamera
    {
        #region Dependencies
        private readonly GameWindow window;
        #endregion

        #region Properties
        /// <summary>
        /// The sprite batch used for drawing.
        /// </summary>
        public SpriteBatch SpriteBatch { get; }

        /// <summary>
        /// The bounds of the camera's view of the world.
        /// </summary>
        public Rectangle ViewBounds { get; private set; }

        /// <summary>
        /// The world position of the centre of the camera's view of the world.
        /// </summary>
        public Point CentrePosition
        {
            get => ViewBounds.Center;
            set => ViewBounds = new Rectangle(value - new Point(ViewSize.X / 2, ViewSize.Y / 2), ViewSize);
        }

        /// <summary>
        /// The size of the camera's view of the world.
        /// </summary>
        public Point ViewSize
        {
            get => ViewBounds.Size;
            set => ViewBounds = new Rectangle(ViewBounds.Location, value);
        }

        public Matrix TransformMatrix => Matrix.CreateTranslation(-ViewBounds.X, -ViewBounds.Y, 0);
        #endregion

        #region Constructors
        public TileCamera(GameWindow window, SpriteBatch spriteBatch)
        {
            this.window = window;
            SpriteBatch = spriteBatch;

            // Initialise the view bounds.
            ViewBounds = new(Point.Zero, window.ClientBounds.Size);
        }
        #endregion

        #region Draw Functions
        public void Begin() => SpriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp, depthStencilState: DepthStencilState.DepthRead, transformMatrix: TransformMatrix);

        public void End() => SpriteBatch.End();

        public void DrawSpriteAtWorldPosition(Sprite sprite, Point worldPosition)
        {
            Rectangle spriteSource = sprite.CalculateSourceRectangle();
            SpriteBatch.Draw(sprite.Spritesheet.Texture, new Rectangle(worldPosition, spriteSource.Size), spriteSource, Color.White);
        }

        public void DrawSpriteAtWorldPosition(Spritesheet spritesheet, ushort index, Point worldPosition)
        {
            Rectangle spriteSource = spritesheet.CalculateSourceRectangle(index);
            SpriteBatch.Draw(spritesheet.Texture, new Rectangle(worldPosition, spriteSource.Size), spriteSource, Color.White);
        }

        public void DrawTileAtTilePosition(Spritesheet spritesheet, int index, int tileX, int tileY)
        {
            Rectangle spriteSource = spritesheet.CalculateSourceRectangle(index);
            SpriteBatch.Draw(spritesheet.Texture, new Rectangle(new(spritesheet.TileSize.X * tileX, spritesheet.TileSize.Y * tileY), spriteSource.Size), spriteSource, Color.White);
        }
        #endregion
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Graphics;
using System;

namespace OpenLegoBattles.Rendering
{
    /// <summary>
    /// A 2D camera used for viewing tilemaps, tile objects, and 3D objects composited onto the map.
    /// </summary>
    public class TileCamera : IDisposable
    {
        #region Constants
        public const float MinimumScale = 0.5f;

        public const float MaximumScale = 12.0f;
        #endregion

        #region Dependencies
        private readonly GameWindow window;
        #endregion

        #region Fields
        private bool isMatrixDirty = true;
        #endregion

        #region Backing Fields
        private float scale = 2f;

        private Vector2 centrePosition;

        private Point viewWorldSize;

        private Matrix transformMatrix;

        private Matrix invertedTransformMatrix;
        #endregion

        #region Properties
        /// <summary>
        /// The sprite batch used for drawing.
        /// </summary>
        public SpriteBatch SpriteBatch { get; }

        /// <summary>
        /// The bounds of the camera's view of the world.
        /// </summary>
        public Rectangle ViewWorldBounds => new(Vector2.Floor(TopLeftPosition).ToPoint(), ViewWorldSize);

        /// <summary>
        /// The world position of the top-left of the camera's view of the world.
        /// </summary>
        public Vector2 TopLeftPosition
        {
            get => CentrePosition - (ViewWorldSize.ToVector2() / 2.0f);
            set => CentrePosition = value + (ViewWorldSize.ToVector2() / 2.0f);
        }

        /// <summary>
        /// The world position of the centre of the camera's view of the world.
        /// </summary>
        public Vector2 CentrePosition
        {
            get => centrePosition;
            set
            {
                centrePosition = value;
                isMatrixDirty = true;
            }
        }

        /// <summary>
        /// The scale of the camera. Where 1 means 1 world pixel is 1 screen pixel.
        /// </summary>
        public float Scale
        {
            get => scale;
            set
            {
                scale = MathHelper.Clamp(value, MinimumScale, MaximumScale);
                isMatrixDirty = true;
                recalculateViewSize();
            }
        }

        /// <summary>
        /// The size of the camera's view of the world.
        /// </summary>
        public Point ViewWorldSize
        {
            get => viewWorldSize;
            set
            {
                viewWorldSize = value;
                isMatrixDirty = true;
            }
        }

        /// <summary>
        /// The matrix of the camera's view.
        /// </summary>
        public Matrix TransformMatrix
        {
            get
            {
                if (isMatrixDirty)
                    recalculateMatrix();
                return transformMatrix;
            }
        }

        /// <summary>
        /// The inverted matrix of the camera's view.
        /// </summary>
        public Matrix InvertedTransformMatrix
        {
            get
            {
                if (isMatrixDirty)
                    recalculateMatrix();
                return invertedTransformMatrix;
            }
        }
        #endregion

        #region Constructors
        public TileCamera(GameWindow window, SpriteBatch spriteBatch)
        {
            this.window = window;
            SpriteBatch = spriteBatch;

            // Listen for the window size changing so the scale can be corrected.
            window.ClientSizeChanged += (_, _) => recalculateViewSize();

            // Initialise the view bounds.
            recalculateViewSize();
        }
        #endregion

        #region Position Functions
        /// <summary>
        /// Converts the given screen position to a world position.
        /// </summary>
        /// <param name="screenPosition"> A position on the screen. </param>
        /// <returns> The position in the world. </returns>
        public Point ToWorldPosition(Point screenPosition)
        {
            Vector3 worldPosition = Vector3.Transform(new Vector3(screenPosition.X, screenPosition.Y, 0), InvertedTransformMatrix);
            return Vector2.Floor(new Vector2(worldPosition.X, worldPosition.Y)).ToPoint();
        }
        #endregion

        #region Spatial Functions
        private void recalculateViewSize()
        {
            ViewWorldSize = Vector2.Floor(window.ClientBounds.Size.ToVector2() / Scale).ToPoint();
        }

        private void recalculateMatrix()
        {
            isMatrixDirty = false;

            transformMatrix = Matrix.CreateTranslation(-TopLeftPosition.X, -TopLeftPosition.Y, 0) * Matrix.CreateScale(Scale);
            invertedTransformMatrix = Matrix.Invert(transformMatrix);
        }
        #endregion

        #region Draw Functions
        public void Begin() => SpriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp, depthStencilState: DepthStencilState.DepthRead, transformMatrix: TransformMatrix);

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

        public void DrawTileAtTilePosition(Spritesheet spritesheet, int index, int tileX, int tileY, Color? colour = null)
        {
            Rectangle spriteSource = spritesheet.CalculateSourceRectangle(index);
            SpriteBatch.Draw(spritesheet.Texture, new Rectangle(new(spritesheet.TileSize.X * tileX, spritesheet.TileSize.Y * tileY), spriteSource.Size), spriteSource, colour ?? Color.White);
        }
        #endregion

        #region Disposal Functions
        public void Dispose()
        {
            SpriteBatch.Dispose();
        }
        #endregion
    }
}
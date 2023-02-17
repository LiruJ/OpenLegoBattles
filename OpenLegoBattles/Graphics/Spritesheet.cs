using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace OpenLegoBattles.Graphics
{
    public class Spritesheet
    {
        #region Fields
        private readonly Dictionary<ushort, Sprite> sprites = new();
        #endregion

        #region Properties
        /// <summary> The width and height in pixels of a single tile. </summary>
        public Point TileSize { get; }

        /// <summary> How many tiles fit into the width of this spritesheet. </summary>
        public int Width { get; }

        /// <summary> How many tiles fit into the height of this spritesheet. </summary>
        public int Height { get; }

        /// <summary> The spritesheet texture itself. </summary>
        public Texture2D Texture { get; }
        #endregion

        #region Constructors
        public Spritesheet(Texture2D texture, int width, int height)
        {
            // Set properties.
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            Width = width;
            Height = height;

            // Calculate the size of a single tile.
            TileSize = new Point(Texture.Width / width, texture.Height / height);
        }
        #endregion

        #region Source Functions
        /// <summary> Calculates and returns the source rectangle containing the sprite at the given <paramref name="tileX"/> and <paramref name="tileY"/> on the spritesheet. </summary>
        /// <param name="tileX"> The x position of the tile. </param>
        /// <param name="tileY"> The y position of the tile. </param>
        /// <returns> The source rectangle containing the sprite at the given <paramref name="tileX"/> and <paramref name="tileY"/> on the spritesheet. </returns>
        public Rectangle CalculateSourceRectangle(int tileX, int tileY) => new(new Point(tileX, tileY) * TileSize, TileSize);

        /// <summary> Calculates and returns the source rectangle containing the sprite at the given 1-dimensional <paramref name="index"/>. </summary>
        /// <param name="index"> The 1-dimensional index of the sprite. </param>
        /// <returns> The source rectangle containing the sprite at the given 1-dimensional <paramref name="index"/>. </returns>
        public Rectangle CalculateSourceRectangle(int index) => new(CalculateXYFromIndex(index) * TileSize, TileSize);

        public Point CalculateXYFromIndex(int index) => CalculateXYFromIndex(index, Width);

        public static Point CalculateXYFromIndex(int index, int width) => new(index % width, index / width);

        public int CalculateIndexFromXY(int x, int y) => CalculateIndexFromXY(x, y, Width);

        public static int CalculateIndexFromXY(int x, int y, int width) => x + y * width;

        /// <summary> Calculates and returns the source rectangle containing the sprite at the 1-dimensional index of the given <paramref name="sprite"/>. </summary>
        /// <param name="sprite"> The sprite. </param>
        /// <returns> The source rectangle containing the sprite at the 1-dimensional index of the given <paramref name="sprite"/>. </returns>
        /// <exception cref="ArgumentException"> Thrown when the given <paramref name="sprite"/>'s spritesheet does not match this spritesheet. </exception>
        public Rectangle CalculateSourceRectangle(Sprite sprite) => sprite.Spritesheet == this ? CalculateSourceRectangle(sprite.Index) : throw new ArgumentException($"Spritesheets did not match. Expected: {this} Got: {sprite.Spritesheet}");
        #endregion

        #region Creation Functions
        public Sprite CreateSprite(ushort index, Color? colour = null)
        {
            if (sprites.TryGetValue(index, out Sprite sprite))
                return sprite;

            sprite = new Sprite(this, index, colour);
            sprites.Add(index, sprite);
            return sprite;
        }
        #endregion
    }
}

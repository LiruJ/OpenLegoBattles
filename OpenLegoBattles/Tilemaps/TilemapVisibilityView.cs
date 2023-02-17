using Microsoft.Xna.Framework;
using OpenLegoBattles.TilemapSystem;
using System;

namespace OpenLegoBattles.Tilemaps
{
    /// <summary>
    /// Represents the view of a tilemap with fog of war. Where a tile can be seen, unseen, and previously seen.
    /// </summary>
    public class TilemapVisibilityView
    {
        #region Operators
        public TileVisibilityType this[int x, int y]
        {
            get => visibilityData[x, y];
            set => visibilityData[x, y] = value;
        }
        #endregion

        #region Fields
        private readonly TileVisibilityType[,] visibilityData;
        #endregion

        #region Properties
        /// <summary>
        /// The tilemap that the visibility is for.
        /// </summary>
        public TilemapData Tilemap { get; }
        #endregion

        #region Constructors
        public TilemapVisibilityView(TilemapData tilemap)
        {
            Tilemap = tilemap;

            // Create the data and initialise it.
            visibilityData = new TileVisibilityType[tilemap.Width, tilemap.Height];
            UnseeAllTiles();
        }
        #endregion

        #region Vision Functions
        public bool IsTileVisible(int x, int y)
        {
            // If the position is out of range, return true.
            if (!Tilemap.IsPositionInRange(x, y)) return true;

            // Return true if the tile has been seen at any point.
            TileVisibilityType visibilityType = visibilityData[x, y];
            return visibilityType == TileVisibilityType.Seen || visibilityType == TileVisibilityType.PreviouslySeen;
        }

        public TileVisibilityType GetTileVisibility(int x, int y, TileVisibilityType defaultToIfOutOfRange = TileVisibilityType.Seen)
            => visibilityData[MathHelper.Clamp(x, 0, Tilemap.Width - 1), MathHelper.Clamp(y, 0, Tilemap.Height - 1)];

        /// <summary>
        /// Sets all tiles as unseen.
        /// </summary>
        public void UnseeAllTiles()
        {
            for (int y = 0; y < Tilemap.Height; y++)
                for (int x = 0; x < Tilemap.Width; x++)
                    visibilityData[x, y] = TileVisibilityType.Unseen;
        }

        public void ClearCurrentVisibility()
        {
            for (int y = 0; y < Tilemap.Height; y++)
                for (int x = 0; x < Tilemap.Width; x++)
                    if (visibilityData[x, y] == TileVisibilityType.Seen)
                        visibilityData[x, y] = TileVisibilityType.PreviouslySeen;
        }

        public void RevealCircle(int centreX, int centreY, int radius)
        {
            // Calculate the direct centre position.
            Vector2 centrePosition = new(centreX + 0.5f, centreY + 0.5f);

            // Go over each tile in a square around the centre.
            for (int currentY = centreY - radius; currentY <= centreY + radius; currentY++)
                for (int currentX = centreX - radius; currentX <= centreX + radius; currentX++)
                {
                    // Check the current position's distance from the centre. If it's within the radius, mark the tile as seen. Add the half-tile offset to the radius, which ends up being 1, and check it against the centre-tile positions.
                    float currentDistanceSquared = Vector2.DistanceSquared(centrePosition, new(currentX + 0.5f, currentY + 0.5f));
                    if (currentDistanceSquared <= MathF.Pow(radius, 2) + 1)
                        visibilityData[currentX, currentY] = TileVisibilityType.Seen;
                }
        }
        #endregion
    }
}

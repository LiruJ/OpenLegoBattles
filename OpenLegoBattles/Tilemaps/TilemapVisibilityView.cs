using Microsoft.Xna.Framework;
using OpenLegoBattles.TilemapSystem;
using OpenLegoBattles.Utils;

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
        /// <summary>
        /// The raw visibility data for each tile in the world.
        /// </summary>
        private readonly TileVisibilityType[,] visibilityData;
        #endregion

        #region Properties
        /// <summary>
        /// The tilemap that the visibility is for.
        /// </summary>
        public TilemapData Tilemap { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new visibility view for the given map.
        /// </summary>
        /// <param name="tilemap"> The tilemap that the visibility is for. </param>
        public TilemapVisibilityView(TilemapData tilemap)
        {
            Tilemap = tilemap;

            // Create the data and initialise it.
            visibilityData = new TileVisibilityType[tilemap.Width, tilemap.Height];
            UnseeAllTiles();
        }
        #endregion

        #region Get Vision Functions
        /// <summary>
        /// Finds if the given tile position can be seen. This is true if the given position or any of its adjacent tiles have been seen before.
        /// </summary>
        /// <param name="position"> The position of the tile. </param>
        /// <returns> <c>true</c> if the given position or any of its adjacent tiles have been seen before; otherwise <c>false</c>. </returns>
        public bool IsTileVisible(Point position) => IsTileVisible(position.X, position.Y);

        /// <summary>
        /// Finds if the given tile position can be seen. This is true if the given position or any of its adjacent tiles have been seen before.
        /// </summary>
        /// <param name="x"> The x position of the tile. </param>
        /// <param name="y"> The y position of the tile. </param>
        /// <returns> <c>true</c> if the given position or any of its adjacent tiles have been seen before; otherwise <c>false</c>. </returns>
        public bool IsTileVisible(int x, int y)
        {
            // Return true if the tile or any of its adjacent tiles have been seen.
            foreach (Point position in Spatial2dUtils.TilesDirectlyAdjacentToAndIncluding(x, y))
            {
                TileVisibilityType visibilityType = GetTileVisibility(position);
                if (visibilityType == TileVisibilityType.Seen || visibilityType == TileVisibilityType.PreviouslySeen) return true;
            }

            // Return false otherwise.
            return false;
        }

        /// <summary>
        /// Gets the tile visibility for the given position.
        /// </summary>
        /// <param name="position"> The position of the tile to get the visibility of. </param>
        /// <returns> The <see cref="TileVisibilityType"/> for the tile at the given position. </returns>
        public TileVisibilityType GetTileVisibility(Point position) => GetTileVisibility(position.X, position.Y);

        /// <summary>
        /// Gets the tile visibility for the given position.
        /// </summary>
        /// <param name="x"> The x position of the tile to get the visibility of. </param>
        /// <param name="y"> The y position of the tile to get the visibility of. </param>
        /// <returns> The <see cref="TileVisibilityType"/> for the tile at the given position. </returns>
        public TileVisibilityType GetTileVisibility(int x, int y)
            => visibilityData[MathHelper.Clamp(x, 0, Tilemap.Width - 1), MathHelper.Clamp(y, 0, Tilemap.Height - 1)];
        #endregion

        #region Set Vision Functions
        /// <summary>
        /// Sets all tiles as unseen.
        /// </summary>
        public void UnseeAllTiles()
        {
            for (int y = 0; y < Tilemap.Height; y++)
                for (int x = 0; x < Tilemap.Width; x++)
                    visibilityData[x, y] = TileVisibilityType.Unseen;
        }

        /// <summary>
        /// Sets all seen tiles as previously seen.
        /// </summary>
        public void ClearCurrentVisibility()
        {
            for (int y = 0; y < Tilemap.Height; y++)
                for (int x = 0; x < Tilemap.Width; x++)
                    if (visibilityData[x, y] == TileVisibilityType.Seen)
                        visibilityData[x, y] = TileVisibilityType.PreviouslySeen;
        }

        /// <summary>
        /// Reveals (sets to <see cref="TileVisibilityType.Seen"/>) all tiles in a <paramref name="radius"/> around the given <paramref name="centreX"/> and <paramref name="centreY"/>.
        /// </summary>
        /// <param name="centreX"> The x position of the centre of the circle. </param>
        /// <param name="centreY"> The y position of the centre of the circle. </param>
        /// <param name="radius"> The radius in tiles around the centre position that is revealed. </param>
        public void RevealCircle(int centreX, int centreY, int radius)
        {
            // Go over each tile in the radius around the position.
            foreach (Point position in Spatial2dUtils.TilesInRadiusAround(centreX, centreY, radius))
            {
                // If the position is out of range, do nothing.
                if (!Tilemap.IsPositionInRange(position.X, position.Y))
                    continue;

                // Set the tile as seen.
                visibilityData[position.X, position.Y] = TileVisibilityType.Seen;
            }
        }
        #endregion
    }
}
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace OpenLegoBattles.Utils
{
    /// <summary>
    /// A helper class for any positional functions that work in 2D space.
    /// </summary>
    public static class Spatial2dUtils
    {
        /// <summary>
        /// Iterates over all tile position in the <paramref name="radius"/> around the given <paramref name="centrePosition"/>.
        /// </summary>
        /// <param name="centrePosition"> The x position of the centre. </param>
        /// <param name="radius"> The radius of the circle. </param>
        /// <returns> Every tile position in the circle. </returns>
        public static IEnumerable<Point> TilesInRadiusAround(Point centrePosition, int radius) => TilesInRadiusAround(centrePosition.X, centrePosition.Y, radius);

        /// <summary>
        /// Iterates over all tile position in the <paramref name="radius"/> around the given centre position.
        /// </summary>
        /// <param name="centreX"> The x position of the centre. </param>
        /// <param name="centreY"> The y position of the centre. </param>
        /// <param name="radius"> The radius of the circle. </param>
        /// <returns> Every tile position in the circle. </returns>
        public static IEnumerable<Point> TilesInRadiusAround(int centreX, int centreY, int radius)
        {
            // Calculate the direct centre position.
            Vector2 centrePosition = new(centreX + 0.5f, centreY + 0.5f);

            // Go over each tile in a square around the centre.
            for (int currentY = centreY - radius; currentY <= centreY + radius; currentY++)
                for (int currentX = centreX - radius; currentX <= centreX + radius; currentX++)
                {
                    // Return the position, if it is in range.
                    float currentDistanceSquared = Vector2.DistanceSquared(centrePosition, new(currentX + 0.5f, currentY + 0.5f));
                    if (currentDistanceSquared <= MathF.Pow(radius, 2) + 1)
                        yield return new Point(currentX, currentY);
                }
        }

        public static IEnumerable<Point> TilesDirectlyAdjacentTo(Point position) => TilesDirectlyAdjacentTo(position.X, position.Y);

        public static IEnumerable<Point> TilesDirectlyAdjacentTo(int x, int y)
        {
            yield return new Point(x, y - 1);
            yield return new Point(x + 1, y);
            yield return new Point(x, y + 1);
            yield return new Point(x - 1, y);
        }

        public static IEnumerable<Point> TilesDirectlyAdjacentToAndIncluding(Point position) => TilesDirectlyAdjacentToAndIncluding(position.X, position.Y);

        public static IEnumerable<Point> TilesDirectlyAdjacentToAndIncluding(int x, int y)
        {
            yield return new Point(x, y);

            foreach (Point adjacentPosition in TilesDirectlyAdjacentTo(x, y))
                yield return adjacentPosition;
        }

    }
}

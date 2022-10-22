using System;
using System.Collections.Generic;
using System.Text;

namespace OpenLegoBattles.TilemapSystem
{
    internal class TreeLayer
    {
        #region Fields
        private readonly byte[,] data;
        #endregion

        #region Properties

        #endregion

        #region Constructors
        public TreeLayer(int width, int height, byte[] treeStrips) => data = CreateMaskFromStrips(width, height, treeStrips);
        #endregion

        #region Query Functions
        /// <summary>
        /// Gets if the tile at the given position has a tree.
        /// </summary>
        /// <param name="x"> The x position. </param>
        /// <param name="y"> The y position. </param>
        /// <returns> <c>true</c> if the given position has a tree; otherwise <c>false</c>. </returns>
        public bool HasTreeAtPosition(int x, int y) => data[x, y] > 0;
        #endregion

        #region Creation Functions
        public static byte[,] CreateMaskFromStrips(int width, int height, byte[] treeStrips)
        {
            // Create the data.
            byte[,] treeMask = new byte[width, height];

            // Go over each strip and set the data.
            int treeIndex = 0;
            bool placeTrees = false;
            for (int i = 0; i < treeStrips.Length; i++)
            {
                // Get the length of the strip.
                byte stripLength = treeStrips[i];

                // Go over the strip and add the trees to the data.
                for (int t = treeIndex; t < treeIndex + stripLength; t++)
                    treeMask[t % width, t / height] = (byte)(placeTrees ? 1 : 0);

                // Increment the tree index by the strip length and invert the tree placement.
                treeIndex += stripLength;
                placeTrees = !placeTrees;
            }

            // TODO: When tree graphics are worked out, that should be applied to the loaded mask.

            // Return the created data.
            return treeMask;
        }
        #endregion
    }
}

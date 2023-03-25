using GlobalShared.DataTypes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace OpenLegoBattles.DataTypes
{
    public struct Direction
    {
        #region Constants
        /// <summary>
        /// The number of possible directions.
        /// </summary>
        private const int numberOfDirections = 8;
        #endregion

        #region Types
        private enum tileDirection : byte
        {
            North = 0,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest,
            West,
            NorthWest,
        }
        #endregion

        #region Fields
        /// <summary>
        /// The underlying value of this direction.
        /// </summary>
        private readonly tileDirection value;
        #endregion

        #region Presets
        /// <summary>
        /// North (0, -1).
        /// </summary>
        public static Direction North => new(tileDirection.North);

        /// <summary>
        /// North east (1, -1).
        /// </summary>
        public static Direction NorthEast => new(tileDirection.NorthEast);

        /// <summary>
        /// East (1, 0).
        /// </summary>
        public static Direction East => new(tileDirection.East);

        /// <summary>
        /// South east (1, 1).
        /// </summary>
        public static Direction SouthEast => new(tileDirection.SouthEast);

        /// <summary>
        /// South (0, 1).
        /// </summary>
        public static Direction South => new(tileDirection.South);

        /// <summary>
        /// South west (-1, 1).
        /// </summary>
        public static Direction SouthWest => new(tileDirection.SouthWest);

        /// <summary>
        /// West (-1, 0).
        /// </summary>
        public static Direction West => new(tileDirection.West);

        /// <summary>
        /// North west (-1, -1)
        /// </summary>
        public static Direction NorthWest => new(tileDirection.NorthWest);
        #endregion

        #region Properties
        /// <summary>
        /// The byte value of the direction.
        /// </summary>
        public byte ByteValue => (byte)value;

        /// <summary> 
        /// The direction to the left of this direction.
        /// </summary>
        public Direction Left => new(wrapValue((tileDirection)((int)value - 1)));

        /// <summary>
        /// The direction to the right of this direction.
        /// </summary>
        public Direction Right => new(wrapValue((tileDirection)((int)value + 1)));

        /// <summary>
        /// The direction opposite of this direction.
        /// </summary>
        public Direction Backwards => new(wrapValue((tileDirection)((int)value - 2)));

        /// <summary>
        /// The tile normal of this direction. Note that the length of this can be more than 1, as it is locked to the grid.
        /// </summary>
        public Point TileNormal
        {
            get
            {
                // Calculate the x.
                int x = 0;
                if (this == NorthEast || this == East || this == SouthEast)
                    x = 1;
                else if (this == SouthWest || this == West || this == NorthWest)
                    x = -1;

                // Calculate the y.
                int y = 0;
                if (this == NorthEast || this == North || this == NorthWest)
                    y = -1;
                else if (this == SouthEast || this == South || this == SouthWest)
                    y = 1;

                // Return the normal.
                return new(x, y);
            }
        }
        #endregion

        #region Constructors
        public Direction(byte value) => this.value = (tileDirection)value;

        private Direction(tileDirection tileDirection) => value = tileDirection;
        #endregion

        #region Operators
        /// <summary>
        /// Compares two directions.
        /// </summary>
        /// <param name="left"> The left direction. </param>
        /// <param name="right"> The right direction. </param>
        /// <returns> If these two directions are equal. </returns>
        public static bool operator ==(Direction left, Direction right) => left.value == right.value;

        /// <summary>
        /// Compares two directions.
        /// </summary>
        /// <param name="left"> The left direction. </param>
        /// <param name="right"> The right direction. </param>
        /// <returns> If these two directions are not equal. </returns>
        public static bool operator !=(Direction left, Direction right) => left.value != right.value;

        public static Direction operator +(Direction left, Direction right) => new(wrapValue((tileDirection)((int)left.value + (int)right.value)));

        public static Direction operator -(Direction left, Direction right) => new(wrapValue((tileDirection)((int)left.value - (int)right.value)));

        public static Direction operator ++(Direction direction) => new(direction.Right.value);

        public static Direction operator --(Direction direction) => new(direction.Left.value);

        public override bool Equals(object obj) => Equals((Direction)obj);

        public bool Equals(Direction other) => value == other.value;

        public override int GetHashCode() => (int)value;
        #endregion

        #region Direction Functions
        private static tileDirection wrapValue(tileDirection tileDirection) => (tileDirection)(((int)tileDirection % numberOfDirections + numberOfDirections) % numberOfDirections);

        /// <summary>
        /// Converts this direction to a mask with the associated bit set.
        /// </summary>
        /// <returns> The associated mask. </returns>
        public DirectionMask ToMask() => (DirectionMask)(1 << (byte)((numberOfDirections - ByteValue) - 1));

        /// <summary>
        /// Checks that the given mask has this direction set.
        /// </summary>
        /// <param name="mask"> The mask to check. </param>
        /// <returns> <c>true</c> if the given mask's associated bit is set, based on this direction; otherwise <c>false</c>. </returns>
        public bool IsMaskDirectionSet(DirectionMask mask)
        {
            // Get the mask representation of this direction and check it against the given mask.
            DirectionMask checkMask = ToMask();
            return (mask & checkMask) == checkMask;
        }
        #endregion

        #region Enumerators
        public static IEnumerable<Direction> GetSurroundingDirectionsEnumator()
        {
            yield return North;
            yield return NorthEast;
            yield return East;
            yield return SouthEast;
            yield return South;
            yield return SouthWest;
            yield return West;
            yield return NorthWest;
        }
        #endregion

        #region Creation Functions
        /// <summary>
        /// Creates a random direction from the given <paramref name="random"/>.
        /// </summary>
        /// <param name="random"> The randomiser. </param>
        /// <returns> A random direction from the given <paramref name="random"/>. </returns>
        public static Direction FromRandom(Random random) => new((tileDirection)random.Next(0, numberOfDirections));

        /// <summary>
        /// Creates a direction from the given mask.
        /// </summary>
        /// <param name="mask"> The mask. Should only be one direction. </param>
        /// <returns> The associated direction. </returns>
        public static Direction FromMask(DirectionMask mask) => mask switch
        {
            DirectionMask.Top => North,
            DirectionMask.TopRight => NorthEast,
            DirectionMask.Right => East,
            DirectionMask.BottomRight => SouthEast,
            DirectionMask.Bottom => South,
            DirectionMask.BottomLeft => SouthWest,
            DirectionMask.Left => West,
            DirectionMask.TopLeft => NorthWest,
            _ => throw new ArgumentException(null, nameof(mask)),
        };
        #endregion
    }
}
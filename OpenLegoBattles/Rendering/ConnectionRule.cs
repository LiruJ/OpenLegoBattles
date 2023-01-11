using GlobalShared.DataTypes;
using GameShared.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.DirectoryServices;

namespace OpenLegoBattles.Rendering
{
    public class ConnectionRule
    {
        #region Types
        /// <summary>
        /// The 3 different states that a tile can be in for a rule.
        /// </summary>
        [Flags]
        private enum tileMaskType : byte { EitherFullOrEmpty = 0, MustBeFull = 0b01, MustBeEmpty = 0b10 }
        #endregion

        #region Properties
        /// <summary>
        /// The collection of tile indices that this rule can use. Indices are randomly selected from this list and all use the same rule.
        /// </summary>
        public IReadOnlyList<ushort> TileIndices { get; }

        /// <summary>
        /// The first defined tile index.
        /// </summary>
        public ushort FirstIndex => TileIndices[0];

        /// <summary>
        /// Is <c>true</c> if this rule defines multiple possible indices; otherwise <c>false</c>.
        /// </summary>
        public bool HasMultipleIndices => TileIndices.Count > 1;

        public IReadOnlyList<ValueTuple<DirectionMask, DirectionMask>> Masks { get; }
        #endregion

        #region Constructors
        public ConnectionRule(IReadOnlyList<ushort> tileIndices, IReadOnlyList<(DirectionMask, DirectionMask)> masks)
        {
            TileIndices = tileIndices;
            Masks = masks;
        }
        #endregion

        #region Rule Functions
        public IEnumerable<DirectionMask> AllPossibleTileMasks()
        {
            // Calculate the masks for every mask in the rule.
            foreach ((DirectionMask full, DirectionMask empty) masks in Masks)
            {
                // Calculate the mask types for each direction.
                tileMaskType[] directionMaskTypes = new tileMaskType[8];
                for (byte i = 0; i < 8; i++)
                    directionMaskTypes[i] = calculateMaskTypeForDirection(new Direction(i), masks);

                // Go over all possiblities from the first bit.
                foreach (DirectionMask possiblity in allPossibleTileMasks(directionMaskTypes, 0, DirectionMask.None))
                    yield return possiblity;
            }
        }

        private IEnumerable<DirectionMask> allPossibleTileMasks(tileMaskType[] directionMaskTypes, byte index, DirectionMask startMask)
        {
            // Go over each remaining index in a mask.
            for (byte i = index; i < 8; i++)
            {
                // Get the current mask type.
                tileMaskType maskType = directionMaskTypes[i];

                // If the mask type is simply that it must be full, set the bit on the mask.
                if (maskType == tileMaskType.MustBeFull)
                    startMask |= new Direction(i).ToMask();
                // Otherwise; recursively calculate all possible masks with this tile being full. Possiblities with this tile being empty will be calculated regardless.
                else if (maskType == tileMaskType.EitherFullOrEmpty)
                    foreach (DirectionMask possiblity in allPossibleTileMasks(directionMaskTypes, (byte)(i + 1), startMask | new Direction(i).ToMask()))
                        yield return possiblity;
            }

            // Return the calculated starting mask.
            yield return startMask;
        }

        private static tileMaskType calculateMaskTypeForDirection(Direction direction, (DirectionMask full, DirectionMask empty) masks)
        {
            // Calculate the mask type based on the state requirements of the tile.
            bool requiresTile = direction.IsMaskDirectionSet(masks.full);
            bool requiresEmpty = direction.IsMaskDirectionSet(masks.empty);
            if (requiresTile && !requiresEmpty) return tileMaskType.MustBeFull;
            else if (!requiresTile && requiresEmpty) return tileMaskType.MustBeEmpty;
            else return tileMaskType.EitherFullOrEmpty;
        }
        #endregion

        #region Load Functions
        public static ConnectionRule LoadFromStream(BinaryReader reader)
        {
            // Read the indices.
            List<ushort> indices = readIndices(reader);

            // Read the masks and return the created rule.
            List<ValueTuple<DirectionMask, DirectionMask>> masks = readMasks(reader);
            return new(indices, masks);
        }

        public static ConnectionRule LoadDefaultFromStream(BinaryReader reader)
        {
            // Read the indices and return the created default rule.
            List<ushort> indices = readIndices(reader);
            return new(indices, new List<ValueTuple<DirectionMask, DirectionMask>>());
        }

        private static List<ushort> readIndices(BinaryReader reader)
        {
            byte indexCount = reader.ReadByte();
            List<ushort> indices = new(indexCount);
            for (int i = 0; i < indexCount; i++)
                indices.Add(reader.ReadUInt16());
            return indices;
        }

        private static List<ValueTuple<DirectionMask, DirectionMask>> readMasks(BinaryReader reader)
        {
            byte masksCount = reader.ReadByte();
            List<ValueTuple<DirectionMask, DirectionMask>> masks = new(masksCount);
            for (int i = 0; i < masksCount; i++)
                masks.Add(new((DirectionMask)reader.ReadByte(), (DirectionMask)reader.ReadByte()));
            return masks;
        }
        #endregion
    }
}
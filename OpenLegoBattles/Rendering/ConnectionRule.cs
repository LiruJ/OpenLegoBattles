using GameShared.DataTypes;
using GlobalShared.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenLegoBattles.Rendering
{
    public class ConnectionRule
    {
        #region Types
        /// <summary>
        /// The 3 different states that a tile can be in for a rule.
        /// </summary>
        //[Flags]
        //private enum tileMaskType : byte { EitherFullOrEmpty = 0, MustBeFull = 0b01, MustBeEmpty = 0b10 }
        #endregion

        #region Dependencies
        private readonly ConnectionRuleSet connectionRuleSet;
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

        /// <summary>
        /// The collection of masks that apply to this rule.
        /// </summary>
        public IReadOnlyList<DirectionMask[]> Masks { get; }
        #endregion

        #region Constructors
        public ConnectionRule(ConnectionRuleSet connectionRuleSet, IReadOnlyList<ushort> tileIndices, IReadOnlyList<DirectionMask[]> masks)
        {
            TileIndices = tileIndices;
            Masks = masks;
            this.connectionRuleSet = connectionRuleSet;
        }
        #endregion

        //#region Rule Functions
        //public IEnumerable<DirectionMask> AllPossibleTileMasks()
        //{
        //    // Calculate the masks for every mask in the rule.
        //    foreach (DirectionMask[] masks in Masks)
        //    {
        //        // Calculate the mask types for each direction.
        //        tileMaskType[] directionMaskTypes = new tileMaskType[8];
        //        for (byte i = 0; i < 8; i++)
        //            directionMaskTypes[i] = calculateMaskTypeForDirection(new Direction(i), masks);

        //        // Go over all possiblities from the first bit.
        //        foreach (DirectionMask possiblity in allPossibleTileMasks(directionMaskTypes, 0, DirectionMask.None))
        //            yield return possiblity;
        //    }
        //}

        //private IEnumerable<DirectionMask> allPossibleTileMasks(tileMaskType[] directionMaskTypes, byte index, DirectionMask startMask)
        //{
        //    // Go over each remaining index in a mask.
        //    for (byte i = index; i < 8; i++)
        //    {
        //        // Get the current mask type.
        //        tileMaskType maskType = directionMaskTypes[i];

        //        // If the mask type is simply that it must be full, set the bit on the mask.
        //        if (maskType == tileMaskType.MustBeFull)
        //            startMask |= new Direction(i).ToMask();
        //        // Otherwise; recursively calculate all possible masks with this tile being full. Possiblities with this tile being empty will be calculated regardless.
        //        else if (maskType == tileMaskType.EitherFullOrEmpty)
        //            foreach (DirectionMask possiblity in allPossibleTileMasks(directionMaskTypes, (byte)(i + 1), startMask | new Direction(i).ToMask()))
        //                yield return possiblity;
        //    }

        //    // Return the calculated starting mask.
        //    yield return startMask;
        //}

        //private static tileMaskType calculateMaskTypeForDirection(Direction direction, (DirectionMask full, DirectionMask empty) masks)
        //{
        //    // Calculate the mask type based on the state requirements of the tile.
        //    bool requiresTile = direction.IsMaskDirectionSet(masks.full);
        //    bool requiresEmpty = direction.IsMaskDirectionSet(masks.empty);
        //    if (requiresTile && !requiresEmpty) return tileMaskType.MustBeFull;
        //    else if (!requiresTile && requiresEmpty) return tileMaskType.MustBeEmpty;
        //    else return tileMaskType.EitherFullOrEmpty;
        //}
        //#endregion

        #region Rule Functions
        public IEnumerable<uint> AllPossibleTileMasks()
        {
            // Calculate the masks for every mask in the rule.
            foreach (DirectionMask[] masks in Masks)
            {
                // Calculate the mask types for each direction.
                HashSet<byte>[] directionMaskTypes = new HashSet<byte>[8];
                for (byte i = 0; i < 8; i++)
                    directionMaskTypes[i] = calculateMaskTypeForDirection(new Direction(i), masks, connectionRuleSet.ValueCount);

                List<uint> result = new List<uint>();
                foreach (uint possiblity in allPossibleTileMasks(directionMaskTypes, 0, 0))
                    result.Add(possiblity);

                // Go over all possiblities from the first bit.
                foreach (uint possiblity in allPossibleTileMasks(directionMaskTypes, 0, 0))
                    yield return possiblity;
            }
        }

        private static HashSet<byte> calculateMaskTypeForDirection(Direction direction, DirectionMask[] masks, byte valueCount)
        {
            // Go over each mask and add any defined tiles to the set.
            byte definedMasks = 0;
            HashSet<byte> possibleValues = new();
            for (byte i = 0; i < valueCount; i++)
                // If the direction is set in the current mask, add its value to the set.
                if (direction.IsMaskDirectionSet(masks[i]))
                {
                    possibleValues.Add(i);
                    definedMasks++;
                }

            // There's no such thing as a "no value", so if the direction is not defined in any of the masks, it means the tile can be any value.
            if (definedMasks == 0)
                for (byte i = 0; i < valueCount; i++)
                    possibleValues.Add(i);

            // Return the possible values.
            return possibleValues;
        }

        private IEnumerable<uint> allPossibleTileMasks(HashSet<byte>[] directionPossibleValues, byte index, uint startMask)
        {
            // Go over each remaining index in a mask.
            uint maskResult = startMask;
            for (byte i = index; i < 8; i++)
            {
                HashSet<byte> possibleValues = directionPossibleValues[i];
                if (possibleValues.Count == 1)
                {
                    byte possibleValue = possibleValues.First();
                    uint directionMask = (uint)(possibleValue << ((7 - i) * connectionRuleSet.BitCount));
                    maskResult |= directionMask;
                }
                else
                {
                    foreach (byte possibleValue in possibleValues)
                    {
                        uint possibleDirectionMask = (uint)(possibleValue << ((7 - i) * connectionRuleSet.BitCount));
                        foreach (uint possibleHash in allPossibleTileMasks(directionPossibleValues, (byte)(i + 1), maskResult | possibleDirectionMask))
                            yield return possibleHash;
                    }
                    yield break;
                }

            }

            // Return the calculated starting mask.
            yield return maskResult;
        }
        #endregion

        #region Load Functions
        public static ConnectionRule LoadFromStream(ConnectionRuleSet connectionRuleSet, BinaryReader reader)
        {
            // Read the indices.
            List<ushort> indices = readIndices(reader);

            // Read the masks and return the created rule.
            List<DirectionMask[]> masks = readMasks(reader, connectionRuleSet.ValueCount);
            return new(connectionRuleSet, indices, masks);
        }

        public static ConnectionRule LoadDefaultFromStream(ConnectionRuleSet connectionRuleSet, BinaryReader reader)
        {
            // Read the indices and return the created default rule.
            List<ushort> indices = readIndices(reader);
            return new(connectionRuleSet, indices, new List<DirectionMask[]>());
        }

        private static List<ushort> readIndices(BinaryReader reader)
        {
            byte indexCount = reader.ReadByte();
            List<ushort> indices = new(indexCount);
            for (int i = 0; i < indexCount; i++)
                indices.Add(reader.ReadUInt16());
            return indices;
        }

        private static List<DirectionMask[]> readMasks(BinaryReader reader, byte valueCount)
        {
            byte masksCount = reader.ReadByte();
            List<DirectionMask[]> ruleMasks = new(masksCount);
            for (int i = 0; i < masksCount; i++)
            {
                // Read each defined mask for this rule.
                DirectionMask[] masks = new DirectionMask[valueCount];
                for (int j = 0; j < valueCount; j++)
                    masks[j] = (DirectionMask)reader.ReadByte();
                ruleMasks.Add(masks);
            }

            return ruleMasks;
        }
        #endregion
    }
}
using GlobalShared.DataTypes;
using LiruGameHelper.XML;
using System.Xml;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal class ConnectionRuleSaver
    {
        #region Properties
        /// <summary>
        /// The collection of indices from the original palette.
        /// </summary>
        public IReadOnlyList<ushort> OriginalIndices { get; }

        /// <summary>
        /// The collection of masks that this rule applies to.
        /// </summary>
        public IReadOnlyList<DirectionMask[]> RuleMasks { get; }
        #endregion

        #region Constructors
        public ConnectionRuleSaver(IReadOnlyList<ushort> originalIndices, IReadOnlyList<DirectionMask[]> ruleMasks)
        {
            OriginalIndices = originalIndices;
            RuleMasks = ruleMasks;
        }
        #endregion

        #region Load Functions
        public static ConnectionRuleSaver LoadFromXmlNode(XmlNode node, byte valueCount)
        {
            // Load the indices. These are required.
            ushort[] indices = node.ParseAttributeListValue("OriginalIndices", ushort.Parse);
            if (indices == null || indices.Length == 0) throw new Exception("Indices missing from rule node");

            // Load the masks.
            List<DirectionMask[]> ruleMasks = new();
            DirectionMask[]? masks = loadMasksFromNode(node, valueCount);
            if (masks != null) ruleMasks.Add(masks);
            foreach (XmlNode maskNode in node.ChildNodes)
            {
                masks = loadMasksFromNode(maskNode, valueCount);
                if (masks != null) ruleMasks.Add(masks);
            }

            // Create and return the rule saver.
            return new(indices, ruleMasks);
        }

        private static DirectionMask[]? loadMasksFromNode(XmlNode node, byte valueCount)
        {
            // If there are only two possible values, check against "FullMask" and "EmptyMask". Empty goes first as it has the value of 0, then Full has a value of 1.
            if (valueCount == 2)
                return node.TryParseAttributeValue("FullMask", Enum.TryParse, out DirectionMask fullMask) &&
                    node.TryParseAttributeValue("EmptyMask", Enum.TryParse, out DirectionMask emptyMask)
                    ? (new DirectionMask[] { emptyMask, fullMask })
                    : null;

            // Otherwise; check each numbered mask and add them to the array. If one is missing, default to DirectionMask.None.
            DirectionMask[] masks = new DirectionMask[valueCount];
            int existingMasks = 0;
            for (int i = 0; i < valueCount; i++)
            {
                if (node.TryParseAttributeValue("Mask" + i, Enum.TryParse, out DirectionMask mask))
                {
                    masks[i] = mask;
                    existingMasks++;
                }
                else
                    masks[i] = DirectionMask.None;
            }
            return existingMasks > 0 ? masks : null;
        }
        #endregion

        #region Save Functions
        public void SaveToFile(BinaryWriter writer, byte valueCount, IndexRemapper? blockIndicesMapper = null)
        {
            writer.Write((byte)OriginalIndices.Count);
            foreach (ushort originalIndex in OriginalIndices)
            {
                ushort remappedIndex = blockIndicesMapper == null ? originalIndex : blockIndicesMapper.GetRemappedBlockIndex(originalIndex);
                writer.Write(remappedIndex);
            }

            if (RuleMasks.Count == 0)
                return;
            
            writer.Write((byte)RuleMasks.Count);
            foreach (DirectionMask[] ruleMask in RuleMasks)
                for (int i = 0; i < valueCount; i++)
                    writer.Write((byte)ruleMask[i]);
        }
        #endregion
    }
}
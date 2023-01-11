using GlobalShared.DataTypes;
using LiruGameHelper.XML;
using System.Xml;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal class ConnectionRuleSaver
    {
        #region Properties
        public IReadOnlyList<ushort> OriginalIndices { get; }

        public IReadOnlyList<Tuple<DirectionMask, DirectionMask>> RuleMasks { get; }
        #endregion

        #region Constructors
        public ConnectionRuleSaver(IReadOnlyList<ushort> originalIndices, IReadOnlyList<Tuple<DirectionMask, DirectionMask>> ruleMasks)
        {
            OriginalIndices = originalIndices;
            RuleMasks = ruleMasks;
        }
        #endregion

        #region Load Functions
        public static ConnectionRuleSaver LoadFromXmlNode(XmlNode node)
        {
            // Load the indices. These are required.
            ushort[] indices = node.ParseAttributeListValue("OriginalIndices", ushort.Parse);
            if (indices == null || indices.Length == 0) throw new Exception("Indices missing from rule node");

            // Load the masks.
            List<Tuple<DirectionMask, DirectionMask>> ruleMasks = new();
            Tuple<DirectionMask, DirectionMask>? masks = loadMasksFromNode(node);
            if (masks != null) ruleMasks.Add(masks);
            foreach (XmlNode maskNode in node.ChildNodes)
            {
                masks = loadMasksFromNode(maskNode);
                if (masks != null) ruleMasks.Add(masks);
            }

            // Create and return the rule saver.
            return new(indices, ruleMasks);
        }

        private static Tuple<DirectionMask, DirectionMask>? loadMasksFromNode(XmlNode node)
        {
            // Parse the masks.
            if (node.TryParseAttributeValue("FullMask", Enum.TryParse, out DirectionMask fullMask) &&
                node.TryParseAttributeValue("EmptyMask", Enum.TryParse, out DirectionMask emptyMask))
                return new(fullMask, emptyMask);
            else return null;
        }
        #endregion

        #region Save Functions
        public void SaveToFile(BinaryWriter writer, IndexRemapper blockIndicesMapper)
        {
            writer.Write((byte)OriginalIndices.Count);
            foreach (ushort originalIndex in OriginalIndices)
                writer.Write(blockIndicesMapper.GetRemappedBlockIndex(originalIndex));

            if (RuleMasks.Count != 0) writer.Write((byte)RuleMasks.Count);
            foreach (Tuple<DirectionMask, DirectionMask> ruleMask in RuleMasks)
            {
                writer.Write((byte)ruleMask.Item1);
                writer.Write((byte)ruleMask.Item2);
            }
        }
        #endregion
    }
}

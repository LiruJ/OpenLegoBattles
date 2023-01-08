using GlobalShared.DataTypes;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal static class TileRuleSaver
    {
        #region Save Functions
        public static void SaveIndexList(BinaryWriter writer, byte indexCount, IEnumerable<ushort> indexList)
        {
            writer.Write(indexCount);
            foreach (ushort index in indexList)
                writer.Write(index);
        }

        public static void SaveConnectionRule(BinaryWriter writer, byte indexCount, IEnumerable<ushort> indexList, DirectionMask fullMask, DirectionMask emptyMask)
        {
            // Save the index list first.
            SaveIndexList(writer, indexCount, indexList);

            // Save the masks.
            writer.Write((byte)fullMask);
            writer.Write((byte)emptyMask);
        }

        public static void SaveConnectionRule(BinaryWriter writer, ushort index, DirectionMask fullMask, DirectionMask emptyMask)
            => SaveConnectionRule(writer, 1, new ushort[] { index }, fullMask, emptyMask);
        #endregion
    }
}

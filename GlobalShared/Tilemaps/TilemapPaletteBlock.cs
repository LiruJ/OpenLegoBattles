using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace GlobalShared.Tilemaps
{
    public class TilemapPaletteBlock : IEnumerable<ushort>
    {
        #region Properties
        public ushort TopLeft { get; }

        public ushort TopMiddle { get; }

        public ushort TopRight { get; }

        public ushort BottomLeft { get; }

        public ushort BottomMiddle { get; }

        public ushort BottomRight { get; }
        #endregion

        #region Constructors
        public TilemapPaletteBlock(ushort topLeft, ushort topMiddle, ushort topRight, ushort bottomLeft, ushort bottomMiddle, ushort bottomRight)
        {
            TopLeft = topLeft;
            TopMiddle = topMiddle;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomMiddle = bottomMiddle;
            BottomRight = bottomRight;
        }
        #endregion

        #region Save Functions
        public void SaveToFile(BinaryWriter writer)
        {
            foreach (ushort subTileIndex in this)
                writer.Write(subTileIndex);
        }
        #endregion

        #region Load Functions
        public static TilemapPaletteBlock LoadUInt16(BinaryReader reader) => new(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

        public static TilemapPaletteBlock LoadUInt8(BinaryReader reader) => new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        #endregion

        #region Enumeration Functions
        public IEnumerator<ushort> GetEnumerator()
        {
            yield return TopLeft;
            yield return TopMiddle;
            yield return TopRight;
            yield return BottomLeft;
            yield return BottomMiddle;
            yield return BottomRight;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}

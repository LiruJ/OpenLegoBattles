using System;

namespace GlobalShared.DataTypes
{
    [Flags]
    public enum DirectionMask : byte
    {
        None = 0,
        All = Top | TopRight | Right | BottomRight | Bottom | BottomLeft | Left | TopLeft,

        Top         = 1 << 7,
        TopRight    = 1 << 6,
        Right       = 1 << 5,
        BottomRight = 1 << 4,
        Bottom      = 1 << 3,
        BottomLeft  = 1 << 2,
        Left        = 1 << 1,
        TopLeft     = 1,
    }
}

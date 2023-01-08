using ContentUnpacker.Spritesheets;
using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using System.Drawing;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal class TreeConnectionRules
    {
        #region Constants
        /// <summary>
        /// The index of the first stump in the <see cref="treeTilePaletteIndices"/> collection.
        /// </summary>
        private const byte stumpStarts = 16;

        /// <summary>
        /// The indices of the tree tiles within the original faction block palette.
        /// </summary>
        private static readonly IReadOnlyList<ushort> treeTilePaletteIndices = new List<ushort>()
        {
            0, 1, 2,
            5, 6, 7,
            10, 11, 12,
            15, 16, 17,
            20, 21, 22,
            175, 176, 177, 178, 179,
            180, 181, 182, 183, 184,
            185, 186, 187, 188, 189,
        };

        /// <summary>
        /// The number of sub-tiles used by the trees. Note that every tree tile has a mask applied to it and as such is unique, so the number of sub-tiles is simply the number of blocks times <c>6</c>.
        /// </summary>
        public static ushort UsedSubIndicesCount => (ushort)(treeTilePaletteIndices.Count * 6);
        #endregion

        #region Rule Functions
        public static void SaveTreeRules(string outputDirectory)
        {
            // Create the writer.
            string filePath = Path.Combine(outputDirectory, "TreeRules.trs");
            Directory.CreateDirectory(outputDirectory);
            FileStream outputFile = File.Create(filePath);
            using BinaryWriter treeWriter = new(outputFile);

            // Save the palette first.
            treeWriter.Write((ushort)treeTilePaletteIndices.Count);
            for (ushort i = 0; i < treeTilePaletteIndices.Count * 6; i++)
                treeWriter.Write(i);

            // Write the default tile.
            // TODO: This should use the stumps eventually.
            byte stumpCount = (byte)(treeTilePaletteIndices.Count - stumpStarts);
            ushort[] defaultIndices = new ushort[stumpCount];
            for (ushort blockIndex = stumpStarts, i = 0; blockIndex < treeTilePaletteIndices.Count; blockIndex++, i++)
                defaultIndices[i] = blockIndex;
            TileRuleSaver.SaveIndexList(treeWriter, stumpCount, defaultIndices);

            // Write the actual rules that define which tiles are used where.
            treeWriter.Write((byte)3);
            TileRuleSaver.SaveConnectionRule(treeWriter, 4, DirectionMask.Top | DirectionMask.Right | DirectionMask.Bottom | DirectionMask.Left, DirectionMask.None);
            TileRuleSaver.SaveConnectionRule(treeWriter, 0, DirectionMask.Bottom | DirectionMask.BottomRight | DirectionMask.Right, DirectionMask.TopLeft | DirectionMask.Left | DirectionMask.Top);
            TileRuleSaver.SaveConnectionRule(treeWriter, 6, DirectionMask.Top | DirectionMask.TopRight | DirectionMask.Right, DirectionMask.Bottom | DirectionMask.BottomLeft | DirectionMask.Left);
        }
        #endregion

        #region Sprite Functions
        public static void AddTreesToSpritesheet(ColourPaletteLoader colourPalette, SpritesheetSaver outputSpritesheet, SpritesheetLoader factionSpritesheet, TilemapBlockPalette blockPalette, string factionTilesetName)
        {
            Bitmap treeMask = new(Image.FromFile(Path.Combine("Masks", factionTilesetName + "TreeMask.png")));
            int maskTileWidth = treeMask.Width / SpritesheetLoader.TileSize;
            for (int i = 0; i < treeTilePaletteIndices.Count; i++)
            {
                // Get the palette block index of the tree, then reverse-index to get the top-left sub-tile index of the mask.
                ushort treeBlockOriginalIndex = treeTilePaletteIndices[i];
                ushort maskIndex = (ushort)((treeBlockOriginalIndex * 3) + (Math.Floor((treeBlockOriginalIndex * 3) / (float)maskTileWidth) * maskTileWidth));

                // Write the block using the mask.
                outputSpritesheet.WriteBlockFromLoader(factionSpritesheet, colourPalette, blockPalette.Blocks[treeBlockOriginalIndex], treeMask, maskIndex);
            }
        }
        #endregion
    }
}

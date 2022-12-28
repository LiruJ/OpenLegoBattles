using ContentUnpacker.Decompressors;
using ContentUnpacker.Loaders;
using ContentUnpacker.Spritesheets;
using System.Collections.Concurrent;
using System.Drawing;

namespace ContentUnpacker.Tilemaps
{
    internal class FactionTilePalette
    {
        #region Constants
        /// <summary>
        /// The count of "usable" tiles in the faction palette.
        /// </summary>
        public const ushort FactionPaletteEndIndex = 440;

        /// <summary>
        /// The indices of the tree tiles within the faction block palette.
        /// </summary>
        private static readonly IReadOnlyList<ushort> treeTilePaletteIndices = new List<ushort>()
        {
            0, 1, 2,
            5, 6, 7,
            10, 11, 12,
            15, 16, 17,
            20, 21, 22,
        };

        /// <summary>
        /// The map of the fog from the fog tileset to a 0-based tileset.
        /// </summary>
        private static readonly IndexRemapper fogMapper = new();

        static FactionTilePalette()
        {
            // Load the fog palette.
            TilemapPalette fogPalette = TilemapPalette.LoadFromFile(Path.Combine("Masks/FogTilePalette"), null, false);

            // Map all the fog tiles.
            foreach (TilemapPaletteBlock fogBlock in fogPalette)
                fogMapper.AddCollection(fogBlock);
        }
        #endregion

        #region Properties
        /// <summary>
        /// The name of the faction owning this tile palette.
        /// </summary>
        public string FactionTilesetName { get; }

        /// <summary>
        /// The name of the tile palette file that this palette uses.
        /// </summary>
        public string TilePaletteName { get; }

        /// <summary>
        /// The raw block palette for this faction, kept exactly as it is defined in the game.
        /// </summary>
        public TilemapPalette FactionPalette { get; }

        /// <summary>
        /// The collection of sub-tiles that are actually used by maps of this faction.
        /// </summary>
        public IndexRemapper UsedTerrainSubTilesMapper { get; } = new();

        /// <summary>
        /// The sub-tile index of the terrain sub-tiles.
        /// </summary>
        public static ushort? TerrainSubTileStartIndex { get; private set; } = null;
        #endregion

        #region Constructors
        public FactionTilePalette(TilemapPalette factionPalette, string factionTilesetName, string tilePaletteName)
        {
            FactionPalette = factionPalette;
            FactionTilesetName = factionTilesetName;
            TilePaletteName = tilePaletteName;
        }
        #endregion

        #region Save Functions
        public void FinaliseAndSave(CommandLineOptions options, SpritesheetLoader fogSpritesheet)
        {
            // Load the colour palette and create the output spritesheet.
            PaletteLoader colourPalette = new();
            colourPalette.Load(new BinaryReader(File.OpenRead(Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, FactionTilesetName + ".NCLR"))));
            using SpritesheetSaver outputSpritesheet = SpritesheetSaver.CreateCustomSpritesheet(32, 8);

            // Load the spritesheet for the faction.
            string factionSpritesheetPath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, FactionTilesetName);
            using SpritesheetLoader factionSpritesheet = SpritesheetLoader.Load(factionSpritesheetPath);

            // Add the trees and fog first.
            addTreesToSpritesheet(colourPalette, outputSpritesheet, factionSpritesheet);
            addFogToSpritesheet(colourPalette, outputSpritesheet, fogSpritesheet);

            // Set the terrain sub-tile index to the current index of the saver, as that is where the terrain tiles will be placed.
            TerrainSubTileStartIndex = outputSpritesheet.CurrentTileIndex;

            // Add the terrain tiles.
            foreach (ushort originalSubTileIndex in UsedTerrainSubTilesMapper)
                outputSpritesheet.WriteTileFromLoader(factionSpritesheet, colourPalette, originalSubTileIndex);

            // Save the output spritesheet.
            outputSpritesheet.Save(options.OutputFolder, FactionTilesetName);
        }

        private void addTreesToSpritesheet(PaletteLoader colourPalette, SpritesheetSaver outputSpritesheet, SpritesheetLoader factionSpritesheet)
        {
            Bitmap treeMask = new(Bitmap.FromFile(Path.Combine("Masks", FactionTilesetName + "TreeMask.png")));
            int maskTileWidth = treeMask.Width / 8;
            for (int i = 0; i < treeTilePaletteIndices.Count; i++)
            {
                // Get the palette block index of the tree, then reverse-index to get the top-left sub-tile index of the mask.
                ushort treeBlockOriginalIndex = treeTilePaletteIndices[i];
                ushort maskIndex = (ushort)((treeBlockOriginalIndex * 3) + (Math.Floor((treeBlockOriginalIndex * 3) / (float)maskTileWidth) * maskTileWidth));

                // Write the block using the mask.
                outputSpritesheet.WriteBlockFromLoader(factionSpritesheet, colourPalette, FactionPalette.Blocks[treeBlockOriginalIndex], treeMask, maskIndex);
            }
        }

        private void addFogToSpritesheet(PaletteLoader colourPalette, SpritesheetSaver outputSpritesheet, SpritesheetLoader fogSpritesheet)
        {
            foreach (ushort originalFogSubTileIndex in fogMapper)
                outputSpritesheet.WriteTileFromLoader(fogSpritesheet, colourPalette, originalFogSubTileIndex);
        }
        #endregion

        #region Block Functions
        /// <summary>
        /// Takes the given block palette for a map and the original index straight from the map file and returns the associated block that would be normally used.
        /// </summary>
        /// <param name="mapPalette"> The palette for the map file. </param>
        /// <param name="originalIndex"> The original block index. </param>
        /// <returns></returns>
        public TilemapPaletteBlock GetOriginalBlock(TilemapPalette? mapPalette, ushort originalIndex)
            => (originalIndex >= FactionTilePalette.FactionPaletteEndIndex) ? mapPalette.Blocks[originalIndex - FactionTilePalette.FactionPaletteEndIndex] : FactionPalette.Blocks[originalIndex];

        #endregion

        #region Sub Tile Functions
        /// <summary>
        /// Calculates and returns the sub-tile index of the given index coming straight from the palette file.
        /// This is based on the sub-tile index after trees and fog have been added, and is only valid after <see cref="FinaliseAndSave(SpritesheetLoader, TilemapPalette)"/> is called.
        /// </summary>
        /// <param name="originalIndex"> The original sub-tile index coming straight from the palette file. </param>
        /// <returns></returns>
        public ushort GetTerrainSubTilePaletteIndex(ushort originalIndex)
        {
            // Ensure the offset exists.
            if (TerrainSubTileStartIndex == null)
                throw new Exception("Faction palette must first be finalised and saved before sub-tiles can be gotten.");

            // Map the original index to the remapped index, add the offset, and return.
            return (ushort)(UsedTerrainSubTilesMapper.GetRemappedBlockIndex(originalIndex) + TerrainSubTileStartIndex.Value);
        }
        #endregion

        #region Global Tile Functions
        public static void SaveTreeRules(string outputDirectory)
        {
            // Create the writer.
            string filePath = Path.Combine(outputDirectory, "TreeRules.trs");
            FileStream outputFile = File.Create(filePath);
            using BinaryWriter treeWriter = new(outputFile);

            // Save the palette first.
            treeWriter.Write((ushort)treeTilePaletteIndices.Count);
            for (ushort i = 0; i < treeTilePaletteIndices.Count * 6; i++)
                treeWriter.Write(i);

            // TODO: Write the actual rules that define which tiles are used where.
        }

        public static void SaveFogRules(string outputDirectory)
        {
            // Load the fog palette.
            TilemapPalette fogPalette = TilemapPalette.LoadFromFile(Path.Combine("Masks/FogTilePalette"), null, false);

            // Create the writer.
            string filePath = Path.Combine(outputDirectory, "FogRules.trs");
            FileStream outputFile = File.Create(filePath);
            using BinaryWriter fogWriter = new(outputFile);

            // Calculate the starting index of the fog sub-tiles in the destination sheet, and the start of the fog sub-tiles in the fog spritesheet.
            ushort fogDestinationSubTileStart = (ushort)(treeTilePaletteIndices.Count * 6);
            ushort fogSourceSubTileStart = fogPalette.Min((fogBlock) => fogBlock.Min());

            // Write the fog palette first, remapped to the optimised spritesheet.
            fogWriter.Write((ushort)fogPalette.Blocks.Count);
            foreach (TilemapPaletteBlock fogBlock in fogPalette)
                foreach (ushort fogSubTileIndex in fogBlock)
                    fogWriter.Write((ushort)((fogSubTileIndex - fogSourceSubTileStart) + fogDestinationSubTileStart));

            // TODO: Write the actual rules that define which tiles are used where.
        }
        #endregion

        #region Load Functions
        public static FactionTilePalette Load(string factionTilesetName)
        {
            // Calculate the tile palette name.
            string tilePaletteName = factionTilesetName[..^2];

            // Load the faction palette.
            string factionPalettePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, "BP", tilePaletteName);
            TilemapPalette factionPalette = TilemapPalette.LoadFromFile(factionPalettePath, FactionPaletteEndIndex);

            // Create and return the faction tile palette.
            return new FactionTilePalette(factionPalette, factionTilesetName, tilePaletteName);
        }
        #endregion
    }
}

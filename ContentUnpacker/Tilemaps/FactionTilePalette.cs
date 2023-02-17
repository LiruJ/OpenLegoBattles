using ContentUnpacker.Decompressors;
using ContentUnpacker.Spritesheets;
using System.Drawing;
using GlobalShared.Tilemaps;
using ContentUnpacker.Tilemaps.TileRules;

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
        /// The map of the fog from the fog tileset to a 0-based tileset.
        /// </summary>
        private static readonly IndexRemapper fogMapper = new();

        static FactionTilePalette()
        {
            // Load the fog palette.
            TilemapBlockPalette fogPalette = TilemapBlockPalette.LoadFromFile(Path.Combine("Masks/FogTilePalette"), null, false);

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
        public TilemapBlockPalette FactionPalette { get; }

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
        public FactionTilePalette(TilemapBlockPalette factionPalette, string factionTilesetName, string tilePaletteName)
        {
            FactionPalette = factionPalette;
            FactionTilesetName = factionTilesetName;
            TilePaletteName = tilePaletteName;
        }
        #endregion

        #region Save Functions
        public void FinaliseAndSave(CommandLineOptions options, SpritesheetLoader fogSpritesheet)
        {
            // The different files are all under the same path but with different extensions. Use one path and let the loaders deal with the extensions.
            string factionFilePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, FactionTilesetName);

            // Load the colour palette and create the output spritesheet.
            ColourPaletteLoader colourPalette = ColourPaletteLoader.Load(factionFilePath);
            using SpritesheetSaver outputSpritesheet = SpritesheetSaver.CreateCustomSpritesheet(32, 8);

            // Load the spritesheet for the faction.
            using SpritesheetLoader factionSpritesheet = SpritesheetLoader.Load(factionFilePath);

            // Add the trees and fog first.
            TreeConnectionRules.AddTreesToSpritesheet(colourPalette, outputSpritesheet, factionSpritesheet, FactionPalette, FactionTilesetName);
            FogConnectionRules.AddFogToSpritesheet(colourPalette, outputSpritesheet, fogSpritesheet);

            // Set the terrain sub-tile index to the current index of the saver, as that is where the terrain tiles will be placed.
            TerrainSubTileStartIndex = outputSpritesheet.CurrentTileIndex;

            // Add the terrain tiles.
            foreach (ushort originalSubTileIndex in UsedTerrainSubTilesMapper)
                outputSpritesheet.WriteTileFromLoader(factionSpritesheet, colourPalette, originalSubTileIndex);

            // Save the output spritesheet.
            outputSpritesheet.Save(options.OutputFolder, FactionTilesetName);
        }
        #endregion

        #region Block Functions
        /// <summary>
        /// Takes the given block palette for a map and the original index straight from the map file and returns the associated block that would be normally used.
        /// </summary>
        /// <param name="mapPalette"> The palette for the map file. </param>
        /// <param name="originalIndex"> The original block index. </param>
        /// <returns></returns>
        public TilemapPaletteBlock GetOriginalBlock(TilemapBlockPalette? mapPalette, ushort originalIndex)
            => (originalIndex >= FactionPaletteEndIndex) ? mapPalette.Blocks[originalIndex - FactionPaletteEndIndex] : FactionPalette.Blocks[originalIndex];
        #endregion

        #region Sub Tile Functions
        /// <summary>
        /// Calculates and returns the sub-tile index of the given index coming straight from the palette file.
        /// This is based on the sub-tile index after trees and fog have been added, and is only valid after <see cref="FinaliseAndSave(SpritesheetLoader, TilemapBlockPalette)"/> is called.
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

        #region Load Functions
        public static FactionTilePalette Load(string factionTilesetName)
        {
            // Calculate the tile palette name.
            string tilePaletteName = factionTilesetName[..^2];

            // Load the faction palette.
            string factionPalettePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, "BP", tilePaletteName);
            TilemapBlockPalette factionPalette = TilemapBlockPalette.LoadFromFile(factionPalettePath, FactionPaletteEndIndex);

            // Create and return the faction tile palette.
            return new FactionTilePalette(factionPalette, factionTilesetName, tilePaletteName);
        }
        #endregion
    }
}

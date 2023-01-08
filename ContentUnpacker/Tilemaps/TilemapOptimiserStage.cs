using ContentUnpacker.Decompressors;
using ContentUnpacker.Spritesheets;
using ContentUnpacker.Tilemaps.TileRules;
using GlobalShared.Content;
using GlobalShared.Tilemaps;

namespace ContentUnpacker.Tilemaps
{
    internal class TilemapOptimiserStage
    {
        #region Stage Functions
        public static async Task BeginAsync(CommandLineOptions options)
        {
            // Analyse the maps to build collections of used palette indices, load the palettes, then analyse the sub-tiles used by the used palette blocks, then finally create the faction data.
            HashSet<string> tilesetNames = new();
            Dictionary<string, TilemapLoader> tilemapDataByName = await loadTilemapDataAsync(tilesetNames);
            Dictionary<string, FactionTilePalette> factionTilePalettesByName = await loadFactionPalettesAsync(tilesetNames);
            Dictionary<string, TilemapBlockPalette> mapPalettesByName = await loadMapPalettesAsync(tilemapDataByName);
            await analyseSubTileDataAsync(factionTilePalettesByName, mapPalettesByName, tilemapDataByName);
            await createFactionSpritesheetsAsync(options, factionTilePalettesByName);

            // Save the maps.
            await saveTilemapsAsync(options, tilemapDataByName, factionTilePalettesByName, mapPalettesByName);

            // Close the readers.
            foreach (TilemapLoader mapData in tilemapDataByName.Values)
                mapData.Dispose();
        }
        #endregion

        #region Load Functions
        private static async Task<Dictionary<string, TilemapLoader>> loadTilemapDataAsync(HashSet<string> tilesetNames)
        {
            // Load each map data.
            Dictionary<string, TilemapLoader> tilemapDataByName = new();

            //foreach (KeyValuePair<string, BinaryReader> nameReaderPair in mapReadersByName)
            foreach (string mapFilepath in Directory.EnumerateFiles(TilemapLoader.InputRootDirectory))
            {
                // Load the map and add it to the collections.
                TilemapLoader tilemapData = TilemapLoader.Load(Path.GetFileNameWithoutExtension(mapFilepath));
                tilemapDataByName.Add(tilemapData.MapName, tilemapData);
                tilesetNames.Add(tilemapData.TilesheetName);
            }

            // Analyse each map data and return.
            await Parallel.ForEachAsync(tilemapDataByName.Values, async (TilemapLoader tilemapData, CancellationToken cancellationToken) => await Task.Run(tilemapData.AnalyseIndices, cancellationToken));
            return tilemapDataByName;
        }

        private static async Task<Dictionary<string, FactionTilePalette>> loadFactionPalettesAsync(HashSet<string> tilesetNames)
        {
            // Load the faction palettes, which are shared between multiple maps.
            Dictionary<string, FactionTilePalette> factionTilePalettesByName = new();
            await Task.Run(() =>
            {
                foreach (string factionTilesetName in tilesetNames)
                {
                    FactionTilePalette factionTilePalette = FactionTilePalette.Load(factionTilesetName);
                    factionTilePalettesByName.Add(factionTilePalette.FactionTilesetName, factionTilePalette);
                }
            });
            return factionTilePalettesByName;
        }

        private static async Task<Dictionary<string, TilemapBlockPalette>> loadMapPalettesAsync(Dictionary<string, TilemapLoader> tilemapDataByName)
        {
            // Load all palettes referenced by map files.
            Dictionary<string, TilemapBlockPalette> mapPalettesByName = new();
            await Task.Run(() =>
            {
                foreach (TilemapLoader tilemapData in tilemapDataByName.Values)
                {
                    // Load the map palette, if it exists. Only the test map is missing a palette.
                    string mapPalettePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, "BP", $"DetailTiles_{tilemapData.MapName}");
                    if (TilemapBlockPalette.TryLoadFromFile(mapPalettePath, out TilemapBlockPalette? mapPalette))
                        mapPalettesByName.Add(tilemapData.MapName, mapPalette);
                }
            });
            return mapPalettesByName;
        }

        private static async Task analyseSubTileDataAsync(Dictionary<string, FactionTilePalette> factionTilePalettesByName, Dictionary<string, TilemapBlockPalette> mapPalettesByName, Dictionary<string, TilemapLoader> tilemapDataByName)
        {
            // Go over each tilemap and add its tile palette sub-tiles to the collections.
            await Task.Run(() =>
            {
                foreach (TilemapLoader tilemapData in tilemapDataByName.Values)
                {
                    // Get the tile palettes for this map and add all of its subtiles to the faction tile palette.
                    FactionTilePalette factionTilePalette = factionTilePalettesByName[tilemapData.TilesheetName];
                    TilemapBlockPalette factionPalette = factionTilePalettesByName[tilemapData.TilesheetName].FactionPalette;
                    mapPalettesByName.TryGetValue(tilemapData.MapName, out TilemapBlockPalette? mapPalette);
                    tilemapData.AddAllUsedTilesToTilePalette(factionTilePalette, mapPalette);
                }
            });
        }

        private static async Task createFactionSpritesheetsAsync(CommandLineOptions options, Dictionary<string, FactionTilePalette> factionTilePalettesByName)
        {
            // Create the global tree/fog palette/rules.
            string tilesetFolderPath = Path.Combine(options.OutputFolder, SpritesheetSaver.DefaultSpriteOutputFolder);
            TreeConnectionRules.SaveTreeRules(tilesetFolderPath);
            FactionTilePalette.SaveFogRules(tilesetFolderPath);

            // Load the fog spritesheet.
            using SpritesheetLoader fogSpritesheet = SpritesheetLoader.Load(Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, "FowTileset"));

            // Create the spritesheets for each faction.
            await Task.Run(() =>
            {
                // Save the tile palettes, which saves the sprites.
                foreach (FactionTilePalette factionTilePalette in factionTilePalettesByName.Values)
                    factionTilePalette.FinaliseAndSave(options, fogSpritesheet);
            });
        }

        private static async Task saveTilemapsAsync(CommandLineOptions options, Dictionary<string, TilemapLoader> tilemapDataByName, Dictionary<string, FactionTilePalette> factionTilePalettesByName, Dictionary<string, TilemapBlockPalette> mapPalettesByName)
        {

            string outputDirectoryPath = Path.Combine(options.OutputFolder, ContentFileUtil.TilemapDirectoryName);

            await Parallel.ForEachAsync(tilemapDataByName.Values, async (TilemapLoader tilemapData, CancellationToken cancellationToken) =>
            {
                await Task.Run(() =>
                {
                    FactionTilePalette factionTilePalette = factionTilePalettesByName[tilemapData.TilesheetName];
                    mapPalettesByName.TryGetValue(tilemapData.MapName, out TilemapBlockPalette? mapPalette);
                    tilemapData.Save(outputDirectoryPath, factionTilePalette, mapPalette);
                }, cancellationToken);
            });
        }
        #endregion
    }
}
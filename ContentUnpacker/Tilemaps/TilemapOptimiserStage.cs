using ContentUnpacker.Decompressors;
using ContentUnpacker.Loaders;
using ContentUnpacker.Spritesheets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ContentUnpacker.Tilemaps
{
    internal class TilemapOptimiserStage
    {
        #region Dependencies
        private readonly CommandLineOptions options;
        #endregion

        #region Constructors
        public TilemapOptimiserStage(CommandLineOptions options)
        {
            this.options = options;
        }
        #endregion

        #region Stage Functions
        public async Task BeginAsync()
        {
            // Analyse the maps to build collections of used palette indices, load the palettes, then analyse the sub-tiles used by the used palette blocks, then finally create the faction data.
            HashSet<string> tilesetNames = new();
            Dictionary<string, TilemapData> tilemapDataByName = await loadTilemapDataAsync(tilesetNames);
            Dictionary<string, FactionTilePalette> factionTilePalettesByName = await loadFactionPalettesAsync(tilesetNames);
            Dictionary<string, TilemapPalette> mapPalettesByName = await loadMapPalettesAsync(tilemapDataByName);
            await analyseSubTileDataAsync(factionTilePalettesByName, mapPalettesByName, tilemapDataByName);
            await createFactionSpritesheetsAsync(factionTilePalettesByName);

            // Save the maps.
            await saveTilemapsAsync(tilemapDataByName, factionTilePalettesByName, mapPalettesByName);

            // Close the readers.
            foreach (TilemapData mapData in tilemapDataByName.Values)
                mapData.Dispose();
        }
        #endregion

        #region Load Functions
        private async Task<Dictionary<string, TilemapData>> loadTilemapDataAsync(HashSet<string> tilesetNames)
        {
            // Load each map data.
            Dictionary<string, TilemapData> tilemapDataByName = new();

            //foreach (KeyValuePair<string, BinaryReader> nameReaderPair in mapReadersByName)
            foreach (string mapFilepath in Directory.EnumerateFiles(Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, TilemapData.MapsDirectoryName)))
            {
                // Load the map and add it to the collections.
                TilemapData tilemapData = TilemapData.Load(Path.GetFileNameWithoutExtension(mapFilepath));
                tilemapDataByName.Add(tilemapData.MapName, tilemapData);
                tilesetNames.Add(tilemapData.TilesheetName);
            }

            // Analyse each map data and return.
            await Parallel.ForEachAsync(tilemapDataByName.Values, async (TilemapData tilemapData, CancellationToken cancellationToken) => await Task.Run(tilemapData.AnalyseIndices, cancellationToken));
            return tilemapDataByName;
        }

        private async Task<Dictionary<string, FactionTilePalette>> loadFactionPalettesAsync(HashSet<string> tilesetNames)
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

        private async Task<Dictionary<string, TilemapPalette>> loadMapPalettesAsync(Dictionary<string, TilemapData> tilemapDataByName)
        {
            // Load all palettes referenced by map files.
            Dictionary<string, TilemapPalette> mapPalettesByName = new();
            await Task.Run(() =>
            {
                foreach (TilemapData tilemapData in tilemapDataByName.Values)
                {
                    // Load the map palette, if it exists. Only the test map is missing a palette.
                    string mapPalettePath = Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, "BP", $"DetailTiles_{tilemapData.MapName}");
                    if (TilemapPalette.TryLoadFromFile(mapPalettePath, out TilemapPalette? mapPalette))
                        mapPalettesByName.Add(tilemapData.MapName, mapPalette);
                }
            });
            return mapPalettesByName;
        }

        private async Task analyseSubTileDataAsync(Dictionary<string, FactionTilePalette> factionTilePalettesByName, Dictionary<string, TilemapPalette> mapPalettesByName, Dictionary<string, TilemapData> tilemapDataByName)
        {
            // Go over each tilemap and add its tile palette sub-tiles to the collections.
            await Task.Run(() =>
            {
                foreach (TilemapData tilemapData in tilemapDataByName.Values)
                {
                    // Get the tile palettes for this map and add all of its subtiles to the faction tile palette.
                    FactionTilePalette factionTilePalette = factionTilePalettesByName[tilemapData.TilesheetName];
                    TilemapPalette factionPalette = factionTilePalettesByName[tilemapData.TilesheetName].FactionPalette;
                    mapPalettesByName.TryGetValue(tilemapData.MapName, out TilemapPalette? mapPalette);
                    tilemapData.AddAllUsedTilesToTilePalette(factionTilePalette, mapPalette);
                }
            });
        }

        private async Task createFactionSpritesheetsAsync(Dictionary<string, FactionTilePalette> factionTilePalettesByName)
        {
            // Create the global tree/fog palette/rules.
            string tilesetFolderPath = Path.Combine(options.OutputFolder, SpritesheetSaver.DefaultSpriteOutputFolder);
            FactionTilePalette.SaveTreeRules(tilesetFolderPath);
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

        private async Task saveTilemapsAsync(Dictionary<string, TilemapData> tilemapDataByName, Dictionary<string, FactionTilePalette> factionTilePalettesByName, Dictionary<string, TilemapPalette> mapPalettesByName)
        {

            string outputDirectoryPath = Path.Combine(options.OutputFolder, TilemapData.MapsDirectoryName);

            await Parallel.ForEachAsync(tilemapDataByName.Values, async (TilemapData tilemapData, CancellationToken cancellationToken) =>
            {
                await Task.Run(() =>
                {
                    FactionTilePalette factionTilePalette = factionTilePalettesByName[tilemapData.TilesheetName];
                    mapPalettesByName.TryGetValue(tilemapData.MapName, out TilemapPalette? mapPalette);
                    tilemapData.Save(outputDirectoryPath, factionTilePalette, mapPalette);
                }, cancellationToken);
            });
        }
        #endregion
    }
}

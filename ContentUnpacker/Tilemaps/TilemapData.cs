using ContentUnpacker.Decompressors;
using GlobalShared.Content;
using GlobalShared.Tilemaps;
using System.Text;

namespace ContentUnpacker.Tilemaps
{
    internal class TilemapData : IDisposable
    {
        #region Constants
        private const string fileExtension = ".map";

        public const string MapsDirectoryName = "Maps";
        #endregion

        #region Dependencies
        private readonly BinaryReader reader;
        #endregion

        #region Fields
        private readonly IndexRemapper blocksMapper = new();
        #endregion

        #region Properties
        /// <summary>
        /// The name of this map's file.
        /// </summary>
        public string MapName { get; }

        /// <summary>
        /// The name of this map's tileset.
        /// </summary>
        public string TilesheetName { get; }

        /// <summary>
        /// The position in the file where the data starts. (The first index of the first data map).
        /// </summary>
        public int DataStart { get; }

        /// <summary>
        /// The width of the map in tiles.
        /// </summary>
        public byte Width { get; }

        /// <summary>
        /// The height of the map in tiles.
        /// </summary>
        public byte Height { get; }

        public ushort HighestIndex { get; private set; }
        #endregion

        #region Constructors
        private TilemapData(BinaryReader reader, byte width, byte height, string mapName, string tilesetName, int dataStart)
        {
            this.reader = reader;
            MapName = mapName;
            TilesheetName = tilesetName;
            DataStart = dataStart;
            Width = width;
            Height = height;
        }
        #endregion

        #region Analysis Functions
        public void AnalyseIndices()
        {
            // Skip the data tiles and trees.
            reader.BaseStream.Position = DataStart + (Width * Height * 3);
            ushort treeStripCount = reader.ReadUInt16();
            reader.BaseStream.Position += treeStripCount;

            // Go over every single detail tile index.
            for (int i = 0; i < Width * Height; i++)
            {
                // Read the index and set the highest, if it is higher than the current, and add it to the mapper.
                ushort originalIndex = reader.ReadUInt16();
                HighestIndex = Math.Max(HighestIndex, originalIndex);
                blocksMapper.TryAdd(originalIndex);
            }
        }

        public void AddAllUsedTilesToTilePalette(FactionTilePalette factionTilePalette, TilemapPalette? mapPalette)
        {
            // Add each used block to the tile palette.
            foreach (ushort usedOriginalBlockIndex in blocksMapper)
            {
                TilemapPaletteBlock originalBlock = factionTilePalette.GetOriginalBlock(mapPalette, usedOriginalBlockIndex);
                factionTilePalette.UsedTerrainSubTilesMapper.AddCollection(originalBlock);
            }
        }
        #endregion

        #region Save Functions
        public void Save(string outputDirectoryPath, FactionTilePalette factionTilePalette, TilemapPalette? mapPalette)
        {
            // Create the writer.
            Directory.CreateDirectory(outputDirectoryPath);
            string filePath = Path.ChangeExtension(Path.Combine(outputDirectoryPath, MapName), ContentFileUtil.TilemapExtension);
            FileStream file = File.Create(filePath);
            using BinaryWriter writer = new(file);

            // Write the basic map data.
            writer.Write(MapName);
            writer.Write(TilesheetName);
            writer.Write(Width);
            writer.Write(Height);

            // Write the remapped palette.
            writer.Write(blocksMapper.Count);
            foreach (ushort originalBlockIndex in blocksMapper)
            {
                // Get the block from the original index, then remap the sub-tiles to the new spritesheet.
                TilemapPaletteBlock terrainBlock = factionTilePalette.GetOriginalBlock(mapPalette, originalBlockIndex);
                foreach (ushort originalSubTileIndex in terrainBlock)
                {
                    ushort remappedIndex = factionTilePalette.GetTerrainSubTilePaletteIndex(originalSubTileIndex);
                    writer.Write(remappedIndex);
                }
            }

            // Create the tile data.
            TileData[] tiles = new TileData[Width * Height];

            // Read the first layer, the tile types.
            reader.BaseStream.Position = DataStart;
            for (int i = 0; i < tiles.Length; i++)
                tiles[i].TileType = (TileType)reader.ReadByte();

            // Position the reader to the start of the trees, read the tree count, then read the trees.
            reader.BaseStream.Position = DataStart + (Width * Height * 3);
            ushort treeStripCount = reader.ReadUInt16();
            int treeIndex = 0;
            bool placeTrees = false;
            for (int i = 0; i < treeStripCount; i++)
            {
                // Get the length of the strip.
                byte stripLength = reader.ReadByte();

                // Go over the strip and add the trees to the data.
                for (int t = treeIndex; t < treeIndex + stripLength; t++)
                    tiles[t].HasTree = placeTrees;

                // Increment the tree index by the strip length and invert the tree placement.
                treeIndex += stripLength;
                placeTrees = !placeTrees;
            }

            // Write the map detail data.
            for (int i = 0; i < tiles.Length; i++)
            {
                // Read the original index, remap it, and write it.
                ushort originalBlockIndex = reader.ReadUInt16();
                ushort remappedBlockIndex = blocksMapper.GetRemappedBlockIndex(originalBlockIndex);
                tiles[i].Index = remappedBlockIndex;
            }

            // Write the tile data.
            for (int i = 0; i < tiles.Length; i++)
                writer.Write(tiles[i].RawData);
        }
        #endregion

        #region Load Functions
        public static TilemapData Load(string mapName)
        {
            // Load the file.
            string filePath = Path.ChangeExtension(Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, MapsDirectoryName, mapName), fileExtension);
            FileStream file = File.OpenRead(filePath);
            BinaryReader reader = new(file);

            // Load the basic header of the map file.
            reader.BaseStream.Position = 7;
            byte width = reader.ReadByte();
            byte height = reader.ReadByte();
            reader.BaseStream.Position += 2;
            string tilesetName = readTilesetName(reader);

            // Create and return the data.
            return new(reader, width, height, mapName, tilesetName, 0x2B);
        }

        private static string readTilesetName(BinaryReader reader)
        {
            // Read the tileset name.
            bool stringContinue = true;
            StringBuilder tilesetNameStringBuilder = new();
            while (stringContinue)
            {
                char currentChar = reader.ReadChar();
                stringContinue = currentChar != 0;
                if (stringContinue)
                    tilesetNameStringBuilder.Append(currentChar);
            }

            // Return the tileset name.
            return tilesetNameStringBuilder.ToString();
        }
        #endregion

        #region Disposal Functions
        public void Dispose()
        {
            reader.Dispose();
        }
        #endregion
    }
}

using ContentUnpacker.Spritesheets;
using GlobalShared.Tilemaps;
using LiruGameHelper.XML;
using System.Drawing;
using System.Xml;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal class TreeConnectionRules
    {
        #region Fields
        private static readonly ConnectionRuleSaver defaultRule;
        
        private static readonly IReadOnlyList<ConnectionRuleSaver> connectionRules;

        /// <summary>
        /// The indices of the tree tiles within the original faction block palette.
        /// </summary>
        private static readonly IReadOnlyList<ushort> treeTilePaletteIndices;

        /// <summary>
        /// The number of values each tile can be.
        /// </summary>
        private static readonly byte valueCount;
        #endregion

        #region Properties
        /// <summary>
        /// The number of sub-tiles used by the trees. Note that every tree tile has a mask applied to it and as such is unique, so the number of sub-tiles is simply the number of blocks times <c>6</c>.
        /// </summary>
        public static ushort UsedSubIndicesCount => (ushort)(treeTilePaletteIndices.Count * 6);
        #endregion

        #region Constructors
        static TreeConnectionRules()
        {
            // Load the xml file.
            XmlDocument treeRulesFile = new();
            treeRulesFile.Load(Path.Combine("Masks", "TreeTileRules.xml"));
            List<ushort> treeTilePaletteIndices = new();
            List<ConnectionRuleSaver> connectionRules = new();
            XmlNode mainNode = treeRulesFile.LastChild ?? throw new Exception("Tree rules file missing main node");

            // Load and calculate the bit values.
            mainNode.ParseAttributeValueOrDefault<byte>("ValueCount", byte.TryParse, out valueCount, 2);

            // Load the default rules.
            XmlNode? defaultRuleNode = mainNode.SelectSingleNode("DefaultRule") ?? throw new Exception("Trees missing default rule node.");
            defaultRule = ConnectionRuleSaver.LoadFromXmlNode(defaultRuleNode, valueCount);
            treeTilePaletteIndices.AddRange(defaultRule.OriginalIndices);

            // Load each rule.
            XmlNode rulesNode = mainNode.SelectSingleNode("Rules") ?? throw new Exception("Trees missing rules node.");
            foreach (XmlNode? ruleNode in rulesNode.ChildNodes)
            {
                // Ensure the node is valid.
                if (ruleNode == null || ruleNode.NodeType != XmlNodeType.Element)
                    continue;

                // Load the rule.
                ConnectionRuleSaver connection = ConnectionRuleSaver.LoadFromXmlNode(ruleNode, valueCount);
                connectionRules.Add(connection);
                treeTilePaletteIndices.AddRange(connection.OriginalIndices);
            }

            // Sort and save the indices.
            treeTilePaletteIndices.Sort();
            TreeConnectionRules.treeTilePaletteIndices = treeTilePaletteIndices;
            TreeConnectionRules.connectionRules = connectionRules;
        }
        #endregion
        
        #region Rule Functions 
        public static void SaveTreeRules(string outputDirectory)
        {
            // Create the writer.
            string filePath = Path.Combine(outputDirectory, "TreeRules.trs");
            Directory.CreateDirectory(outputDirectory);
            using FileStream outputFile = File.Create(filePath);
            using BinaryWriter treeWriter = new(outputFile);

            // Create the mapper between original and new tree indices.
            IndexRemapper blockIndicesMapper = new();
            blockIndicesMapper.AddCollection(treeTilePaletteIndices);

            // Save the palette first. Note that trees are each unique and thus the palette is contiguous, so the indices can be saved with a simple for loop.
            treeWriter.Write((ushort)treeTilePaletteIndices.Count);
            for (ushort i = 0; i < treeTilePaletteIndices.Count * 6; i++)
                treeWriter.Write(i);

            // Write the value count.
            treeWriter.Write(valueCount);

            // Write the default tile.
            defaultRule.SaveToFile(treeWriter, valueCount, blockIndicesMapper);

            // Write a 0 for the number of rules targeting the value 0 (which is none).
            treeWriter.Write((byte)0);

            // Write the actual rules that define which tiles are used where.
            treeWriter.Write((byte)connectionRules.Count);
            foreach (ConnectionRuleSaver rule in connectionRules)
                rule.SaveToFile(treeWriter, valueCount, blockIndicesMapper);
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

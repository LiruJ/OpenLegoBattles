using ContentUnpacker.Spritesheets;
using GlobalShared.Tilemaps;
using LiruGameHelper.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal static class FogConnectionRules
    {

        // Each tile can be defined in each mask.
        //  For example; with 3 different tile states, there are 3 separate masks.
        //  Each mask defines that the tile can have that value.
        //  If a tile is defined in multiple masks, it can be any of those values.
        //  If a tile is defined in all or no masks, it can be any value.
        //      There's no such thing as "no value", hence why not defining a value at all means it can be anything.
        // 
        // On the game side, a collection of possible values is generated for each tile, and a mask is generated with all possible values.
        

        #region Fields
        /// <summary>
        /// The block palette as it originally exists in the game, loaded from the tbp file.
        /// </summary>
        private static readonly TilemapBlockPalette originalPalette;

        /// <summary>
        /// The map between the original sub-tile indices of the fog to the new packed indices.
        /// </summary>
        private static readonly IndexRemapper fogMapper;

        private static readonly ConnectionRuleSaver defaultRule;

        private static readonly IReadOnlyList<ConnectionRuleSaver> connectionRules;

        /// <summary>
        /// The number of values each tile can be.
        /// </summary>
        private static readonly byte valueCount;
        #endregion

        #region Constructors
        static FogConnectionRules()
        {
            // Load the original block palette and create the mapper between original and new fog indices.
            originalPalette = TilemapBlockPalette.LoadFromFile(Path.Combine("Masks", "FogTilePalette"), null, false);
            List<ConnectionRuleSaver> connectionRules = new();
            fogMapper = new();
            foreach (TilemapPaletteBlock block in originalPalette)
                fogMapper.AddCollection(block);

            // Load the xml file.
            XmlDocument fogRulesFile = new();
            fogRulesFile.Load(Path.Combine("Masks", "FogTileRules.xml"));
            XmlNode mainNode = fogRulesFile.LastChild ?? throw new Exception("Fog rules file missing main node");

            // Load and calculate the bit values.
            mainNode.ParseAttributeValueOrDefault<byte>("ValueCount", byte.TryParse, out valueCount, 2);

            // Load the default rules.
            XmlNode? defaultRuleNode = mainNode.SelectSingleNode("DefaultRule") ?? throw new Exception("Fog missing default rule node.");
            defaultRule = ConnectionRuleSaver.LoadFromXmlNode(defaultRuleNode, valueCount);

            // Load each rule.
            XmlNode rulesNode = mainNode.SelectSingleNode("Rules") ?? throw new Exception("Fog missing rules node.");
            foreach (XmlNode? ruleNode in rulesNode.ChildNodes)
            {
                // Ensure the node is valid.
                if (ruleNode == null || ruleNode.NodeType != XmlNodeType.Element)
                    continue;

                // Load the rule.
                ConnectionRuleSaver connection = ConnectionRuleSaver.LoadFromXmlNode(ruleNode, valueCount);
                connectionRules.Add(connection);
            }
            FogConnectionRules.connectionRules = connectionRules;
        }
        #endregion

        #region Save Functions
        public static void SaveFogRules(string outputDirectory)
        {
            // Create the writer.
            string filePath = Path.Combine(outputDirectory, "FogRules.trs");
            Directory.CreateDirectory(outputDirectory);
            using FileStream outputFile = File.Create(filePath);
            using BinaryWriter fogWriter = new(outputFile);

            // Save the palette first.
            fogWriter.Write((ushort)originalPalette.Count);
            foreach (TilemapPaletteBlock block in originalPalette)
                foreach (ushort originalSubIndex in block)
                {
                    ushort remappedIndex = fogMapper.GetRemappedBlockIndex(originalSubIndex);
                    fogWriter.Write((ushort)(TreeConnectionRules.UsedSubIndicesCount + remappedIndex));
                }

            // Write the value count.
            fogWriter.Write(valueCount);

            // Write the default tile.
            defaultRule.SaveToFile(fogWriter, valueCount);

            // Write the actual rules that define which tiles are used where.
            fogWriter.Write((byte)connectionRules.Count);
            foreach (ConnectionRuleSaver rule in connectionRules)
                rule.SaveToFile(fogWriter, valueCount);
        }
        #endregion

        #region Sprite Functions
        public static void AddFogToSpritesheet(ColourPaletteLoader colourPalette, SpritesheetSaver outputSpritesheet, SpritesheetLoader fogSpritesheet)
        {
            // Go over each original index in the mapper. This will naturally ignore any duplicates and put the tiles into the spritesheet in the correct order.
            foreach (ushort originalSubIndex in fogMapper)
                outputSpritesheet.WriteTileFromLoader(fogSpritesheet, colourPalette, originalSubIndex);
        }
        #endregion
    }
}
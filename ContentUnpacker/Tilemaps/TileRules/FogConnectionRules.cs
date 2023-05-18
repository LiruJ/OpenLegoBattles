using ContentUnpacker.Spritesheets;
using GlobalShared.Tilemaps;
using LiruGameHelper.XML;
using System.Xml;

namespace ContentUnpacker.Tilemaps.TileRules
{
    internal static class FogConnectionRules
    {
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

        private static readonly IReadOnlyList<ConnectionRuleSaver>[] connectionRulesByValueType;

        /// <summary>
        /// The number of values each tile can be.
        /// </summary>
        private static readonly byte valueCount;
        #endregion

        #region Constructors
        static FogConnectionRules()
        {
            // Load the original block palette and create the mapper between original and new fog indices.
            originalPalette = TilemapBlockPalette.LoadFromFile(Path.Combine("Masks", "FogTilePalette"), false);
            fogMapper = new();
            foreach (TilemapPaletteBlock block in originalPalette)
                fogMapper.AddCollection(block);

            // Load the xml file.
            XmlDocument fogRulesFile = new();
            fogRulesFile.Load(Path.Combine("Masks", "FogTileRules.xml"));
            XmlNode mainNode = fogRulesFile.LastChild ?? throw new Exception("Fog rules file missing main node");

            // Load and calculate the bit values.
            mainNode.ParseAttributeValueOrDefault<byte>("ValueCount", byte.TryParse, out valueCount, 2);

            // Create the collection of rules for each value type.
            List<ConnectionRuleSaver>[] connectionRulesByValueType = new List<ConnectionRuleSaver>[valueCount];

            // Load the default rules.
            XmlNode? defaultRuleNode = mainNode.SelectSingleNode("DefaultRule") ?? throw new Exception("Fog missing default rule node.");
            defaultRule = ConnectionRuleSaver.LoadFromXmlNode(defaultRuleNode, valueCount);

            // Go over each value type.
            XmlNode targetValuesNode = mainNode.SelectSingleNode("Rules") ?? throw new Exception("Fog missing rules node.");
            foreach (XmlNode targetValueNode in targetValuesNode)
            {
                // Ensure the node is valid.
                if (targetValueNode.NodeType != XmlNodeType.Element)
                    continue;
                if (!targetValueNode.TryParseAttributeValue("Value", byte.TryParse, out byte targetValue) || targetValue < 0 || targetValue >= valueCount)
                    throw new Exception("Target node missing value attribute or it is out of range.");

                // Load each rule.
                foreach (XmlNode ruleNode in targetValueNode)
                {
                    // Ensure the node is valid.
                    if (ruleNode.NodeType != XmlNodeType.Element)
                        continue;

                    // Get the collection for the rules of this target value, creating one if it does not already exist.
                    List<ConnectionRuleSaver> connectionRules = connectionRulesByValueType[targetValue];
                    if (connectionRules == null)
                    {
                        connectionRules = new List<ConnectionRuleSaver>();
                        connectionRulesByValueType[targetValue] = connectionRules;
                    }

                    // Load the rule.
                    ConnectionRuleSaver connection = ConnectionRuleSaver.LoadFromXmlNode(ruleNode, valueCount);
                    connectionRules.Add(connection);
                }
            }

            FogConnectionRules.connectionRulesByValueType = connectionRulesByValueType;
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
            foreach (IReadOnlyList<ConnectionRuleSaver> connectionRules in connectionRulesByValueType)
            {
                if (connectionRules == null)
                    fogWriter.Write((byte)0);
                else
                {
                    fogWriter.Write((byte)connectionRules.Count);
                    foreach (ConnectionRuleSaver rule in connectionRules)
                        rule.SaveToFile(fogWriter, valueCount);
                }
            }
        }
        #endregion

        #region Sprite Functions
        public static void AddFogToSpritesheet(NDSColourPalette colourPalette, SpritesheetWriter outputSpritesheet, NDSTileReader fogSpritesheet)
        {
            // Go over each original index in the mapper. This will naturally ignore any duplicates and put the tiles into the spritesheet in the correct order.
            foreach (ushort originalSubIndex in fogMapper)
                outputSpritesheet.WriteTileFromReader(fogSpritesheet, colourPalette, originalSubIndex);
        }
        #endregion
    }
}
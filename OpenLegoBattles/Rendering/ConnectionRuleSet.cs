using GlobalShared.Content;
using GlobalShared.Tilemaps;
using GlobalShared.Utils;
using System.Collections.Generic;
using System.IO;

namespace OpenLegoBattles.Rendering
{
    public class ConnectionRuleSet
    {
        #region Fields
        private readonly Dictionary<uint, ConnectionRule> rulesByPossibleHashes = new();
        #endregion

        #region Properties
        /// <summary>
        /// The palette for this rule set, where the sub-tile indices relate to sub-tiles in the tilesheet.
        /// </summary>
        public TilemapBlockPalette BlockPalette { get; }

        /// <summary>
        /// The number of blocks in this rule set's palette.
        /// </summary>
        public ushort PaletteCount => (ushort)BlockPalette.Count;

        /// <summary>
        /// The offset that this rule set uses on the main block palette. This is applied to the internal index of a block within a rule to obtain the index of the block in the main palette.
        /// </summary>
        public ushort BlockPaletteOffset { get; }

        public ConnectionRule DefaultRule { get; }

        /// <summary>
        /// The number of values that a single tile can have.
        /// </summary>
        public byte ValueCount { get; }

        /// <summary>
        /// The number of bits required to store all possible tile values.
        /// </summary>
        public byte BitCount { get; }
        #endregion

        #region Constructors
        private ConnectionRuleSet(byte valueCount, TilemapBlockPalette blockPalette, ushort blockPaletteOffset, BinaryReader reader)
        {
            ValueCount = valueCount;
            BitCount = BitUtils.calculateBitCount(valueCount - 1);

            BlockPalette = blockPalette;
            BlockPaletteOffset = blockPaletteOffset;

            // Load and register the rules.
            DefaultRule = ConnectionRule.LoadDefaultFromStream(this, reader);
            IEnumerable<ConnectionRule> rules = readConnectionRules(this, reader);
            registerAllRuleHashes(rules);
        }
        #endregion

        #region Tile Functions
        public ushort GetBlockForTileHash(uint hash) 
            => (ushort)(BlockPaletteOffset + (rulesByPossibleHashes.TryGetValue(hash, out ConnectionRule rule) ? rule : DefaultRule).FirstIndex);
        #endregion

        #region Load Functions
        public static ConnectionRuleSet LoadFromFile(string filePath, ushort blockPaletteOffset)
        {
            // Load the file.
            using FileStream file = File.OpenRead(Path.ChangeExtension(filePath, ContentFileUtil.TileRuleSetExtension));
            using BinaryReader reader = new(file);

            // Load the palette.
            TilemapBlockPalette blockPalette = TilemapBlockPalette.LoadFromFile(reader);

            // Load the value count.
            byte valueCount = reader.ReadByte();

            // Create and return the rule set.
            return new(valueCount, blockPalette, blockPaletteOffset, reader);
        }

        private static IEnumerable<ConnectionRule> readConnectionRules(ConnectionRuleSet connectionRuleSet, BinaryReader reader)
        {
            byte ruleCount = reader.ReadByte();
            for (int i = 0; i < ruleCount; i++)
                yield return ConnectionRule.LoadFromStream(connectionRuleSet, reader);
        }

        private void registerAllRuleHashes(IEnumerable<ConnectionRule> rules)
        {
            foreach (ConnectionRule rule in rules)
                foreach (uint possibleHash in rule.AllPossibleTileMasks())
                        rulesByPossibleHashes.TryAdd(possibleHash, rule);
        }
        #endregion
    }
}

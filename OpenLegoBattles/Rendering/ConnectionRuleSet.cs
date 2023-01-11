using GlobalShared.Content;
using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenLegoBattles.Rendering
{
    internal class ConnectionRuleSet
    {
        #region Fields
        private readonly Dictionary<DirectionMask, ConnectionRule> rulesByPossibleHashes = new();
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
        #endregion

        #region Constructors
        private ConnectionRuleSet(TilemapBlockPalette blockPalette, ushort blockPaletteOffset, IReadOnlyList<ConnectionRule> rules, ConnectionRule defaultRule)
        {
            this.BlockPalette = blockPalette;
            BlockPaletteOffset = blockPaletteOffset;
            this.DefaultRule = defaultRule;

            // Register the rules.
            registerAllRuleHashes(rules);
        }
        #endregion

        #region Tile Functions
        public ushort GetBlockForTileHash(DirectionMask hash) 
            => (ushort)(BlockPaletteOffset + (rulesByPossibleHashes.TryGetValue(hash, out ConnectionRule rule) ? rule : DefaultRule).FirstIndex);
        #endregion

        #region Load Functions
        public static ConnectionRuleSet LoadFromFile(string filePath, ushort blockPaletteOffset)
        {
            // Load the file.
            FileStream file = File.OpenRead(Path.ChangeExtension(filePath, ContentFileUtil.TileRuleSetExtension));
            using BinaryReader reader = new(file);

            // Load the palette.
            TilemapBlockPalette blockPalette = TilemapBlockPalette.LoadFromFile(reader);

            // Load the rules.
            ConnectionRule defaultRule = ConnectionRule.LoadDefaultFromStream(reader);
            byte ruleCount = reader.ReadByte();
            List<ConnectionRule> rules = new(ruleCount);
            for (int i = 0; i < ruleCount; i++)
                rules.Add(ConnectionRule.LoadFromStream(reader));

            // Create and return the rule set.
            return new(blockPalette, blockPaletteOffset, rules, defaultRule);
        }

        private void registerAllRuleHashes(IReadOnlyList<ConnectionRule> rules)
        {
            foreach (ConnectionRule rule in rules)
                foreach (DirectionMask possibleHash in rule.AllPossibleTileMasks())
                    //if (!
                        rulesByPossibleHashes.TryAdd(possibleHash, rule);
                        //throw new Exception("Hash collision between two separate rules.");
        }
        #endregion
    }
}

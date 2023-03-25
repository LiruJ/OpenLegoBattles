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
        private readonly Dictionary<uint, ConnectionRule>[] hashRulesByTargetIndex;
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
            // Set number of possible values that are taken into account by the ruleset, and the number of bits required to store all values.
            ValueCount = valueCount;
            BitCount = BitUtils.calculateBitCount(valueCount - 1);

            // Create the collection of rulesets by targets.
            hashRulesByTargetIndex = new Dictionary<uint, ConnectionRule>[ValueCount];

            BlockPalette = blockPalette;
            BlockPaletteOffset = blockPaletteOffset;

            // Load and register the rules.
            DefaultRule = ConnectionRule.LoadDefaultFromStream(this, reader);
            loadAndRegisterAllRules(reader);
        }
        #endregion

        #region Tile Functions
        public ushort GetBlockForBinaryTileHash(uint hash)
        {
#if DEBUG
            if (ValueCount > 2) throw new System.Exception("Binary tile hash can only be used on a ruleset with a count of 2.");
#endif

            // Get the rule for the given hash in the rules for the value 1, defaulting if none is found.
            if (!hashRulesByTargetIndex[1].TryGetValue(hash, out ConnectionRule rule)) rule = DefaultRule;
            return (ushort)(BlockPaletteOffset + rule.FirstIndex);
        }

        public ushort GetBlockForTileHash(byte targetValue, uint hash)
        {
#if DEBUG
            if (!TargetValueHasRules(targetValue)) throw new System.Exception("Target value has no assigned rules.");
#endif
            // TODO: Random indices.
            // Get the rules for the target value, try get a rule for the hash (defaulting if none is found), and return an index from it.
            Dictionary<uint, ConnectionRule> rulesByPossibleHashes = hashRulesByTargetIndex[targetValue];
            if (!rulesByPossibleHashes.TryGetValue(hash, out ConnectionRule rule)) rule = DefaultRule;
            return (ushort)(BlockPaletteOffset + rule.FirstIndex);
        }


        /// <summary>
        /// Finds if the given target value has rules defined to it.
        /// </summary>
        /// <param name="targetValue"> The value of the centre tile of the rules to query. </param>
        /// <returns> <c>True</c> if the given value has rules defined; otherwise <c>false</c>. </returns>
        public bool TargetValueHasRules(byte targetValue) => targetValue >= 0 && targetValue <= ValueCount && hashRulesByTargetIndex[targetValue] != null;
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

        private void loadAndRegisterAllRules(BinaryReader reader)
        {
            // Load the defined rules for each possible value.
            for (int currentRuleValue = 0; currentRuleValue < ValueCount; currentRuleValue++)
            {
                // Read the number of rules and create the collection for it, if there are rules to add.
                byte ruleCount = reader.ReadByte();
                if (ruleCount == 0) continue;

                Dictionary<uint, ConnectionRule> rulesByPossibleHashes = ruleCount > 0 ? hashRulesByTargetIndex[currentRuleValue] : null;
                if (rulesByPossibleHashes == null)
                {
                    rulesByPossibleHashes = new Dictionary<uint, ConnectionRule>();
                    hashRulesByTargetIndex[currentRuleValue] = rulesByPossibleHashes;
                }

                // Load each rule and add it to the collection.
                for (int ruleIndex = 0; ruleIndex < ruleCount; ruleIndex++)
                {
                    // Load the rule and add each of its possible hashes to the hash dictionary.
                    ConnectionRule rule = ConnectionRule.LoadFromStream(this, reader);
                    foreach (uint possibleHash in rule.AllPossibleTileMasks())
                        rulesByPossibleHashes.TryAdd(possibleHash, rule);
                }
            }
        }
        #endregion
    }
}
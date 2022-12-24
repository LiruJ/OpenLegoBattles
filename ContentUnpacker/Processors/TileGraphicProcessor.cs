using ContentUnpacker.Loaders;
using ContentUnpacker.Utils;
using Shared.Content;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;

namespace ContentUnpacker.Processors
{
    internal class TileGraphicProcessor : ContentProcessor
    {
        #region Constants
        /// <summary>
        /// The magic number of the tile graphic section.
        /// </summary>
        public const uint MagicWord = 0x4E434752;

        /// <summary>
        /// The magic number of the tile graphic data section.
        /// </summary>
        private const uint tileGraphicMagicWord = 0x43484152;
        #endregion

        #region XML Constants
        /// <summary>
        /// The name of the XML attribute specifying the name of the palette file to use.
        /// </summary>
        private const string paletteAttributeName = "Palette";

        /// <summary>
        /// The name of the XML attribute determining wether a separate file will also be created holding the tileset-specific data.
        /// </summary>
        private const string createTilesetAttributeName = "CreateTilesetFile";
        #endregion

        #region Constructors
        public TileGraphicProcessor(RomUnpacker romUnpacker, BinaryReader reader, XmlNode contentNode) : base(romUnpacker, reader, contentNode)
        {
        }
        #endregion

        #region Load Functions
        public override void Process()
        {
            // Create the directory.
            createOutputDirectory();

            // Load the header.
            NDSFileUtil.loadGenericHeader(reader);

            // Load the palette.
            contentNode.TryGetTextAttribute(paletteAttributeName, out string paletteName);
            PaletteLoader palette = romUnpacker.DataCache.GetOrLoadData<PaletteLoader>(paletteName);

            // Load the tile graphic header.
            ushort tileCount = loadHeader();

            // Calculate the size of the texture.
            int textureSize = (int)Math.Ceiling(Math.Sqrt(tileCount) * 8);

            // If an extra file should be created with the data of the tileset itself (tile dimensions and such), do so.
            if (contentNode.TryGetBooleanAttribute(createTilesetAttributeName, out bool shouldCreateTilesetFile) && shouldCreateTilesetFile)
                createTilesetFile(tileCount, (byte)(textureSize / 8));

            // Create the texture, load everything into it, then save it.
            Bitmap texture = new(textureSize, textureSize);
            writeToImage(texture, palette, textureSize, tileCount);
            texture.Save(Path.ChangeExtension(outputFilePath, ContentFileUtil.SpriteExtension), ImageFormat.Png);
        }

        /// <summary>
        /// Loads the NCGR header.
        /// </summary>
        /// <remarks>
        /// https://problemkaputt.de/gbatek.htm
        /// </remarks>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private ushort loadHeader()
        {
            // Ensure the magic string matches.
            if (tileGraphicMagicWord != reader.ReadUInt32())
                throw new Exception($"Node with name {contentNode.Name} has invalid CHAR magic word.");

            // Load the header.
            uint sectionSize = reader.ReadUInt32();
            ushort tileDataSizeKB = reader.ReadUInt16();
            reader.BaseStream.Position += 2;
            uint bitDepth = reader.ReadUInt32();
            reader.BaseStream.Position += 8;
            uint tileDataSizeBytes = reader.ReadUInt32();
            uint headerOffset = reader.ReadUInt32();

            // Return the tile count.
            return (ushort)(tileDataSizeBytes / 64);
        }

        private void writeToImage(Bitmap texture, PaletteLoader palette, int textureSize, ushort tileCount)
        {
            // Write each tile.
            int tileX = 0;
            int tileY = 0;
            for (int tileIndex = 0; tileIndex < tileCount; tileIndex++)
            {
                // Write the tile itself.
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                    {
                        // Get the colour index and associated colour.
                        byte colourIndex = reader.ReadByte();
                        Color colour = palette.GetColourAtIndex(colourIndex);

                        // Write the colour.
                        texture.SetPixel((tileX * 8) + x, (tileY * 8) + y, colour);
                    }

                // Handle incrementing the tile count.
                if (tileX + 1 >= textureSize / 8)
                {
                    tileX = 0;
                    tileY++;
                } 
                else tileX++;
            }
        }

        private void createTilesetFile(ushort tileCount, byte tilesetSizeTiles)
        {
            // Create the tileset directory.
            string outputDirectory = Path.Combine(romUnpacker.Options.OutputFolder, ContentFileUtil.TilesetDirectoryName);
            Directory.CreateDirectory(outputDirectory);

            // Create the writer.
            using BinaryWriter writer = new(File.OpenWrite(Path.ChangeExtension(Path.Combine(outputDirectory, Path.GetFileName(outputFilePath)), ContentFileUtil.TilesetExtension)));

            // Write the total tile count.
            writer.Write(tileCount);

            // Write the size of the tileset in tiles.
            writer.Write(tilesetSizeTiles);
            writer.Write(tilesetSizeTiles);

            // Write the name of the texture.
            writer.Write(Path.GetFileName(outputFilePath));
        }
        #endregion
    }
}

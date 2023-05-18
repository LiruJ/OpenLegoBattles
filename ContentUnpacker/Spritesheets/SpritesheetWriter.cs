using GlobalShared.Tilemaps;

namespace ContentUnpacker.Spritesheets
{
    /// <summary>
    /// Handles writing tile graphics to an image using <see cref="NDSColourPalette"/> and <see cref="NDSTileReader"/>s.
    /// </summary>
    internal class SpritesheetWriter : IDisposable
    {
        #region Constants
        /// <summary>
        /// The default folder name for tileset files.
        /// </summary>
        public const string DefaultTilesetOutputFolder = "Tilesets";

        /// <summary>
        /// The default folder name for sprites.
        /// </summary>
        public const string DefaultSpriteOutputFolder = "Sprites";
        #endregion

        #region Fields
        /// <summary>
        /// The image file that is being saved to.
        /// </summary>
        private readonly Image<Rgba32> texture;
        #endregion

        #region Properties
        /// <summary>
        /// The number of tiles that fit into the width of the spritesheet.
        /// </summary>
        public byte WidthInTiles { get; }

        /// <summary>
        /// The number of tiles that fit into the height of the spritesheet.
        /// </summary>
        public byte HeightInTiles { get; }

        /// <summary>
        /// The width of a single tile in pixels.
        /// </summary>
        public byte TileWidth { get; }

        /// <summary>
        /// The height of a single tile in pixels.
        /// </summary>
        public byte TileHeight { get; }

        /// <summary>
        /// The index of the next tile that will be written to.
        /// </summary>
        public ushort CurrentTileIndex { get; private set; } = 0;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new spritesheet writer with the given dimensions.
        /// </summary>
        /// <param name="widthInTiles"> The number of tiles that fit into the width of the spritesheet. </param>
        /// <param name="heightInTiles"> The number of tiles that fit into the height of the spritesheet. </param>
        /// <param name="tileWidth"> The width of a single tile in pixels. </param>
        /// <param name="tileHeight"> The height of a single tile in pixels. </param>
        public SpritesheetWriter(byte widthInTiles, byte heightInTiles, byte tileWidth, byte tileHeight)
        {
            WidthInTiles = widthInTiles;
            HeightInTiles = heightInTiles;
            TileWidth = tileWidth;
            TileHeight = tileHeight;

            texture = new(WidthInTiles * tileWidth, HeightInTiles * tileHeight);
        }

        /// <summary>
        /// Creates a custom spritesheet with the given dimensions.
        /// </summary>
        /// <param name="widthHeightInTiles"> The width and height of the texture in tiles. </param>
        /// <param name="tileSize"> The width and height of a tile in pixels. </param>
        public SpritesheetWriter(byte widthHeightInTiles, byte tileSize) : this(widthHeightInTiles, widthHeightInTiles, tileSize, tileSize) { }
        #endregion

        #region Tile Functions
        /// <summary>
        /// Writes the tile from the given <paramref name="source"/> reader using the given <paramref name="colourPalette"/> into the data at the current <see cref="CurrentTileIndex"/>, using the optional <paramref name="mask"/>.
        /// Handles incrementing <see cref="CurrentTileIndex"/>.
        /// </summary>
        /// <param name="source"> The source reader for the tiles. A total of 64 indices will be read from it. </param>
        /// <param name="colourPalette"> The source colour palette. </param>
        /// <param name="sourceTileIndex"> The index of the tile that is being read from the <paramref name="source"/>. </param>
        /// <param name="mask"> The optional mask file. If this is given, the <paramref name="maskTileIndex"/> should also be given. The mask is sampled, and any value that is 0 will not be read from the <paramref name="source"/>. </param>
        /// <param name="maskTileIndex"> The index of the tile on the mask to sample. </param>
        public void WriteTileFromReader(NDSTileReader source, NDSColourPalette colourPalette, ushort sourceTileIndex, Image<A8>? mask = null, ushort? maskTileIndex = null)
        {
            // Calculate the x and y position on the saved spritesheet of the tile's top-left position.
            int tileX = CurrentTileIndex % WidthInTiles;
            int tileY = (int)Math.Floor((float)CurrentTileIndex / WidthInTiles);

            // Move the loader to the tile index.
            source.CurrentTileIndex = sourceTileIndex;

            // Handle masking.
            bool hasMask = mask != null && maskTileIndex != null;
            int maskPixelX = 0, maskPixelY = 0;
            if (hasMask)
            {
                maskPixelX = (int)Math.Floor(maskTileIndex.Value % Math.Floor(mask.Width / 8f)) * 8;
                maskPixelY = (int)Math.Floor(maskTileIndex.Value / Math.Floor(mask.Width / 8f)) * 8;
            }

            // Go over each pixel and copy from the source.
            for (int subTileY = 0; subTileY < 8; subTileY++)
                for (int subTileX = 0; subTileX < 8; subTileX++)
                {
                    // Calculate the pixel position.
                    int pixelPositionX = (tileX * 8) + subTileX;
                    int pixelPositionY = (tileY * 8) + subTileY;

                    // If a mask exists, check its value at the position. If it is over 0, skip setting this pixel.
                    bool pixelIsMasked = hasMask && mask[maskPixelX + subTileX, maskPixelY + subTileY].PackedValue > 0;
                    if (pixelIsMasked) continue;

                    // Get the colour index and associated colour.
                    byte colourIndex = source.ReadNextByte();
                    Color colour = colourPalette[colourIndex];
                    texture[pixelPositionX, pixelPositionY] = colour;
                }

            // Increment the index.
            CurrentTileIndex++;
        }

        /// <summary>
        /// Writes each tile from the given <paramref name="block"/> and <paramref name="source"/> reader using the given <paramref name="colourPalette"/>, using the optional <paramref name="mask"/>.
        /// </summary>
        /// <param name="source"> The source reader for the tiles. A total of 64 indices will be read from it. </param>
        /// <param name="colourPalette"> The source colour palette. </param>
        /// <param name="block"> The tilemap block to read from. Each index of it will be written. </param>
        /// <param name="mask"> The optional mask file. If this is given, the <paramref name="maskTileIndex"/> should also be given. The mask is sampled, and any value that is 0 will not be read from the <paramref name="source"/>. </param>
        /// <param name="maskTileIndex"> The index of the tile on the mask to sample. </param>
        public void WriteBlockFromReader(NDSTileReader source, NDSColourPalette colourPalette, TilemapPaletteBlock block, Image<A8>? mask = null, ushort? maskBlockIndex = null)
        {
            // If there is a mask, start the tile offset at 0.
            ushort? maskOffset = mask == null ? null : 0;

            foreach (ushort subTileIndex in block)
            {
                // Write the current tile to the file.
                WriteTileFromReader(source, colourPalette, subTileIndex, mask, (ushort?)(maskBlockIndex + maskOffset));

                // If a mask was given, handle incrementing the offset so that it goes down to the next row once 3 tiles have been written.
                if (mask != null)
                    maskOffset = (ushort)(maskOffset == 2 ? (mask.Width / TileWidth) : maskOffset + 1);
            }
        }
        #endregion

        #region Write Functions
        /// <summary>
        /// Saves the image asyncronously.
        /// </summary>
        /// <param name="baseOutputDirectoryPath"> The folder into which to save. </param>
        /// <param name="filename"> The desired name of the file. The extension will always be changed to png. </param>
        /// <returns> The async task. </returns>
        public async Task SaveAsync(string baseOutputDirectoryPath, string filename)
        {
            // Create the file path.
            string outputFilePath = Path.ChangeExtension(Path.Combine(baseOutputDirectoryPath, DefaultSpriteOutputFolder, filename), ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            // Create the tileset file and be done with the image.
            createTilesetFile(CurrentTileIndex, WidthInTiles, HeightInTiles, baseOutputDirectoryPath, filename);
            await texture.SaveAsPngAsync(outputFilePath);
        }

        /// <summary>
        /// Creates a tileset file from the given parameters.
        /// </summary>
        /// <param name="tileCount"> The number of tiles in the tileset. </param>
        /// <param name="widthInTiles"> The width of the texture file in tiles. </param>
        /// <param name="heightInTiles"> The height of the texture file in tiles. </param>
        /// <param name="baseOutputDirectoryPath"> The base output path, where the <see cref="DefaultTilesetOutputFolder"/> will be appended, along with the <paramref name="filename"/>. </param>
        /// <param name="filename"> The name of the texture file, as well as the name of the desired tileset file. </param>
        private static void createTilesetFile(ushort tileCount, byte widthInTiles, byte heightInTiles, string baseOutputDirectoryPath, string filename)
        {
            // Create the tileset directory.
            string outputFilePath = Path.ChangeExtension(Path.Combine(baseOutputDirectoryPath, DefaultTilesetOutputFolder, filename), ".tst");
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            // Create the writer.
            FileStream fileStream = new(outputFilePath, FileMode.Create);
            using BinaryWriter writer = new(fileStream);

            // Write the total tile count.
            writer.Write(tileCount);

            // Write the size of the tileset in tiles.
            writer.Write(widthInTiles);
            writer.Write(heightInTiles);

            // Write the name of the texture.
            writer.Write(Path.GetFileName(filename));
        }
        #endregion

        #region Disposal Functions
        /// <summary>
        /// Disposes the underlying texture.
        /// </summary>
        public void Dispose() => texture.Dispose();
        #endregion
    }
}

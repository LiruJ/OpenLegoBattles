using ContentUnpacker.Loaders;
using ContentUnpacker.Tilemaps;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.Spritesheets
{
    internal class SpritesheetSaver : IDisposable
    {
        #region Constants
        public const string DefaultTilesetOutputFolder = "Tilesets";

        public const string DefaultSpriteOutputFolder = "Sprites";
        #endregion

        #region Fields
        private readonly Bitmap texture;
        #endregion

        #region Properties
        public byte WidthInTiles { get; }

        public byte HeightInTiles { get; }

        public byte TileWidth { get; }

        public byte TileHeight { get; }

        public ushort CurrentTileIndex { get; private set; } = 0;
        #endregion

        #region Constructors
        private SpritesheetSaver(byte widthInTiles, byte heightInTiles, byte tileWidth, byte tileHeight)
        {
            WidthInTiles = widthInTiles;
            HeightInTiles = heightInTiles;
            TileWidth = tileWidth;
            TileHeight = tileHeight;

            texture = new Bitmap(WidthInTiles * tileWidth, HeightInTiles * tileHeight);
        }
        #endregion

        #region Tile Functions
        public void WriteTileFromLoader(SpritesheetLoader source, PaletteLoader colourPalette, ushort sourceTileIndex, Bitmap? mask = null, ushort? maskTileIndex = null)
        {
            // Calculate the x and y position on the saved spritesheet.
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

            for (int subTileY = 0; subTileY < 8; subTileY++)
                for (int subTileX = 0; subTileX < 8; subTileX++)
                {
                    // Calculate the pixel position.
                    int pixelPositionX = (tileX * 8) + subTileX;
                    int pixelPositionY = (tileY * 8) + subTileY;

                    // If a mask exists, check its value at the position.
                    bool pixelIsMasked = hasMask && mask.GetPixel(maskPixelX + subTileX, maskPixelY + subTileY).A > 0;

                    if (pixelIsMasked)
                    {
                        int t = Color.Transparent.ToArgb();
                        Color co = mask.GetPixel(maskPixelX + subTileX, maskPixelY + subTileY);
                        int c = mask.GetPixel(maskPixelX + subTileX, maskPixelY + subTileY).ToArgb();
                    }

                    // Get the colour index and associated colour.
                    byte colourIndex = source.ReadNextByte();
                    if (!pixelIsMasked)
                    {
                        Color colour = colourPalette.GetColourAtIndex(colourIndex);

                        // Write the colour.
                        texture.SetPixel(pixelPositionX, pixelPositionY, colour);
                    }
                }

            // Increment the index.
            CurrentTileIndex++;
        }

        public void WriteBlockFromLoader(SpritesheetLoader source, PaletteLoader colourPalette, TilemapPaletteBlock block, Bitmap? mask = null, ushort? maskBlockIndex = null)
        {
            ushort? maskOffset = mask == null ? null : 0;

            foreach (ushort subTileIndex in block)
            {
                WriteTileFromLoader(source, colourPalette, subTileIndex, mask, (ushort?)(maskBlockIndex + maskOffset));

                if (mask != null)
                    maskOffset = (ushort)(maskOffset == 2 ? (mask.Width / 8) : maskOffset + 1);
            }
        }
        #endregion

        #region Write Functions
        public void Save(string baseOutputDirectoryPath, string filename)
        {
            // Create the file path.
            string outputFilePath = Path.ChangeExtension(Path.Combine(baseOutputDirectoryPath, DefaultSpriteOutputFolder, filename), ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            // Create the tileset file and be done with the image.
            createTilesetFile(CurrentTileIndex, WidthInTiles, HeightInTiles, baseOutputDirectoryPath, filename);
            texture.Save(outputFilePath, ImageFormat.Png);
            texture.Dispose();
        }

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

        #region Creation Functions
        public static SpritesheetSaver CreateCustomSpritesheet(byte widthHeightInTiles, byte tileSize) => CreateCustomSpritesheet(widthHeightInTiles, widthHeightInTiles, tileSize, tileSize);

        public static SpritesheetSaver CreateCustomSpritesheet(byte widthInTiles, byte heightInTiles, byte tileWidth, byte tileHeight)
        {
            return new SpritesheetSaver(widthInTiles, heightInTiles, tileWidth, tileHeight);
        }
        #endregion

        #region Disposal Functions
        public void Dispose()
        {
            texture.Dispose();
        }
        #endregion
    }
}

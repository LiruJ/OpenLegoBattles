using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenLegoBattles.Graphics;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.TilemapSystem;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using System.Collections.Generic;
using System.IO;

namespace OpenLegoBattles
{
    public class Game1 : Game
    {

        // TODO:

        // Get all map files loaded. Perhaps look into the NDS file system.
        // Run some tests on the maps and palettes to find which palette indices are actually used. Trees have palette indices but aren't defined in the maps, for example.
        // So essentially we want a list of TilePresets that are used at least once in a map. This list should contain no duplicates.
        // Get a list of every single subtile that is used at least once. Using this, create a black and white mask that can be overlaid atop the spritesheet and hide all subtiles that are not used in the maps.
        // Remap the subtiles so that all unused subtiles are omitted.

        // Develop specification for optimised tile palettes. These will be bundled in the map files and will only contain the tiles used by the map.
        // As the tree tiles will no longer by referenced by the tile palettes, they don't need to be added to the faction's spritesheet for now. Do the same with the fog tiles. Ensure indices are correctly updated.
        // 
        // Hand-develop palettes for tree tiles and fog tiles. These have to be custom remade in code anyway, so might as well make them neat. These will be used regardless of faction.

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Spritesheet spritesheet;

        private Tilemap tilemap;

        private RomContentManager romContentManager;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create the content loader.
            romContentManager = new RomContentManager(GraphicsDevice, Content.RootDirectory);

            // Unpack the rom file.
            if (!romContentManager.HasUnpacked)
            {
                OpenFileDialog fileDialog = new()
                {
                    Filter = "Rom Files (*.nds)|*.nds",
                    Title = "Choose Lego Battles ROM file"
                };

                while (fileDialog.ShowDialog() != DialogResult.OK)
                {

                    DialogResult messageBoxResult = MessageBox.Show("Rom file is required to play", "Rom is required", MessageBoxButtons.OK);
                    if (messageBoxResult == DialogResult.Abort)
                    {
                        Exit();
                        return;
                    }
                }
                romContentManager.UnpackRomAsync(fileDialog.FileName).Wait();
            }

            // Load the map.
            tilemap = romContentManager.Load<Tilemap>("mp01");

            for (int i = 0; i < tilemap.TilePalette.Count; i++)
            {
                if (tilemap.TilePalette[i].TopLeft == 812)
                {
                    Console.WriteLine();
                }
            }

            SortedDictionary<ushort, int> indexCounts = new SortedDictionary<ushort, int>();
            for (int x = 0; x < tilemap.Width; x++)
            {
                for (int y = 0; y < tilemap.Height; y++)
                {
                    if (!indexCounts.ContainsKey(tilemap.GetDetailTileAt(x, y).Index)) indexCounts.Add(tilemap.GetDetailTileAt(x, y).Index, 0);
                    indexCounts[tilemap.GetDetailTileAt(x, y).Index]++;
                }
            }

            // Load the spritesheet.
            spritesheet = romContentManager.Load<Spritesheet>(tilemap.TilesheetName);

            graphics.PreferredBackBufferWidth = spritesheet.PresetTileSize.X * tilemap.Width;
            graphics.PreferredBackBufferHeight = spritesheet.PresetTileSize.Y * tilemap.Height;
            graphics.ApplyChanges();

            //testSaveTiles(5, 100);

            fogTilesheet = romContentManager.Load<Spritesheet>("FogTileset");

            using BinaryReader reader = new BinaryReader(File.OpenRead("FogTilePalette"));

            for (int i = 0; i < reader.BaseStream.Length / 6; i++)
            {
                TilePreset tilePreset = new TilePreset((ushort)i, reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                fogTiles.Add(tilePreset);
            }
        }
        private Spritesheet fogTilesheet;
        private List<TilePreset> fogTiles = new List<TilePreset>();
        private void testSaveTiles(int tileWidth, int tileHeight)
        {
            RenderTarget2D renderTarget = new RenderTarget2D(GraphicsDevice, tileWidth * spritesheet.PresetTileSize.X, tileHeight * spritesheet.PresetTileSize.Y);

            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin();

            testSaveAllTiles(tileWidth, tileHeight);

            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            FileStream fileStream = File.Create("KingTiles.png");
            renderTarget.SaveAsPng(fileStream, renderTarget.Width, renderTarget.Height);
            fileStream.Close();

            renderTarget.Dispose();
        }
        private void testSaveAllTiles(int tileWidth, int tileHeight)
        {
            int tileIndex = 0;
            for (int y = 0; y < tileHeight; y++)
            {
                for (int x = 0; x < tileWidth; x++)
                {
                    tilemap.TilePalette[tileIndex].Draw(spriteBatch, spritesheet, x * spritesheet.PresetTileSize.X, y * spritesheet.PresetTileSize.Y);


                    tileIndex++;
                    if (tileIndex == 440) return;
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            spriteBatch.Begin();

            //for (int x = 0; x < tilemap.Width; x++)
            //{
            //    for (int y = 0; y < tilemap.Height; y++)
            //    {

            //        TilePreset tilePreset = tilemap.GetDetailTileAt(x, y);

            //        tilePreset.Draw(spriteBatch, spritesheet, x * spritesheet.PresetTileSize.X, y * spritesheet.PresetTileSize.Y);

            //        //Rectangle source = spritesheet.CalculateSourceRectangle(tilePreset.TopLeft);
            //        //spriteBatch.Draw(spritesheet.Texture, new Rectangle(x * 24, y * 16, 8, 8), source, Color.White);

            //        //source = spritesheet.CalculateSourceRectangle(tilePreset.TopMiddle);
            //        //spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 8, y * 16, 8, 8), source, Color.White);

            //        //source = spritesheet.CalculateSourceRectangle(tilePreset.TopRight);
            //        //spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 16, y * 16, 8, 8), source, Color.White);

            //        //source = spritesheet.CalculateSourceRectangle(tilePreset.BottomLeft);
            //        //spriteBatch.Draw(spritesheet.Texture, new Rectangle(x * 24, (y * 16) + 8, 8, 8), source, Color.White);

            //        //source = spritesheet.CalculateSourceRectangle(tilePreset.BottomMiddle);
            //        //spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 8, (y * 16) + 8, 8, 8), source, Color.White);

            //        //source = spritesheet.CalculateSourceRectangle(tilePreset.BottomRight);
            //        //spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 16, (y * 16) + 8, 8, 8), source, Color.White);

            //        if (tilemap.HasTreeAtPosition(x, y))
            //        {
            //            Rectangle source = spritesheet.CalculateSourceRectangle(20);
            //            spriteBatch.Draw(spritesheet.Texture, new Rectangle(x * 24, y * 16, 24, 16), source, Color.White);
            //        }
            //    }
            //}

            int i = 0;
            for (int y = 0; y < 8 && i < fogTiles.Count; y++)
            {
                for (int x = 0; x < 24 && i < fogTiles.Count; x++)
                {
                    fogTiles[i].Draw(spriteBatch, fogTilesheet, fogTilesheet.PresetTileSize.X * x, fogTilesheet.PresetTileSize.Y * y);
                    i++;
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
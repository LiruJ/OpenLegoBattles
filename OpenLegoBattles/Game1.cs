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

namespace OpenLegoBattles
{
    public class Game1 : Game
    {
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
            tilemap = romContentManager.Load<Tilemap>("pp1_1");

            // Load the spritesheet.
            spritesheet = romContentManager.Load<Spritesheet>(tilemap.TilesheetName);

            graphics.PreferredBackBufferWidth = spritesheet.PresetTileSize.X * tilemap.Width;
            graphics.PreferredBackBufferHeight = spritesheet.PresetTileSize.Y * tilemap.Height;
            graphics.ApplyChanges();

            // TODO: use this.Content to load your game content here
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

            for (int x = 0; x < tilemap.Width; x++)
            {
                for (int y = 0; y < tilemap.Height; y++)
                {

                    TilePreset tilePreset = tilemap.GetDetailTileAt(x, y);

                    Rectangle source = spritesheet.CalculateSourceRectangle(tilePreset.TopLeft);
                    spriteBatch.Draw(spritesheet.Texture, new Rectangle(x * 24, y * 16, 8, 8), source, Color.White);

                    source = spritesheet.CalculateSourceRectangle(tilePreset.TopMiddle);
                    spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 8, y * 16, 8, 8), source, Color.White);

                    source = spritesheet.CalculateSourceRectangle(tilePreset.TopRight);
                    spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 16, y * 16, 8, 8), source, Color.White);

                    source = spritesheet.CalculateSourceRectangle(tilePreset.BottomLeft);
                    spriteBatch.Draw(spritesheet.Texture, new Rectangle(x * 24, (y * 16) + 8, 8, 8), source, Color.White);

                    source = spritesheet.CalculateSourceRectangle(tilePreset.BottomMiddle);
                    spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 8, (y * 16) + 8, 8, 8), source, Color.White);

                    source = spritesheet.CalculateSourceRectangle(tilePreset.BottomRight);
                    spriteBatch.Draw(spritesheet.Texture, new Rectangle((x * 24) + 16, (y * 16) + 8, 8, 8), source, Color.White);

                    if (tilemap.HasTreeAtPosition(x, y))
                    {
                        source = spritesheet.CalculateSourceRectangle(20);
                        spriteBatch.Draw(spritesheet.Texture, new Rectangle(x * 24, y * 16, 24, 16), source, Color.White);
                    }
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
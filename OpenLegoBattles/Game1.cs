using GlobalShared.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.TilemapSystem;
using System.Windows.Forms;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace OpenLegoBattles
{
    public class Game1 : Game
    {

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private TileGraphicsManager tileGraphicsManager;

        private TilemapData tilemap;

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
            tilemap = romContentManager.Load<TilemapData>("pp1_1");

            tileGraphicsManager = TileGraphicsManager.Load(romContentManager, GraphicsDevice);
            tileGraphicsManager.LoadDataForMap(tilemap);

            graphics.PreferredBackBufferWidth = tileGraphicsManager.Tilesheet.TileSize.X * tilemap.Width;
            graphics.PreferredBackBufferHeight = tileGraphicsManager.Tilesheet.TileSize.Y * tilemap.Height;
            graphics.ApplyChanges();
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
                    int screenX = x * tileGraphicsManager.Tilesheet.TileSize.X;
                    int screenY = y * tileGraphicsManager.Tilesheet.TileSize.Y;

                    Rectangle source = tileGraphicsManager.GetTerrainBlockSource(tilemap[x, y].Index);

                    spriteBatch.Draw(tileGraphicsManager.Tilesheet.Texture, new Rectangle(screenX, screenY, tileGraphicsManager.Tilesheet.TileSize.X, tileGraphicsManager.Tilesheet.TileSize.Y), source, Color.White);

                    if (tilemap.HasTreeAtPosition(x, y))
                    {
                        DirectionMask mask = tileGraphicsManager.CreateTreeMask(tilemap, x, y);
                        ushort index = tileGraphicsManager.TreeRuleSet.GetBlockForTileHash(mask);
                        Rectangle treeSource = tileGraphicsManager.Tilesheet.CalculateSourceRectangle(index);
                        spriteBatch.Draw(tileGraphicsManager.Tilesheet.Texture, new Rectangle(screenX, screenY, tileGraphicsManager.Tilesheet.TileSize.X, tileGraphicsManager.Tilesheet.TileSize.Y), treeSource, Color.White);
                    }
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
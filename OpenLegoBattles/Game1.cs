using GuiCookie;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OpenLegoBattles.GameStates;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using System;
using System.Reflection;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace OpenLegoBattles
{
    public class Game1 : Game
    {
        #region Dependencies
        private readonly CommandLineOptions options;
        #endregion

        #region Fields
        private GraphicsDeviceManager graphics;

        private RomContentManager romContentManager;

        private GameStateManager gameStateManager;
        #endregion

        internal Game1(CommandLineOptions options)
        {
            graphics = new(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.options = options;
        }

        #region Initialisation Functions
        protected override void Initialize()
        {
            // Create the gamestate manager.
            gameStateManager = new(Services);
            initialiseServices();


            base.Initialize();
        }

        private void initialiseServices()
        {
            // Set the window title.
            Window.Title = "Open Lego Battles";

            // Initialise the services.
            Services.AddService(Window);
            Services.AddService(Services);
            Services.AddService(new Random());
            Services.AddService(gameStateManager);
            Services.AddService(GraphicsDevice);

            UIManager uiManager = new(this);
            uiManager.RegisterElementNamespace(Assembly.GetExecutingAssembly(), "OpenLegoBattles.Gui.Elements");
            uiManager.RegisterComponentNamespace(Assembly.GetExecutingAssembly(), "OpenLegoBattles.Gui.Elements");
            Services.AddService(uiManager);

            Services.AddService(Content);
        }
        #endregion

        protected override void LoadContent()
        {
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            // Create the content loader and set up anything that needs it.
            romContentManager = new(GraphicsDevice, Content.RootDirectory);
            Services.AddService(romContentManager);

#if CONTENTTEST
            // Do the basic content process without doing the whole decompression of the rom.
            string contentTestRomPath = "../../../../ContentUnpacker/LEGO Battles.nds";
            RomUnpacker romUnpacker = new(romContentManager.BaseGameDirectory);
            romUnpacker.UnpackRomAsync(contentTestRomPath).Wait();
#endif

            // Start with the intro screen which also checks for the rom content. If the intro should be skipped, just go straight to the main menu.
            if (options.SkipIntro && RomUnpacker.FindIfHasUnpacked(romContentManager.BaseGameDirectory))
            {
                Services.AddService(TileGraphicsManager.Load(romContentManager, GraphicsDevice));
                gameStateManager.CreateAndAddGameState<RuleTestState>();
            }
            else gameStateManager.CreateAndAddGameState<IntroState>();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            gameStateManager.RemoveAll();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            gameStateManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            gameStateManager.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}
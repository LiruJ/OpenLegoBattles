﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using System;
using System.Threading.Tasks;

namespace OpenLegoBattles.GameStates
{
    internal class IntroState : IGameState
    {
        #region Types
        private enum introState
        {
            Started = 0,
            AwaitingFile,
            UnpackingContent,
            PlayingIntro
        }
        #endregion

        #region Constants
        private const double introFadeTimeSeconds = 2;

        private const double introLogoTimeSeconds = 2;

        private const double introTotalTimeSeconds = introFadeTimeSeconds + introLogoTimeSeconds;
        #endregion

        #region Dependencies
        private readonly GameWindow window;
        private readonly ContentManager contentManager;
        private readonly GraphicsDevice graphicsDevice;
        private readonly RomContentManager romContentManager;
        private readonly GameStateManager gameStateManager;
        private readonly GameServiceContainer services;
        #endregion

        #region Fields
        private SpriteBatch spriteBatch;

        private introState currentState = introState.Started;

        private double currentIntroTime = 0;

        private Texture2D logo;

        private RomUnpacker romUnpacker;

        private Task unpackingTask = null;

        private string currentText = string.Empty;

        private SpriteFont unpackerFont;
        #endregion

        #region Properties
        public bool UpdateUnder => false;

        public bool DrawUnder => false;
        #endregion

        #region Constructors
        public IntroState(GameWindow window, ContentManager contentManager, GraphicsDevice graphicsDevice, RomContentManager romContentManager, GameStateManager gameStateManager, GameServiceContainer services)
        {
            // Set dependencies.
            this.window = window;
            this.contentManager = contentManager;
            this.graphicsDevice = graphicsDevice;
            this.romContentManager = romContentManager;
            this.gameStateManager = gameStateManager;
            this.services = services;
        }
        #endregion

        #region Load Functions
        public void Load()
        {
            // Create the spritebatch.
            spriteBatch = new(graphicsDevice);

            // Load the logo.
            unpackerFont = contentManager.Load<SpriteFont>("Fonts/UnpackerFont");
            logo = contentManager.Load<Texture2D>("LegoLogo");

            // Handle the state based on if the rom has already been unpacked.
            if (RomUnpacker.FindIfHasUnpacked(romContentManager.BaseGameDirectory)) currentState = introState.PlayingIntro;
            else startWaitingForRomFile();
        }

        public void Unload()
        {
            spriteBatch.Dispose();
            contentManager.UnloadAsset("Fonts/UnpackerFont");
            contentManager.UnloadAsset("LegoLogo");
        }
        #endregion

        #region Content Functions
        private void startWaitingForRomFile()
        {
            // Listen for the event and start awaiting the file drop.
            window.FileDrop += startUnpacking;
            currentState = introState.AwaitingFile;

            // Load the font and set the text.
            currentText = "Please drop the Lego Battles nds file onto the window";

            // Create the unpacker.
            romUnpacker = new(romContentManager.BaseGameDirectory);
        }

        private void startUnpacking(object sender, FileDropEventArgs dropArgs)
        {
            // Handle invalid an invalid file path.
            if (dropArgs.Files == null || dropArgs.Files.Length != 1)
            {
                currentText = "Please drop only the Lego Battles nds file onto the window!";
                return;
            }
            string filePath = dropArgs.Files[0];

            try
            {
                unpackingTask = romUnpacker.UnpackRomAsync(filePath);
                currentText = "Unpacking rom file";
            }
            catch (Exception exception)
            {
                currentText = "There was an error unpacking the file:\n" + exception.Message;
                return;
            }

            currentState = introState.UnpackingContent;
        }
        #endregion

        #region Update Functions
        public void Update(GameTime gameTime)
        {
            switch (currentState)
            {
                // Increment the intro time and handle starting the game when it's over.
                case introState.PlayingIntro:
                    currentIntroTime += gameTime.ElapsedGameTime.TotalSeconds;
                    if (currentIntroTime >= introTotalTimeSeconds)
                    {
                        // Start the main menu state.
                        // TODO: put main menu here.
                        services.AddService(TileGraphicsManager.Load(romContentManager, graphicsDevice));
                        gameStateManager.CreateAndAddGameState<RuleTestState>();
                        gameStateManager.Remove(this);
                    }
                    break;
                // If the rom file is currently being unpacked, handle updating the text and checking if it is done.
                case introState.UnpackingContent:
                    if (unpackingTask.IsCompleted)
                    {
                        currentState = introState.PlayingIntro;

                        // Unsubscribe from the file drop event.
                        window.FileDrop -= startUnpacking;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(romUnpacker.LastUnpackerMessage))
                            currentText = romUnpacker.LastUnpackerMessage;
                    }
                    break;
            }
        }
        #endregion

        #region Draw Functions
        public void Draw(GameTime gameTime)
        {
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.LinearClamp);

            // If the intro is playing, draw it.
            if (currentState == introState.PlayingIntro)
            {
                // Calculate the fade amount.
                float fadeAlpha = 1 - (float)Math.Max((currentIntroTime - introLogoTimeSeconds) / introFadeTimeSeconds, 0);
                
                // Draw the logo.
                Vector2 logoPosition = (window.ClientBounds.Size.ToVector2() / 2) - (logo.Bounds.Size.ToVector2() / 2);
                spriteBatch.Draw(logo, logoPosition, new Color(Color.White, fadeAlpha));
                
                // Draw the logo text.
                string logoText = "Lovebirb";
                Vector2 textSize = unpackerFont.MeasureString(logoText);
                Vector2 textPosition = new Vector2(window.ClientBounds.Width / 2, window.ClientBounds.Height - (textSize.Y * 2)) - (textSize / 2.0f);
                spriteBatch.DrawString(unpackerFont, logoText, textPosition, new Color(255, 109, 0, (int)(fadeAlpha * byte.MaxValue)));
            }
            // If the rom file has not yet been unpacked, handle the text screen.
            else
                drawCurrentText();

            spriteBatch.End();
        }

        private void drawCurrentText()
        {
            Vector2 textSize = unpackerFont.MeasureString(currentText);
            Vector2 textPosition = graphicsDevice.Viewport.Bounds.Center.ToVector2() - (textSize / 2.0f);
            spriteBatch.DrawString(unpackerFont, currentText, textPosition, Color.White);
        }
        #endregion
    }
}

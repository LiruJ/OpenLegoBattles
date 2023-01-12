using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using OpenLegoBattles.RomContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const double introFadeTimeSeconds = 1;

        private const double introTotalTimeSeconds = introFadeTimeSeconds + 3;
        #endregion

        #region Dependencies
        private readonly GameWindow window;
        private readonly ContentManager contentManager;
        private readonly GraphicsDevice graphicsDevice;
        private readonly RomContentManager romContentManager;
        #endregion

        #region Fields

        private readonly SpriteBatch spriteBatch;

        private introState currentState = introState.Started;

        private double currentIntroTime = 0;

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
        public IntroState(GameWindow window, ContentManager contentManager, GraphicsDevice graphicsDevice, RomContentManager romContentManager)
        {
            // Set dependencies.
            this.window = window;
            this.contentManager = contentManager;
            this.graphicsDevice = graphicsDevice;
            this.romContentManager = romContentManager;

            // Create the spritebatch.
            spriteBatch = new(graphicsDevice);

            // Handle the state based on if the rom has already been unpacked.
            if (RomUnpacker.FindIfHasUnpacked(romContentManager.BaseGameDirectory)) currentState = introState.PlayingIntro;
            else startWaitingForRomFile();
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
            unpackerFont = contentManager.Load<SpriteFont>("Fonts/UnpackerFont");

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
            spriteBatch.Begin();

            // If the intro is playing, draw it.
            if (currentState == introState.PlayingIntro)
            {
                // TODO: Draw the logo and fade it out.
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

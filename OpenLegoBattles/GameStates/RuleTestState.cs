using GlobalShared.Tilemaps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenLegoBattles.Rendering;
using OpenLegoBattles.RomContent;
using OpenLegoBattles.Scenes;
using OpenLegoBattles.TilemapSystem;
using OpenLegoBattles.Utils;
using System;

namespace OpenLegoBattles.GameStates
{
    internal class RuleTestState : IGameState
    {
        #region Constants
        private const Keys debugInfoKey = Keys.F5;

        private const Keys unseeKey = Keys.Space;

        private const float cameraSpeed = 24 * 15;
        #endregion

        #region Dependencies
        private readonly RomContentManager romContentManager;
        private readonly ContentManager contentManager;
        private readonly TileGraphicsManager tileGraphicsManager;
        private readonly GameWindow window;
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Fields
        private SpriteFont debugFont;

        private MouseState lastMouseState;

        private KeyboardState lastKeyboardState;

        private bool debugInfoVisible = false;

        private bool treePlacementState;

        private int currentRadius = 3;
        #endregion

        #region Properties
        public bool UpdateUnder => false;

        public bool DrawUnder => false;

        public BattleScene Scene { get; private set; }

        public SceneRenderManager RenderManager { get; private set; }
        #endregion

        #region Constructors
        public RuleTestState(RomContentManager romContentManager, ContentManager contentManager, TileGraphicsManager tileGraphicsManager, GameWindow window, GraphicsDevice graphicsDevice)
        {
            this.romContentManager = romContentManager;
            this.contentManager = contentManager;
            this.tileGraphicsManager = tileGraphicsManager;
            this.window = window;
            this.graphicsDevice = graphicsDevice;
        }
        #endregion

        #region Load Functions
        public void Load()
        {
            debugFont = contentManager.Load<SpriteFont>("Fonts/UnpackerFont");

            TilemapData tilemap = romContentManager.Load<TilemapData>("mp01");
            Scene = new(tilemap);

            RenderManager = SceneRenderManager.CreateFromScene(Scene, tileGraphicsManager, window, graphicsDevice);
            RenderManager.TileGraphicsManager.LoadDataForMap(tilemap);
            RenderManager.CalculateAllFogTiles();

            RenderManager.Camera.CentrePosition = (RenderManager.TileGraphicsManager.Tilesheet.TileSize * Scene.Tilemap.Size).ToVector2() / 2.0f;

            window.AllowUserResizing = true;
        }

        public void Unload()
        {
            contentManager.UnloadAsset("Fonts/UnpackerFont");
            romContentManager.Unload();
            RenderManager.Unload();
        }
        #endregion

        #region Update Functions
        public void Update(GameTime gameTime)
        {
            // Get the input data.
            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();
            Point mouseWorldPosition = RenderManager.Camera.ToWorldPosition(currentMouseState.Position);
            Point currentMouseTilePosition = Vector2.Floor(mouseWorldPosition.ToVector2() / RenderManager.TileGraphicsManager.Tilesheet.TileSize.ToVector2()).ToPoint();

            // Update the camera.
            updateCameraInput(currentMouseState, currentKeyboardState, gameTime);

            // Keep track of any changes.
            bool isFogDirty = false;
            bool areTreesDirty = false;

            // Handle changing the radius.
            if (currentKeyboardState.IsKeyUp(Keys.LeftShift))
            {
                if (currentMouseState.ScrollWheelValue > lastMouseState.ScrollWheelValue)
                    currentRadius = Math.Min(currentRadius + 1, 15);
                else if (currentMouseState.ScrollWheelValue < lastMouseState.ScrollWheelValue)
                    currentRadius = Math.Max(currentRadius - 1, 0);
            }

            // Handle dispelling fog.
            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                Scene.VisibilityView.RevealCircle(currentMouseTilePosition.X, currentMouseTilePosition.Y, currentRadius);
                isFogDirty = true;
            }

            // Handle unseeing revealed area.
            if (currentKeyboardState.IsKeyDown(unseeKey) && lastKeyboardState.IsKeyUp(unseeKey))
            {
                Scene.VisibilityView.ClearCurrentVisibility();
                isFogDirty = true;
            }

            // Handle placing trees.
            if (currentMouseState.RightButton == ButtonState.Pressed)
            {
                // If the right mouse button was started on this frame, get the tree status at the position. The inverted value is what should be placed.
                if (lastMouseState.RightButton == ButtonState.Released)
                    treePlacementState = !Scene.Tilemap.HasTreeAtPosition(currentMouseTilePosition.X, currentMouseTilePosition.Y);

                // Place the trees in a radius around the mouse position.
                foreach (Point position in Spatial2dUtils.TilesInRadiusAround(currentMouseTilePosition, currentRadius))
                {
                    // If the position is out of range or not grass, do nothing.
                    if (!Scene.Tilemap.IsPositionInRange(position) || Scene.Tilemap[position].TileType != TileType.Grass)
                        continue;

                    // Place the tree.
                    Scene.Tilemap.SetTreeAtPosition(position, treePlacementState);
                    areTreesDirty = true;
                }
            }

            // Handle toggling debug display.
            if (currentKeyboardState.IsKeyDown(debugInfoKey) && lastKeyboardState.IsKeyUp(debugInfoKey))
                debugInfoVisible = !debugInfoVisible;

            // Recalculate graphics if they were changed.
            if (isFogDirty)
                RenderManager.CalculateAllFogTiles();
            if (areTreesDirty)
                RenderManager.CalculateAllTreeTiles();

            // Set the last input states.
            lastMouseState = currentMouseState;
            lastKeyboardState = currentKeyboardState;
        }

        private void updateCameraInput(MouseState currentMouseState, KeyboardState currentKeyboardState, GameTime gameTime)
        {
            // TODO: Replace with proper input system.

            // Handle wasd movement.
            Vector2 movementDirection = Vector2.Zero;
            if (currentKeyboardState.IsKeyDown(Keys.A))
                movementDirection.X = -1;
            else if (currentKeyboardState.IsKeyDown(Keys.D))
                movementDirection.X = 1;

            if (currentKeyboardState.IsKeyDown(Keys.W))
                movementDirection.Y = -1;
            else if (currentKeyboardState.IsKeyDown(Keys.S))
                movementDirection.Y = 1;

            // Handle zooming.
            if (currentKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                float scrollDelta = (currentMouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue) / 400f;
                if (scrollDelta != 0)
                    RenderManager.Camera.Scale += scrollDelta;
            }

            // Apply movement.
            if (movementDirection != Vector2.Zero)
            {
                movementDirection.Normalize();
                RenderManager.Camera.TopLeftPosition += movementDirection * (float)gameTime.ElapsedGameTime.TotalSeconds * cameraSpeed;
            }
        }
        #endregion

        #region Draw Functions
        public void Draw(GameTime gameTime)
        {
            graphicsDevice.Clear(Color.Black);

            // Draw the scene.
            RenderManager.Draw();

            // If the debug information should be shown, do so. Start the spritebatch manually to avoid the text being transformed by the camera matrix.
            if (debugInfoVisible)
            {
                RenderManager.Camera.SpriteBatch.Begin();
                RenderManager.Camera.SpriteBatch.DrawString(debugFont, $"{debugInfoKey} to hide debug info\nCurrent radius: {currentRadius} (mouse wheel to change)\nLeft click to reveal area\nRight click to toggle trees in area\n{unseeKey} to unsee revealed area", Vector2.Zero, Color.Black);
                RenderManager.Camera.SpriteBatch.End();
            }
        }
        #endregion
    }
}
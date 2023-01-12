using LiruGameHelper.Reflection;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenLegoBattles.GameStates
{
    /// <summary> Manages, creates, and destroys <see cref="IGameState"/>s using a stack-like collection. </summary>
    public class GameStateManager
    {
        #region Fields
        /// <summary> The list of all currently loaded <see cref="IGameState"/>s. Renders and updates going from top to bottom. </summary>
        private readonly LinkedList<IGameState> gameStates;

        private readonly IServiceProvider services;
        #endregion

        #region Properties
        public IGameState Current => gameStates.Count > 0 ? gameStates.Last.Value : null;
        #endregion

        #region Constructors
        public GameStateManager(GameServiceContainer services)
        {
            // Set the service collection.
            this.services = services ?? throw new ArgumentNullException(nameof(services));

            // Initialise the collections.
            gameStates = new LinkedList<IGameState>();
        }
        #endregion

        #region GameState Functions
        /// <summary> Completely removes the given <paramref name="gameState"/> from the manager. </summary>
        /// <param name="gameState"> The <see cref="IGameState"/> to remove. </param>
        public void Remove(IGameState gameState)
        {
            // Find the node of the given gamestate within the list.
            LinkedListNode<IGameState> gameStateNode = gameStates.Find(gameState);

            // If the node is null, the gamestate does not exist, so throw an exception.
            if (gameStateNode is null) throw new Exception("Given gamestate does not exist within the manager, so cannot be removed.");

            // Remove the gamestate.
            gameStates.Remove(gameStateNode);
        }

        /// <summary> Pushes the given <paramref name="gameState"/> to the top, making it the current <see cref="IGameState"/>. </summary>
        /// <param name="gameState"> The <see cref="IGameState"/> to push to the top. </param>
        public void PushToTop(IGameState gameState)
        {
            // Find the node of the given gamestate within the list.
            LinkedListNode<IGameState> gameStateNode = gameStates.Find(gameState);

            // If the node is null, the gamestate does not exist, so throw an exception.
            if (gameStateNode is null) throw new Exception("Given gamestate does not exist within the manager, so cannot be pushed to the top.");

            // Remove the gamestate, then re-add it at the top.
            gameStates.Remove(gameStateNode);
            gameStates.AddLast(gameStateNode);
        }

        public IGameState CreateAndAddGameState(Type type, params object[] inputs)
        {
            // If the type does not use IGameState at all, throw an exception.
            if (type.GetInterface(typeof(IGameState).Name) is null) throw new Exception($"{type.Name} does not implement the {typeof(IGameState).Name} interface.");

            ConstructorInfo constructorInfo = type.GetOnlyConstructor();

            // Create the gamestate, add it to the list, and return it.
#if DEBUG
            IGameState gameState;

            try { gameState = (IGameState)(Dependencies.CreateObjectWithDependencies(constructorInfo, services, inputs)); }
            catch (TargetInvocationException targetException) { throw targetException.InnerException; }
            catch (Exception) { throw; }
#else
            IGameState gameState = (IGameState)(Dependencies.CreateObjectWithDependencies(constructorInfo, services, inputs));
#endif
            gameStates.AddLast(gameState);
            return gameState;
        }

        public T CreateAndAddGameState<T>(params object[] inputs) where T : IGameState => (T)CreateAndAddGameState(typeof(T), inputs);
        #endregion

        #region Update Functions
        public void Update(GameTime gameTime)
        {
            // If there are no gamestates, do nothing.
            if (gameStates.Count == 0) return;

            // Traverse the gamestates backwards until the end is reached, or a gamestate that does not update under is found.
            LinkedListNode<IGameState> currentNode = gameStates.Last;
            do
            {
                currentNode.Value.Update(gameTime);
                currentNode = currentNode.Previous;
            } while (currentNode != null && currentNode.Next.Value.UpdateUnder);
        }
        #endregion

        #region Draw Functions
        public void Draw(GameTime gameTime)
        {
            // If there are no gamestates, do nothing.
            if (gameStates.Count == 0) return;

            // Traverse the gamestates backwards until the end is reached, or a gamestate that does not draw under is found.
            LinkedListNode<IGameState> currentNode = gameStates.Last;
            while (currentNode.Value.DrawUnder && currentNode.Previous != null) currentNode = currentNode.Previous;

            // Start drawing from the last node that should be drawn, and travel upwards. This ensures that a proper draw order is achieved.
            do
            {
                currentNode.Value.Draw(gameTime);
                currentNode = currentNode.Next;
            }
            while (currentNode != null);
        }
        #endregion
    }
}

using System;

namespace OpenLegoBattles.RomContent
{
    /// <summary>
    /// Handles loading content of a certain type.
    /// </summary>
    public interface IRomContentLoader
    {
        #region Properties
        /// <summary>
        /// The type of content that is loaded by this loader.
        /// </summary>
        Type ContentType { get; }
        #endregion

        #region Load Functions
        /// <summary>
        /// Loads and returns the content at the given path.
        /// </summary>
        /// <param name="path"> The path of the content. </param>
        /// <returns> The loaded content. </returns>
        object LoadObjectFromPath(string path);

        void Unload();
        #endregion
    }
}
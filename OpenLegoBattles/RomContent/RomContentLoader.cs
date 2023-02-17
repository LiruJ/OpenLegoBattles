using System;

namespace OpenLegoBattles.RomContent
{
    /// <inheritdoc cref="IRomContentLoader"/>
    /// <typeparam name="T"> The type of content this loader loads. </typeparam>
    public abstract class RomContentLoader<T> : IRomContentLoader where T : class
    {
        #region Dependencies
        protected readonly RomContentManager romContentManager;
        #endregion

        #region Properties
        /// <inheritdoc cref="IRomContentLoader.ContentType"/>
        public Type ContentType => typeof(T);
        #endregion

        #region Constructors
        public RomContentLoader(RomContentManager romContentManager)
        {
            this.romContentManager = romContentManager;
        }
        #endregion

        #region Load Functions
        /// <inheritdoc cref="IRomContentLoader.LoadObjectFromPath(string)"/>
        public object LoadObjectFromPath(string path) => LoadFromPath(path);

        /// <inheritdoc cref="IRomContentLoader.LoadObjectFromPath(string)"/>
        public virtual T LoadFromPath(string path) => null;
        #endregion
    }
}

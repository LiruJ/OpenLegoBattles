using System.IO;

namespace GlobalShared.Content
{
    /// <summary>
    /// The util used for content files.
    /// </summary>
    public static class ContentFileUtil
    {
        #region Extensions
        /// <summary>
        /// The extension used for raw binary files.
        /// </summary>
        public const string BinaryExtension = "bin";

        /// <summary>
        /// The extension used for temporary files.
        /// </summary>
        public const string TemporaryExtension = "tmp";

        /// <summary>
        /// The extension used for sprite image files.
        /// </summary>
        public const string SpriteExtension = "png";

        /// <summary>
        /// The extension used for tileset files.
        /// </summary>
        public const string TilesetExtension = "tst";

        /// <summary>
        /// The extension used for tilemap files.
        /// </summary>
        public const string TilemapExtension = "map";
        #endregion

        #region Directories
        /// <summary>
        /// The directory used to store sprites, relative to the "Content/BaseGame" folder.
        /// </summary>
        public const string SpriteDirectoryName = "Sprites";

        /// <summary>
        /// The directory used to store tilesets, relative to the "Content/BaseGame" folder.
        /// </summary>
        public const string TilesetDirectoryName = "Tilesets";

        /// <summary>
        /// The directory used to store tilemaps, relative to the "Content/BaseGame" folder.
        /// </summary>
        public const string TilemapDirectoryName = "Maps";
        #endregion

        #region Directory Functions
        /// <summary>
        /// Creates a full file path for a content file.
        /// </summary>
        /// <param name="rootDirectory"> The main root directory relative to the content folder. e.g. "BaseGame" </param>
        /// <param name="subDirectory"> The directory relative to the root directory. e.g. "Sprites" </param>
        /// <param name="filename"> The filename. </param>
        /// <param name="extension"> The extension of the filename. </param>
        /// <returns> The full path. </returns>
        public static string CreateFullFilePath(string rootDirectory, string subDirectory, string filename, string extension) => Path.GetFullPath(Path.ChangeExtension(Path.Combine(rootDirectory, subDirectory, filename), extension));

        /// <summary>
        /// Creates a full file path for a content file.
        /// </summary>
        /// <param name="rootDirectory"> The main root directory relative to the content folder. e.g. "BaseGame" </param>
        /// <param name="relativeFilePath"> The directory and filename relative to the root directory. e.g. "Sprites/KingTileset" </param>
        /// <param name="extension"> The extension of the filename. </param>
        /// <returns> The full path. </returns>
        public static string CreateFullFilePath(string rootDirectory, string relativeFilePath, string extension) => Path.GetFullPath(Path.ChangeExtension(Path.Combine(rootDirectory, relativeFilePath), extension));
        #endregion
    }
}

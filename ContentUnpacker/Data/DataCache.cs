using ContentUnpacker.Decompressors;
using ContentUnpacker.Loaders;
using GlobalShared.Content;

namespace ContentUnpacker.Data
{
    /// <summary>
    /// Handles loading and storing any data that is needed multiple times but requires special processing before it can be used; such as palettes.
    /// </summary>
    internal class DataCache
    {
        #region Dependencies
        private readonly RomUnpacker unpacker;
        #endregion

        #region Fields
        private readonly Dictionary<string, ContentLoader> cachedDataByFilePath = new();
        #endregion

        #region Constructors
        public DataCache(RomUnpacker unpacker)
        {
            this.unpacker = unpacker;
        }
        #endregion

        #region Get Functions
        public T GetOrLoadData<T>(string filename) where T : ContentLoader
        {
            // Convert the file path so that it's full. This ensures the same path always gives the same loader.
            string filePath = Path.GetFullPath(Path.ChangeExtension(Path.Combine(RomUnpacker.WorkingFolderName, DecompressionStage.OutputFolderPath, filename), ContentFileUtil.BinaryExtension));

            // If there is already a loader for the filepath, return it.
            if (cachedDataByFilePath.TryGetValue(filePath, out var data))
                return (T)data;
            // Otherwise; the data needs to be loaded.
            else
            {
                // Get a reader for the file.
                BinaryReader reader = unpacker.GetReaderForFilePath(filePath, out bool manualClose);

                // Create the loader and load.
                T loader = (T?)Activator.CreateInstance(typeof(T), unpacker) ?? throw new ArgumentException("Given type could not create a loader.", nameof(T));
                loader.Load(reader);

                // If the reader needs to be manually closed, do so.
                if (manualClose) reader.Close();
                else unpacker.ReturnReader(reader);

                // Add the loader to the cache.
                cachedDataByFilePath.Add(filePath, loader);

                // Return the loader.
                return loader;
            }
        }
        #endregion
    }
}

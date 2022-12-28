using System.Collections;

namespace ContentUnpacker.Tilemaps
{
    internal class IndexRemapper : IEnumerable<ushort>
    {
        #region Fields
        private readonly Dictionary<ushort, ushort> originalToRemapped = new();

        private readonly List<ushort> remappedToOriginal = new();
        #endregion

        #region Properties
        /// <summary>
        /// The total number of indices held by this collection.
        /// </summary>
        public ushort Count => (ushort)remappedToOriginal.Count;
        #endregion

        #region Get Functions
        public ushort GetRemappedBlockIndex(ushort originalIndex) => originalToRemapped[originalIndex];

        public ushort GetOriginalBlockIndex(ushort remappedIndex) => remappedToOriginal[remappedIndex];
        #endregion

        #region Add Functions
        /// <summary>
        /// Adds the given original index to the collection.
        /// </summary>
        /// <param name="originalIndex"> The original index to add. </param>
        public void TryAdd(ushort originalIndex)
        {
            if (originalToRemapped.TryAdd(originalIndex, Count))
                remappedToOriginal.Add(originalIndex);
        }

        /// <summary>
        /// Adds the given collection to the collection.
        /// </summary>
        /// <param name="collection"> The collection of indices to add. </param>
        public void AddCollection(IEnumerable<ushort> collection)
        {
            foreach (ushort originalIndex in collection)
                TryAdd(originalIndex);
        }
        #endregion

        #region Enumeration Functions
        public IEnumerator<ushort> GetEnumerator() => remappedToOriginal.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => remappedToOriginal.GetEnumerator();
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.NDSFS
{
    [DebuggerDisplay("{Name,nq} ({ID,nq,h})")]
    internal class NDSFile
    {
        #region Fields
        private string? path = null;
        #endregion

        #region Properties
        /// <summary>
        /// The relative path of this file.
        /// </summary>
        public string? Path
        {
            get => path;
            set
            {
                // Ensure the state is correct.
                if (value == null || path != null)
                    throw new Exception("Path has already been set or is being set to invalid value.");

                // Set the path.
                path = value;
            }
        }

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The ID of the file.
        /// </summary>
        public ushort ID { get; }

        /// <summary>
        /// The offset of this file's data in the rom file.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The size of this file's data.
        /// </summary>
        public int Size { get; }
        #endregion

        #region Constructors
        public NDSFile(string name, ushort id, int offset, int size)
        {
            Name = name;
            ID = id;
            Offset = offset;
            Size = size;
        }
        #endregion
    }
}

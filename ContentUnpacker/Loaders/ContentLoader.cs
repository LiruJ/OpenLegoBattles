using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.Loaders
{
    internal class ContentLoader
    {
        #region Dependencies
        protected readonly RomUnpacker romUnpacker;
        #endregion

        #region Constructors
        public ContentLoader(RomUnpacker romUnpacker)
        {
            this.romUnpacker = romUnpacker;
        }
        #endregion

        #region Load Functions
        public virtual void Load(BinaryReader reader) { }
        #endregion
    }
}

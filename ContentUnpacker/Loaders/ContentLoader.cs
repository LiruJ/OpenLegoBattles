using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.Loaders
{
    internal class ContentLoader
    {
        #region Load Functions
        public virtual void Load(BinaryReader reader) { }
        #endregion
    }
}

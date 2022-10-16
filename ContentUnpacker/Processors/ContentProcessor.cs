using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentUnpacker.Processors
{
    internal abstract class ContentProcessor
    {
        #region Process Functions
        public virtual async Task ProcessAsync(string inputPath, string outputRootPath)
        {

        }
        #endregion
    }
}

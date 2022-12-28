using ContentUnpacker.Utils;
using GlobalShared.Content;
using System.Xml;

namespace ContentUnpacker.Processors
{
    internal abstract class ContentProcessor
    {
        #region Dependencies
        protected readonly RomUnpacker romUnpacker;
        #endregion

        #region Fields
        protected readonly BinaryReader reader;

        protected readonly XmlNode contentNode;
        
        protected readonly string outputFilePath;
        #endregion

        #region Constructors
        public ContentProcessor(RomUnpacker romUnpacker, BinaryReader reader, XmlNode contentNode)
        {
            // Set the dependencies.
            this.romUnpacker = romUnpacker;
            this.reader = reader;
            this.contentNode = contentNode;
            contentNode.TryGetOutputPathAttribute(out outputFilePath);
            outputFilePath = Path.Combine(romUnpacker.Options.OutputFolder, outputFilePath);
        }
        #endregion

        #region File Functions
        protected void createOutputDirectory() => Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

        protected BinaryWriter createOutputWriter(string extension = ContentFileUtil.BinaryExtension) => new(File.OpenWrite(Path.ChangeExtension(outputFilePath, extension)));
        #endregion

        #region Process Functions
        public virtual void Process() { }
        #endregion
    }
}
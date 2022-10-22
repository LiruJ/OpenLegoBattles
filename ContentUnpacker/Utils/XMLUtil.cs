using System.Globalization;
using System.Xml;

namespace ContentUnpacker.Utils
{
    internal static class XMLUtil
    {
        #region XML Constants
        /// <summary>
        /// The name of the xml attribute responsible for specifying the offset within the file.
        /// </summary>
        private const string offsetAttributeName = "Offset";

        /// <summary>
        /// The name of the xml attribute responsible for specifying the output file.
        /// </summary>
        private const string outputAttributeName = "Output";
        #endregion

        #region Overloads
        public static bool TryParseOffsetAttribute(this XmlNode node, out int offset)
        {
            // Parse the offset.
            XmlAttribute? offsetAttribute = node.Attributes?[offsetAttributeName];
            offset = -1;
            return offsetAttribute != null && int.TryParse(offsetAttribute.Value, NumberStyles.HexNumber, null, out offset);
        }

        public static bool TryGetOutputPathAttribute(this XmlNode node, out string text) => node.TryGetTextAttribute(outputAttributeName, out text);

        public static bool TryGetTextAttribute(this XmlNode node, string attributeName, out string text)
        {
            XmlAttribute? textAttribute = node.Attributes?[attributeName];
            if (string.IsNullOrWhiteSpace(textAttribute?.Value))
            {
                text = string.Empty;
                return false;
            }
            else
            {
                text = textAttribute.Value;
                return true;
            }
        }

        public static bool TryGetBooleanAttribute(this XmlNode node, string attributeName, out bool result)
        {
            result = false;
            XmlAttribute? attribute = node.Attributes?[attributeName];
            return attribute == null || bool.TryParse(attribute.Value, out result);
        }
        #endregion
    }
}

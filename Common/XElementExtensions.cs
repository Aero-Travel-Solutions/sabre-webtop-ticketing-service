using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SabreWebtopTicketingService.Common
{
    public static class XElementExtensions
    {
        public static XAttribute GetAttribute(this XElement source, string localName)
        {
            return source.Attributes().SingleOrDefault(e => e.Name.LocalName == localName);
        }

        public static IEnumerable<XElement> GetElements(this XElement source, string localName)
        {
            return source.Elements().Where(e => e.Name.LocalName == localName);
        }

        public static XElement GetFirstElement(this XElement source, string localName)
        {
            return source.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
        }

        public static string Serialize(object dataToSerialize)
        {
            if (dataToSerialize == null) return null;

            using (StringWriter stringwriter = new StringWriter())
            {
                var serializer = new XmlSerializer(dataToSerialize.GetType());
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
        }

        public static T Deserialize<T>(string xmlText)
        {
            if (string.IsNullOrWhiteSpace(xmlText)) return default;

            using (StringReader stringReader = new StringReader(xmlText))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stringReader);
            }
        }
    }
}

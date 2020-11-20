using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SabreWebtopTicketingService.Common
{
    public static class ObjectExtentions
    {

        public static XElement ConvertObjectToXElement<T>(object obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (TextWriter streamWriter = new StreamWriter(memoryStream))
                {
                    MethodInfo method = typeof(XmlSerializer).GetMethod("set_Mode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    method.Invoke(null, new object[] { 1 });
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    xmlSerializer.Serialize(streamWriter, obj);
                    return XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray()));
                }
            }
        }

        public static List<XElement> ConvertListObjectToXElement<T>(List<T> objs)
        {
            List<XElement> returnlist = new List<XElement>();
            foreach (var obj in objs)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (TextWriter streamWriter = new StreamWriter(memoryStream))
                    {
                        MethodInfo method = typeof(XmlSerializer).GetMethod("set_Mode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        method.Invoke(null, new object[] { 1 });
                        var xmlSerializer = new XmlSerializer(typeof(T));
                        xmlSerializer.Serialize(streamWriter, obj);
                        returnlist.Add(XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray())));
                    }
                }
            }

            return returnlist;
        }

        public static XmlDocument ConvertObjectToXMLDocument<T>(object obj)
        {
            var xmlDocument = new XmlDocument();
            XElement xelement = ConvertObjectToXElement<T>(obj);
            xelement.
                Attributes().
                Where(w => w.IsNamespaceDeclaration).
                Remove();
            xmlDocument.Load(xelement.ToString());

            return xmlDocument;
        }



        public static List<XmlDocument> ConvertListObjectToXMLDocument<T>(List<T> objs)
        {
            List<XmlDocument> returnlist = new List<XmlDocument>();
            foreach (var obj in objs)
            {
                var xmlDocument = new XmlDocument();
                XElement xelement = ConvertObjectToXElement<T>(obj);
                xelement.
                    Attributes().
                    Where(w => w.IsNamespaceDeclaration).
                    Remove();
                xmlDocument.Load(xelement.ToString());
                returnlist.Add(xmlDocument);
            }

            return returnlist;
        }

        public static T ConvertXmlStringtoObject<T>(string xmlString)
        {
            T classObject;
            MethodInfo method = typeof(XmlSerializer).GetMethod("set_Mode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { 1 });
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader stringReader = new StringReader(xmlString))
            {
                classObject = (T)xmlSerializer.Deserialize(stringReader);
            }
            return classObject;
        }
    }
}

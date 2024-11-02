using System.Text;
using System.Xml.Serialization;

namespace MediathekArr.Utilities
{
    public class XmlHelper
    {
        public static string SerializeToXmlWithSabnzbdNamespace<T>(T obj)
        {
            // Use StringBuilder to capture XML
            var xmlSerializer = new XmlSerializer(typeof(T));
            var stringBuilder = new StringBuilder();

            using (var stringWriter = new StringWriter(stringBuilder))
            {
                // Serialize the object to XML
                xmlSerializer.Serialize(stringWriter, obj);
            }

            // Perform a string replacement to correct the namespace prefix issue
            var xmlOutput = stringBuilder.ToString();
            xmlOutput = xmlOutput.Replace("newznab_x003A_", "newznab:");

            return xmlOutput;
        }
    }
}

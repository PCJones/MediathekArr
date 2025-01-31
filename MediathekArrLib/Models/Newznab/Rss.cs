using System.Xml;
using System.Xml.Serialization;

namespace MediathekArr.Models.Newznab;

[XmlRoot("rss")]
public class Rss
{
    [XmlAttribute("version")]
    public string Version { get; set; } = "2.0";

    [XmlElement("channel")]
    public Channel Channel { get; set; }

    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Xmlns { get; } = new XmlSerializerNamespaces(
    [
        new XmlQualifiedName("newznab", "http://www.newznab.com/DTD/2010/feeds/attributes/")
    ]);
}

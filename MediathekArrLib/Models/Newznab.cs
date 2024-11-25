using System.Xml;
using System.Xml.Serialization;

namespace MediathekArrLib.Models
{
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

    public class Channel
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("newznab:response", Namespace = "http://www.newznab.com/DTD/2010/feeds/attributes/")]
        public NewznabResponse Response { get; set; }

        [XmlElement("item")]
        public List<Item> Items { get; set; } = [];
    }

    public class NewznabResponse
    {
        [XmlAttribute("offset")]
        public int Offset { get; set; }

        [XmlAttribute("total")]
        public int Total { get; set; }
    }

    public class Item
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("guid")]
        public Guid Guid { get; set; }

        [XmlElement("link")]
        public string Link { get; set; }

        [XmlElement("comments")]
        public string Comments { get; set; }

        [XmlElement("pubDate")]
        public string PubDate { get; set; }

        [XmlElement("category")]
        public string Category { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("enclosure")]
        public Enclosure Enclosure { get; set; }

        [XmlElement("newznab:attr", Namespace = "http://www.newznab.com/DTD/2010/feeds/attributes/")]
        public List<NewznabAttribute> Attributes { get; set; } = [];
    }

    public class Enclosure
    {
        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("length")]
        public long Length { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }

    public class NewznabAttribute
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    public class Guid
    {
        [XmlAttribute("isPermaLink")]
        public bool IsPermaLink { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}

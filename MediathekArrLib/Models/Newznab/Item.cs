using System.Xml;
using System.Xml.Serialization;

namespace MediathekArr.Models.Newznab;

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
    public List<Attribute> Attributes { get; set; } = [];
}

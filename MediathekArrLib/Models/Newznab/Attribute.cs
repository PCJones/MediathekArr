using System.Xml;
using System.Xml.Serialization;

namespace MediathekArr.Models.Newznab;

public class Attribute
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("value")]
    public string Value { get; set; }
}

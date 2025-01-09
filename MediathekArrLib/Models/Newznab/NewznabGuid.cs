using System.Xml;
using System.Xml.Serialization;

namespace MediathekArr.Models.Newznab;

[XmlRoot("Guid")]
public class NewznabGuid
{
    [XmlAttribute("isPermaLink")]
    public bool IsPermaLink { get; set; }

    [XmlText]
    public string Value { get; set; }
}

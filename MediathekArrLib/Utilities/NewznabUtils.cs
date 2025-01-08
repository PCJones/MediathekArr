using MediathekArrLib.Models.Newznab;
using System.Xml.Serialization;

namespace MediathekArrLib.Utilities;
public static class NewznabUtils
{
    public static List<Models.Newznab.Attribute> GenerateAttributes(string? season, string? episode, string[] categoryValues)
    {
        var attributes = new List<Models.Newznab.Attribute>();

        foreach (var categoryValue in categoryValues)
        {
            attributes.Add(new Models.Newznab.Attribute { Name = "category", Value = categoryValue });
        }

        if (season != null)
        {
            attributes.Add(new Models.Newznab.Attribute { Name = "season", Value = season });
        }

        if (episode != null)
        {
            attributes.Add(new Models.Newznab.Attribute { Name = "episode", Value = episode });
        }

        return attributes;
    }
    public static string SerializeRss(Rss rss)
    {
        var serializer = new XmlSerializer(typeof(Rss));

        // Define the namespaces and add the newznab namespace
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("newznab", "http://www.newznab.com/DTD/2010/feeds/attributes/");

        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, rss, namespaces);

        // TODO quick fix
        string result = stringWriter.ToString();
        result = result.Replace(":newznab_x003A_", ":");

        return result;
    }

    public static Rss GetEmptyRssResult()
    {
        return new Rss
        {
            Channel = new Channel
            {
                Title = "MediathekArr",
                Description = "MediathekArr API results",
                Response = new Response
                {
                    Offset = 0,
                    Total = 0
                },
                Items = []
            }
        };
    }
}
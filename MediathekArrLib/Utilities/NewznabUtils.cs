using MediathekArrLib.Models;
using MediathekArrLib.Models.Newznab;
using MediathekArrLib.Models.Rulesets;
using System.Xml.Serialization;
using Attribute = MediathekArrLib.Models.Newznab.Attribute;

namespace MediathekArrLib.Utilities;
public static class NewznabUtils
{
    public static List<Attribute> GenerateAttributes(ApiResultItem item, string? season, string? episode, string[] categoryValues, EpisodeType episodeType, DateTime? airDate = null)
    {
        var attributes = new List<Attribute>();

        foreach (var categoryValue in categoryValues)
        {
            attributes.Add(new Attribute { Name = "category", Value = categoryValue });
        }

        if (season != null)
        {
            attributes.Add(new Attribute { Name = "season", Value = season });
        }

        if (episode != null)
        {
            attributes.Add(new Attribute { Name = "episode", Value = episode });
        }

        if (airDate != null)
        {
            attributes.Add(new Attribute { Name = "tvairdate", Value = airDate.Value.ToString("yyyy-MM-dd") });
        }

        if (string.IsNullOrEmpty(item.UrlSubtitle))
        {
            attributes.Add(new Attribute { Name = "subs", Value = "German" });
        }

        attributes.Add(new Attribute { Name = "seriestype", Value = episodeType.ToString() }); // this is no official newznab attribute

        return attributes;
    }

    public static List<Attribute> GenerateAttributes(MatchedEpisodeInfo matchedEpisodeInfo, string[] categoryValues, EpisodeType episodeType)
    {
        return GenerateAttributes(matchedEpisodeInfo.Item, matchedEpisodeInfo.Episode.PaddedSeason, matchedEpisodeInfo.Episode.PaddedEpisode, categoryValues, episodeType, matchedEpisodeInfo.Episode.Aired); 
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
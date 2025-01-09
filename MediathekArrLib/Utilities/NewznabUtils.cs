using MediathekArr.Models;
using MediathekArr.Models.Newznab;
using MediathekArr.Models.Rulesets;
using System.Xml.Serialization;
using Attribute = MediathekArr.Models.Newznab.Attribute;

namespace MediathekArr.Utilities;
public static class NewznabUtils
{
    public static class Application
    {
        public const string Nzb = "application/x-nzb";
    }

    public static List<Attribute> GenerateAttributes(ApiResultItem item, string? season, string? episode, string[] categoryValues, EpisodeType episodeType)
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

        if (string.IsNullOrEmpty(item.UrlSubtitle))
        {
            attributes.Add(new Attribute { Name = "subs", Value = "German" });
        }

        attributes.Add(new Attribute { Name = "seriestype", Value = episodeType.ToString() }); // this is no official newznab attribute

        return attributes;
    }

    public static List<Attribute> GenerateAttributes(MatchedEpisodeInfo matchedEpisodeInfo, string[] categoryValues, EpisodeType episodeType)
    {
        return GenerateAttributes(matchedEpisodeInfo.Item, matchedEpisodeInfo.Episode.PaddedSeason, matchedEpisodeInfo.Episode.PaddedEpisode, categoryValues, episodeType);
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

    public static Rss GetEmptyRssResult() => Factories.RssFactory.Empty;
}
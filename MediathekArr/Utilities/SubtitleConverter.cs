using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MediathekArr.Utilities;

public partial class SubtitleConverter
{
    public static string? ConvertXmlToSrt(string xmlContent)
    {
        try
        {
            // Extract subtitle blocks
            var subtitleMatches = XMLSubtitleLineRegex().Matches(xmlContent);

            var srtBuilder = new StringBuilder();
            int index = 1;

            if (subtitleMatches.Count == 0)
            {
                return null;
            }

            foreach (Match match in subtitleMatches)
            {
                if (match.Groups.Count != 4) continue;

                var startTime = match.Groups[1].Value.Replace('.', ',');
                var endTime = match.Groups[2].Value.Replace('.', ',');
                var text = CleanText(match.Groups[3].Value);

                if (string.IsNullOrWhiteSpace(text)) continue;

                srtBuilder.AppendLine(index.ToString())
                          .AppendLine($"{startTime} --> {endTime}")
                          .AppendLine(text)
                          .AppendLine();
                index++;
            }

            return srtBuilder.ToString().Trim();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to convert XML subtitles to SRT format.", ex);
        }
    }

    private static string CleanText(string text)
    {
        // Remove unnecessary tags and decode special characters
        text = Regex.Replace(text, @"<tt:span[^>]*>", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"</tt:span>", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"<tt:br\s*/>", "\n", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<.*?>", string.Empty, RegexOptions.Singleline);

        // Decode special characters
        text = HttpUtility.HtmlDecode(text);

        // Normalize line breaks and trim whitespace
        text = Regex.Replace(text, @"\s*\n\s*", "\n").Trim();

        return text;
    }

    [GeneratedRegex(@"<tt:p.*?begin=""([^""]+)"" end=""([^""]+)"".*?>(.*?)</tt:p>", RegexOptions.Singleline)]
    private static partial Regex XMLSubtitleLineRegex();
}

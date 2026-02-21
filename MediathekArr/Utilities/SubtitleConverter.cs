using System.Text;
using System.Xml.Linq;

namespace MediathekArr.Utilities;

public class SubtitleConverter
{
    private static readonly XNamespace TtmlNamespace = "http://www.w3.org/ns/ttml";

    public static string? ConvertXmlToSrt(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var paragraphs = doc.Descendants(TtmlNamespace + "p")
            .Where(p => p.Attribute("begin") != null && p.Attribute("end") != null)
            .ToList();

        if (paragraphs.Count == 0)
        {
            return null;
        }

        var srtBuilder = new StringBuilder();
        var index = 1;

        foreach (var paragraph in paragraphs)
        {
            var startTime = paragraph.Attribute("begin")!.Value.Replace('.', ',');
            var endTime = paragraph.Attribute("end")!.Value.Replace('.', ',');
            var text = NormalizeText(ExtractText(paragraph));

            if (string.IsNullOrWhiteSpace(text)) continue;

            srtBuilder.AppendLine(index.ToString())
                      .AppendLine($"{startTime} --> {endTime}")
                      .AppendLine(text)
                      .AppendLine();
            index++;
        }

        return srtBuilder.ToString().Trim();
    }

    private static string ExtractText(XElement element)
    {
        var sb = new StringBuilder();

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText textNode:
                    sb.Append(textNode.Value);
                    break;
                case XElement child when child.Name.LocalName == "br":
                    sb.Append('\n');
                    break;
                case XElement child:
                    sb.Append(ExtractText(child));
                    break;
            }
        }

        return sb.ToString();
    }

    private static string NormalizeText(string text)
    {
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        return string.Join("\n", lines);
    }
}

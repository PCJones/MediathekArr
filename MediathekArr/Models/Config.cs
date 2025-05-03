namespace MediathekArrDownloader.Models;

public record Config
{
    public string IncompletePath { get; set; } = string.Empty;
    public string CompletePath { get; set; } = string.Empty;
    public string[] Categories { get; set; } = ["tv", "movies", "mediathek"];
}

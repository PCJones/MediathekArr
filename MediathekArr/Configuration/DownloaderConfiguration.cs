namespace MediathekArr.Configuration;

public record DownloaderConfiguration
{
    public string IncompletePath { get; set; } = string.Empty;
    public string CompletePath { get; set; } = string.Empty;
}
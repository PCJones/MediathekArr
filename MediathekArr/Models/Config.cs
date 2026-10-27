namespace MediathekArr.Models;

public record Config
{
    public string IncompletePath { get; set; } = string.Empty;
    public string CompletePath { get; set; } = string.Empty;
    public string[] Categories { get; set; } = ["tv", "movies", "mediathek"];
    public int MaxParallelDownloads { get; set; } = 2;
    public bool HideNoMatchResults { get; set; } = false;
    public bool HideUncertainMatchResults { get; set; } = false;
}

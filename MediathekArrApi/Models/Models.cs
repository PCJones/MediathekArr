namespace MediathekArr.Models;

public class SeriesApiResponse
{
    public string Status { get; set; }
    public SeriesApiData Data { get; set; }
}

public class SeriesApiData
{
    public string Token { get; set; }
    public SeriesData Data { get; set; }
}

public class SeriesData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> NameTranslations { get; set; }
    public List<Alias> Aliases { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? NextAired { get; set; }
    public DateTime? LastAired { get; set; }
    public List<EpisodeData> Episodes { get; set; }
}

public class Alias
{
    public string Name { get; set; }
    public string Language { get; set; }
}

public class EpisodeData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Aired { get; set; }
    public int Runtime { get; set; }
    public int SeasonNumber { get; set; }
    public int Number { get; set; }
}
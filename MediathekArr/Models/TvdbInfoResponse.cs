namespace MediathekArr.Models
{
    public record TvdbInfoResponse(string Status, TvdbData Data);

    public record TvdbData(int Id, string Name, string GermanName, List<TvdbAlias> Aliases, List<TvdbEpisode> Episodes)
    {
        /// <summary>
        /// Finds an episode by its air date.
        /// </summary>
        /// <param name="airDate">The air date to search for.</param>
        /// <returns>The TvdbEpisode if found, or null if not found.</returns>
        public TvdbEpisode? FindEpisodeByAirDate(DateTime airDate)
        {
            return Episodes?.FirstOrDefault(episode => episode.Aired?.Date == airDate.Date);
        }

        /// <summary>
        /// Finds episodes by their air month.
        /// </summary>
        /// <param name="year">The year of the episodes to search for.</param>
        /// <param name="month">The month of the episodes to search for.</param>
        /// <returns>A list of TvdbEpisode objects that aired in the specified year and month.</returns>
        public List<TvdbEpisode>? FindEpisodeByAirMonth(int year, int month)
        {
            return Episodes?
                .Where(episode => episode.Aired.HasValue &&
                                  episode.Aired.Value.Year == year &&
                                  episode.Aired.Value.Month == month)
                .ToList();
        }

        /// <summary>
        /// Finds all episodes from a given season.
        /// </summary>
        /// <param name="seasonNumber">The season number to search for.</param>
        /// <returns>A list of TvdbEpisode objects in the specified season, or an empty list if none are found.</returns>
        public List<TvdbEpisode> FindEpisodesBySeason(int seasonNumber)
        {
            return Episodes?.Where(episode => episode.SeasonNumber == seasonNumber).ToList() ?? new List<TvdbEpisode>();
        }

        /// <summary>
        /// Finds all episodes from a given season.
        /// </summary>
        /// <param name="seasonNumber">The season number to search for.</param>
        /// <returns>A list of TvdbEpisode objects in the specified season, or an empty list if none are found.</returns>
        public List<TvdbEpisode> FindEpisodesBySeason(string? seasonNumber)
        {
            if (int.TryParse(seasonNumber, out int parsedSeason))
            {
                return FindEpisodesBySeason(parsedSeason);
            }

            return new List<TvdbEpisode>();
        }

        /// <summary>
        /// Finds a specific episode by season and episode number.
        /// </summary>
        /// <param name="seasonNumber">The season number of the episode.</param>
        /// <param name="episodeNumber">The episode number within the season.</param>
        /// <returns>The TvdbEpisode if found, or null if not found.</returns>
        public TvdbEpisode? FindEpisodeBySeasonAndNumber(int seasonNumber, int episodeNumber)
        {
            return Episodes?.FirstOrDefault(episode =>
                episode.SeasonNumber == seasonNumber && episode.EpisodeNumber == episodeNumber);
        }

        /// <summary>
        /// Finds a specific episode by season and episode number.
        /// </summary>
        /// <param name="seasonNumber">The season number of the episode.</param>
        /// <param name="episodeNumber">The episode number within the season.</param>
        /// <returns>The TvdbEpisode if found, or null if not found.</returns>
        public TvdbEpisode? FindEpisodeBySeasonAndNumber(string? seasonNumber, string? episodeNumber)
        {
            if (int.TryParse(seasonNumber, out int parsedSeason) &&
                int.TryParse(episodeNumber, out int parsedEpisode))
            {
                return FindEpisodeBySeasonAndNumber(parsedSeason, parsedEpisode);
            }

            return null;
        }
    }

    public record TvdbEpisode(string Name, DateTime? Aired, int SeasonNumber, int EpisodeNumber);

    public record TvdbAlias(string Language, string Name);
}

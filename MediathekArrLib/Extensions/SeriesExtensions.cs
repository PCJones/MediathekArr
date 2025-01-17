using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Models.Tvdb;

namespace MediathekArr.Extensions;

public static class SeriesExtensions
{
    /// <summary>
    /// Finds an episode by its air date.
    /// </summary>
    /// <param name="airDate">The air date to search for.</param>
    /// <returns>The TvdbEpisode if found, or null if not found.</returns>
    public static Episode? FindEpisodeByAirDate(this Series series, DateTime airDate)
    {
        return series.Episodes?.FirstOrDefault(episode => episode.Aired?.Date == airDate.Date);
    }

    /// <summary>
    /// Finds episodes by their air month.
    /// </summary>
    /// <param name="year">The year of the episodes to search for.</param>
    /// <param name="month">The month of the episodes to search for.</param>
    /// <returns>A list of TvdbEpisode objects that aired in the specified year and month.</returns>
    public static List<Episode>? FindEpisodeByAirMonth(this Series series, int year, int month)
    {
        return series.Episodes?
            .Where(episode => episode.Aired.HasValue &&
                              episode.Aired.Value.Year == year &&
                              episode.Aired.Value.Month == month)
            .ToList();
    }

    /// <summary>
    /// Finds all episodes aired in a specified year.
    /// </summary>
    /// <param name="year">The year to search for.</param>
    /// <returns>A list of TvdbEpisode objects aired in the specified year, or an empty list if none are found.</returns>
    public static List<Episode> FindEpisodesByAirYear(this Series series, int year)
    {
        return series.Episodes?
            .Where(episode => episode.Aired?.Year == year)
            .ToList() ?? [];
    }

    /// <summary>
    /// Finds all episodes from a given season.
    /// </summary>
    /// <param name="seasonNumber">The season number to search for.</param>
    /// <returns>A list of TvdbEpisode objects in the specified season, or an empty list if none are found.</returns>
    public static List<Episode> FindEpisodesBySeason(this Series series, int seasonNumber)
    {
        return series.Episodes?.Where(episode => episode.SeasonNumber == seasonNumber).ToList() ?? [];
    }

    /// <summary>
    /// Finds all episodes from a given season.
    /// </summary>
    /// <param name="seasonNumber">The season number to search for.</param>
    /// <returns>A list of TvdbEpisode objects in the specified season, or an empty list if none are found.</returns>
    public static List<Episode> FindEpisodesBySeason(this Series series, string? seasonNumber)
    {
        if (int.TryParse(seasonNumber, out int parsedSeason))
        {
            return series.FindEpisodesBySeason(parsedSeason);
        }

        return [];
    }

    /// <summary>
    /// Finds a specific episode by season and episode number.
    /// </summary>
    /// <param name="seasonNumber">The season number of the episode.</param>
    /// <param name="episodeNumber">The episode number within the season.</param>
    /// <returns>The TvdbEpisode if found, or null if not found.</returns>
    public static Episode? FindEpisodeBySeasonAndNumber(this Series series, int seasonNumber, int episodeNumber)
    {
        return series.Episodes?.FirstOrDefault(episode =>
            episode.SeasonNumber == seasonNumber && episode.EpisodeNumber == episodeNumber);
    }

    /// <summary>
    /// Finds a specific episode by season and episode number.
    /// </summary>
    /// <param name="seasonNumber">The season number of the episode.</param>
    /// <param name="episodeNumber">The episode number within the season.</param>
    /// <returns>The TvdbEpisode if found, or null if not found.</returns>
    public static Episode? FindEpisodeBySeasonAndNumber(this Series series, string? seasonNumber, string? episodeNumber)
    {
        if (int.TryParse(seasonNumber, out int parsedSeason) &&
            int.TryParse(episodeNumber, out int parsedEpisode))
        {
            return series.FindEpisodeBySeasonAndNumber(parsedSeason, parsedEpisode);
        }

        return null;
    }

    /// <summary>
    /// Finds a specific episode by absolute episode number. Dangerous, unless the show only has one season or absolute season numbers
    /// </summary>
    /// <param name="absoluteEpisodeNumber">The absolute episode number of the episode.</param>
    /// <returns>The TvdbEpisode if found, or null if not found.</returns>
    public static Episode? FindEpisodeByAbsoluteEpisodeNumber(this Series series, int absoluteEpisodeNumber)
    {
        if (absoluteEpisodeNumber == 0)
        {
            return null;
        }

        return series.Episodes?.FirstOrDefault(episode =>
            episode.EpisodeNumber == absoluteEpisodeNumber);
    }
}

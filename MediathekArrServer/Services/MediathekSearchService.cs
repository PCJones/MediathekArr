using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediathekArr.Infrastructure;
using MediathekArr.Extensions;
using MediathekArr.Models;
using MediathekArr.Models.Newznab;
using MediathekArr.Models.Rulesets;
using MediathekArr.Utilities;
using Microsoft.Extensions.Caching.Memory;
using MatchType = MediathekArr.Models.Rulesets.MatchType;

namespace MediathekArr.Services;

public partial class MediathekSearchService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ItemLookupService itemLookupService)
{
    private readonly IMemoryCache _cache = cache;
    private readonly ItemLookupService _itemLookupService = itemLookupService;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Constants.MediathekArrConstants.Mediathek_HttpClient);
    private readonly TimeSpan _cacheTimeSpan = TimeSpan.FromMinutes(55);
    private static readonly string[] _skipTitleKeywords = ["Audiodeskription", "Hörfassung", "(klare Sprache)", "Gebärdensprache", "Trailer", "Outtakes:"];
    private static readonly string[] _skipUrlKeywords = ["YXVkaW9kZXNrcmlwdGlvbg"]; // base64 for ARD, YXVkaW9kZXNrcmlwdGlvbg = audiodeskription
    private static readonly string[] _queryFields = ["topic", "title"];
    private readonly ConcurrentDictionary<string, List<Ruleset>> _rulesetsByTopic = new();

    public async Task UpdateRulesetsAsync()
    {
        var allRulesets = new List<Ruleset>();
        int currentPage = 1;

        while (true && currentPage < 100)
        {
            var response = await _httpClient.GetAsync($"https://mediathekarr.pcjones.de/metadata/api/rulesets.php?page={currentPage++}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var rulesetResponse = JsonSerializer.Deserialize<RulesetApiResponse>(responseContent);

                if (rulesetResponse?.Rulesets != null)
                {
                    allRulesets.AddRange(rulesetResponse.Rulesets);
                }

                if (rulesetResponse?.Pagination?.CurrentPage >= rulesetResponse?.Pagination.TotalPages)
                {
                    break;
                }
            }
            else
            {
                // Exit if the request fails
                Console.WriteLine("Failed to fetch rulesets from the API.");
                break;
            }
        }

        _rulesetsByTopic.Clear();
        foreach (var ruleset in allRulesets)
        {
            foreach (var topic in ruleset.Topics) // Iterate over all topics for the ruleset
            {
                if (!_rulesetsByTopic.TryGetValue(topic, out List<Ruleset>? value))
                {
                    value = [];
                    _rulesetsByTopic[topic] = value;
                }

                value.Add(ruleset);
            }
        }

        // Sort each topic group by priority
        foreach (var topic in _rulesetsByTopic.Keys.ToList())
        {
            _rulesetsByTopic[topic] = [.. _rulesetsByTopic[topic].OrderBy(ruleset => ruleset.Priority)];
        }
    }

    private async Task<string> FetchMediathekViewApiResponseAsync(List<object> queries, int size)
    {
        var requestBody = new
        {
            queries,
            sortBy = "filmlisteTimestamp",
            sortOrder = "desc",
            future = true,
            offset = 0,
            size
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8);
        var response = await _httpClient.PostAsync("https://mediathekviewweb.de/api/query", requestContent);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return string.Empty;
    }

    public async Task<string> FetchSearchResultsFromApiById(Series tvdbData, string? season, string? episodeNumber, int limit, int offset)
    {
        var cacheKey = $"tvdb_{tvdbData.SeriesId}_{season ?? "null"}_{episodeNumber ?? "null"}_{limit}_{offset}";

        if (_cache.TryGetValue(cacheKey, out string? cachedResponse))
        {
            return cachedResponse ?? "";
        }

        List<Episode>? desiredEpisodes = GetDesiredEpisodes(tvdbData, season, episodeNumber);
        if (season != null && desiredEpisodes?.Count == 0)
        {
            var response = NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult());
            _cache.Set(cacheKey, response, _cacheTimeSpan);
            return response;
        }

        var mediathekViewRequestCacheKey = $"mediathekapi_{tvdbData.SeriesId}";
        string apiResponse;
        if (_cache.TryGetValue(mediathekViewRequestCacheKey, out string? cachedApiResponse))
        {
            apiResponse = cachedApiResponse ?? string.Empty;
        }
        else
        {
            var queries = new List<object>
            {
                new { fields = _queryFields, query = tvdbData.GermanName.Replace(" & ", " ") ?? tvdbData.Name.Replace(" & ", " ") }
            };

            apiResponse = await FetchMediathekViewApiResponseAsync(queries, 10000);
            if (string.IsNullOrEmpty(apiResponse))
            {
                return NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult());
            }

            _cache.Set(mediathekViewRequestCacheKey, apiResponse, _cacheTimeSpan);
        }

        var results = JsonSerializer.Deserialize<MediathekApiResponse>(apiResponse)?.Result.Results ?? [];
        var (matchedEpisodes, unmatchedFilteredResultItems) = await ApplyRulesetFilters(results, tvdbData);
        var matchedDesiredEpisodes = ApplyDesiredEpisodeFilter(matchedEpisodes, desiredEpisodes);

        List<Item>? newznabItems;
        if (matchedDesiredEpisodes.Count == 0 && desiredEpisodes?.Count > 0)
        {
            // Fallback to best effort matching 
            newznabItems = desiredEpisodes
                .SelectMany(episode => MediathekSearchFallbackHandler.GetFallbackSearchResultItemsById(apiResponse, episode, tvdbData))
                .ToList();
        }
        else
        {
            newznabItems = matchedDesiredEpisodes.SelectMany(GenerateRssItems).ToList();
        }

        var newznabRssResponse = ConvertNewznabItemsToRss(newznabItems, limit, offset);

        _cache.Set(cacheKey, newznabRssResponse, _cacheTimeSpan);

        return newznabRssResponse;
    }

    private static List<Episode>? GetDesiredEpisodes(Series tvdbData, string? season, string? episodeNumber)
    {
        List<Episode>? desiredEpisodes;
        if (season != null)
        {
            desiredEpisodes = [];
            if (episodeNumber is null)
            {
                desiredEpisodes.AddRange(tvdbData.FindEpisodesBySeason(season));
                if (season.Length == 4 && int.TryParse(season, out var year))
                {
                    if (year >= 1900 && year <= 2100)
                    {
                        desiredEpisodes.AddRange(tvdbData.FindEpisodesByAirYear(year));
                        desiredEpisodes = desiredEpisodes.Distinct().ToList();
                    }
                }
            }
            else
            {
                Episode? desiredEpisode;
                if (season?.Length == 4 && episodeNumber.Contains('/'))
                {
                    var episodeNumberSplitted = episodeNumber?.Split('/');
                    if (episodeNumberSplitted?.Length == 2 && DateTime.TryParse($"{season}-{episodeNumberSplitted[0]}-{episodeNumberSplitted[1]}", out DateTime searchAirDate))
                    {
                        desiredEpisode = tvdbData.FindEpisodeByAirDate(searchAirDate);
                    }
                    else
                    {
                        desiredEpisode = null;
                    }
                }
                else
                {
                    desiredEpisode = tvdbData.FindEpisodeBySeasonAndNumber(season, episodeNumber);
                }

                if (desiredEpisode != null)
                {
                    desiredEpisodes.Add(desiredEpisode);
                }
            }
        }
        else
        {
            desiredEpisodes = null;
        }

        return desiredEpisodes;
    }

    private static string ConvertNewznabItemsToRss(List<Item> items, int limit, int offset)
    {
        if (items == null || items.Count == 0)
        {
            return NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult());
        }

        var paginatedItems = items.Skip(offset).Take(limit).ToList();

        // TODO: use Factory
        var rss = new Rss
        {
            Channel = new Channel
            {
                Title = "MediathekArr",
                Description = "MediathekArr API results",
                Response = new Response
                {
                    Offset = offset,
                    Total = items.Count
                },
                Items = paginatedItems,
            }
        };

        return NewznabUtils.SerializeRss(rss);
    }

    private static List<MatchedEpisodeInfo> ApplyDesiredEpisodeFilter(List<MatchedEpisodeInfo> matchedEpisodes, List<Episode>? desiredEpisodes)
    {
        if (desiredEpisodes is null)
        {
            return matchedEpisodes;
        }

        return [.. matchedEpisodes.Where(matched =>
            desiredEpisodes.Any(desiredEpisode =>
                desiredEpisode.SeasonNumber == matched.Episode.SeasonNumber &&
                desiredEpisode.EpisodeNumber == matched.Episode.EpisodeNumber
            )
        )];
    }

    private async Task<MatchedEpisodeInfo?> MatchesSeasonAndEpisode(ApiResultItem item, Ruleset ruleset)
    {
        // Fetch TVDB episode information
        var tvdbData = await _itemLookupService.GetShowInfoById(ruleset.Media.TvdbId);

        if (tvdbData?.Episodes == null || tvdbData.Episodes.Count == 0)
        {
            return null;
        }

        // Use generated title if available, otherwise read from mediathek response
        string? title = ruleset.TitleRegexRules.Count != 0 ? 
            BuildTitleFromRegexRules(item, ruleset.TitleRegexRules) : 
            GetFieldValue(item, "title");

        // Extract season and episode from the item using the ruleset
        string? season;
        if (ruleset.SeasonRegex != null && StaticSeasonRegex().IsMatch(ruleset.SeasonRegex))
        {
            // If SeasonRegex is in the format "S0nnn", use it directly
            season = ruleset.SeasonRegex[1..];
        }
        else
        {
            // Otherwise, attempt to extract the value using the regex
            season = ExtractValueUsingRegex(title, ruleset.SeasonRegex);
        }

        string? episode;
        if (ruleset.SeasonRegex != null && StaticEpisodeRegex().IsMatch(ruleset.EpisodeRegex))
        {
            // If EpisodeRegex is in the format "E0nnn", use it directly
            episode = ruleset.EpisodeRegex[1..];
        }
        else
        {
            // Otherwise, attempt to extract the value using the regex
            episode = ExtractValueUsingRegex(title, ruleset.EpisodeRegex);
        }

        if (string.IsNullOrEmpty(season) || string.IsNullOrEmpty(episode))
        {
            return null;
        }

        if (!int.TryParse(season, out var seasonNumber) || !int.TryParse(episode, out var episodeNumber))
        {
            return null; // Invalid season or episode format
        }

        // Find the matching episode in the TVDB data
        var matchedEpisode = tvdbData.FindEpisodeBySeasonAndNumber(seasonNumber, episodeNumber);

        if (matchedEpisode == null)
        {
            return null; // No matching episode found
        }

        return new MatchedEpisodeInfo(
            Episode: matchedEpisode,
            Item: item,
            ShowName: string.IsNullOrEmpty(tvdbData.Name) ? tvdbData.GermanName : tvdbData.Name,
            MatchedTitle: $"S{season}E{episode}"
        );
    }

    private async Task<MatchedEpisodeInfo?> MatchesAbsoluteEpisodeNumber(ApiResultItem item, Ruleset ruleset)
    {
        // Fetch TVDB episode information
        var tvdbData = await _itemLookupService.GetShowInfoById(ruleset.Media.TvdbId);

        if (tvdbData?.Episodes == null || tvdbData.Episodes.Count == 0)
        {
            return null;
        }

        // Use generated title if available, otherwise read from mediathek response
        string? title = ruleset.TitleRegexRules.Count != 0 ?
            BuildTitleFromRegexRules(item, ruleset.TitleRegexRules) :
            GetFieldValue(item, "title");

        // Extract absolute episode number from the item using the ruleset

        string? absoluteEpisode = ExtractValueUsingRegex(title, ruleset.EpisodeRegex);

        if (string.IsNullOrEmpty(absoluteEpisode))
        {
            return null;
        }

        if (!int.TryParse(absoluteEpisode, out var absoluteEpisodeNumber))
        {
            return null; // Invalid season or episode format
        }

        // Find the matching episode in the TVDB data
        var matchedEpisode = tvdbData.FindEpisodeByAbsoluteEpisodeNumber(absoluteEpisodeNumber);

        if (matchedEpisode == null)
        {
            return null; // No matching episode found
        }

        return new MatchedEpisodeInfo(
            Episode: matchedEpisode,
            Item: item,
            ShowName: string.IsNullOrEmpty(tvdbData.Name) ? tvdbData.GermanName : tvdbData.Name,
            MatchedTitle: $"E{absoluteEpisode}"
        );
    }

    /// <summary>
    /// Extracts a value from the item using the specified regex rule.
    /// </summary>
    /// <param name="item">The API result item.</param>
    /// <param name="regexRule">The regex rule.</param>
    /// <returns>The extracted value, or null if not found.</returns>
    private static string? ExtractValueUsingRegex(string? source, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return null;
        }

        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        var match = Regex.Match(source, pattern);

        return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : null;
    }

    private async Task<MatchedEpisodeInfo?> MatchesItemTitleIncludes(ApiResultItem item, Ruleset ruleset)
    {
        // Fetch TVDB episode information
        var tvdbData = await _itemLookupService.GetShowInfoById(ruleset.Media.TvdbId);

        if (tvdbData?.Episodes == null || tvdbData.Episodes.Count == 0)
        {
            return null;
        }

        // Construct the title based on ruleset
        var constructedTitle = BuildTitleFromRegexRules(item, ruleset.TitleRegexRules);

        if (string.IsNullOrEmpty(constructedTitle))
        {
            return null;
        }

        // Check if the constructed title is included in any episode title
        var matchedEpisode = 
            tvdbData.Episodes
            .FirstOrDefault(episode => FormatTitle(episode.Name)
            .Contains(FormatTitle(constructedTitle), StringComparison.OrdinalIgnoreCase));

        if (matchedEpisode is null)
        {
            return null;
        }

        return new MatchedEpisodeInfo(
            Episode: matchedEpisode,
            Item: item,
            ShowName: string.IsNullOrEmpty(tvdbData.Name) ? tvdbData.GermanName : tvdbData.Name,
            MatchedTitle: constructedTitle
			);
    }

    private async Task<MatchedEpisodeInfo?> MatchesItemTitleExact(ApiResultItem item, Ruleset ruleset)
		{
			// Fetch TVDB episode information
			var tvdbData = await _itemLookupService.GetShowInfoById(ruleset.Media.TvdbId);

			if (tvdbData?.Episodes == null || tvdbData.Episodes.Count == 0)
			{
				return null;
			}

			// Construct the title based on ruleset
			var constructedTitle = BuildTitleFromRegexRules(item, ruleset.TitleRegexRules);

			if (string.IsNullOrEmpty(constructedTitle))
			{
				return null;
			}

        var formattedConstructedTitle = FormatTitle(constructedTitle);

			// Check if the constructed title matches any episode title exactly
			var matchedEpisodes =
				tvdbData.Episodes
				.Where(episode => FormatTitle(episode.Name)
				.Equals(formattedConstructedTitle, StringComparison.OrdinalIgnoreCase))
				.ToArray();

        Episode? matchedEpisode = GuessCorrectMatch(item, matchedEpisodes);

			if (matchedEpisode != null)
			{
				return new MatchedEpisodeInfo(
					Episode: matchedEpisode,
					Item: item,
					ShowName: string.IsNullOrEmpty(tvdbData.Name) ? tvdbData.GermanName : tvdbData.Name,
					MatchedTitle: constructedTitle
				);
			}

			return null;
		}

		private static Episode? GuessCorrectMatch(ApiResultItem item, Episode[] matchedEpisodes)
		{
			if (matchedEpisodes.Length == 1)
			{
				return matchedEpisodes[0];
			}
			else // multiple matched episodes found, we try to guess which one is the best
			{
				// Try to match by aired date
				var matchedEpisodeByAirDate = matchedEpisodes.FirstOrDefault(episode => episode.Aired == DateTimeOffset.FromUnixTimeSeconds(item.Timestamp).UtcDateTime.Date);
				if (matchedEpisodeByAirDate != null)
				{
					return matchedEpisodeByAirDate;
				}
            // chose the newest one
            return matchedEpisodes.OrderByDescending(episode => episode.Aired).FirstOrDefault();
			}
		}

		private async Task<MatchedEpisodeInfo?> MatchesItemTitleEqualsAirdate(ApiResultItem item, Ruleset ruleset)
    {
        // Fetch TVDB episode information
        var tvdbData = await _itemLookupService.GetShowInfoById(ruleset.Media.TvdbId);

        if (tvdbData?.Episodes == null || tvdbData.Episodes.Count == 0)
        {
            return null;
        }

        // Construct the title based on ruleset
        var constructedTitle = BuildTitleFromRegexRules(item, ruleset.TitleRegexRules);

        if (string.IsNullOrEmpty(constructedTitle))
        {
            return null;
        }

        if (TryParseDate(constructedTitle, out var parsedDate))
        {
            // Find the episode by airdate
            var matchedEpisode = tvdbData.FindEpisodeByAirDate(parsedDate);

            if (matchedEpisode != null)
            {
                return new MatchedEpisodeInfo(
                    Episode: matchedEpisode,
                    Item: item,
                    ShowName: string.IsNullOrEmpty(tvdbData.Name) ? tvdbData.GermanName : tvdbData.Name,
                    MatchedTitle: constructedTitle
					);
            }
        }

        return null;
    }

    private static bool TryParseDate(string dateString, out DateTime date)
    {
        // Attempt parsing with various formats
        var formats = new[]
        {
            "d. MMMM yyyy", // e.g., "7. Juni 2024"
            "dd.MM.yyyy",    // e.g., "31.12.2017"
            "yyyy-MM-dd",    // e.g., "2017-12-01"
            "yyyyMMdd",       // e.g., "20171201"
            "dd. MMMM yyyy", // e.g., "07. Juni 2024"
        };

        return DateTime.TryParseExact(
            dateString,
            formats,
            CultureInfo.GetCultureInfo("de-DE"),
            DateTimeStyles.None,
            out date
        );
    }

    private static string? BuildTitleFromRegexRules(ApiResultItem item, List<TitleRegexRule> titleRegexRules)
    {
        var stringBuilder = new StringBuilder();

        foreach (var rule in titleRegexRules)
        {
            switch (rule.Type)
            {
                case TitleRegexRuleType.Static:
                    // Append the static value directly
                    if (!string.IsNullOrEmpty(rule.Value))
                    {
                        stringBuilder.Append(rule.Value);
                    }
                    break;

                case TitleRegexRuleType.Regex:
                    // Extract substring using the regex pattern from the specified field
                    if (!string.IsNullOrEmpty(rule.Pattern) && !string.IsNullOrEmpty(rule.Field))
                    {
                        var fieldValue = GetFieldValue(item, rule.Field);
                        if (!string.IsNullOrEmpty(fieldValue))
                        {
                            var match = Regex.Match(fieldValue, rule.Pattern);
                            if (match.Success && match.Groups[^1].Length > 0)
                            {
                                // Use the last group
                                stringBuilder.Append(match.Groups[^1].Value);
                            }
                            else
                            {
                                // abort if regex match failed
                                return null;
                            }
                        }
                    }
                    break;
            }
        }

        return stringBuilder.ToString();
    }

    private static string GetFieldValue(ApiResultItem item, string fieldName)
    {
        return fieldName switch
        {
            "channel" => item.Channel,
            "topic" => item.Topic,
            "title" => item.Title,
            "description" => item.Description,
            "timestamp" => item.Timestamp.ToString(),
            "duration" => item.Duration.ToString(),
            "size" => item.Size.ToString(),
            "url_website" => item.UrlWebsite,
            "url_video" => item.UrlVideo,
            "url_video_low" => item.UrlVideoLow,
            "url_video_hd" => item.UrlVideoHd,
            _ => string.Empty
        };
    }


    private static bool FilterMatches(ApiResultItem item, Filter filter)
    {
        string? attributeValue = GetFieldValue(item, filter.Attribute);

        return filter.Type switch
        {
            MatchType.ExactMatch => attributeValue.Equals(filter.Value.ToString(), StringComparison.OrdinalIgnoreCase),
            MatchType.Contains => attributeValue.Contains(filter.Value.ToString(), StringComparison.OrdinalIgnoreCase),
            MatchType.Regex => Regex.IsMatch(attributeValue, filter.Value.ToString()),
            MatchType.GreaterThan => double.TryParse(attributeValue, out var attrValue) && double.TryParse(filter.Value.ToString(), out var filterValue) && attrValue > filterValue * 60,
            MatchType.LessThan => double.TryParse(attributeValue, out var attrValue) && double.TryParse(filter.Value.ToString(), out var filterValue) && attrValue < filterValue * 60,
            _ => false,
        };
    }

    private List<Ruleset> GetRulesetsForTopic(string topic)
    {
        return _rulesetsByTopic.TryGetValue(topic, out var rulesets) ? rulesets : [];
    }

    private async Task<(List<MatchedEpisodeInfo> matchedEpisodes, List<ApiResultItem> unmatchedFilteredResultItems)> ApplyRulesetFilters(List<ApiResultItem> results, Series? tvdbData = null)
    {
        var matchedFilteredResults = new List<MatchedEpisodeInfo>();
        var unmatchedFilteredResults = new List<ApiResultItem>(results);

        foreach (var item in results)
        {
            if(ShouldSkipItem(item))
            {
                unmatchedFilteredResults.Remove(item);
                continue;
            }

            // Get applicable rulesets for the topic or specific TVDB data
            var rulesets = tvdbData is null
                ? GetRulesetsForTopic(item.Topic)
                : GetRulesetsForTopic(item.Topic).Where(r => r.Media?.TvdbId == tvdbData.SeriesId).ToList();

            foreach (var ruleset in rulesets)
            {
                if (!ruleset.Filters.All(filter => FilterMatches(item, filter)))
                {
                    unmatchedFilteredResults.Remove(item);
                    continue; // Skip this ruleset if any filter fails
                }

                MatchedEpisodeInfo? matchInfo = null;

                switch (ruleset.MatchingStrategy)
                {
                    case MatchingStrategy.SeasonAndEpisodeNumber:
                        matchInfo = await MatchesSeasonAndEpisode(item, ruleset);
                        break;
                    case MatchingStrategy.ItemTitleIncludes:
                        matchInfo = await MatchesItemTitleIncludes(item, ruleset);
                        break;
                    case MatchingStrategy.ItemTitleExact:
                        matchInfo = await MatchesItemTitleExact(item, ruleset);
                        break;
                    case MatchingStrategy.ItemTitleEqualsAirdate:
                        matchInfo = await MatchesItemTitleEqualsAirdate(item, ruleset);
                        break;
                    case MatchingStrategy.ByAbsoluteEpisodeNumber:
                        matchInfo = await MatchesAbsoluteEpisodeNumber(item, ruleset);
                        break;
                }

                if (matchInfo != null)
                {
                    matchedFilteredResults.Add(matchInfo);
                    break;
                }
                else
                {
                    unmatchedFilteredResults.Remove(item);
                }
            }
        }

        return (matchedFilteredResults, unmatchedFilteredResults);
    }

    public async Task<string> FetchSearchResultsForRssSync(int limit, int offset)
    {
        var cacheKey = $"rss_{limit}_{offset}";

        // Return cached response if it exists
        if (_cache.TryGetValue(cacheKey, out string? cachedResponse))
        {
            return cachedResponse ?? "";
        }

        var mediathekViewRequestCacheKey = "rss_mediathekview_results";
        List<ApiResultItem> results;
        if (_cache.TryGetValue(mediathekViewRequestCacheKey, out List<ApiResultItem>? cachedResults))
        {
            results = cachedResults ?? [];
        }
        else
        {
            var queries = new List<object>();
            var apiResponse = await FetchMediathekViewApiResponseAsync(queries, 6000);

            if (string.IsNullOrEmpty(apiResponse))
            {
                return NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult());
            }

            results = JsonSerializer.Deserialize<MediathekApiResponse>(apiResponse)?.Result.Results ?? [];
            _cache.Set(mediathekViewRequestCacheKey, results, TimeSpan.FromMinutes(20));
        }

        // Deserialize the API response and apply ruleset filters
        var (matchedEpisodes, unmatchedFilteredResultItems) = await ApplyRulesetFilters(results);

        List<Item>? newznabItemsByRuleset = matchedEpisodes.SelectMany(GenerateRssItems).ToList();
        List<Item>? newznabItemsByFallback = MediathekSearchFallbackHandler.GetFallbackSearchResultItemsByString(unmatchedFilteredResultItems, null);

        // Combine the results from ruleset matching and fallback handler
        var newznabRssResponse = ConvertNewznabItemsToRss([.. newznabItemsByRuleset, .. newznabItemsByFallback], limit, offset);

        // Cache the response and return it
        _cache.Set(cacheKey, newznabRssResponse, _cacheTimeSpan);
        return newznabRssResponse;
    }

    public async Task<string> FetchSearchResultsFromApiByString(string? q, string? season, int limit, int offset)
    {
        var cacheKey = $"q_{q ?? "null"}_{season ?? "null"}_{limit}_{offset}";

        // Return cached response if it exists
        if (_cache.TryGetValue(cacheKey, out string? cachedResponse))
        {
            return cachedResponse ?? "";
        }

        var mediathekViewRequestCacheKey = $"mediathekapi_{q ?? "null"}_{season ?? "null"}";
        string apiResponse;
        if (_cache.TryGetValue(mediathekViewRequestCacheKey, out string? cachedApiResponse))
        {
            apiResponse = cachedApiResponse ?? string.Empty;
        }
        else
        {
            var queries = new List<object>();
            if (q != null)
            {
                queries.Add(new { fields = _queryFields, query = q });
            }

            if (!string.IsNullOrEmpty(season))
            {
                var zeroBasedSeason = season.Length >= 2 ? season : $"0{season}";
                queries.Add(new { fields = new[] { "title" }, query = $"S{zeroBasedSeason}" });
            }

            apiResponse = await FetchMediathekViewApiResponseAsync(queries, 1500);
            if (string.IsNullOrEmpty(apiResponse))
            {
                return NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult());
            }

            _cache.Set(mediathekViewRequestCacheKey, apiResponse, _cacheTimeSpan);
        }
        // Deserialize the API response and apply ruleset filters
        var results = JsonSerializer.Deserialize<MediathekApiResponse>(apiResponse)?.Result.Results ?? [];
        var (matchedEpisodes, unmatchedFilteredResultItems) = await ApplyRulesetFilters(results);

        List<Item>? newznabItemsByRuleset = matchedEpisodes.SelectMany(GenerateRssItems).ToList();
        List<Item>? newznabItemsByFallback = MediathekSearchFallbackHandler.GetFallbackSearchResultItemsByString(unmatchedFilteredResultItems, season);

        // Combine the results from ruleset matching and fallback handler
        var newznabRssResponse = ConvertNewznabItemsToRss([.. newznabItemsByRuleset, .. newznabItemsByFallback], limit, offset);

        // Cache the response and return it
        _cache.Set(cacheKey, newznabRssResponse, _cacheTimeSpan);
        return newznabRssResponse;
    }

    private List<Item> GenerateRssItems(MatchedEpisodeInfo matchedEpisodeInfo)
    {
        var items = new List<Item>();

        string[] categories = ["5000", "2000"];

        if (!string.IsNullOrEmpty(matchedEpisodeInfo.Item.UrlVideoHd))
        {
            items.AddRange(CreateRssItems(matchedEpisodeInfo, "1080p", 1.75, "TV > HD", [.. categories, "5040", "2040"], matchedEpisodeInfo.Item.UrlVideoHd));
        }

        if (!string.IsNullOrEmpty(matchedEpisodeInfo.Item.UrlVideo))
        {
            items.AddRange(CreateRssItems(matchedEpisodeInfo, "720p", 1.0, "TV > HD", [.. categories, "5040", "2040"], matchedEpisodeInfo.Item.UrlVideo));
        }

        if (!string.IsNullOrEmpty(matchedEpisodeInfo.Item.UrlVideoLow))
        {
            items.AddRange(CreateRssItems(matchedEpisodeInfo, "480p", 0.4, "TV > SD", [.. categories, "5030", "2030"], matchedEpisodeInfo.Item.UrlVideoLow));

        }

        return items;
    }

    private static List<Item> CreateRssItems(MatchedEpisodeInfo matchedEpisodeInfo, string quality, double sizeMultiplier, string category, string[] categoryValues, string url)
    {
        var items = new List<Item>
        {
            CreateRssItem(matchedEpisodeInfo, quality, sizeMultiplier, category, categoryValues, url, EpisodeType.Standard)
        };

        // also create daily type if season is a year
        if (matchedEpisodeInfo.Episode.SeasonNumber > 1950)
        {
            items.Add(CreateRssItem(matchedEpisodeInfo, quality, sizeMultiplier, category, categoryValues, url, EpisodeType.Daily));
        }

        return items;
    }

    private static string FormatTitle(string title)
    {
        // Remove unwanted characters
        title = title.Replace("–", "-");
        title = title.RemoveAccentButKeepGermanUmlauts();
        title = TitleRegexUnd().Replace(title, "and");
        title = TitleRegexSymbols().Replace(title, ""); // Remove various symbols
        title = TitleRegexWhitespace().Replace(title, ".").Replace("..", ".");

        return title;
    }


    private static Item CreateRssItem(MatchedEpisodeInfo matchedEpisodeInfo, string quality, double sizeMultiplier, string category, string[] categoryValues, string url, EpisodeType episodeType)
    {
        var item = matchedEpisodeInfo.Item;
        var adjustedSize = (long)(matchedEpisodeInfo.Item.Size * sizeMultiplier);
        if (!string.IsNullOrEmpty(matchedEpisodeInfo.Item.UrlSubtitle))
        {
            adjustedSize += 15000000; // Add 15MB to size if subs are available
        }
        var parsedTitle = GenerateTitle(matchedEpisodeInfo, quality, episodeType);
        var formattedTitle = FormatTitle(parsedTitle);
        var translatedTitle = formattedTitle;
        var encodedTitle = Convert.ToBase64String(Encoding.UTF8.GetBytes(translatedTitle));
        var encodedVideoUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedSubtitleUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.UrlSubtitle));

        // Generate the full URL for the fake_nzb_download endpoint
        var fakeDownloadUrl = $"/api/fake_nzb_download?encodedVideoUrl={encodedVideoUrl}&encodedSubtitleUrl={encodedSubtitleUrl}&encodedTitle={encodedTitle}";

        return new Item
        {
            Title = translatedTitle,
            Guid = new NewznabGuid
            {
                IsPermaLink = true,
					Value = $"{item.UrlWebsite}#{quality}{(episodeType == EpisodeType.Daily ? "" : "-d")}-{item.Language}",
				},
            Link = url,
            Comments = item.UrlWebsite,
            PubDate = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp).ToString("R"),
            Category = category,
            Description = item.Description,
            Enclosure = new Enclosure
            {
                Url = fakeDownloadUrl,
                Length = adjustedSize,
                Type = NewznabUtils.Application.Nzb
            },
            Attributes = NewznabUtils.GenerateAttributes(matchedEpisodeInfo, categoryValues, episodeType)
        };
    }

    private static string GenerateTitle(MatchedEpisodeInfo matchedEpisodeInfo, string quality, EpisodeType episodeType)
    {
        var episode = matchedEpisodeInfo.Episode;

        if (episodeType == EpisodeType.Daily)
        {
            return $"{matchedEpisodeInfo.ShowName}.{episode.Aired:yyyy-MM-dd}.{episode.Name}.{matchedEpisodeInfo.Item.Language}.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
        }
        return $"{matchedEpisodeInfo.ShowName}.S{episode.PaddedSeason}E{episode.PaddedEpisode}.{episode.Name}.{matchedEpisodeInfo.Item.Language}.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
    }

    public static bool ShouldSkipItem(ApiResultItem item)
    {
        return item.UrlVideo.EndsWith(".m3u8") || _skipTitleKeywords.Any(item.Title.Contains) || _skipUrlKeywords.Any(item.UrlWebsite.Contains);
    }

    [GeneratedRegex(@"[&]")]
    private static partial Regex TitleRegexUnd();
    [GeneratedRegex(@"[/:;,""'’@#?$%^*+=!|<>,()|]")]
    private static partial Regex TitleRegexSymbols();
    [GeneratedRegex(@"\s+")]
    private static partial Regex TitleRegexWhitespace();
    [GeneratedRegex(@"^S\d{1,4}$")]
    private static partial Regex StaticSeasonRegex();
    [GeneratedRegex(@"^E\d{1,4}$")]
    private static partial Regex StaticEpisodeRegex();
}
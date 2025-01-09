using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tvdb.Models;
using Tvdb.Types;

namespace Tvdb.Clients;

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IArtworkClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a single artwork base record.
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<ArtworkResponse> ArtworkAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a single artwork extended record.
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<ArtworkExtendedResponse> ExtendedAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IArtwork_StatusesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns list of artwork status records.
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<ArtworkStatusResponse> StatusesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IArtwork_TypesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a list of artworkType records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<ArtworkTypeResponse> TypesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IAwardsClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a list of award base records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<AwardsResponse> AwardsGetAsync(CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a single award base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<AwardResponse> AwardsGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a single award extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<AwardExtendedResponse> ExtendedAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IAward_CategoriesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a single award category base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<AwardCategoryResponse> CategoriesAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a single award category extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<AwardCategoryExtendedResponse> ExtendedAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ICharactersClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns character base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<CharacterResponse> CharactersAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ICompaniesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns a paginated list of company records
    /// </remarks>
    /// <param name="page">name</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response12> CompaniesGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns all company type records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response13> TypesAsync(CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns a company record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response14> CompaniesGetAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IContent_RatingsClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list content rating records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response15> RatingsAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ICountriesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of country records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response16> CountriesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IEntity_TypesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns the active entity types
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response17> EntitiesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IEpisodesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a list of episodes base records with the basic attributes.&lt;br&gt; Note that all episodes are returned, even those that may not be included in a series' default season order.
    /// </remarks>
    /// <param name="page">page number</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response18> EpisodesGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns episode base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response19> EpisodesGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns episode extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="meta">meta</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response20> ExtendedAsync(double id, EpisodesMeta? meta = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns episode translation record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="language">language</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response21> TranslationsAsync(double id, string language, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IGendersClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of gender records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response22> GendersAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IGenresClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of genre records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response23> GenresGetAsync(CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns genre record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response24> GenresGetAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IInspirationTypesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of inspiration types records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response25> TypesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ILanguagesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of language records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response26> LanguagesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IListsClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of list base records
    /// </remarks>
    /// <param name="page">page number</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response27> ListsGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns an list base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response28> ListsGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns an list base record search by slug
    /// </remarks>
    /// <param name="slug">slug</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response29> SlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns a list extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response30> ExtendedAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns list translation record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="language">language</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response31> TranslationsAsync(double id, string language, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IMoviesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of movie base records
    /// </remarks>
    /// <param name="page">page number</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response32> MoviesGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns movie base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response33> MoviesGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns movie extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="meta">meta</param>
    /// <param name="short">reduce the payload and returns the short version of this record without characters, artworks and trailers.</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response34> ExtendedAsync(double id, MoviesMeta? meta = null, bool? @short = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Search movies based on filter parameters
    /// </remarks>
    /// <param name="country">country of origin</param>
    /// <param name="lang">original language</param>
    /// <param name="company">production company</param>
    /// <param name="contentRating">content rating id base on a country</param>
    /// <param name="genre">genre</param>
    /// <param name="sort">sort by results</param>
    /// <param name="status">status</param>
    /// <param name="year">release year</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response35> FilterAsync(string country, string lang, double? company = null, double? contentRating = null, double? genre = null, Sort? sort = null, double? status = null, double? year = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns movie base record search by slug
    /// </remarks>
    /// <param name="slug">slug</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response36> SlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns movie translation record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="language">language</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response37> TranslationsAsync(double id, string language, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IMovie_StatusesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of status records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response38> StatusesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IPeopleClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns a list of people base records with the basic attributes.
    /// </remarks>
    /// <param name="page">page number</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response39> PeopleGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns people base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response40> PeopleGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns people extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="meta">meta</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response41> ExtendedAsync(double id, PeopleMeta? meta = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns people translation record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="language">language</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response42> TranslationsAsync(double id, string language, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IPeople_TypesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of peopleType records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response43> TypesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ISearchClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Our search index includes series, movies, people, and companies. Search is limited to 5k results max.
    /// </remarks>
    /// <param name="query">The primary search string, which can include the main title for a record including all translations and aliases.</param>
    /// <param name="q">Alias of the "query" parameter.  Recommend using query instead as this field will eventually be deprecated.</param>
    /// <param name="type">Restrict results to a specific entity type.  Can be movie, series, person, or company.</param>
    /// <param name="year">Restrict results to a specific year. Currently only used for series and movies.</param>
    /// <param name="company">Restrict results to a specific company (original network, production company, studio, etc).  As an example, "The Walking Dead" would have companies of "AMC", "AMC+", and "Disney+".</param>
    /// <param name="country">Restrict results to a specific country of origin. Should contain a 3 character country code. Currently only used for series and movies.</param>
    /// <param name="director">Restrict results to a specific director.  Generally only used for movies.  Should include the full name of the director, such as "Steven Spielberg".</param>
    /// <param name="language">Restrict results to a specific primary language.  Should include the 3 character language code.  Currently only used for series and movies.</param>
    /// <param name="primaryType">Restrict results to a specific type of company.  Should include the full name of the type of company, such as "Production Company".  Only used for companies.</param>
    /// <param name="network">Restrict results to a specific network.  Used for TV and TV movies, and functions the same as the company parameter with more specificity.</param>
    /// <param name="remote_id">Search for a specific remote id.  Allows searching for an IMDB or EIDR id, for example.</param>
    /// <param name="offset">Offset results.</param>
    /// <param name="limit">Limit results.</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response44> SearchAsync(string query = null, string q = null, string type = null, double? year = null, string company = null, string country = null, string director = null, string language = null, string primaryType = null, string network = null, string remote_id = null, double? offset = null, double? limit = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Search a series, movie, people, episode, company or season by specific remote id and returns a base record for that entity.
    /// </remarks>
    /// <param name="remoteId">Search for a specific remote id.  Allows searching for an IMDB or EIDR id, for example.</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response45> RemoteidAsync(string remoteId, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ISeasonsClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of seasons base records
    /// </remarks>
    /// <param name="page">page number</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response46> SeasonsGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns season base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response47> SeasonsGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns season extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response48> ExtendedAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns season type records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response49> TypesAsync(CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns season translation record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="language">language</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response50> TranslationsAsync(double id, string language, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ISeriesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of series base records
    /// </remarks>
    /// <param name="page">page number</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response51> SeriesGetAsync(double? page = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series base record
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response52> SeriesGetAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series artworks base on language and type. &lt;br&gt; Note&amp;#58; Artwork type is an id that can be found using **/artwork/types** endpoint.
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="lang">lang</param>
    /// <param name="type">type</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response53> ArtworksAsync(double id, string lang = null, int? type = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series base record including the nextAired field. &lt;br&gt; Note&amp;#58; nextAired was included in the base record endpoint but that field will deprecated in the future so developers should use the nextAired endpoint.
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response54> NextAiredAsync(double id, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series extended record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="meta">meta</param>
    /// <param name="short">reduce the payload and returns the short version of this record without characters and artworks</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response55> ExtendedAsync(double id, SeriesMeta? meta = null, bool? @short = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series episodes from the specified season type, default returns the episodes in the series default season type
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="season_type">season-type</param>
    /// <param name="airDate">airDate of the episode, format is yyyy-mm-dd</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response56> EpisodesGetAsync(int page, double id, string season_type, int? season = null, int? episodeNumber = null, string airDate = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series base record with episodes from the specified season type and language. Default returns the episodes in the series default season type.
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="season_type">season-type</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response57> EpisodesGetAsync(int page, double id, string season_type, string lang, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Search series based on filter parameters
    /// </remarks>
    /// <param name="country">country of origin</param>
    /// <param name="lang">original language</param>
    /// <param name="company">production company</param>
    /// <param name="contentRating">content rating id base on a country</param>
    /// <param name="genre">Genre id. This id can be found using **/genres** endpoint.</param>
    /// <param name="sort">sort by results</param>
    /// <param name="sortType">sort type ascending or descending</param>
    /// <param name="status">status</param>
    /// <param name="year">release year</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response58> FilterAsync(string country, string lang, double? company = null, double? contentRating = null, double? genre = null, Sort2? sort = null, SortType? sortType = null, double? status = null, double? year = null, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series base record searched by slug
    /// </remarks>
    /// <param name="slug">slug</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response59> SlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns series translation record
    /// </remarks>
    /// <param name="id">id</param>
    /// <param name="language">language</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response60> TranslationsAsync(double id, string language, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ISeries_StatusesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of status records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response61> StatusesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface ISource_TypesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns list of sourceType records
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response62> TypesAsync(CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IUpdatesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// Returns updated entities.  methodInt indicates a created record (1), an updated record (2), or a deleted record (3).  If a record is deleted because it was a duplicate of another record, the target record's information is provided in mergeToType and mergeToId.
    /// </remarks>
    /// <param name="page">name</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response63> UpdatesAsync(double since, UpdateEntity? type = null, UpdateAction? action = null, double? page = null, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IUser_infoClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns user info
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response64> UserGetAsync(CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns user info by user id
    /// </remarks>
    /// <param name="id">id</param>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response65> UserGetAsync(double id, CancellationToken cancellationToken = default);

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial interface IFavoritesClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// returns user favorites
    /// </remarks>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<Response66> FavoritesGetAsync(CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <remarks>
    /// creates a new user favorite
    /// </remarks>
    /// <returns>Ok</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task FavoritesPostAsync(FavoriteRecord body = null, CancellationToken cancellationToken = default);

}
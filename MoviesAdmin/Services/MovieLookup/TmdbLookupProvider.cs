using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MoviesAdmin.Services.MovieLookup
{
    // The Movie Database (https://developer.themoviedb.org). Best source for posters, trailers,
    // studios, and US certifications. Registered as a typed HttpClient in Program.cs.
    public sealed class TmdbLookupProvider : IMovieLookupProvider
    {
        private const string PosterBaseUrl = "https://image.tmdb.org/t/p/w500";

        private readonly HttpClient _http;
        private readonly MovieLookupOptions _options;

        public TmdbLookupProvider(HttpClient http, IOptions<MovieLookupOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public string Key => "tmdb";

        public string DisplayName => "TMDB (The Movie Database)";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.TmdbApiKey);

        public bool IsValidExternalId(string externalId) => int.TryParse(externalId, out var id) && id > 0;

        public async Task<IReadOnlyList<MovieLookupResult>> SearchAsync(string query, int? year, CancellationToken cancellationToken)
        {
            var path = $"search/movie?include_adult=false&query={Uri.EscapeDataString(query)}";
            if (year.HasValue)
            {
                path += $"&primary_release_year={year.Value}";
            }

            var response = await SendAsync<SearchResponse>(path, cancellationToken);
            return (response?.Results ?? new())
                .Take(10)
                .Select(r => new MovieLookupResult(
                    r.Id.ToString(),
                    r.Title ?? "(untitled)",
                    LookupText.Year(r.ReleaseDate),
                    r.PosterPath is null ? null : PosterBaseUrl + r.PosterPath))
                .ToList();
        }

        public async Task<MovieLookupDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
        {
            var movie = await SendAsync<MovieResponse>(
                $"movie/{externalId}?append_to_response=credits,videos,release_dates", cancellationToken);
            if (movie == null)
            {
                return null;
            }

            // Prefer an official YouTube trailer, then any YouTube trailer, then a teaser.
            var trailer = (movie.Videos?.Results ?? new())
                .Where(v => v.Site == "YouTube" && !string.IsNullOrEmpty(v.Key))
                .OrderByDescending(v => v.Type == "Trailer")
                .ThenByDescending(v => v.Official)
                .FirstOrDefault(v => v.Type is "Trailer" or "Teaser");

            // US theatrical certification (type 3) first, then any US release with one set.
            var certification = (movie.ReleaseDates?.Results ?? new())
                .Where(r => r.Country == "US")
                .SelectMany(r => r.ReleaseDates ?? new())
                .Where(d => !string.IsNullOrWhiteSpace(d.Certification))
                .OrderByDescending(d => d.Type == 3)
                .Select(d => d.Certification)
                .FirstOrDefault();

            return new MovieLookupDetails
            {
                Source = DisplayName,
                ExternalId = movie.Id.ToString(),
                SourceUrl = $"https://www.themoviedb.org/movie/{movie.Id}",
                Title = movie.Title ?? string.Empty,
                Synopsis = LookupText.CleanSynopsis(movie.Overview),
                ReleaseDate = DateTime.TryParseExact(movie.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var released) ? released : null,
                RuntimeMinutes = movie.Runtime is > 0 ? movie.Runtime : null,
                Certification = LookupText.Clean(certification),
                PosterUrl = movie.PosterPath is null ? null : PosterBaseUrl + movie.PosterPath,
                TrailerUrl = trailer is null ? null : $"https://www.youtube.com/watch?v={trailer.Key}",
                DirectorName = movie.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name,
                StudioName = movie.ProductionCompanies?.FirstOrDefault()?.Name,
                GenreNames = (movie.Genres ?? new()).Select(g => g.Name).OfType<string>().ToList(),
                Ratings = movie.VoteCount > 0
                    ? new[] { new ExternalRating("TMDB users", $"{movie.VoteAverage:0.0}/10") }
                    : Array.Empty<ExternalRating>()
            };
        }

        private async Task<T?> SendAsync<T>(string pathAndQuery, CancellationToken cancellationToken) where T : class
        {
            var key = _options.TmdbApiKey?.Trim() ?? throw new MovieLookupException("TMDB isn't configured.");

            // v4 read access tokens are JWTs (always start "eyJ") sent as a bearer token; v3 API
            // keys go in the query string.
            using var request = new HttpRequestMessage(HttpMethod.Get, key.StartsWith("eyJ", StringComparison.Ordinal)
                ? pathAndQuery
                : $"{pathAndQuery}&api_key={Uri.EscapeDataString(key)}");
            if (key.StartsWith("eyJ", StringComparison.Ordinal))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            }

            using var response = await _http.SendAsync(request, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return null;
                case HttpStatusCode.Unauthorized:
                    throw new MovieLookupException("TMDB rejected the API key. Check MovieLookup:TmdbApiKey in user secrets.");
                case HttpStatusCode.TooManyRequests:
                    throw new MovieLookupException("TMDB rate limit reached. Try again in a few seconds.");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        // --- TMDB response shapes (only the fields used above) -------------------------------

        private sealed class SearchResponse
        {
            [JsonPropertyName("results")] public List<SearchItem>? Results { get; set; }
        }

        private sealed class SearchItem
        {
            [JsonPropertyName("id")] public int Id { get; set; }
            [JsonPropertyName("title")] public string? Title { get; set; }
            [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
            [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
        }

        private sealed class MovieResponse
        {
            [JsonPropertyName("id")] public int Id { get; set; }
            [JsonPropertyName("title")] public string? Title { get; set; }
            [JsonPropertyName("overview")] public string? Overview { get; set; }
            [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
            [JsonPropertyName("runtime")] public int? Runtime { get; set; }
            [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
            [JsonPropertyName("vote_average")] public double VoteAverage { get; set; }
            [JsonPropertyName("vote_count")] public int VoteCount { get; set; }
            [JsonPropertyName("genres")] public List<NamedItem>? Genres { get; set; }
            [JsonPropertyName("production_companies")] public List<NamedItem>? ProductionCompanies { get; set; }
            [JsonPropertyName("credits")] public CreditsResponse? Credits { get; set; }
            [JsonPropertyName("videos")] public VideosResponse? Videos { get; set; }
            [JsonPropertyName("release_dates")] public ReleaseDatesResponse? ReleaseDates { get; set; }
        }

        private sealed class NamedItem
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
        }

        private sealed class CreditsResponse
        {
            [JsonPropertyName("crew")] public List<CrewMember>? Crew { get; set; }
        }

        private sealed class CrewMember
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("job")] public string? Job { get; set; }
        }

        private sealed class VideosResponse
        {
            [JsonPropertyName("results")] public List<Video>? Results { get; set; }
        }

        private sealed class Video
        {
            [JsonPropertyName("key")] public string? Key { get; set; }
            [JsonPropertyName("site")] public string? Site { get; set; }
            [JsonPropertyName("type")] public string? Type { get; set; }
            [JsonPropertyName("official")] public bool Official { get; set; }
        }

        private sealed class ReleaseDatesResponse
        {
            [JsonPropertyName("results")] public List<CountryReleases>? Results { get; set; }
        }

        private sealed class CountryReleases
        {
            [JsonPropertyName("iso_3166_1")] public string? Country { get; set; }
            [JsonPropertyName("release_dates")] public List<ReleaseDateItem>? ReleaseDates { get; set; }
        }

        private sealed class ReleaseDateItem
        {
            [JsonPropertyName("certification")] public string? Certification { get; set; }
            [JsonPropertyName("type")] public int Type { get; set; }
        }
    }
}

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace MoviesAdmin.Services.MovieLookup
{
    // OMDb (https://www.omdbapi.com): IMDb's catalog data plus the IMDb, Rotten Tomatoes, and
    // Metacritic scores. Neither IMDb nor Rotten Tomatoes offers a public API of its own, so this
    // is the source behind the "IMDb + Rotten Tomatoes" option. Registered as a typed HttpClient.
    public sealed class OmdbLookupProvider : IMovieLookupProvider
    {
        private static readonly Regex ImdbIdPattern = new(@"^tt\d{7,10}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        private readonly HttpClient _http;
        private readonly MovieLookupOptions _options;

        public OmdbLookupProvider(HttpClient http, IOptions<MovieLookupOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public string Key => "omdb";

        public string DisplayName => "IMDb + Rotten Tomatoes (via OMDb)";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.OmdbApiKey);

        public bool IsValidExternalId(string externalId) => ImdbIdPattern.IsMatch(externalId);

        public async Task<IReadOnlyList<MovieLookupResult>> SearchAsync(string query, int? year, CancellationToken cancellationToken)
        {
            var path = $"?type=movie&s={Uri.EscapeDataString(query)}";
            if (year.HasValue)
            {
                path += $"&y={year.Value}";
            }

            var response = await SendAsync<SearchResponse>(path, cancellationToken);
            return (response?.Search ?? new())
                .Where(r => r.ImdbId != null)
                .Take(10)
                .Select(r => new MovieLookupResult(r.ImdbId!, r.Title ?? "(untitled)", LookupText.Year(r.Year), LookupText.Clean(r.Poster)))
                .ToList();
        }

        public async Task<MovieLookupDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
        {
            var movie = await SendAsync<MovieResponse>($"?plot=full&i={Uri.EscapeDataString(externalId)}", cancellationToken);
            if (movie == null)
            {
                return null;
            }

            return new MovieLookupDetails
            {
                Source = DisplayName,
                ExternalId = externalId,
                SourceUrl = $"https://www.imdb.com/title/{externalId}/",
                Title = movie.Title ?? string.Empty,
                Synopsis = LookupText.CleanSynopsis(movie.Plot),
                // e.g. "16 Sep 2005"
                ReleaseDate = DateTime.TryParseExact(LookupText.Clean(movie.Released), "dd MMM yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var released) ? released : null,
                // e.g. "129 min"
                RuntimeMinutes = int.TryParse(LookupText.Clean(movie.Runtime)?.Split(' ')[0], out var minutes) && minutes > 0 ? minutes : null,
                Certification = LookupText.Clean(movie.Rated),
                PosterUrl = LookupText.Clean(movie.Poster),
                DirectorName = LookupText.Clean(movie.Director)?.Split(',')[0].Trim(),
                StudioName = LookupText.Clean(movie.Production),
                GenreNames = SplitList(movie.Genre),
                Ratings = (movie.Ratings ?? new())
                    .Where(r => r.Source != null && r.Value != null)
                    .Select(r => new ExternalRating(r.Source == "Internet Movie Database" ? "IMDb" : r.Source!, r.Value!))
                    .ToList()
            };
        }

        private static List<string> SplitList(string? value) =>
            (LookupText.Clean(value) ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        private async Task<T?> SendAsync<T>(string query, CancellationToken cancellationToken) where T : OmdbResponse
        {
            var key = _options.OmdbApiKey?.Trim() ?? throw new MovieLookupException("OMDb isn't configured.");

            using var response = await _http.GetAsync($"{query}&apikey={Uri.EscapeDataString(key)}", cancellationToken);
            // OMDb answers 401 with a JSON body for bad keys and exhausted daily quotas.
            var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

            if (body?.Response == "True")
            {
                return body;
            }

            var error = body?.Error ?? $"OMDb responded with {(int)response.StatusCode}.";
            // "Movie not found!" / "Incorrect IMDb ID." are just empty results, not failures.
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase) || error.Contains("Incorrect IMDb ID", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw new MovieLookupException($"OMDb: {error}");
        }

        // --- OMDb response shapes (OMDb uses PascalCase names) --------------------------------

        private abstract class OmdbResponse
        {
            public string? Response { get; set; }
            public string? Error { get; set; }
        }

        private sealed class SearchResponse : OmdbResponse
        {
            public List<SearchItem>? Search { get; set; }
        }

        private sealed class SearchItem
        {
            public string? Title { get; set; }
            public string? Year { get; set; }
            [JsonPropertyName("imdbID")] public string? ImdbId { get; set; }
            public string? Poster { get; set; }
        }

        private sealed class MovieResponse : OmdbResponse
        {
            public string? Title { get; set; }
            public string? Rated { get; set; }
            public string? Released { get; set; }
            public string? Runtime { get; set; }
            public string? Genre { get; set; }
            public string? Director { get; set; }
            public string? Plot { get; set; }
            public string? Poster { get; set; }
            public string? Production { get; set; }
            public List<RatingItem>? Ratings { get; set; }
        }

        private sealed class RatingItem
        {
            public string? Source { get; set; }
            public string? Value { get; set; }
        }
    }
}

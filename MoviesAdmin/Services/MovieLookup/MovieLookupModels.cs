namespace MoviesAdmin.Services.MovieLookup
{
    // API keys for the external lookup sources, bound from the "MovieLookup" configuration section.
    // Never committed: in Development they come from user secrets (scripts/setup-secrets.sh copies
    // TMDB_API_KEY / OMDB_API_KEY from .env), elsewhere from MovieLookup__TmdbApiKey etc.
    public class MovieLookupOptions
    {
        public const string SectionName = "MovieLookup";

        // TMDB v3 API key (32 hex chars) or v4 read access token (a JWT); either works.
        public string? TmdbApiKey { get; set; }

        public string? OmdbApiKey { get; set; }
    }

    // One search hit, shown in the form's lookup results list.
    public record MovieLookupResult(string ExternalId, string Title, int? Year, string? PosterUrl);

    // A score from a review site, e.g. ("Rotten Tomatoes", "87%"). Shown next to the lookup result;
    // not stored.
    public record ExternalRating(string Source, string Value);

    // Everything a source knows about one movie, normalized across sources. Names (genres, director,
    // studio) are mapped onto this database's ids by MovieLookupController, not here.
    public class MovieLookupDetails
    {
        public string Source { get; init; } = string.Empty;
        public string ExternalId { get; init; } = string.Empty;
        public string? SourceUrl { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Synopsis { get; init; }
        public DateTime? ReleaseDate { get; init; }
        public int? RuntimeMinutes { get; init; }
        // US certification as published ("PG-13", "R", "Not Rated", ...).
        public string? Certification { get; init; }
        public string? PosterUrl { get; init; }
        public string? TrailerUrl { get; init; }
        public string? DirectorName { get; init; }
        public string? StudioName { get; init; }
        public IReadOnlyList<string> GenreNames { get; init; } = Array.Empty<string>();
        public IReadOnlyList<ExternalRating> Ratings { get; init; } = Array.Empty<ExternalRating>();
    }

    // A source answered, but with an error worth showing the admin (bad API key, quota exceeded).
    public class MovieLookupException : Exception
    {
        public MovieLookupException(string message) : base(message)
        {
        }
    }
}

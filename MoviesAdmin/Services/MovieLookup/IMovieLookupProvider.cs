namespace MoviesAdmin.Services.MovieLookup
{
    // One external movie database the Add/Edit form can pull details from (the form's "Source"
    // dropdown lists every registered provider). Results only pre-fill the form; the admin can
    // change anything before saving.
    public interface IMovieLookupProvider
    {
        // Stable id used in URLs and the dropdown's option values ("tmdb", "omdb").
        string Key { get; }

        string DisplayName { get; }

        // False when its API key isn't configured; the dropdown then shows it disabled.
        bool IsConfigured { get; }

        // Cheap shape check on an id before it's put into an outbound request.
        bool IsValidExternalId(string externalId);

        Task<IReadOnlyList<MovieLookupResult>> SearchAsync(string query, int? year, CancellationToken cancellationToken);

        Task<MovieLookupDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken);
    }

    // Small parsing helpers shared by the providers.
    internal static class LookupText
    {
        // OMDb uses "N/A" for unknown values; both sources sometimes send empty strings.
        public static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) || value.Trim() == "N/A" ? null : value.Trim();

        // Synopsis must fit Movie.Synopsis's rules (no '<' or '>', at most 2000 characters).
        public static string? CleanSynopsis(string? value)
        {
            var text = Clean(value)?.Replace("<", string.Empty).Replace(">", string.Empty);
            return text is { Length: > 2000 } ? text[..1997] + "..." : text;
        }

        public static int? Year(string? value) =>
            value is { Length: >= 4 } && int.TryParse(value[..4], out var year) ? year : null;
    }
}

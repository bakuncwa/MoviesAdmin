namespace MoviesAdmin.Services.PosterStorage
{
    // Where admin-uploaded poster images live. The value SaveAsync returns is stored in
    // Movie.PosterImagePath: an absolute https:// URL for Azure Blob Storage, or a path under
    // wwwroot (e.g. "images/movies/abc.jpg") for local storage. MoviePosterResolver turns either
    // into an <img src>.
    public interface IPosterStorage
    {
        // The file has already passed [PosterFile] (type and size) by the time it gets here.
        Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

        // Best-effort: a missing or foreign poster is ignored, never an error.
        Task DeleteAsync(string? posterImagePath, CancellationToken cancellationToken = default);
    }

    // "PosterStorage" configuration section (user secrets in Development, written from .env by
    // scripts/setup-secrets.sh; PosterStorage__ConnectionString etc. elsewhere). With no connection
    // string, posters are saved under wwwroot/images/movies instead.
    public class PosterStorageOptions
    {
        public const string SectionName = "PosterStorage";

        public string? ConnectionString { get; set; }

        public string ContainerName { get; set; } = "posters";

        public bool UseAzureBlob => !string.IsNullOrWhiteSpace(ConnectionString);
    }

    internal static class PosterFileNames
    {
        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

        // A generated name, so a user-supplied file name never reaches a path or blob name.
        public static string NewName(IFormFile file) =>
            $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";

        public static string ContentType(string fileName) =>
            ContentTypes.TryGetValue(Path.GetExtension(fileName), out var type) ? type : "application/octet-stream";
    }
}

namespace MoviesAdmin.Services.PosterStorage
{
    // Where admin-uploaded poster images live. The value UploadFileAsync returns is stored in
    // Movie.PosterImagePath: the blob's absolute URL for Azure Blob Storage (http://127.0.0.1:10000/
    // devstoreaccount1/posters/... with Azurite, https://... in Azure), or a path under
    // wwwroot (e.g. "images/movies/abc.jpg") for local storage. MoviePosterResolver turns either
    // into an <img src>.
    public interface IPosterStorage
    {
        // Saves the file under fileName (the controller makes it unique: GUID.ext) and returns the
        // poster's URL/path. The file has already passed PosterFileAttribute (type and size).
        Task<string> UploadFileAsync(IFormFile file, string fileName, CancellationToken cancellationToken = default);

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

        public static string ContentType(string fileName) =>
            ContentTypes.TryGetValue(Path.GetExtension(fileName), out var type) ? type : "application/octet-stream";
    }
}

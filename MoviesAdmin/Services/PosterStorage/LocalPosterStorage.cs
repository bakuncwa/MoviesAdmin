namespace MoviesAdmin.Services.PosterStorage
{
    // Saves posters under wwwroot/images/movies. Used when no Azure Blob connection string is
    // configured, and by AzureStorageService to clean up posters uploaded before it was.
    public sealed class LocalPosterStorage : IPosterStorage
    {
        private const string UploadsRelativeFolder = "images/movies";

        private readonly IWebHostEnvironment _environment;

        public LocalPosterStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string fileName, CancellationToken cancellationToken = default)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, UploadsRelativeFolder);
            Directory.CreateDirectory(uploadsFolder);

            await using (var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return $"{UploadsRelativeFolder}/{fileName}";
        }

        public Task DeleteAsync(string? posterImagePath, CancellationToken cancellationToken = default)
        {
            // Absolute URLs belong to blob storage, not to this folder.
            if (string.IsNullOrWhiteSpace(posterImagePath) || Uri.IsWellFormedUriString(posterImagePath, UriKind.Absolute))
            {
                return Task.CompletedTask;
            }

            var fullPath = Path.Combine(_environment.WebRootPath, posterImagePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup; an orphaned file under wwwroot/images/movies is harmless.
            }

            return Task.CompletedTask;
        }
    }
}

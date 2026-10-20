using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace MoviesAdmin.Services.PosterStorage
{
    // Saves posters to an Azure Blob Storage container whose access level is "Blob" (public read
    // access for blobs only): anyone can load a poster by its URL, but nobody can list the
    // container. The blob's URL is what gets stored on the movie, so views link to it directly.
    public sealed class AzureStorageService : IPosterStorage
    {
        private readonly BlobContainerClient _container;
        private readonly LocalPosterStorage _localPosters;
        private readonly ILogger<AzureStorageService> _logger;
        private bool _containerChecked;

        public AzureStorageService(IOptions<PosterStorageOptions> options, LocalPosterStorage localPosters, ILogger<AzureStorageService> logger)
        {
            _container = new BlobContainerClient(options.Value.ConnectionString, options.Value.ContainerName);
            _localPosters = localPosters;
            _logger = logger;
        }

        // Returns the URL of the new blob, e.g. http://127.0.0.1:10000/devstoreaccount1/posters/<guid>.jpg
        // with Azurite in Docker.
        public async Task<string> UploadFileAsync(IFormFile file, string fileName, CancellationToken cancellationToken = default)
        {
            await EnsureContainerAsync(cancellationToken);

            var blobClient = _container.GetBlobClient(fileName);
            var options = new BlobUploadOptions
            {
                // The content type is what lets a browser show the blob inline as an image.
                HttpHeaders = new BlobHttpHeaders { ContentType = PosterFileNames.ContentType(fileName) }
            };

            await using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, options, cancellationToken);
            }

            return blobClient.Uri.ToString(); // the URL of the new file
        }

        public async Task DeleteAsync(string? posterImagePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(posterImagePath))
            {
                return;
            }

            // Posters saved under wwwroot before blob storage was configured.
            if (!Uri.IsWellFormedUriString(posterImagePath, UriKind.Absolute))
            {
                await _localPosters.DeleteAsync(posterImagePath, cancellationToken);
                return;
            }

            // Only ever delete blobs from this container, whatever URL the movie row holds.
            var containerPrefix = _container.Uri.ToString().TrimEnd('/') + "/";
            if (!posterImagePath.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var blobName = Uri.UnescapeDataString(posterImagePath[containerPrefix.Length..]);
                await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                // Best-effort cleanup; an orphaned blob is harmless.
                _logger.LogWarning(ex, "Couldn't delete poster blob {PosterUrl}.", posterImagePath);
            }
        }

        // Creates the container on first use if it doesn't exist yet. An existing container (e.g. one
        // set up in the Azure portal) is left exactly as configured there.
        private async Task EnsureContainerAsync(CancellationToken cancellationToken)
        {
            if (_containerChecked)
            {
                return;
            }

            if (!await _container.ExistsAsync(cancellationToken))
            {
                await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            }

            _containerChecked = true;
        }
    }
}

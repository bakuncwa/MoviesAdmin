using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.Services.MovieLookup;

namespace MoviesAdmin.Controllers
{
    // Backs the "Fill from an online source" panel on the movie Add/Edit form (wwwroot/js/movies.js).
    // Search lists candidates from the chosen source; Details returns one movie already shaped like
    // the form (genre/director/studio names matched to this database's ids), which the browser
    // copies into the inputs. Nothing is saved here: the admin reviews and edits the form, then saves.
    [Authorize(Policy = "RequireAdmin")]
    public class MovieLookupController : Controller
    {
        // TMDB and OMDb name a few genres differently from the seeded list.
        private static readonly Dictionary<string, string> GenreAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Science Fiction"] = "Sci-Fi",
            ["Sci Fi"] = "Sci-Fi",
            ["Action & Adventure"] = "Action"
        };

        private readonly IEnumerable<IMovieLookupProvider> _providers;
        private readonly IGenreRepository _genreRepository;
        private readonly IDirectorRepository _directorRepository;
        private readonly IStudioRepository _studioRepository;
        private readonly ILogger<MovieLookupController> _logger;

        public MovieLookupController(
            IEnumerable<IMovieLookupProvider> providers,
            IGenreRepository genreRepository,
            IDirectorRepository directorRepository,
            IStudioRepository studioRepository,
            ILogger<MovieLookupController> logger)
        {
            _providers = providers;
            _genreRepository = genreRepository;
            _directorRepository = directorRepository;
            _studioRepository = studioRepository;
            _logger = logger;
        }

        // GET: MovieLookup/Search?source=tmdb&q=pride&year=2005
        [HttpGet]
        public Task<IActionResult> Search(
            [Required] string source,
            [Required(ErrorMessage = "Enter a title to search for.")]
            [StringLength(200, ErrorMessage = "Search can be at most {1} characters.")]
            string q,
            [Range(1888, 2100, ErrorMessage = "Year must be between {1} and {2}.")] int? year,
            CancellationToken cancellationToken) =>
            CallProviderAsync(source, async provider => await provider.SearchAsync(q.Trim(), year, cancellationToken));

        // GET: MovieLookup/Details?source=tmdb&id=4348
        [HttpGet]
        public Task<IActionResult> Details([Required] string source, [Required] string id, CancellationToken cancellationToken) =>
            CallProviderAsync(source, async provider =>
            {
                var details = await provider.GetDetailsAsync(id, cancellationToken);
                return details == null ? null : await ToFormValuesAsync(details);
            }, externalId: id);

        // Shared plumbing: resolve the provider, run the call, and turn every failure mode into a
        // JSON { error } the form can show inline.
        private async Task<IActionResult> CallProviderAsync(string source, Func<IMovieLookupProvider, Task<object?>> call, string? externalId = null)
        {
            if (!ModelState.IsValid)
            {
                var message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
                return BadRequest(new { error = message ?? "Invalid lookup request." });
            }

            var provider = _providers.FirstOrDefault(p => p.Key == source);
            if (provider == null)
            {
                return BadRequest(new { error = "Unknown lookup source." });
            }

            if (!provider.IsConfigured)
            {
                return BadRequest(new { error = $"{provider.DisplayName} has no API key configured. See README → Movie lookup." });
            }

            // Checked before the id goes anywhere near an outbound URL.
            if (externalId != null && !provider.IsValidExternalId(externalId))
            {
                return BadRequest(new { error = $"\"{externalId}\" isn't a valid {provider.DisplayName} id." });
            }

            try
            {
                var result = await call(provider);
                return result == null ? NotFound(new { error = "No match found." }) : Json(result);
            }
            catch (MovieLookupException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Movie lookup against {Source} failed.", provider.Key);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = $"Couldn't reach {provider.DisplayName}. Try again in a moment." });
            }
        }

        // Shapes a lookup into the form's field values. Ids are filled only for names that already
        // exist here; anything unmatched is returned by name so the form can mention it.
        private async Task<object> ToFormValuesAsync(MovieLookupDetails details)
        {
            var genres = (await _genreRepository.GetAllAsync()).ToList();
            var matchedGenres = details.GenreNames
                .Select(name => GenreAliases.TryGetValue(name, out var alias) ? alias : name)
                .Select(name => (name, genre: genres.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase))))
                .ToList();

            var director = details.DirectorName == null ? null : await _directorRepository.GetByNameAsync(details.DirectorName);
            var studio = details.StudioName == null ? null : await _studioRepository.GetByNameAsync(details.StudioName);

            return new
            {
                source = details.Source,
                sourceUrl = details.SourceUrl,
                title = details.Title,
                synopsis = details.Synopsis,
                releaseDate = details.ReleaseDate?.ToString("yyyy-MM-dd"),
                runtimeHours = details.RuntimeMinutes / 60,
                runtimeMinutes = details.RuntimeMinutes % 60,
                contentRating = ParseCertification(details.Certification)?.ToString(),
                certification = details.Certification,
                posterUrl = details.PosterUrl,
                trailerUrl = details.TrailerUrl,
                genreIds = matchedGenres.Where(m => m.genre != null).Select(m => m.genre!.Id).Distinct(),
                directorId = director?.Id,
                studioId = studio?.Id,
                unmatched = new
                {
                    genres = matchedGenres.Where(m => m.genre == null).Select(m => m.name),
                    director = director == null ? details.DirectorName : null,
                    studio = studio == null ? details.StudioName : null
                },
                ratings = details.Ratings
            };
        }

        private static ContentRating? ParseCertification(string? certification) => certification?.Trim().ToUpperInvariant() switch
        {
            "G" => ContentRating.G,
            "PG" => ContentRating.PG,
            "PG-13" => ContentRating.PG13,
            "R" => ContentRating.R,
            "NC-17" => ContentRating.NC17,
            _ => null
        };
    }
}

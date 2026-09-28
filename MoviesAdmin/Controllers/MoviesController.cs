using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.ViewModels.Movies;

namespace MoviesAdmin.Controllers
{
    // Admin CRUD for Movies: poster-grid/list Index with a featured hero and async search, Create/Edit as AJAX-loaded modals,
    // Details as an AJAX-loaded modal, Delete confirmed client-side (SweetAlert2 + a 10-second
    // "Undo" window in wwwroot/js/movies.js) before this controller ever removes anything.
    //
    // IAM: gated behind the "Admin" policy set up in Program.cs. The Admin role is seeded by the
    // AddIamRoles migration and granted to the first account that registers.
    [Authorize(Policy = "RequireAdmin")]
    public class MoviesController : Controller
    {
        private const string UploadsRelativeFolder = "images/movies";
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxPosterImageBytes = 5 * 1024 * 1024; // 5 MB

        // Defense-in-depth for the async search box: EF Core already parameterizes this query
        // (SearchAsync builds a LINQ expression, not raw SQL), so this isn't preventing SQL
        // injection so much as rejecting anything outside a safe "movie title" character set
        // before it ever reaches the repository. Compiled + a timeout guards against ReDoS.
        private static readonly Regex SafeSearchTermRegex = new(
            @"^[\p{L}\p{N}\s\-':,.&!?()]{0,200}$",
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

        private readonly IMovieRepository _movieRepository;
        private readonly IGenreRepository _genreRepository;
        private readonly IDirectorRepository _directorRepository;
        private readonly IStudioRepository _studioRepository;
        // Trailer has no queries beyond plain CRUD, so it's resolved through the generic
        // IRepository<T> registration (see Program.cs) rather than a dedicated repository pair.
        private readonly IRepository<Trailer> _trailerRepository;
        private readonly IWebHostEnvironment _environment;

        public MoviesController(
            IMovieRepository movieRepository,
            IGenreRepository genreRepository,
            IDirectorRepository directorRepository,
            IStudioRepository studioRepository,
            IRepository<Trailer> trailerRepository,
            IWebHostEnvironment environment)
        {
            _movieRepository = movieRepository;
            _genreRepository = genreRepository;
            _directorRepository = directorRepository;
            _studioRepository = studioRepository;
            _trailerRepository = trailerRepository;
            _environment = environment;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _movieRepository.SearchAsync(null, null, null, MovieSortOrder.ReleaseDateDesc);
            var rows = movies.Select(MovieListItemViewModel.FromEntity).ToList();
            ViewData["Featured"] = PickFeatured(rows);
            return View(rows);
        }

        // GET: Movies/Hero
        // Re-rendered by movies.js after a create/edit/delete so the featured banner never shows a
        // stale or deleted movie.
        [HttpGet]
        public async Task<IActionResult> Hero()
        {
            var movies = await _movieRepository.SearchAsync(null, null, null, MovieSortOrder.ReleaseDateDesc);
            var rows = movies.Select(MovieListItemViewModel.FromEntity).ToList();
            return PartialView("_MovieHero", PickFeatured(rows));
        }

        // The hero banner features the most recently added movie (highest Id).
        private static MovieListItemViewModel? PickFeatured(IEnumerable<MovieListItemViewModel> rows) =>
            rows.OrderByDescending(m => m.Id).FirstOrDefault();

        // GET: Movies/Search?q=...
        // Async search bar: returns the catalog partial (poster grid + list table) so the client can
        // swap #movie-catalog's content.
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            q = q?.Trim();

            if (!string.IsNullOrEmpty(q) && !SafeSearchTermRegex.IsMatch(q))
            {
                ModelState.AddModelError(nameof(q), "Search term can only contain letters, numbers, spaces, and common punctuation (-':,.&!?()), up to 200 characters.");
                return BadRequest(ModelState);
            }

            var movies = await _movieRepository.SearchAsync(q, null, null, MovieSortOrder.ReleaseDateDesc);
            var rows = movies.Select(MovieListItemViewModel.FromEntity).ToList();
            return PartialView("_MovieCatalog", rows);
        }

        // GET: Movies/CreateModal
        [HttpGet]
        public async Task<IActionResult> CreateModal()
        {
            var model = new MovieFormViewModel();
            await PopulateFormOptionsAsync(model);
            return PartialView("_MovieFormModal", model);
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await FormValidationFailedAsync(model, existingPosterImagePath: null);
            }

            var movie = new Movie
            {
                Title = model.Title,
                Synopsis = model.Synopsis,
                ReleaseDate = model.ReleaseDate,
                RuntimeMinutes = model.TotalRuntimeMinutes,
                ContentRating = model.ContentRating,
                PosterUrl = model.PosterUrl,
                DirectorId = model.DirectorId,
                StudioId = model.StudioId
            };

            if (model.PosterImageFile != null)
            {
                var (success, pathOrError) = await TrySavePosterImageAsync(model.PosterImageFile);
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.PosterImageFile), pathOrError);
                    return await FormValidationFailedAsync(model, existingPosterImagePath: null);
                }

                movie.PosterImagePath = pathOrError;
            }

            AssignGenres(movie, model.SelectedGenreIds);

            if (!string.IsNullOrWhiteSpace(model.TrailerUrl))
            {
                // Attached via navigation rather than a separate AddAsync call: EF inserts it
                // alongside the movie (and fills in its MovieId) when SaveChangesAsync runs below.
                movie.Trailer = new Trailer { YouTubeUrl = model.TrailerUrl };
            }

            await _movieRepository.AddAsync(movie);
            await _movieRepository.SaveChangesAsync();

            return Json(new { success = true, id = movie.Id });
        }

        // GET: Movies/EditModal/5
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            var model = new MovieFormViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Synopsis = movie.Synopsis,
                ReleaseDate = movie.ReleaseDate,
                ContentRating = movie.ContentRating,
                PosterUrl = movie.PosterUrl,
                ExistingPosterImagePath = movie.PosterImagePath,
                TrailerUrl = movie.Trailer?.YouTubeUrl,
                DirectorId = movie.DirectorId,
                StudioId = movie.StudioId,
                SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList()
            };
            model.SetRuntime(movie.RuntimeMinutes);
            await PopulateFormOptionsAsync(model);

            return PartialView("_MovieFormModal", model);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return await FormValidationFailedAsync(model, movie.PosterImagePath);
            }

            movie.Title = model.Title;
            movie.Synopsis = model.Synopsis;
            movie.ReleaseDate = model.ReleaseDate;
            movie.RuntimeMinutes = model.TotalRuntimeMinutes;
            movie.ContentRating = model.ContentRating;
            movie.PosterUrl = model.PosterUrl;
            movie.DirectorId = model.DirectorId;
            movie.StudioId = model.StudioId;

            if (model.PosterImageFile != null)
            {
                var (success, pathOrError) = await TrySavePosterImageAsync(model.PosterImageFile);
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.PosterImageFile), pathOrError);
                    return await FormValidationFailedAsync(model, movie.PosterImagePath);
                }

                DeleteUploadedPosterIfAny(movie.PosterImagePath);
                movie.PosterImagePath = pathOrError;
            }

            // Modeled after the standard EF Core many-to-many-via-explicit-join-entity tutorial
            // pattern (e.g. the Contoso University CourseAssignment sample): clear + rebuild the
            // join collection rather than diffing it entry by entry. MovieGenre is a required
            // dependent with a composite key (no independent Id), so entries removed from this
            // tracked collection are deleted by SaveChangesAsync, and re-added entries are inserted.
            movie.MovieGenres.Clear();
            AssignGenres(movie, model.SelectedGenreIds);

            // Trailer is one-to-one and optional: clearing the field removes it, filling an empty
            // field creates it, and editing it in place just updates the URL.
            if (string.IsNullOrWhiteSpace(model.TrailerUrl))
            {
                if (movie.Trailer != null)
                {
                    _trailerRepository.Remove(movie.Trailer);
                    movie.Trailer = null;
                }
            }
            else if (movie.Trailer == null)
            {
                movie.Trailer = new Trailer { MovieId = movie.Id, YouTubeUrl = model.TrailerUrl };
            }
            else
            {
                movie.Trailer.YouTubeUrl = model.TrailerUrl;
            }

            _movieRepository.Update(movie);
            await _movieRepository.SaveChangesAsync();

            return Json(new { success = true, id = movie.Id });
        }

        // GET: Movies/DetailsModal/5
        // View modal: poster image on the left, movie details on the right.
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            return PartialView("_MovieDetailsModal", MovieDetailsViewModel.FromEntity(movie));
        }

        // GET: Movies/TrailerEmbed/5
        // Lazily loads the trailer <iframe> only when the admin clicks "Watch Trailer" in the
        // Details modal, rather than embedding a YouTube player for every row up front.
        [HttpGet]
        public async Task<IActionResult> TrailerEmbed(int id)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            var embedUrl = movie?.Trailer?.EmbedUrl;
            if (embedUrl == null)
            {
                return NotFound();
            }

            return PartialView("_TrailerEmbed", embedUrl);
        }

        // POST: Movies/Delete/5
        // This actually deletes. wwwroot/js/movies.js shows a SweetAlert2 confirmation and then a
        // 10-second "Undo" toast BEFORE ever calling this endpoint — the row is only removed
        // server-side once that countdown elapses without the user clicking Undo, so nothing is
        // destroyed during the grace period.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
            {
                // Already gone (e.g. a second Undo-window delete completed first) — not an error
                // from the client's point of view, since the end state it wanted is achieved.
                return Json(new { success = true });
            }

            DeleteUploadedPosterIfAny(movie.PosterImagePath);

            _movieRepository.Remove(movie);
            await _movieRepository.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task<IActionResult> FormValidationFailedAsync(MovieFormViewModel model, string? existingPosterImagePath)
        {
            model.ExistingPosterImagePath = existingPosterImagePath;
            await PopulateFormOptionsAsync(model);
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView("_MovieFormModal", model);
        }

        private static void AssignGenres(Movie movie, IEnumerable<int> genreIds)
        {
            foreach (var genreId in genreIds.Distinct())
            {
                movie.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = genreId });
            }
        }

        // Fills the Genre/Director/Studio pickers the form needs to render, whether this is a
        // fresh Create/Edit load or a re-render after a validation failure.
        private async Task PopulateFormOptionsAsync(MovieFormViewModel model)
        {
            var genres = await _genreRepository.GetAllAsync();
            model.AvailableGenres = genres
                .OrderBy(g => g.Name)
                .Select(g => new NamedOptionViewModel { Id = g.Id, Name = g.Name })
                .ToList();

            var directors = await _directorRepository.GetAllAsync();
            model.AvailableDirectors = directors
                .OrderBy(d => d.Name)
                .Select(d => new NamedOptionViewModel { Id = d.Id, Name = d.Name })
                .ToList();

            var studios = await _studioRepository.GetAllAsync();
            model.AvailableStudios = studios
                .OrderBy(s => s.Name)
                .Select(s => new NamedOptionViewModel { Id = s.Id, Name = s.Name })
                .ToList();
        }

        private async Task<(bool Success, string PathOrError)> TrySavePosterImageAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                return (false, "Poster image must be a .jpg, .jpeg, .png, .gif, or .webp file.");
            }

            if (file.Length == 0 || file.Length > MaxPosterImageBytes)
            {
                return (false, "Poster image must be larger than 0 bytes and no more than 5 MB.");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, UploadsRelativeFolder);
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, $"{UploadsRelativeFolder}/{fileName}");
        }

        private void DeleteUploadedPosterIfAny(string? posterImagePath)
        {
            if (string.IsNullOrWhiteSpace(posterImagePath))
            {
                return;
            }

            var fullPath = Path.Combine(_environment.WebRootPath, posterImagePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup; an orphaned file under wwwroot/images/movies is harmless.
            }
        }
    }
}

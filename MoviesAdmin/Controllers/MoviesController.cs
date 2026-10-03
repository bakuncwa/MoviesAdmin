using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.Services.MovieLookup;
using MoviesAdmin.Validation;
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

        private readonly IMovieRepository _movieRepository;
        private readonly IGenreRepository _genreRepository;
        private readonly IDirectorRepository _directorRepository;
        private readonly IStudioRepository _studioRepository;
        // Trailer has no queries beyond plain CRUD, so it's resolved through the generic
        // IRepository<T> registration (see Program.cs) rather than a dedicated repository pair.
        private readonly IRepository<Trailer> _trailerRepository;
        private readonly IEnumerable<IMovieLookupProvider> _lookupProviders;
        private readonly IWebHostEnvironment _environment;

        public MoviesController(
            IMovieRepository movieRepository,
            IGenreRepository genreRepository,
            IDirectorRepository directorRepository,
            IStudioRepository studioRepository,
            IRepository<Trailer> trailerRepository,
            IEnumerable<IMovieLookupProvider> lookupProviders,
            IWebHostEnvironment environment)
        {
            _movieRepository = movieRepository;
            _genreRepository = genreRepository;
            _directorRepository = directorRepository;
            _studioRepository = studioRepository;
            _trailerRepository = trailerRepository;
            _lookupProviders = lookupProviders;
            _environment = environment;
        }

        // GET: Movies?q=&genreId=&year=&rating=&sort=
        // Same query parameters as Search, so a filtered/sorted catalog survives a reload (movies.js
        // mirrors the toolbar into the URL). A hand-edited, invalid query falls back to the defaults.
        public async Task<IActionResult> Index([FromQuery] MovieCatalogQuery query)
        {
            if (!ModelState.IsValid)
            {
                query = new MovieCatalogQuery();
            }

            var featured = await _movieRepository.SearchAsync(null, null, null, null, MovieSortOrder.ReleaseDateDesc);
            ViewData["Featured"] = PickFeatured(featured.Select(MovieListItemViewModel.FromEntity));

            var genres = await _genreRepository.GetAllAsync();
            var model = new MovieCatalogViewModel
            {
                Query = query,
                Movies = await SearchCatalogAsync(query),
                GenreOptions = genres
                    .OrderBy(g => g.Name)
                    .Select(g => new NamedOptionViewModel { Id = g.Id, Name = g.Name })
                    .ToList(),
                YearOptions = await _movieRepository.GetReleaseYearsAsync()
            };

            return View(model);
        }

        // GET: Movies/Hero
        // Re-rendered by movies.js after a create/edit/delete so the featured banner never shows a
        // stale or deleted movie.
        [HttpGet]
        public async Task<IActionResult> Hero()
        {
            var movies = await _movieRepository.SearchAsync(null, null, null, null, MovieSortOrder.ReleaseDateDesc);
            return PartialView("_MovieHero", PickFeatured(movies.Select(MovieListItemViewModel.FromEntity)));
        }

        // The hero banner features the most recently added movie (highest Id).
        private static MovieListItemViewModel? PickFeatured(IEnumerable<MovieListItemViewModel> rows) =>
            rows.OrderByDescending(m => m.Id).FirstOrDefault();

        // GET: Movies/Search?q=&genreId=&year=&rating=&sort=
        // Async toolbar (search box, filters, sort): returns the catalog partial (poster grid + list
        // table) so the client can swap #movie-catalog's content. Validation rules for each
        // parameter live on MovieCatalogQuery.
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] MovieCatalogQuery query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var model = new MovieCatalogViewModel { Query = query, Movies = await SearchCatalogAsync(query) };
            return PartialView("_MovieCatalog", model);
        }

        private async Task<List<MovieListItemViewModel>> SearchCatalogAsync(MovieCatalogQuery query)
        {
            var movies = await _movieRepository.SearchAsync(query.Q?.Trim(), query.GenreId, query.Year, query.Rating, query.Sort);
            return movies.Select(MovieListItemViewModel.FromEntity).ToList();
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
            await ValidateReferencesAsync(model);
            if (!ModelState.IsValid)
            {
                return await FormValidationFailedAsync(model, existingPosterImagePath: null);
            }

            var movie = new Movie
            {
                Title = model.Title,
                Synopsis = model.Synopsis,
                ReleaseDate = model.ReleaseDate!.Value,
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

            await ValidateReferencesAsync(model);
            if (!ModelState.IsValid)
            {
                return await FormValidationFailedAsync(model, movie.PosterImagePath);
            }

            movie.Title = model.Title;
            movie.Synopsis = model.Synopsis;
            movie.ReleaseDate = model.ReleaseDate!.Value;
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

        // The dropdowns only offer existing ids, but a tampered post could send any number; catch
        // that as a validation message here instead of as a foreign-key exception on save.
        private async Task ValidateReferencesAsync(MovieFormViewModel model)
        {
            if (model.DirectorId.HasValue && await _directorRepository.GetByIdAsync(model.DirectorId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.DirectorId), "That director no longer exists. Choose another from the list.");
            }

            if (model.StudioId.HasValue && await _studioRepository.GetByIdAsync(model.StudioId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.StudioId), "That studio no longer exists. Choose another from the list.");
            }

            if (model.SelectedGenreIds.Count > 0)
            {
                var knownGenreIds = (await _genreRepository.GetAllAsync()).Select(g => g.Id).ToHashSet();
                if (!model.SelectedGenreIds.All(knownGenreIds.Contains))
                {
                    ModelState.AddModelError(nameof(model.SelectedGenreIds), "One of the selected genres no longer exists. Reload the form and try again.");
                }
            }
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

            model.LookupSources = _lookupProviders
                .Select(p => new LookupSourceViewModel { Key = p.Key, Name = p.DisplayName, IsAvailable = p.IsConfigured })
                .ToList();
        }

        // Type and size are already enforced by [PosterFile] on MovieFormViewModel; this re-checks
        // because it's the last line before a user-supplied file is written under wwwroot.
        private async Task<(bool Success, string PathOrError)> TrySavePosterImageAsync(IFormFile file)
        {
            var posterFile = new PosterFileAttribute();
            if (!posterFile.IsValid(file))
            {
                return (false, posterFile.ErrorMessage!);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

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

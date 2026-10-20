// using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.Services.MovieLookup;
using MoviesAdmin.Services.PosterStorage;
using MoviesAdmin.Validation;
using MoviesAdmin.ViewModels.Movies;

namespace MoviesAdmin.Controllers
{
    // Admin CRUD for Movies: a records table with async search/filter/sort, Create/Edit/Details as
    // AJAX-loaded modals, Delete confirmed client-side (SweetAlert2 + a 10-second "Undo" window in
    // wwwroot/js/movies.js) before this controller ever removes anything.
    //
    // Each partial lives in Views/Movies/Partials/ under the name of the action that renders it:
    //   GET  Movies              -> Index.cshtml (+ Partials/_Index)
    //   GET  Movies/Search       -> Partials/_Index      (async refresh of the records table)
    //   GET  Movies/Create       -> Partials/_Create     POST Movies/Create
    //   GET  Movies/Edit/5       -> Partials/_Edit       POST Movies/Edit/5
    //   GET  Movies/Details/5    -> Partials/_Details
    //   GET  Movies/Trailer/5    -> Partials/_Trailer
    //   POST Movies/Delete/5
    //
    // IAM: login/logout is switched off for now, so CRUD is open to anyone. To restore it, uncomment
    // this attribute along with the Identity setup in Program.cs and AccountController.
    // [Authorize(Policy = "RequireAdmin")]
    public class MoviesController : Controller
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IGenreRepository _genreRepository;
        private readonly IDirectorRepository _directorRepository;
        private readonly IStudioRepository _studioRepository;
        // Trailer has no queries beyond plain CRUD, so it's resolved through the generic
        // IRepository<T> registration (see Program.cs) rather than a dedicated repository pair.
        private readonly IRepository<Trailer> _trailerRepository;
        private readonly IEnumerable<IMovieLookupProvider> _lookupProviders;
        private readonly IPosterStorage _posterStorage;

        public MoviesController(
            IMovieRepository movieRepository,
            IGenreRepository genreRepository,
            IDirectorRepository directorRepository,
            IStudioRepository studioRepository,
            IRepository<Trailer> trailerRepository,
            IEnumerable<IMovieLookupProvider> lookupProviders,
            IPosterStorage posterStorage)
        {
            _movieRepository = movieRepository;
            _genreRepository = genreRepository;
            _directorRepository = directorRepository;
            _studioRepository = studioRepository;
            _trailerRepository = trailerRepository;
            _lookupProviders = lookupProviders;
            _posterStorage = posterStorage;
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

        // GET: Movies/Search?q=&genreId=&year=&rating=&sort=
        // Async toolbar (search box, filters, sort): returns the records table partial so the client
        // can swap #movie-catalog's content. Validation rules for each
        // parameter live on MovieCatalogQuery.
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] MovieCatalogQuery query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var model = new MovieCatalogViewModel { Query = query, Movies = await SearchCatalogAsync(query) };
            return PartialView("Partials/_Index", model);
        }

        private async Task<List<MovieListItemViewModel>> SearchCatalogAsync(MovieCatalogQuery query)
        {
            var movies = await _movieRepository.SearchAsync(query.Q?.Trim(), query.GenreId, query.Year, query.Rating, query.Sort);
            return movies.Select(MovieListItemViewModel.FromEntity).ToList();
        }

        // GET: Movies/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new MovieFormViewModel();
            await PopulateViewBagAsync(model);
            return PartialView("Partials/_Create", model);
        }

        // POST: Movies/Create
        // The poster file is posted as its own ImageUpload parameter (the file input's name);
        // [PosterFile] checks its type and size like any other validation attribute.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model, [PosterFile] IFormFile? ImageUpload)
        {
            // Validate if the poster is missing (a poster URL filled in from an online source counts)
            if (ImageUpload == null && string.IsNullOrWhiteSpace(model.PosterUrl))
            {
                // Add a validation error
                ModelState.AddModelError("ImageUpload", "Poster image file is required.");
            }

            await ValidateReferencesAsync(model);
            if (!ModelState.IsValid) // validate input
            {
                return await FormValidationFailedAsync(model);
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

            if (ImageUpload != null)
            {
                // Upload file to Azure Storage (after input validation occurs) and set the
                // image URL in the movie record
                movie.PosterImagePath = await UploadPosterAsync(ImageUpload);
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

        // GET: Movies/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
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
                PosterImagePath = movie.PosterImagePath,
                TrailerUrl = movie.Trailer?.YouTubeUrl,
                DirectorId = movie.DirectorId,
                StudioId = movie.StudioId,
                SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList()
            };
            model.SetRuntime(movie.RuntimeMinutes);
            await PopulateViewBagAsync(model);

            return PartialView("Partials/_Edit", model);
        }

        // POST: Movies/Edit/5
        // PosterImagePath comes back in a hidden field; the existing poster is kept unless a new
        // ImageUpload replaces it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieFormViewModel model, [PosterFile] IFormFile? ImageUpload)
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

            // The saved poster always comes from the database, never from the posted hidden field.
            model.PosterImagePath = movie.PosterImagePath;

            await ValidateReferencesAsync(model);
            if (!ModelState.IsValid)
            {
                return await FormValidationFailedAsync(model);
            }

            movie.Title = model.Title;
            movie.Synopsis = model.Synopsis;
            movie.ReleaseDate = model.ReleaseDate!.Value;
            movie.RuntimeMinutes = model.TotalRuntimeMinutes;
            movie.ContentRating = model.ContentRating;
            movie.PosterUrl = model.PosterUrl;
            movie.DirectorId = model.DirectorId;
            movie.StudioId = model.StudioId;

            if (ImageUpload != null)
            {
                // Replace the poster: upload the new file, then remove the old blob/file.
                var oldPosterImagePath = movie.PosterImagePath;
                movie.PosterImagePath = await UploadPosterAsync(ImageUpload);
                await _posterStorage.DeleteAsync(oldPosterImagePath);
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

        // GET: Movies/Details/5
        // View modal: poster image on the left, movie details on the right.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            return PartialView("Partials/_Details", MovieDetailsViewModel.FromEntity(movie));
        }

        // GET: Movies/Trailer/5
        // Lazily loads the trailer <iframe> only when the admin clicks "Watch Trailer" in the
        // Details modal, rather than embedding a YouTube player for every row up front.
        [HttpGet]
        public async Task<IActionResult> Trailer(int id)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            var embedUrl = movie?.Trailer?.EmbedUrl;
            if (embedUrl == null)
            {
                return NotFound();
            }

            return PartialView("Partials/_Trailer", embedUrl);
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

            await _posterStorage.DeleteAsync(movie.PosterImagePath);

            _movieRepository.Remove(movie);
            await _movieRepository.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task<IActionResult> FormValidationFailedAsync(MovieFormViewModel model)
        {
            await PopulateViewBagAsync(model);
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView(model.IsEdit ? "Partials/_Edit" : "Partials/_Create", model);
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

        // Fills the Director/Studio dropdowns (SelectLists, used with asp-items), the Genre checkboxes
        // and the lookup sources the form needs to render, whether this is a fresh Create/Edit load
        // or a re-render after a validation failure.
        private async Task PopulateViewBagAsync(MovieFormViewModel model)
        {
            var directors = await _directorRepository.GetAllAsync();
            ViewBag.DirectorId = new SelectList(directors.OrderBy(d => d.Name), "Id", "Name", model.DirectorId);

            var studios = await _studioRepository.GetAllAsync();
            ViewBag.StudioId = new SelectList(studios.OrderBy(s => s.Name), "Id", "Name", model.StudioId);

            var genres = await _genreRepository.GetAllAsync();
            ViewBag.Genres = genres.OrderBy(g => g.Name).ToList();

            ViewBag.LookupSources = _lookupProviders
                .Select(p => new LookupSourceViewModel { Key = p.Key, Name = p.DisplayName, IsAvailable = p.IsConfigured })
                .ToList();
        }

        // Saves the poster under a unique filename and returns its URL (Azure Blob Storage) or
        // wwwroot path; ImageUpload has already passed [PosterFile] by the time it gets here.
        private async Task<string> UploadPosterAsync(IFormFile imageUpload)
        {
            // Make a unique filename (GUID.ext)
            string extension = Path.GetExtension(imageUpload.FileName).ToLowerInvariant(); // includes the leading dot, e.g. ".jpg"
            string newFileName = $"{Guid.NewGuid()}{extension}";

            // Upload file to Azure Storage
            return await _posterStorage.UploadFileAsync(imageUpload, newFileName, HttpContext.RequestAborted);
        }
    }
}

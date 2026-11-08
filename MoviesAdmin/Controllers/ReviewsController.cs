using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.Services.ReviewText;
using MoviesAdmin.ViewModels.Reviews;

namespace MoviesAdmin.Controllers
{
    // Critics' reviews. Every query goes through IReviewRepository's *ForCritic methods with
    // User.GetUserId(), so a critic can only list, view, edit or delete their own reviews; asking
    // for anyone else's review id is a 404, same as one that doesn't exist.
    //
    //   GET  Reviews                     my reviews (drafts and published)
    //   GET  Reviews/Create?movieId=5    POST Reviews/Create     (save as draft, or publish now)
    //   GET  Reviews/Details/5
    //   GET  Reviews/Edit/5              POST Reviews/Edit/5     (drafts only)
    //   POST Reviews/Publish/5                                   (drafts only; locks the review)
    //   GET  Reviews/Delete/5            POST Reviews/Delete/5   (drafts only)
    [Authorize(Roles = Roles.Critic)]
    public class ReviewsController : Controller
    {
        // The name of the submit button that publishes instead of saving a draft.
        private const string PublishAction = "publish";

        private readonly IReviewRepository _reviewRepository;
        private readonly IMovieRepository _movieRepository;

        public ReviewsController(IReviewRepository reviewRepository, IMovieRepository movieRepository)
        {
            _reviewRepository = reviewRepository;
            _movieRepository = movieRepository;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            return View(await _reviewRepository.GetByCriticAsync(User.GetUserId()));
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // GET: Reviews/Create?movieId=5
        // One review per movie: if this critic already reviewed the movie, open that review instead.
        public async Task<IActionResult> Create(int? movieId)
        {
            if (movieId.HasValue)
            {
                var existing = await _reviewRepository.GetByMovieForCriticAsync(movieId.Value, User.GetUserId());
                if (existing != null)
                {
                    TempData["ReviewMessage"] = "You've already reviewed this movie.";
                    return RedirectToAction(nameof(Details), new { id = existing.Id });
                }
            }

            var model = new ReviewFormViewModel { MovieId = movieId };
            await PopulateMoviesAsync(model.MovieId);
            return View(model);
        }

        // POST: Reviews/Create
        // submit=publish publishes straight away; anything else saves a draft.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewFormViewModel model, string? submit)
        {
            var userId = User.GetUserId();
            CleanReviewText(model);

            if (model.MovieId.HasValue)
            {
                if (await _movieRepository.GetByIdAsync(model.MovieId.Value) == null)
                {
                    ModelState.AddModelError(nameof(model.MovieId), "That movie no longer exists. Choose another from the list.");
                }
                else if (await _reviewRepository.GetByMovieForCriticAsync(model.MovieId.Value, userId) != null)
                {
                    ModelState.AddModelError(nameof(model.MovieId), "You've already reviewed this movie. Each critic can review a movie once.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateMoviesAsync(model.MovieId);
                return View(model);
            }

            var publish = submit == PublishAction;
            var review = new Review
            {
                MovieId = model.MovieId!.Value,
                CreatedBy = userId,
                CriticName = User.Identity?.Name,
                Rating = model.Rating!.Value,
                Text = model.Text,
                IsPublished = publish,
                PublishedAt = publish ? DateTime.UtcNow : null
            };

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            TempData["ReviewMessage"] = publish ? "Review published." : "Draft saved.";
            return RedirectToAction(nameof(Details), new { id = review.Id });
        }

        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            if (review.IsPublished)
            {
                return PublishedIsLocked(review);
            }

            return View(new ReviewFormViewModel
            {
                Id = review.Id,
                MovieId = review.MovieId,
                MovieTitle = review.Movie.Title,
                Rating = review.Rating,
                Text = review.Text
            });
        }

        // POST: Reviews/Edit/5
        // The movie can't be changed: the posted MovieId is ignored in favour of the saved one.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReviewFormViewModel model, string? submit)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            if (review.IsPublished)
            {
                return PublishedIsLocked(review);
            }

            model.MovieId = review.MovieId;
            model.MovieTitle = review.Movie.Title;
            ModelState.Remove(nameof(model.MovieId));
            CleanReviewText(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            review.Rating = model.Rating!.Value;
            review.Text = model.Text;
            review.UpdatedAt = DateTime.UtcNow;

            var publish = submit == PublishAction;
            if (publish)
            {
                review.IsPublished = true;
                review.PublishedAt = DateTime.UtcNow;
            }

            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();

            TempData["ReviewMessage"] = publish ? "Review published." : "Draft saved.";
            return RedirectToAction(nameof(Details), new { id = review.Id });
        }

        // POST: Reviews/Publish/5
        // One-way: once published, Edit and Delete refuse the review from then on.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            if (review.IsPublished)
            {
                return PublishedIsLocked(review);
            }

            review.IsPublished = true;
            review.PublishedAt = DateTime.UtcNow;
            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();

            TempData["ReviewMessage"] = "Review published.";
            return RedirectToAction(nameof(Details), new { id = review.Id });
        }

        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            if (review.IsPublished)
            {
                return PublishedIsLocked(review);
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            if (review.IsPublished)
            {
                return PublishedIsLocked(review);
            }

            _reviewRepository.Remove(review);
            await _reviewRepository.SaveChangesAsync();

            TempData["ReviewMessage"] = "Draft deleted.";
            return RedirectToAction(nameof(Index));
        }

        // Sanitizes the editor's HTML in place (only Quill's formatting survives) and checks the
        // requirement's limits on what the reader actually sees: some text, at most 1000 characters.
        private void CleanReviewText(ReviewFormViewModel model)
        {
            model.Text = ReviewHtml.Sanitize(model.Text);
            var length = ReviewHtml.VisibleLength(model.Text);
            if (ModelState.GetFieldValidationState(nameof(model.Text)) == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
            {
                // [Required] already reported a blank post; don't repeat it.
            }
            else if (length == 0)
            {
                ModelState.AddModelError(nameof(model.Text), "Review text is required.");
            }
            else if (length > ReviewHtml.MaxTextLength)
            {
                ModelState.AddModelError(nameof(model.Text), $"Review text can be at most {ReviewHtml.MaxTextLength} characters (this one has {length}).");
            }
        }

        // Edit/Delete/Publish on a published review: back to its Details page with an explanation.
        private RedirectToActionResult PublishedIsLocked(Review review)
        {
            TempData["ReviewMessage"] = "This review is published, so it can no longer be changed or deleted.";
            return RedirectToAction(nameof(Details), new { id = review.Id });
        }

        // Movie dropdown for Create: every movie this critic hasn't reviewed yet.
        private async Task PopulateMoviesAsync(int? selectedMovieId)
        {
            var reviewedMovieIds = (await _reviewRepository.GetByCriticAsync(User.GetUserId()))
                .Select(r => r.MovieId)
                .ToHashSet();
            var movies = (await _movieRepository.GetAllAsync())
                .Where(m => !reviewedMovieIds.Contains(m.Id))
                .OrderBy(m => m.Title)
                .Select(m => new { m.Id, Title = $"{m.Title} ({m.ReleaseDate.Year})" });

            ViewBag.MovieId = new SelectList(movies, "Id", "Title", selectedMovieId);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    // Write/Edit are pages; View (Details) and Delete open as modals on My Reviews, loaded by
    // wwwroot/js/my-reviews.js. Everything that finishes with a review ends up back on My Reviews
    // with that review's View modal open (TempData["OpenReview"]).
    //
    //   GET  Reviews                     my reviews (drafts and published)
    //   GET  Reviews/Create?movieId=5    POST Reviews/Create     (page; save as draft, or publish now)
    //   GET  Reviews/Details/5                                   (modal)
    //   GET  Reviews/Edit/5              POST Reviews/Edit/5     (page; drafts only)
    //   POST Reviews/Publish/5                                   (drafts only; locks the review)
    //   GET  Reviews/Delete/5            POST Reviews/Delete/5   (modal; drafts only)
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
        // The View modal's content. Opened directly (a bookmark, a typed URL), it shows My Reviews
        // with the same modal open instead.
        public async Task<IActionResult> Details(int id)
        {
            var review = await _reviewRepository.GetByIdForCriticAsync(id, User.GetUserId());
            if (review == null)
            {
                return NotFound();
            }

            if (!IsModalRequest())
            {
                return ShowOnMyReviews(review.Id);
            }

            return PartialView("_DetailsModal", review);
        }

        // GET: Reviews/Create?movieId=5
        // Opened from a movie's Review button, so the movie is fixed: the form shows it read-only.
        // One review per movie: if this critic already reviewed the movie, open that review instead.
        public async Task<IActionResult> Create(int? movieId)
        {
            if (!movieId.HasValue)
            {
                return RedirectToAction("Index", "Movies");
            }

            var movie = await _movieRepository.GetByIdAsync(movieId.Value);
            if (movie == null)
            {
                return NotFound();
            }

            var existing = await _reviewRepository.GetByMovieForCriticAsync(movie.Id, User.GetUserId());
            if (existing != null)
            {
                TempData["ReviewMessage"] = "You've already reviewed this movie.";
                return ShowOnMyReviews(existing.Id);
            }

            return View(new ReviewFormViewModel { MovieId = movie.Id, MovieTitle = MovieTitle(movie) });
        }

        // POST: Reviews/Create
        // submit=publish publishes straight away; anything else saves a draft.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewFormViewModel model, string? submit)
        {
            var userId = User.GetUserId();
            CleanReviewText(model);

            var movie = model.MovieId.HasValue ? await _movieRepository.GetByIdAsync(model.MovieId.Value) : null;
            if (movie == null)
            {
                return NotFound();
            }

            if (await _reviewRepository.GetByMovieForCriticAsync(movie.Id, userId) != null)
            {
                ModelState.AddModelError(string.Empty, "You've already reviewed this movie. Each critic can review a movie once.");
            }

            if (!ModelState.IsValid)
            {
                model.MovieTitle = MovieTitle(movie);
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
            return ShowOnMyReviews(review.Id);
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
                MovieTitle = MovieTitle(review.Movie),
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
            model.MovieTitle = MovieTitle(review.Movie);
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
            return ShowOnMyReviews(review.Id);
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
            return ShowOnMyReviews(review.Id);
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

            if (!IsModalRequest())
            {
                return ShowOnMyReviews(review.Id);
            }

            return PartialView("_DeleteModal", review);
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

        // Edit/Delete/Publish on a published review: back to its View modal with an explanation.
        private RedirectToActionResult PublishedIsLocked(Review review)
        {
            TempData["ReviewMessage"] = "This review is published, so it can no longer be changed or deleted.";
            return ShowOnMyReviews(review.Id);
        }

        // My Reviews, with this review's View modal opened on arrival (wwwroot/js/my-reviews.js).
        private RedirectToActionResult ShowOnMyReviews(int reviewId)
        {
            TempData["OpenReview"] = reviewId;
            return RedirectToAction(nameof(Index));
        }

        // The modals fetch their content with this header; a normal page load doesn't send it.
        private bool IsModalRequest() => Request.Headers.XRequestedWith == "XMLHttpRequest";

        // How the form names the (read-only) movie: "Chungking Express (1994)".
        private static string MovieTitle(Movie movie) => $"{movie.Title} ({movie.ReleaseDate.Year})";
    }
}

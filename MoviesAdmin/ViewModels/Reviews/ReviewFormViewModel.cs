using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.ViewModels.Reviews
{
    // Backs Views/Reviews/Create and Edit. Only what a critic may type is bound here; CreatedBy,
    // IsPublished and the timestamps are always set by ReviewsController, never posted.
    public class ReviewFormViewModel : IValidatableObject
    {
        // 0 on Create; the review being edited on Edit.
        public int Id { get; set; }

        [Required(ErrorMessage = "Choose a movie.")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose a movie.")]
        [Display(Name = "Movie")]
        public int? MovieId { get; set; }

        // Shown on Edit, where the movie can't be changed.
        public string? MovieTitle { get; set; }

        // Set by the star picker (a hidden input): 0 to 5 in half-star steps.
        [Required(ErrorMessage = "Choose a rating.")]
        [Range(0, 5, ErrorMessage = "Rating must be between {1} and {2}.")]
        [Display(Name = "Rating (out of 5)")]
        public decimal? Rating { get; set; }

        // HTML from the Quill editor. The 1000-character limit on the visible text is checked by
        // ReviewsController after sanitizing (ReviewHtml); this cap only stops oversized posts.
        [Required(ErrorMessage = "Review text is required.")]
        [StringLength(20000, ErrorMessage = "Review text is too long.")]
        [Display(Name = "Review")]
        public string Text { get; set; } = string.Empty;

        public bool IsEdit => Id > 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Rating.HasValue && Rating.Value * 2 != decimal.Truncate(Rating.Value * 2))
            {
                yield return new ValidationResult("Rating must be in half-star steps (e.g. 3 or 3.5).", [nameof(Rating)]);
            }
        }
    }
}

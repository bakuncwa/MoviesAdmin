using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.ViewModels.Reviews
{
    // Backs Views/Reviews/Create and Edit. Only what a critic may type is bound here; CreatedBy,
    // IsPublished and the timestamps are always set by ReviewsController, never posted.
    public class ReviewFormViewModel
    {
        // 0 on Create; the review being edited on Edit.
        public int Id { get; set; }

        [Required(ErrorMessage = "Choose a movie.")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose a movie.")]
        [Display(Name = "Movie")]
        public int? MovieId { get; set; }

        // Shown on Edit, where the movie can't be changed.
        public string? MovieTitle { get; set; }

        [Required(ErrorMessage = "Choose a rating.")]
        [Range(0, 5, ErrorMessage = "Rating must be between {1} and {2}.")]
        [Display(Name = "Rating (out of 5)")]
        public int? Rating { get; set; }

        [Required(ErrorMessage = "Review text is required.")]
        [StringLength(1000, ErrorMessage = "Review text can be at most {1} characters.")]
        [Display(Name = "Review")]
        public string Text { get; set; } = string.Empty;

        public bool IsEdit => Id > 0;
    }
}

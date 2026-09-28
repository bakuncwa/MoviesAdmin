using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MoviesAdmin.ViewModels.Movies
{
    // Backs the Create/Edit modal form (_MovieFormModal.cshtml). Id is null for Create.
    // Validation attributes mirror the entities they map to (Models/Movie.cs, Models/Trailer.cs)
    // so the form rejects bad input with the same rules the entity itself enforces, before
    // anything reaches the repository/DB.
    public class MovieFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(200)]
        [RegularExpression(@"^[\p{L}\p{N}\s\-':,.&!?()]+$", ErrorMessage = "Title can only contain letters, numbers, spaces, and common punctuation (-':,.&!?()).")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Synopsis cannot contain '<' or '>' characters.")]
        public string? Synopsis { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Release date")]
        public DateTime ReleaseDate { get; set; } = DateTime.Today;

        [Range(1, 1000)]
        [Display(Name = "Runtime (minutes)")]
        public int? RuntimeMinutes { get; set; }

        [StringLength(500)]
        [RegularExpression(@"^https?://\S+$", ErrorMessage = "Poster URL must be a valid http:// or https:// address.")]
        [Display(Name = "Poster URL (external)")]
        public string? PosterUrl { get; set; }

        [Display(Name = "Poster image")]
        public IFormFile? PosterImageFile { get; set; }

        // Carries the already-saved image path across the round trip so a validation failure (or
        // an edit that doesn't touch the image) doesn't lose it; not user-editable.
        [ValidateNever]
        public string? ExistingPosterImagePath { get; set; }

        [StringLength(500)]
        [RegularExpression(
            @"^https?://(www\.)?(youtube\.com/(watch\?v=|embed/)|youtu\.be/)[A-Za-z0-9_\-]{6,20}([?&]\S*)?$",
            ErrorMessage = "Trailer URL must be a youtube.com or youtu.be link.")]
        [Display(Name = "Trailer URL (YouTube)")]
        public string? TrailerUrl { get; set; }

        [Display(Name = "Director")]
        public int? DirectorId { get; set; }

        [Display(Name = "Studio")]
        public int? StudioId { get; set; }

        [Display(Name = "Genres")]
        public List<int> SelectedGenreIds { get; set; } = new();

        // Populated by the controller for rendering the form's dropdowns/checkboxes; never bound
        // from the post itself (ValidateNever — these are reference data, not user input).
        [ValidateNever]
        public List<NamedOptionViewModel> AvailableGenres { get; set; } = new();

        [ValidateNever]
        public List<NamedOptionViewModel> AvailableDirectors { get; set; } = new();

        [ValidateNever]
        public List<NamedOptionViewModel> AvailableStudios { get; set; } = new();

        [ValidateNever]
        public bool IsEdit => Id.HasValue;

        [ValidateNever]
        public string ExistingPosterImageUrl => string.IsNullOrWhiteSpace(ExistingPosterImagePath)
            ? MoviePosterResolver.PlaceholderImageUrl
            : "/" + ExistingPosterImagePath.TrimStart('/');
    }

    // Shared by the Genre/Director/Studio pickers on the movie form — each is just an Id/Name pair.
    public class NamedOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

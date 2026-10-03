using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MoviesAdmin.Models;
using MoviesAdmin.Validation;

namespace MoviesAdmin.ViewModels.Movies
{
    // Backs the Create/Edit modal form (_MovieFormModal.cshtml). Id is null for Create.
    // Validation attributes mirror the entities they map to (Models/Movie.cs, Models/Trailer.cs)
    // so the form rejects bad input with the same rules the entity itself enforces, before
    // anything reaches the repository/DB.
    public class MovieFormViewModel : IValidatableObject
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between {2} and {1} characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s\-':,.&!?()]+$", ErrorMessage = "Title can only contain letters, numbers, spaces, and common punctuation (-':,.&!?()).")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Synopsis can be at most {1} characters.")]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Synopsis cannot contain '<' or '>' characters.")]
        public string? Synopsis { get; set; }

        // Nullable + [Required] rather than a plain DateTime: a blank date then fails with the
        // message below instead of MVC's generic "The value '' is invalid." binding error.
        [Required(ErrorMessage = "Release date is required.")]
        [DataType(DataType.Date)]
        [ReleaseDate]
        [Display(Name = "Release date")]
        public DateTime? ReleaseDate { get; set; } = DateTime.Today;

        // Runtime is entered as hours + minutes and stored as a single total (Movie.RuntimeMinutes).
        // Leaving both blank means "unknown".
        [Range(0, 16, ErrorMessage = "Hours must be between {1} and {2}.")]
        [Display(Name = "Hours")]
        public int? RuntimeHours { get; set; }

        [Range(0, 59, ErrorMessage = "Minutes must be between {1} and {2}.")]
        [Display(Name = "Minutes")]
        public int? RuntimeMinutesPart { get; set; }

        [Required(ErrorMessage = "Choose a rating (G, PG, PG-13, R, or NC-17).")]
        [EnumDataType(typeof(ContentRating), ErrorMessage = "Choose a rating from the list.")]
        [Display(Name = "Rating")]
        public ContentRating? ContentRating { get; set; }

        [StringLength(500, ErrorMessage = "Poster URL can be at most {1} characters.")]
        [RegularExpression(@"^https?://\S+$", ErrorMessage = "Poster URL must be a valid http:// or https:// address.")]
        [Display(Name = "Poster URL (external)")]
        public string? PosterUrl { get; set; }

        [PosterFile]
        [Display(Name = "Poster image")]
        public IFormFile? PosterImageFile { get; set; }

        // Carries the already-saved image path across the round trip so a validation failure (or
        // an edit that doesn't touch the image) doesn't lose it; not user-editable.
        [ValidateNever]
        public string? ExistingPosterImagePath { get; set; }

        [StringLength(500, ErrorMessage = "Trailer URL can be at most {1} characters.")]
        [RegularExpression(
            @"^https?://(www\.)?(youtube\.com/(watch\?v=|embed/)|youtu\.be/)[A-Za-z0-9_\-]{6,20}([?&]\S*)?$",
            ErrorMessage = "Trailer URL must be a youtube.com or youtu.be link.")]
        [Display(Name = "Trailer URL (YouTube)")]
        public string? TrailerUrl { get; set; }

        // Optional; when set, the controller also checks the id exists (see ValidateReferencesAsync).
        [Range(1, int.MaxValue, ErrorMessage = "Choose a director from the list.")]
        [Display(Name = "Director")]
        public int? DirectorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Choose a studio from the list.")]
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

        // Options for the "Fill from an online source" dropdown (Services/MovieLookup).
        [ValidateNever]
        public List<LookupSourceViewModel> LookupSources { get; set; } = new();

        [ValidateNever]
        public bool IsEdit => Id.HasValue;

        // Total runtime in minutes for the entity, or null when neither box was filled in.
        [ValidateNever]
        public int? TotalRuntimeMinutes => RuntimeHours == null && RuntimeMinutesPart == null
            ? null
            : (RuntimeHours ?? 0) * 60 + (RuntimeMinutesPart ?? 0);

        public void SetRuntime(int? totalMinutes)
        {
            RuntimeHours = totalMinutes / 60;
            RuntimeMinutesPart = totalMinutes % 60;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TotalRuntimeMinutes is 0)
            {
                yield return new ValidationResult("Runtime must be at least 1 minute (or leave both boxes blank).", new[] { nameof(RuntimeHours) });
            }
        }

        // Form preview: the uploaded poster if there is one, else the external PosterUrl, else the
        // placeholder (same priority as MoviePosterResolver uses for the catalog).
        [ValidateNever]
        public string ExistingPosterImageUrl => !string.IsNullOrWhiteSpace(ExistingPosterImagePath)
            ? "/" + ExistingPosterImagePath.TrimStart('/')
            : !string.IsNullOrWhiteSpace(PosterUrl) ? PosterUrl : MoviePosterResolver.PlaceholderImageUrl;
    }

    public class LookupSourceViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        // False when the source's API key isn't configured; the option renders disabled.
        public bool IsAvailable { get; set; }
    }

    // Shared by the Genre/Director/Studio pickers on the movie form — each is just an Id/Name pair.
    public class NamedOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

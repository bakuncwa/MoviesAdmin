using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.ViewModels.Movies
{
    // Posted by the Director picker's "Add ..." option on the movie form (MovieOptionsController).
    // Same rules as Models/Director.cs, so a name accepted here can always be saved.
    public class NewDirectorInput
    {
        [Required(ErrorMessage = "Director name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Director name must be between {2} and {1} characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s\-'.]+$", ErrorMessage = "Director name can only contain letters, numbers, spaces, hyphens, apostrophes, and periods.")]
        public string Name { get; set; } = string.Empty;
    }

    // Same idea for the Studio picker; rules mirror Models/Studio.cs.
    public class NewStudioInput
    {
        [Required(ErrorMessage = "Studio name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Studio name must be between {2} and {1} characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s\-&.,']+$", ErrorMessage = "Studio name can only contain letters, numbers, spaces, and common punctuation (-&.,').")]
        public string Name { get; set; } = string.Empty;
    }
}

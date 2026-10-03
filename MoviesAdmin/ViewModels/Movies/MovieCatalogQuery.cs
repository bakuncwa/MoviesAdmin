using System.ComponentModel.DataAnnotations;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;

namespace MoviesAdmin.ViewModels.Movies
{
    // The catalog's search/filter/sort state, bound from the query string by MoviesController.Index
    // and MoviesController.Search (e.g. /Movies?q=pride&genreId=4&year=2005&rating=PG&sort=TitleAsc).
    // MovieRepository.SearchAsync turns it into one SQL query, so filtering and sorting happen in the
    // database before ToListAsync() rather than in memory.
    public class MovieCatalogQuery
    {
        // Defense-in-depth for the search box: EF Core already parameterizes the query, so this
        // isn't preventing SQL injection so much as rejecting anything outside a safe "movie title"
        // character set before it reaches the repository. The match timeout guards against ReDoS.
        [StringLength(200, ErrorMessage = "Search can be at most {1} characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s\-':,.&!?()]*$",
            ErrorMessage = "Search can only contain letters, numbers, spaces, and common punctuation (-':,.&!?()).",
            MatchTimeoutInMilliseconds = 200)]
        public string? Q { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Choose a genre from the list.")]
        public int? GenreId { get; set; }

        [Range(1888, 2100, ErrorMessage = "Year must be between {1} and {2}.")]
        public int? Year { get; set; }

        [EnumDataType(typeof(ContentRating), ErrorMessage = "Choose a rating from the list.")]
        public ContentRating? Rating { get; set; }

        [EnumDataType(typeof(MovieSortOrder), ErrorMessage = "Choose a sort order from the list.")]
        public MovieSortOrder Sort { get; set; } = MovieSortOrder.ReleaseDateDesc;

        public bool HasFilters => !string.IsNullOrWhiteSpace(Q) || GenreId.HasValue || Year.HasValue || Rating.HasValue;

        public string SortLabel => SortOptions.TryGetValue(Sort, out var label) ? label : SortOptions[MovieSortOrder.ReleaseDateDesc];

        // Sort dropdown options, in display order.
        public static readonly IReadOnlyDictionary<MovieSortOrder, string> SortOptions = new Dictionary<MovieSortOrder, string>
        {
            [MovieSortOrder.ReleaseDateDesc] = "Release date, newest first",
            [MovieSortOrder.ReleaseDateAsc] = "Release date, oldest first",
            [MovieSortOrder.TitleAsc] = "Title, A–Z",
            [MovieSortOrder.TitleDesc] = "Title, Z–A",
            [MovieSortOrder.RatingDesc] = "Review score, highest first",
            [MovieSortOrder.RuntimeDesc] = "Runtime, longest first",
            [MovieSortOrder.RuntimeAsc] = "Runtime, shortest first"
        };
    }

    // Index page model: the query, the matching rows, and the options its filter dropdowns offer.
    // _MovieCatalog.cshtml renders the same type (options left empty) for async refreshes.
    public class MovieCatalogViewModel
    {
        public MovieCatalogQuery Query { get; set; } = new();

        public List<MovieListItemViewModel> Movies { get; set; } = new();

        public List<NamedOptionViewModel> GenreOptions { get; set; } = new();

        public List<int> YearOptions { get; set; } = new();
    }
}

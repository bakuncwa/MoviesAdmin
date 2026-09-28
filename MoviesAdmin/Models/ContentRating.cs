namespace MoviesAdmin.Models
{
    // MPAA-style audience rating (the "PG-13" kind of rating), distinct from the 1-5 star review
    // average. Stored as its name (e.g. "PG13") via a string conversion in ApplicationDbContext,
    // so the column stays readable and reordering these members never corrupts existing rows.
    public enum ContentRating
    {
        G,
        PG,
        PG13,
        R,
        NC17
    }

    public static class ContentRatingExtensions
    {
        // Display form used everywhere in the UI ("PG-13" rather than the enum name "PG13").
        public static string ToLabel(this ContentRating rating) => rating switch
        {
            ContentRating.PG13 => "PG-13",
            ContentRating.NC17 => "NC-17",
            _ => rating.ToString()
        };

        public static string ToLabel(this ContentRating? rating) => rating?.ToLabel() ?? "Not Rated";
    }
}

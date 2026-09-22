namespace MoviesAdmin.Models
{
    // Join entity for the Movie <-> Genre many-to-many relationship.
    public class MovieGenre
    {
        // Half of the composite primary key (MovieId, GenreId); FK to the movie side of the pair.
        public int MovieId { get; set; }
        // Navigation to the movie itself.
        public Movie Movie { get; set; } = null!;

        // Half of the composite primary key; FK to the genre side of the pair.
        public int GenreId { get; set; }
        // Navigation to the genre itself.
        public Genre Genre { get; set; } = null!;
    }
}

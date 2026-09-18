using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    public class modelMovie
    {
        // Add a class for Model (id, name, description)
        // Display and use in for-each loop
            public int Id { get; set; }

            [Required]
            [StringLength(200)]
            public string Title { get; set; } = string.Empty;

            [StringLength(2000)]
            public string? Synopsis { get; set; }
        
    }
}

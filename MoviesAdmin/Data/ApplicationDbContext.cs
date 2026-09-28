using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Models;

namespace MoviesAdmin.Data
{
    // IdentityDbContext already brings the Users/Roles/Claims tables needed for authentication.
    // The single-generic-argument form implicitly uses IdentityRole/string keys, i.e. it is
    // equivalent to IdentityDbContext<ApplicationUser, IdentityRole, string>.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Director> Directors => Set<Director>();
        public DbSet<Studio> Studios => Set<Studio>();
        public DbSet<Trailer> Trailers => Set<Trailer>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Genre>()
                .HasIndex(g => g.Name)
                .IsUnique();

            builder.Entity<MovieGenre>()
                .HasKey(mg => new { mg.MovieId, mg.GenreId });

            builder.Entity<MovieGenre>()
                .HasOne(mg => mg.Movie)
                .WithMany(m => m.MovieGenres)
                .HasForeignKey(mg => mg.MovieId);

            builder.Entity<MovieGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MovieGenres)
                .HasForeignKey(mg => mg.GenreId);

            builder.Entity<Review>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Director>()
                .HasIndex(d => d.Name)
                .IsUnique();

            builder.Entity<Studio>()
                .HasIndex(s => s.Name)
                .IsUnique();

            builder.Entity<Movie>()
                .HasOne(m => m.Director)
                .WithMany(d => d.Movies)
                .HasForeignKey(m => m.DirectorId)
                // Restrict (not Cascade): deleting a Director shouldn't silently take its movies
                // with it — the admin has to reassign or clear DirectorId on those movies first.
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Movie>()
                .HasOne(m => m.Studio)
                .WithMany(s => s.Movies)
                .HasForeignKey(m => m.StudioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Trailer>()
                .HasIndex(t => t.MovieId)
                .IsUnique();

            builder.Entity<Trailer>()
                .HasOne(t => t.Movie)
                .WithOne(m => m.Trailer)
                .HasForeignKey<Trailer>(t => t.MovieId)
                // Cascade here (unlike Director/Studio): a trailer has no meaning without its
                // movie, so deleting the movie should take its one trailer row with it.
                .OnDelete(DeleteBehavior.Cascade);

            // --- Reference data (SeedDirectorsStudios migration) ------------------------------
            // Five directors, each paired with the studio behind one of their signature films,
            // so the Director/Studio pickers on the movie form aren't empty on a fresh database.
            builder.Entity<Director>().HasData(
                new Director { Id = 1, Name = "Joe Wright", Bio = "Director of Pride & Prejudice (2005), produced by Working Title Films." },
                new Director { Id = 2, Name = "Greta Gerwig", Bio = "Director of Little Women (2019), released by Columbia Pictures." },
                new Director { Id = 3, Name = "Christopher Nolan", Bio = "Director of Inception (2010), produced by Syncopy." },
                new Director { Id = 4, Name = "Denis Villeneuve", Bio = "Director of Dune (2021), produced by Legendary Pictures." },
                new Director { Id = 5, Name = "Bong Joon-ho", Bio = "Director of Parasite (2019), produced by Barunson E&A." }
            );

            builder.Entity<Studio>().HasData(
                new Studio { Id = 1, Name = "Working Title Films" },
                new Studio { Id = 2, Name = "Columbia Pictures" },
                new Studio { Id = 3, Name = "Syncopy" },
                new Studio { Id = 4, Name = "Legendary Pictures" },
                new Studio { Id = 5, Name = "Barunson E&A" }
            );

            // Sample catalog entry. The poster is an external TMDB link (PosterUrl), not an
            // uploaded file, so deleting the movie never removes anything from wwwroot.
            builder.Entity<Movie>().HasData(
                new Movie
                {
                    Id = 1,
                    Title = "Pride & Prejudice",
                    Synopsis = "Sparks fly when spirited Elizabeth Bennet meets single, rich, and proud Mr. Darcy. But Mr. Darcy reluctantly finds himself falling in love with a woman beneath his class. Can each overcome their own pride and prejudice?",
                    ReleaseDate = new DateTime(2005, 9, 16),
                    RuntimeMinutes = 129,
                    PosterUrl = "https://image.tmdb.org/t/p/w500/o8UhmEbWPHmTUxP0lMuCoqNkbB3.jpg",
                    DirectorId = 1,
                    StudioId = 1
                }
            );

            // --- IAM (Identity/Access Management) setup -----------------------------------
            // Baseline RBAC roles for the Admin viewpoint described in the README: "Admin" can
            // manage Movies/Genres/Reviews, "Viewer" is a signed-in, read-only account. HasData
            // needs fixed keys so migrations can diff it deterministically across environments.
            //
            // Applied by the AddIamRoles migration. The first registered account is assigned
            // "Admin" (see AccountController.Register); later accounts get "Viewer".
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "9a8e6b1a-9e3b-4b3a-8a2e-000000000001",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    // IdentityRole defaults this to Guid.NewGuid(), which makes HasData
                    // non-deterministic (PendingModelChangesWarning on every update), so pin it.
                    ConcurrencyStamp = "9a8e6b1a-9e3b-4b3a-8a2e-100000000001"
                },
                new IdentityRole
                {
                    Id = "9a8e6b1a-9e3b-4b3a-8a2e-000000000002",
                    Name = "Viewer",
                    NormalizedName = "VIEWER",
                    ConcurrencyStamp = "9a8e6b1a-9e3b-4b3a-8a2e-100000000002"
                }
            );
        }
    }
}

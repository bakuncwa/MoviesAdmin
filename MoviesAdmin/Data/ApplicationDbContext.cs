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

            // --- IAM (Identity/Access Management) setup -----------------------------------
            // Baseline RBAC roles for the Admin viewpoint described in the README: "Admin" can
            // manage Movies/Genres/Reviews, "Viewer" is a signed-in, read-only account. HasData
            // needs fixed keys so migrations can diff it deterministically across environments.
            //
            // NOT YET MIGRATED: this seed only takes effect once a migration for it is generated
            // and applied (`dotnet ef migrations add AddIamRoles` / `dotnet ef database update`),
            // same as the rest of the schema per the README's Code First notes. Left un-migrated
            // for now, per instruction, until the Movies CRUD groundwork is confirmed.
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "9a8e6b1a-9e3b-4b3a-8a2e-000000000001",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "9a8e6b1a-9e3b-4b3a-8a2e-000000000002",
                    Name = "Viewer",
                    NormalizedName = "VIEWER"
                }
            );
        }
    }
}

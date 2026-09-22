# Movies Admin Application (MVC - ASP.NET)

**Course:** INET2005 - Web Application Programming I
**Instructor:** Michael Trumbull, Nova Scotia Community College

## Project Description

MoviesAdmin is an ASP.NET Core MVC web application for managing and reviewing movies, modeled after a Rotten Tomatoes-style review platform. The Admin RBAC viewpoint of the application will allow users to browse movies abd manage movie/genre data through an administrative interface. This is an atomic endpoint for Administrator access, and is part of the larger Movie Reviews solution, which includes browsing and submission of ratings/feedbacks/reviews.

## Technology Stack

| Category | Technology | Purpose | Status |
| --- | --- | --- | --- |
| Framework | ASP.NET Core MVC (.NET 10) | Web application framework | In use |
| Programming language | C# | Application programming language | In use |
| Data access | Entity Framework Core | Database access and persistence | In use |
| Database | SQL Server / LocalDB | Relational database storage | In use (migration generated, not yet applied) |
| Authentication | ASP.NET Core Identity | User accounts, login/register, role-based access | In use |
| View technology | Razor Views (`.cshtml`) | Server-rendered user interface | In use |
| Front end | HTML5 / CSS3 / JavaScript | Front-end structure, styling, and behavior | In use |
| UI library | Bootstrap 5.3 (vendored under `wwwroot/lib/bootstrap`) | Responsive UI components, layout, and native `data-bs-theme` light/dark color modes | In use |
| API integration | External movie APIs | Movie metadata, posters, trailers, and ratings | Possible integration |

## CLI/Terminal Commands for Set-Up

```bash
# Restore project dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project MoviesAdmin

# Restore the local dotnet-ef tool (see dotnet-tools.json)
dotnet tool restore

# Apply the existing migration to create/update the database
dotnet ef database update --project MoviesAdmin

# Add a new migration after model changes
dotnet ef migrations add <MigrationName> --project MoviesAdmin
```

## Code First / Database First

- **Approach:** Code First — models drive the schema via EF Core migrations
- **Schema:** `InitialMoviesReviewsSchema` migration covers ASP.NET Core Identity tables plus `Movie`, `Genre`, `MovieGenre` (many-to-many join), and `Review`, with Fluent API config for cascade/restrict deletes and a unique index on genre name. `AddMoviePosterImagePath` follows it, adding `Movie.PosterImagePath` for an admin-uploaded poster image (kept separate from `PosterUrl`, which is reserved for posters sourced from an external movie API). Both migrations are generated but not yet applied to a database — update `DefaultConnection` in `appsettings.json` and run `dotnet ef database update` to apply them.
- **Repository / Dependency Injection Pattern:** Implemented — generic `IRepository<T>`/`Repository<T>` base plus entity-specific `IMovieRepository`/`IGenreRepository`/`IReviewRepository`, all registered in `Program.cs`
- **Authentication:** ASP.NET Core Identity (`ApplicationUser`, `ApplicationDbContext : IdentityDbContext`) with `AccountController` (Login/Register/Logout/AccessDenied) and matching views/view models; admin navbar in `_Layout.cshtml` exposes Dashboard/Movies/Genres/Reviews links with auth-aware login/logout controls

## Current Core, Enhancing, and Enabling Features

*(Reference wireframe: Rotten Tomatoes)*

| Feature Category | Feature | Status |
| --- | --- | --- |
| Core | Movie listing / browse page | Planned |
| Core | Movie detail page | Planned |
| Core | User-submitted reviews and ratings | Schema in place; UI planned |
| Enhancing | Search and filter by genre, rating, or release year | Planned |
| Enhancing | Sorting by rating, popularity, or release date | Planned |
| Enhancing | User authentication and profile management | In use (login/register/logout, role-ready via Identity) |
| Enabling | Admin dashboard for managing movies, genres, and reviews | Navbar wired up; pages planned |
| Enabling | Database persistence via Entity Framework | Models, repositories, and migrations in place; not yet applied |
| Enabling | Responsive UI layout for desktop and mobile | In use |
| Enabling | Light/dark mode | In use — light by default, follows OS preference, toggle in navbar |

## Light/Dark Mode

The site defaults to a light background and switches to dark mode automatically when the visitor's OS is set to dark, using Bootstrap 5.3's native `data-bs-theme` color modes (no extra CSS framework needed):

- An inline script in `_Layout.cshtml`'s `<head>` sets `data-bs-theme` on `<html>` before first paint (checks `localStorage`, then falls back to `prefers-color-scheme`), so there's no light-then-dark flash on load.
- The sun/moon button in the navbar (`#theme-toggle` in `_Layout.cshtml`, wired up in `wwwroot/js/site.js`) lets a visitor override the OS preference; the choice is remembered in `localStorage` and takes priority over OS changes from then on.
- Navbar and link classes use theme-aware Bootstrap utilities (`bg-body-tertiary`, default `nav-link` color, etc.) instead of hardcoded `bg-white`/`text-dark`, so they repaint correctly in both modes.

## Suggested Future Data Models

Beyond the current `Movie` / `Genre` / `MovieGenre` / `Review` / `ApplicationUser` schema, these are natural next additions as the Rotten-Tomatoes-style feature set grows:

| Model | Purpose | Relationship |
| --- | --- | --- |
| `Actor` | Cast member profile (name, bio, photo) | Many-to-many with `Movie` via a `MovieActor` join (with a `Role`/character-name field on the join) |
| `Director` | Director profile | One-to-many with `Movie` (a movie has one primary director) or many-to-many if co-directed |
| `Studio` | Production company / distributor | One-to-many with `Movie` |
| `ContentRating` | MPAA-style rating (G, PG, PG-13, R, etc.) | One-to-many with `Movie` |
| `Genre` *(existing)* | — | Already many-to-many via `MovieGenre` |
| `Tag` | Free-form keywords (e.g. "based on a true story") | Many-to-many with `Movie` via a `MovieTag` join |
| `Watchlist` / `WatchlistItem` | Per-user "want to watch" list | `Watchlist` one-to-one with `ApplicationUser`; `WatchlistItem` many-to-one with `Watchlist` and `Movie` |
| `FavoriteMovie` | Per-user favorites/likes | Join between `ApplicationUser` and `Movie` |
| `ReviewComment` | Replies/discussion on a `Review` | Many-to-one with `Review` and `ApplicationUser` |
| `MovieImage` | Additional stills/gallery images beyond the poster | Many-to-one with `Movie` |
| `Award` | Awards/nominations won by a movie | Many-to-one with `Movie` (or many-to-many if shared across movies, e.g. franchise awards) |

These aren't scheduled yet — they're a roadmap for extending the Code First schema once the core `Movies`/`Genres`/`Reviews` admin screens are built out.
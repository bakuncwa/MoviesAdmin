# Movies Admin Application (MVC - ASP.NET)

**Course:** INET2005 - Web Application Programming I
**Instructor:** Michael Trumbull, Nova Scotia Community College

## Project Description

MoviesAdmin is an ASP.NET Core MVC web application for managing and reviewing movies, modeled after a Rotten Tomatoes-style review platform. The Admin RBAC viewpoint of the application will allow users to browse movies abd manage movie/genre data through an administrative interface. This is an atomic endpoint for Administrator access, and is part of the larger Movie Reviews solution, which includes browsing and submission of ratings/feedbacks/reviews.

## Screenshots

| Movies: featured hero and poster grid | Movies: list view |
| --- | --- |
| ![Movies poster grid](docs/screenshots/movies-grid.png) | ![Movies list view](docs/screenshots/movies-list.png) |
| **Movie details** | **Add/Edit movie form** |
| ![Movie details modal](docs/screenshots/movie-details.png) | ![Edit movie form](docs/screenshots/movie-edit.png) |
| **Delete confirmation (SweetAlert2)** | **Admin sign-in** |
| ![Delete confirmation](docs/screenshots/movie-delete.png) | ![Login page](docs/screenshots/login.png) |

## Sprint 1 Requirements

| Requirement | How it's met |
| --- | --- |
| CRUD for movie data stored in a database | `MoviesController` Create/Edit/Delete/Details against SQL Server (Docker) through EF Core and `IMovieRepository` |
| Movie data: title, synopsis, genre, rating (e.g. PG-13), runtime hours/minutes, release date | `Movie` has `Title`, `Synopsis`, genres (many-to-many via `MovieGenre`, 8 seeded genres), `ContentRating` (G/PG/PG-13/R/NC-17), `RuntimeMinutes` (entered as hours + minutes, shown as e.g. "2h 9m"), and `ReleaseDate` |
| Summary list of all movies, sorted by release date, with add/view/update/delete | `/Movies` lists every movie sorted by release date (newest first) as a poster grid or table, with "Add Movie" in the toolbar and View/Edit/Delete on each movie |
| Good design principles and the site's brand | MoviesAdmin brand (logo mark, crimson accent, Outfit type), consistent dark-first theme, responsive layout, accessible labels and focus states |

## Technology Stack

| Category | Technology | Purpose | Status |
| --- | --- | --- | --- |
| Framework | ASP.NET Core MVC (.NET 10) | Web application framework | In use |
| Programming language | C# | Application programming language | In use |
| Data access | Entity Framework Core | Database access and persistence | In use |
| Database | SQL Server 2022 (Developer Edition) in Docker | Relational database storage | In use — 6 migrations applied |
| Containerization | Docker Desktop + Docker Compose (`docker-compose.yml`) | Runs the local SQL Server with a persistent volume and health check | In use |
| Authentication | ASP.NET Core Identity | Config-provisioned accounts, login/logout, role-based access | In use |
| View technology | Razor Views (`.cshtml`) | Server-rendered user interface | In use |
| Front end | HTML5 / CSS3 / JavaScript | Front-end structure, styling, and behavior | In use |
| UI library | Bootstrap 5.3 (vendored under `wwwroot/lib/bootstrap`) | Responsive UI components, layout, and native `data-bs-theme` light/dark color modes | In use |
| Icons / typography | Font Awesome (solid, vendored) + Outfit (Google Fonts) | Button icons and the cinematic display type | In use |
| UI feedback | SweetAlert2 (vendored under `wwwroot/lib/sweetalert2`) | Delete confirmation dialogs and the undo/success toasts on the Movies admin page | In use |
| API integration | External movie APIs | Movie metadata, posters, trailers, and ratings | Possible integration; YouTube trailers embedded via `<iframe>` from an admin-entered URL are in use today |

## CLI/Terminal Commands for Set-Up

Run commands from the repository root (`~/Desktop/MoviesAdmin`), one line at a time. macOS's zsh doesn't treat `#` as a comment in an interactive shell by default, so don't paste comment lines into the terminal (or run `setopt interactive_comments` first).

**Prerequisites:** .NET 10 SDK and Docker Desktop.

### First-time setup

| Step | Command |
| --- | --- |
| 1. Restore NuGet packages | `dotnet restore` |
| 2. Restore the local `dotnet-ef` tool (pinned in `dotnet-tools.json`) | `dotnet tool restore` |
| 3. Start SQL Server in Docker | `docker compose up -d` |
| 4. Wait until the status shows `(healthy)` (~30–60s on Apple Silicon) | `docker compose ps` |
| 5. Create the database and apply all migrations | `dotnet ef database update --project MoviesAdmin` |
| 6. Run the app at http://localhost:5126 | `dotnet run --project MoviesAdmin` |
| 7. Sign in | `admin@moviesadmin.local` / `MoviesAdmin2026` (from `SeedUsers` in `appsettings.Development.json`) |

### Day-to-day

| Task | Command |
| --- | --- |
| Start the database | `docker compose up -d` |
| Run the app | `dotnet run --project MoviesAdmin` |
| Run with hot reload | `dotnet watch --project MoviesAdmin` |
| Build only | `dotnet build` |
| Stop the app | `Ctrl+C` in its terminal, or `kill $(lsof -t -iTCP:5126 -sTCP:LISTEN)` |
| Stop the database (data is kept) | `docker compose down` |

### Docker database

The container is defined in `docker-compose.yml`: image `mcr.microsoft.com/mssql/server:2022-latest` pinned to `linux/amd64` (runs under Rosetta on Apple Silicon), container name `mssql_inet2005_701`, host port `1433`, and data in the named volume `moviesadmin_mssql-data`. The app connects through `DefaultConnection` in `appsettings.Development.json` (`Server=localhost,1433;Database=MoviesAdmin;User Id=sa;...`).

| Task | Command |
| --- | --- |
| Start / create the container | `docker compose up -d` |
| Status and health | `docker compose ps` |
| Follow SQL Server logs | `docker compose logs -f mssql` |
| Restart | `docker compose restart mssql` |
| Stop and remove the container (volume kept) | `docker compose down` |
| **Delete everything, including the database volume** | `docker compose down -v` |
| Open an interactive SQL shell | `docker exec -it mssql_inet2005_701 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 2026MoviesAdmin -C -d MoviesAdmin` |
| Run a one-off query | `docker exec mssql_inet2005_701 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 2026MoviesAdmin -C -d MoviesAdmin -Q "SELECT Id, Title FROM Movies"` |
| Check what holds port 1433 | `docker ps --filter publish=1433` |
| Pull a newer SQL Server image, then recreate | `docker compose pull` then `docker compose up -d` |

To change the container (e.g. the SA password or host port), edit `docker-compose.yml` and run `docker compose up -d` again. Compose recreates the container and keeps the volume. If you change the SA password, update `DefaultConnection` too. SQL Server only reads `MSSQL_SA_PASSWORD` when it first creates the `master` database, so an existing volume keeps its old password until you run `docker compose down -v`.

Troubleshooting:
- **`Bind for 0.0.0.0:1433 failed: port is already allocated`**: another container already uses 1433. Find it with `docker ps --filter publish=1433` and stop it with `docker stop <name>`.
- **`Login timeout expired` right after starting**: SQL Server is still booting under emulation. Wait for `(healthy)`.
- **`WARNING: The requested image's platform (linux/amd64) does not match...`**: harmless. Compose pins the platform, so it only appears with a hand-typed `docker run`.

### EF Core migrations

| Task | Command |
| --- | --- |
| List migrations (unapplied ones are marked `(Pending)`) | `dotnet ef migrations list --project MoviesAdmin` |
| Check whether the model has changes without a migration | `dotnet ef migrations has-pending-model-changes --project MoviesAdmin` |
| Create a migration after changing `Models/` or `ApplicationDbContext` | `dotnet ef migrations add <MigrationName> --project MoviesAdmin` |
| Apply all pending migrations to the Docker database | `dotnet ef database update --project MoviesAdmin` |
| Roll the database back to an earlier migration | `dotnet ef database update <MigrationName> --project MoviesAdmin` |
| Roll back everything (empty schema) | `dotnet ef database update 0 --project MoviesAdmin` |
| Remove the last migration (only if it's not applied) | `dotnet ef migrations remove --project MoviesAdmin` |
| Generate an idempotent SQL script of all migrations | `dotnet ef migrations script --idempotent --project MoviesAdmin -o migrations.sql` |
| Drop the database (asks for confirmation) | `dotnet ef database drop --project MoviesAdmin` |

Seed data uses `HasData` in `ApplicationDbContext`, so every value must be a constant. A dynamic value such as `Guid.NewGuid()` or `DateTime.Now` changes the model on every build and makes `database update` fail with `PendingModelChangesWarning`.

## Code First / Database First

- **Approach:** Code First — models drive the schema via EF Core migrations
- **Schema:** Six migrations, all applied to the Docker SQL Server database:
  1. `InitialMoviesReviewsSchema` — ASP.NET Core Identity tables plus `Movie`, `Genre`, `MovieGenre` (many-to-many join), and `Review`, with Fluent API config for cascade/restrict deletes and a unique index on genre name.
  2. `AddMoviePosterImagePath` — adds `Movie.PosterImagePath` for an admin-uploaded poster image (kept separate from `PosterUrl`, which is reserved for posters sourced from an external movie API).
  3. `AddDirectorStudioTrailer` — adds the `Director` and `Studio` entities (each one-to-many with `Movie`, `Restrict` delete so removing one doesn't silently orphan its movies) and the `Trailer` entity (one-to-one with `Movie`, `Cascade` delete, stores just the admin-entered YouTube URL).
  4. `AddIamRoles` — seeds the `Admin` and `Viewer` roles (fixed Ids and `ConcurrencyStamp`s so the seed is deterministic).
  5. `SeedDirectorsStudios` — seeds five director/studio pairs (Joe Wright / Working Title Films, Greta Gerwig / Columbia Pictures, Christopher Nolan / Syncopy, Denis Villeneuve / Legendary Pictures, Bong Joon-ho / Barunson E&A) and a sample movie, *Pride & Prejudice* (2005), with its TMDB poster as `PosterUrl`.
  6. `SeedGenresContentRating` — adds `Movie.ContentRating` (stored as the enum name, e.g. `PG13`), seeds eight genres (Action, Animation, Comedy, Drama, Horror, Romance, Sci-Fi, Thriller), and rates *Pride & Prejudice* PG with Drama/Romance.
- **Repository / Dependency Injection Pattern:** Implemented — generic `IRepository<T>`/`Repository<T>` base plus entity-specific `IMovieRepository`/`IGenreRepository`/`IReviewRepository`/`IDirectorRepository`/`IStudioRepository`, all registered in `Program.cs`. `Trailer` has no queries beyond plain CRUD, so it resolves through the generic `IRepository<Trailer>` registration instead of a dedicated pair.
- **Authentication:** ASP.NET Core Identity (`ApplicationUser`, `ApplicationDbContext : IdentityDbContext`) with `AccountController` (Login/Logout/AccessDenied only — no registration) and matching views/view models; the navbar in `_Layout.cshtml` exposes Dashboard/Movies/Genres/Reviews links with auth-aware login/logout controls

## IAM (Identity/Access Management)

- **No self-registration.** `AccountController` only signs in/out. Every account is declared in the `SeedUsers` configuration section (email, password, display name, and **role**), and `Data/IdentitySeeder.cs` creates any missing account on startup and assigns its role. Existing passwords are never overwritten from config.
- **Roles** `Admin` and `Viewer` are seeded via `HasData` in `ApplicationDbContext` (migration `AddIamRoles`, with fixed `ConcurrencyStamp`s so the seed is deterministic).
- `Program.cs` registers a `"RequireAdmin"` policy; `MoviesController` is gated behind it, so only `Admin` accounts can manage movies. Anonymous visitors are redirected to `/Account/Login`; other roles get `/Account/AccessDenied`.
- Development credentials live in `appsettings.Development.json` (`admin@moviesadmin.local`). For anything beyond local development, supply `SeedUsers` through user secrets or environment variables (e.g. `SeedUsers__0__Password`) instead of committing them.

## Movies Admin CRUD

`MoviesController` + `Views/Movies/*` + `wwwroot/js/movies.js` implement a full CRUD screen at `/Movies`, styled after streaming sites such as Movy/Cineby:

- **Featured hero (`_MovieHero.cshtml`):** the most recently added movie over a blurred copy of its poster, with View details/Edit actions. Re-fetched from `GET /Movies/Hero` after every create/edit/delete so it never shows a stale or deleted movie.
- **Catalog (`_MovieCatalog.cshtml`):** every movie, sorted by release date (newest first). One partial renders both a responsive poster grid (hover lift and zoom, play button to view, edit/delete bar; year, runtime, rating badge, and genres under each poster) and a list table (poster thumbnail, title, release date, runtime, content rating, genres, review average, and View/Edit/Delete kept together in one cell per row). A grid/list toggle switches between them and is remembered per browser. Populated by `IMovieRepository.SearchAsync`.
- **Toolbar:** search, grid/list toggle, and "Add Movie" share one sticky glass toolbar.
- **Async search:** the search box (`#movie-search`) debounces input (300ms) and calls `GET /Movies/Search?q=...`, which validates the term against a compiled, timeout-guarded regex (`^[\p{L}\p{N}\s\-':,.&!?()]{0,200}$`) before querying, and returns the catalog partial to swap into `#movie-catalog`.
- **Create/Edit:** "Add Movie" and each row's "Edit" button load `_MovieFormModal.cshtml` into a shared Bootstrap modal via `GET /Movies/CreateModal` / `GET /Movies/EditModal/{id}`, submitted back via `fetch()` + `FormData` (so the poster file upload works) to `POST /Movies/Create` / `POST /Movies/Edit/{id}`. A 422 response re-renders the same partial with validation messages without closing the modal. The form's image column doubles as an image-left/details-right layout, and includes a required content-rating dropdown (G/PG/PG-13/R/NC-17), runtime as separate hours and minutes inputs, Director/Studio dropdowns, a Genre checkbox list, and a Trailer YouTube URL field.
- **View (Details):** each row's "View" button loads `_MovieDetailsModal.cshtml` via `GET /Movies/DetailsModal/{id}` — poster on the left, details (release date, runtime, director, studio, genres, rating, synopsis) on the right. If the movie has a trailer, a "Watch Trailer" button lazily fetches the `<iframe>` embed from `GET /Movies/TrailerEmbed/{id}` only when clicked, rather than embedding a YouTube player for every row up front.
- **Delete:** a SweetAlert2 confirmation dialog (styled with the same poster-left/details-right layout, built from `data-*` attributes on the row's Delete button — no extra round trip) precedes deletion. On confirm, the row is greyed out client-side and a SweetAlert2 toast with a 10-second countdown and an "Undo" button appears; `POST /Movies/Delete/{id}` is only called if the countdown fully elapses without Undo being clicked, so nothing is actually removed from the database during the grace window.
- **Modal styling:** `.modal-backdrop.show` gets a `backdrop-filter: blur(6px)` in `site.css` so Create/Edit/View dialogs blur the page behind them; the View modal adds a blurred-poster wash. SweetAlert2 dialogs follow the site theme (`theme: 'dark'`/`'light'`).
- **Poster images:** uploaded files are validated (`.jpg/.jpeg/.png/.gif/.webp`, ≤5MB) and saved under `wwwroot/images/movies/` with a generated filename; `Movie.PosterImagePath` takes display priority over `Movie.PosterUrl`, which falls back to a generated placeholder SVG when neither is set.

## Current Core, Enhancing, and Enabling Features

*(Reference wireframe: Rotten Tomatoes; visual style: Movy / Cineby)*

### Core
- [x] Movie browse page: featured hero, poster grid, and list view at `/Movies`, sorted by release date (newest first)
- [x] Movie data: title, synopsis, genres, content rating (G/PG/PG-13/R/NC-17), runtime in hours/minutes, release date
- [x] Movie details: modal with poster, meta, genres, director, studio, synopsis, and lazy-loaded YouTube trailer
- [x] Movie create/edit: modal form posted via `fetch()` through `MoviesController` into the Docker database, with server-side validation (422 re-render)
- [x] Movie delete: SweetAlert2 confirmation plus a 10-second Undo window before anything is removed
- [x] Poster images: upload (validated type/size) or external URL, with a placeholder fallback
- [ ] User-submitted reviews and ratings (schema in place, UI planned)

### Enhancing
- [x] Async title search (debounced, regex-validated)
- [x] Grid/list catalog toggle, remembered per browser
- [x] Director and studio dropdowns populated with seeded data
- [x] Eight seeded genres, selectable as checkboxes on the form
- [ ] Filter by genre / release year (supported in `IMovieRepository.SearchAsync`, not yet in the UI)
- [ ] User-selectable sort order (release date is the fixed default; `MovieSortOrder` also supports title and rating)

### Enabling
- [x] Code First schema with 6 EF Core migrations applied
- [x] SQL Server 2022 containerized with Docker Compose (persistent volume, health check)
- [x] Repository + dependency injection pattern
- [x] ASP.NET Core Identity login/logout
- [x] Role-based access control: `Admin`/`Viewer` roles, `RequireAdmin` policy on `MoviesController`
- [x] Accounts provisioned from configuration (`SeedUsers` → `IdentitySeeder`); self-registration removed
- [x] Cinematic dark-first UI (glass navbar, hero, poster cards, themed modals and alerts)
- [x] Responsive layout for desktop and mobile
- [x] Light/dark mode toggle
- [ ] Admin screens for Genres, Directors, Studios, and Reviews
- [ ] Containerize the ASP.NET app itself (Dockerfile + compose service)

## Light/Dark Mode

The site is dark by default (the cinematic theme is designed dark-first), with a light variant behind the navbar toggle. It uses Bootstrap 5.3's native `data-bs-theme` color modes:

- An inline script in `_Layout.cshtml`'s `<head>` sets `data-bs-theme` on `<html>` before first paint (a saved choice in `localStorage`, otherwise `dark`), so there's no flash on load.
- The sun/moon button in the navbar (`#theme-toggle`, wired up in `wwwroot/js/site.js`) switches modes and remembers the choice in `localStorage`.
- All custom colors are `--ma-*` tokens in `wwwroot/css/site.css`, defined for both modes; Bootstrap's `--bs-*` variables are pointed at them so stock components (forms, tables, modals) match.

## Suggested Future Data Models

`Director`, `Studio`, and `Trailer` (below) have since been implemented. Beyond the current
`Movie` / `Genre` / `MovieGenre` / `Review` / `ApplicationUser` / `Director` / `Studio` / `Trailer`
schema, these are natural next additions as the Rotten-Tomatoes-style feature set grows:

| Model | Purpose | Relationship |
| --- | --- | --- |
| `Actor` | Cast member profile (name, bio, photo) | Many-to-many with `Movie` via a `MovieActor` join (with a `Role`/character-name field on the join) |
| `ContentRating` | MPAA-style rating (G, PG, PG-13, R, etc.) | One-to-many with `Movie` |
| `Tag` | Free-form keywords (e.g. "based on a true story") | Many-to-many with `Movie` via a `MovieTag` join |
| `Watchlist` / `WatchlistItem` | Per-user "want to watch" list | `Watchlist` one-to-one with `ApplicationUser`; `WatchlistItem` many-to-one with `Watchlist` and `Movie` |
| `FavoriteMovie` | Per-user favorites/likes | Join between `ApplicationUser` and `Movie` |
| `ReviewComment` | Replies/discussion on a `Review` | Many-to-one with `Review` and `ApplicationUser` |
| `MovieImage` | Additional stills/gallery images beyond the poster | Many-to-one with `Movie` |
| `Award` | Awards/nominations won by a movie | Many-to-one with `Movie` (or many-to-many if shared across movies, e.g. franchise awards) |

These aren't scheduled yet — they're a roadmap for extending the Code First schema further once the core `Movies`/`Genres`/`Reviews` admin screens are built out.

### Implemented: Director, Studio, Trailer

- **`Director`** — `Id`, `Name` (unique), optional `Bio`. One-to-many with `Movie` (`Movie.DirectorId`, nullable, `Restrict` delete).
- **`Studio`** — `Id`, `Name` (unique). One-to-many with `Movie` (`Movie.StudioId`, nullable, `Restrict` delete).
- **`Trailer`** — `Id`, `MovieId` (unique — one-to-one), `YouTubeUrl`, `AddedAt`. Only the URL is persisted; `YouTubeVideoId`/`EmbedUrl` are computed (`[NotMapped]`) from it on read via `YouTubeTrailerHelper`, so nothing user-supplied is ever written straight into the page as HTML. `Movie.Trailer` is the reverse navigation (`Cascade` delete — a trailer has no meaning without its movie).

`Director`/`Studio` don't have their own management screens yet (same gap as `Genre` today). They're wired into the Movie Create/Edit form as dropdowns and into the Details view, and five of each are seeded by the `SeedDirectorsStudios` migration. Adding more currently means extending that `HasData` seed and adding a migration, until an admin screen exists.
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
| UI feedback | SweetAlert2 (vendored under `wwwroot/lib/sweetalert2`) | Delete confirmation dialogs and the undo/success toasts on the Movies admin page | In use |
| API integration | External movie APIs | Movie metadata, posters, trailers, and ratings | Possible integration; YouTube trailers embedded via `<iframe>` from an admin-entered URL are in use today |

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
- **Schema:** Three migrations so far, none yet applied to a database:
  1. `InitialMoviesReviewsSchema` — ASP.NET Core Identity tables plus `Movie`, `Genre`, `MovieGenre` (many-to-many join), and `Review`, with Fluent API config for cascade/restrict deletes and a unique index on genre name.
  2. `AddMoviePosterImagePath` — adds `Movie.PosterImagePath` for an admin-uploaded poster image (kept separate from `PosterUrl`, which is reserved for posters sourced from an external movie API).
  3. `AddDirectorStudioTrailer` — adds the `Director` and `Studio` entities (each one-to-many with `Movie`, `Restrict` delete so removing one doesn't silently orphan its movies) and the `Trailer` entity (one-to-one with `Movie`, `Cascade` delete, stores just the admin-entered YouTube URL).

  Update `DefaultConnection` in `appsettings.json` and run `dotnet ef database update` to apply all three. A fourth change — baseline IAM roles seeded via `HasData` in `ApplicationDbContext` — is deliberately **not yet migrated**; see IAM below.
- **Repository / Dependency Injection Pattern:** Implemented — generic `IRepository<T>`/`Repository<T>` base plus entity-specific `IMovieRepository`/`IGenreRepository`/`IReviewRepository`/`IDirectorRepository`/`IStudioRepository`, all registered in `Program.cs`. `Trailer` has no queries beyond plain CRUD, so it resolves through the generic `IRepository<Trailer>` registration instead of a dedicated pair.
- **Authentication:** ASP.NET Core Identity (`ApplicationUser`, `ApplicationDbContext : IdentityDbContext`) with `AccountController` (Login/Register/Logout/AccessDenied) and matching views/view models; admin navbar in `_Layout.cshtml` exposes Dashboard/Movies/Genres/Reviews links with auth-aware login/logout controls

## IAM (Identity/Access Management)

Groundwork for role-based access, per the project's "Admin RBAC viewpoint" description — set up but **not yet live**, by design:

- `Program.cs` registers a `"RequireAdmin"` authorization policy (`RequireRole("Admin")`) and gates `MoviesController` behind `[Authorize(Policy = "RequireAdmin")]`.
- `ApplicationDbContext` seeds two baseline roles (`Admin`, `Viewer`) via `HasData` with fixed GUIDs.
- That seed has **no migration yet** — it was intentionally left out of `AddDirectorStudioTrailer` (generated with the `HasData` call temporarily commented out, then restored) so it can land as its own dedicated migration later. Until `dotnet ef migrations add AddIamRoles` (or similar) is generated, applied, and an account is assigned the `Admin` role, nothing satisfies the policy — `/Movies` will simply redirect an unauthenticated visitor to `/Account/Login`, same as any other page, rather than 500ing.

## Movies Admin CRUD

`MoviesController` + `Views/Movies/*` + `wwwroot/js/movies.js` implement a full tabulated CRUD screen at `/Movies`:

- **Table (Index):** poster thumbnail, title, release date, runtime, genres, average rating, and row actions. Populated by `IMovieRepository.SearchAsync`.
- **Async search:** the search box (`#movie-search`) debounces input (300ms) and calls `GET /Movies/Search?q=...`, which validates the term against a compiled, timeout-guarded regex (`^[\p{L}\p{N}\s\-':,.&!?()]{0,200}$`) before querying, and returns just the `<tr>` rows partial to swap into the table body.
- **Create/Edit:** "Add Movie" and each row's "Edit" button load `_MovieFormModal.cshtml` into a shared Bootstrap modal via `GET /Movies/CreateModal` / `GET /Movies/EditModal/{id}`, submitted back via `fetch()` + `FormData` (so the poster file upload works) to `POST /Movies/Create` / `POST /Movies/Edit/{id}`. A 422 response re-renders the same partial with validation messages without closing the modal. The form's image column doubles as an image-left/details-right layout, and includes Director/Studio dropdowns, a Genre checkbox list, and a Trailer YouTube URL field.
- **View (Details):** each row's "View" button loads `_MovieDetailsModal.cshtml` via `GET /Movies/DetailsModal/{id}` — poster on the left, details (release date, runtime, director, studio, genres, rating, synopsis) on the right. If the movie has a trailer, a "Watch Trailer" button lazily fetches the `<iframe>` embed from `GET /Movies/TrailerEmbed/{id}` only when clicked, rather than embedding a YouTube player for every row up front.
- **Delete:** a SweetAlert2 confirmation dialog (styled with the same poster-left/details-right layout, built from `data-*` attributes on the row's Delete button — no extra round trip) precedes deletion. On confirm, the row is greyed out client-side and a SweetAlert2 toast with a 10-second countdown and an "Undo" button appears; `POST /Movies/Delete/{id}` is only called if the countdown fully elapses without Undo being clicked, so nothing is actually removed from the database during the grace window.
- **Modal styling:** `.modal-backdrop.show` gets a `backdrop-filter: blur(4px)` in `site.css` so Create/Edit/View/Delete dialogs blur the page behind them instead of Bootstrap's plain dim overlay.
- **Poster images:** uploaded files are validated (`.jpg/.jpeg/.png/.gif/.webp`, ≤5MB) and saved under `wwwroot/images/movies/` with a generated filename; `Movie.PosterImagePath` takes display priority over `Movie.PosterUrl`, which falls back to a generated placeholder SVG when neither is set.

## Current Core, Enhancing, and Enabling Features

*(Reference wireframe: Rotten Tomatoes)*

| Feature Category | Feature | Status |
| --- | --- | --- |
| Core | Movie listing / browse page | In use — tabulated admin view at `/Movies` with async search |
| Core | Movie detail page | In use — modal (poster left, details right), incl. trailer playback |
| Core | Movie create/edit/delete | In use — modal Create/Edit, SweetAlert2-confirmed Delete with 10s undo |
| Core | User-submitted reviews and ratings | Schema in place; UI planned |
| Enhancing | Search and filter by genre, rating, or release year | Title search in use (async, regex-validated); genre/year filters exist in `IMovieRepository.SearchAsync` but aren't wired to the UI yet |
| Enhancing | Sorting by rating, popularity, or release date | Supported in `IMovieRepository.SearchAsync` (`MovieSortOrder`); not yet exposed in the UI |
| Enhancing | User authentication and profile management | In use (login/register/logout, role-ready via Identity) |
| Enabling | Admin dashboard for managing movies, genres, and reviews | Movies CRUD in use; Genres/Reviews screens still planned |
| Enabling | Database persistence via Entity Framework | Models, repositories, and migrations in place; not yet applied |
| Enabling | Responsive UI layout for desktop and mobile | In use |
| Enabling | Light/dark mode | In use — light by default, follows OS preference, toggle in navbar |
| Enabling | Role-based access control (IAM) | Policy + role seed in place; not yet migrated/enforced — see IAM section |

## Light/Dark Mode

The site defaults to a light background and switches to dark mode automatically when the visitor's OS is set to dark, using Bootstrap 5.3's native `data-bs-theme` color modes (no extra CSS framework needed):

- An inline script in `_Layout.cshtml`'s `<head>` sets `data-bs-theme` on `<html>` before first paint (checks `localStorage`, then falls back to `prefers-color-scheme`), so there's no light-then-dark flash on load.
- The sun/moon button in the navbar (`#theme-toggle` in `_Layout.cshtml`, wired up in `wwwroot/js/site.js`) lets a visitor override the OS preference; the choice is remembered in `localStorage` and takes priority over OS changes from then on.
- Navbar and link classes use theme-aware Bootstrap utilities (`bg-body-tertiary`, default `nav-link` color, etc.) instead of hardcoded `bg-white`/`text-dark`, so they repaint correctly in both modes.

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

`Director`/`Studio` don't have their own management screens yet (same gap as `Genre` today) — they're wired into the Movie Create/Edit form as dropdowns and into the Details view, but adding new directors/studios currently requires seeding data directly (e.g. via `dotnet ef migrations` seed data or a future admin screen).
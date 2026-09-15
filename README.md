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
| UI library | Bootstrap | Responsive UI components and layout | In use |
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
- **Schema:** `InitialMoviesReviewsSchema` migration covers ASP.NET Core Identity tables plus `Movie`, `Genre`, `MovieGenre` (many-to-many join), and `Review`, with Fluent API config for cascade/restrict deletes and a unique index on genre name. Generated but not yet applied to a database — update `DefaultConnection` in `appsettings.json` and run `dotnet ef database update` to apply it.
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
| Enabling | Database persistence via Entity Framework | Models, repositories, and migration in place; migration not yet applied |
| Enabling | Responsive UI layout for desktop and mobile | In use |
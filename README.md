# Movies Admin Application (MVC - ASP.NET)

**Course:** INET2005 - Web Application Programming I
**Instructor:** Michael Trumbull, Nova Scotia Community College

## Project Description

MoviesAdmin is an ASP.NET Core MVC web application for managing and reviewing movies, modeled after a Rotten Tomatoes-style review platform. The application will allow users to browse movies, submit reviews and ratings, and manage movie/genre data through an administrative interface.

## Technology Stack

| Category | Technology | Purpose | Status |
| --- | --- | --- | --- |
| Framework | ASP.NET Core MVC (.NET 10) | Web application framework | In use |
| Programming language | C# | Application programming language | In use |
| Data access | Entity Framework Core | Database access and persistence | Planned |
| Database | SQL Server / LocalDB | Relational database storage | Planned |
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

# Add Entity Framework Core packages (placeholder)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools

# Add / update database migrations (placeholder)
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Code First / Database First — Placeholder

- **Approach:** TBD (Code First vs. Database First)
- **ETL / Schema:** Placeholder — schema definitions and seed data to be added as models are finalized
- **Repository / Dependency Injection Pattern:** Placeholder — repository interfaces and service registrations to be implemented (e.g., `IMovieRepository`, `IReviewRepository` registered in `Program.cs`)

## Current Core, Enhancing, and Enabling Features

*(Reference wireframe: Rotten Tomatoes)*

| Feature Category | Feature | Status |
| --- | --- | --- |
| Core | Movie listing / browse page | Planned |
| Core | Movie detail page | Planned |
| Core | User-submitted reviews and ratings | Planned |
| Enhancing | Search and filter by genre, rating, or release year | Planned |
| Enhancing | Sorting by rating, popularity, or release date | Planned |
| Enhancing | User authentication and profile management | Planned |
| Enabling | Admin dashboard for managing movies, genres, and reviews | Planned |
| Enabling | Database persistence via Entity Framework | Planned |
| Enabling | Responsive UI layout for desktop and mobile | Planned |
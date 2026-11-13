using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.Services.MovieLookup;
using MoviesAdmin.Services.PosterStorage;
using MoviesAdmin.Services.UserSnapshots;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Plain-English replacements for MVC's model-binding messages ("The value '' is invalid."),
    // which fire before any [Required]/[Range] attribute runs — e.g. a blank non-nullable field
    // or letters typed into a number box. {0} is the attempted value, {1} the field's display name.
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustNotBeNullAccessor(field => $"{field} is required.");
    messages.SetMissingBindRequiredValueAccessor(field => $"{field} is required.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"\"{value}\" isn't a valid value for {field}.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Enter a valid value for {field}.");
    messages.SetValueIsInvalidAccessor(value => $"\"{value}\" isn't a valid value.");
    messages.SetValueMustBeANumberAccessor(field => $"{field} must be a number.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"\"{value}\" isn't a valid value.");
});

// Not in appsettings*.json: in Development it comes from user secrets (written from .env by
// scripts/setup-secrets.sh, using the same port docker-compose.yml publishes); elsewhere from the
// ConnectionStrings__DefaultConnection environment variable.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. Copy .env.example to .env, then run " +
        "./scripts/setup-secrets.sh (or set the ConnectionStrings__DefaultConnection environment variable).");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Auth0 login ------------------------------------------------------------------------------
// Auth0 stores the user accounts and roles. Signing in redirects to Auth0's login page, which sends
// the user back to /callback with an id token; the SDK turns that into the auth cookie. Domain and
// ClientId come from user secrets in Development (written from .env by scripts/setup-secrets.sh)
// and from Auth0__Domain / Auth0__ClientId environment variables elsewhere.
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0ClientId = builder.Configuration["Auth0:ClientId"];
if (string.IsNullOrWhiteSpace(auth0Domain) || string.IsNullOrWhiteSpace(auth0ClientId))
{
    throw new InvalidOperationException(
        "Auth0:Domain and Auth0:ClientId are not configured. Fill in AUTH0_DOMAIN and AUTH0_CLIENT_ID in .env " +
        "(Auth0 Dashboard > Applications > your app > Settings), then run ./scripts/setup-secrets.sh.");
}

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = auth0Domain;
    options.ClientId = auth0ClientId;
    // "email" adds the user's email to the id token, for the UserSnapshot table below.
    options.Scope = "openid profile email";
    // Every login adds or updates the user's UserSnapshot row (name, email, roles) before the auth
    // cookie is issued.
    options.OpenIdConnectEvents = new OpenIdConnectEvents
    {
        OnTokenValidated = async context =>
        {
            if (context.Principal is not null)
            {
                var snapshots = context.HttpContext.RequestServices.GetRequiredService<UserSnapshotService>();
                await snapshots.RecordLoginAsync(context.Principal);
            }
        }
    };
});

// Auth0 leaves roles out of the id token by default. The "Add Roles to Token" post-login Action
// adds them as a custom claim named "<namespace>/roles" (Auth0:RolesClaim in appsettings.json must
// match the Action's namespace); telling the OIDC handler that's the role claim is what makes
// [Authorize(Roles = ...)] and User.IsInRole work. Only these two settings are changed, so the
// issuer/audience checks the Auth0 SDK set up stay in place.
builder.Services.Configure<OpenIdConnectOptions>(Auth0Constants.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = builder.Configuration["Auth0:RolesClaim"]
        ?? throw new InvalidOperationException("Auth0:RolesClaim is not configured (see appsettings.json).");
});

// Generic repository registration: any IRepository<T> resolves to Repository<T>
// for entities that don't need queries beyond plain CRUD.
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Entity-specific repositories layer their own queries (search/filter/sort, eager loading)
// on top of the same generic base via inheritance.
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IDirectorRepository, DirectorRepository>();
builder.Services.AddScoped<IStudioRepository, StudioRepository>();
builder.Services.AddScoped<UserSnapshotService>();
// Trailer has no queries beyond plain CRUD, so it resolves through the generic IRepository<T>
// registration above rather than needing its own ITrailerRepository/TrailerRepository pair.

// External movie lookup for the Add/Edit form (MovieLookupController). Each source is a typed
// HttpClient, also exposed as IMovieLookupProvider so the form's "Source" dropdown lists them all.
// API keys come from the "MovieLookup" section (user secrets in Development); a source without a
// key stays listed but disabled. Wikidata needs no key and is registered first, so it's the
// dropdown's default.
builder.Services.Configure<MovieLookupOptions>(builder.Configuration.GetSection(MovieLookupOptions.SectionName));
builder.Services.AddHttpClient<WikidataLookupProvider>(client =>
{
    // Wikimedia's API etiquette asks every client to identify itself with a descriptive User-Agent.
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ReelboxAdmin/1.0 (INET2005 school project; https://github.com/bakuncwa/MoviesAdmin)");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<TmdbLookupProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<OmdbLookupProvider>(client =>
{
    client.BaseAddress = new Uri("https://www.omdbapi.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddTransient<IMovieLookupProvider>(sp => sp.GetRequiredService<WikidataLookupProvider>());
builder.Services.AddTransient<IMovieLookupProvider>(sp => sp.GetRequiredService<TmdbLookupProvider>());
builder.Services.AddTransient<IMovieLookupProvider>(sp => sp.GetRequiredService<OmdbLookupProvider>());

// Uploaded posters: an Azure Blob container (public read access for blobs) when
// PosterStorage:ConnectionString is set, otherwise wwwroot/images/movies. LocalPosterStorage is
// always registered so the blob store can still clean up posters uploaded locally before.
builder.Services.Configure<PosterStorageOptions>(builder.Configuration.GetSection(PosterStorageOptions.SectionName));
builder.Services.AddSingleton<LocalPosterStorage>();
if (builder.Configuration.GetSection(PosterStorageOptions.SectionName).Get<PosterStorageOptions>()?.UseAzureBlob == true)
{
    builder.Services.AddSingleton<IPosterStorage, AzureStorageService>();
}
else
{
    builder.Services.AddSingleton<IPosterStorage>(sp => sp.GetRequiredService<LocalPosterStorage>());
}

var app = builder.Build();

// Resolve the poster store once up front, so a malformed PosterStorage:ConnectionString stops the
// app at startup with a clear message rather than failing the first Movies request.
try
{
    app.Services.GetRequiredService<IPosterStorage>();
}
catch (FormatException ex)
{
    throw new InvalidOperationException(
        "PosterStorage:ConnectionString isn't a valid Azure Storage connection string. Copy it from the " +
        "storage account's Access keys page into AZURE_STORAGE_CONNECTION_STRING in .env, then re-run " +
        "./scripts/setup-secrets.sh.", ex);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Catalog filtered to one genre by name: /Movies/drama, /Movies/action-adventure.
// Registered after "default" so real actions (/Movies/Create, /Movies/Search, ...) still win.
// The constraint only lets through what MovieDisplayFormat.Slug can produce (letters, digits,
// hyphens; Genre.Name is at most 50 characters), so anything else is a 404 before Index runs.
app.MapControllerRoute(
    name: "movies-genre",
    pattern: @"Movies/{genre:regex(^[\p{{L}}\p{{N}}-]{{1,60}}$)}",
    defaults: new { controller = "Movies", action = "Index" });


app.Run();

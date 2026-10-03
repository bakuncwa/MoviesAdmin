using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.Services.MovieLookup;

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

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// --- IAM (Identity/Access Management) setup ------------------------------------------------
// Movie management (MoviesController) is restricted to the "Admin" role, matching the README's
// "Admin RBAC viewpoint" description. The role is seeded via HasData in ApplicationDbContext
// (AddIamRoles migration); accounts holding it come from the "SeedUsers" config (IdentitySeeder).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
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
// Trailer has no queries beyond plain CRUD, so it resolves through the generic IRepository<T>
// registration above rather than needing its own ITrailerRepository/TrailerRepository pair.

// External movie lookup for the Add/Edit form (MovieLookupController). Each source is a typed
// HttpClient, also exposed as IMovieLookupProvider so the form's "Source" dropdown lists them all.
// API keys come from the "MovieLookup" section (user secrets in Development); a source without a
// key stays listed but disabled.
builder.Services.Configure<MovieLookupOptions>(builder.Configuration.GetSection(MovieLookupOptions.SectionName));
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
builder.Services.AddTransient<IMovieLookupProvider>(sp => sp.GetRequiredService<TmdbLookupProvider>());
builder.Services.AddTransient<IMovieLookupProvider>(sp => sp.GetRequiredService<OmdbLookupProvider>());

var app = builder.Build();

// Registration is disabled: create the configured "SeedUsers" accounts and bind them to their roles.
await IdentitySeeder.SeedAsync(app.Services);

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


app.Run();

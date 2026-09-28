using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

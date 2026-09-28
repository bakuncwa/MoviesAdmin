using Microsoft.AspNetCore.Identity;
using MoviesAdmin.Models;

namespace MoviesAdmin.Data
{
    // One entry of the "SeedUsers" configuration section (appsettings.{Environment}.json, user
    // secrets, or environment variables such as SeedUsers__0__Password).
    public class SeedUserOptions
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // Self-registration is disabled, so every account that can sign in is declared in configuration
    // and bound to one of the application roles seeded by the AddIamRoles migration (Admin/Viewer).
    // Runs on startup: creates missing accounts and makes sure each holds its configured role.
    // Existing passwords are never overwritten, so changing one later is done through Identity,
    // not by editing config.
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var seedUsers = configuration.GetSection("SeedUsers").Get<List<SeedUserOptions>>() ?? new();
            if (seedUsers.Count == 0)
            {
                logger.LogWarning("No SeedUsers configured; with registration disabled, nobody will be able to sign in.");
                return;
            }

            try
            {
                foreach (var seed in seedUsers)
                {
                    if (!await roleManager.RoleExistsAsync(seed.Role))
                    {
                        logger.LogError("Seed user {Email} references unknown role {Role}; skipped.", seed.Email, seed.Role);
                        continue;
                    }

                    var user = await userManager.FindByEmailAsync(seed.Email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = seed.Email,
                            Email = seed.Email,
                            DisplayName = seed.DisplayName,
                            EmailConfirmed = true
                        };

                        var created = await userManager.CreateAsync(user, seed.Password);
                        if (!created.Succeeded)
                        {
                            logger.LogError("Could not create seed user {Email}: {Errors}", seed.Email,
                                string.Join("; ", created.Errors.Select(e => e.Description)));
                            continue;
                        }

                        logger.LogInformation("Created seed user {Email} ({Role}).", seed.Email, seed.Role);
                    }

                    if (!await userManager.IsInRoleAsync(user, seed.Role))
                    {
                        await userManager.AddToRoleAsync(user, seed.Role);
                    }
                }
            }
            catch (Exception ex)
            {
                // Most likely the database isn't reachable or migrated yet; let the app start anyway
                // so the error page/logs explain it, rather than crashing on boot.
                logger.LogError(ex, "Seeding accounts failed. Is SQL Server running (docker compose up -d) and migrated (dotnet ef database update)?");
            }
        }
    }
}

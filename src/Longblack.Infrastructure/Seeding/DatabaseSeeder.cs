using Longblack.Domain.Identity;
using Longblack.Domain.Stores;
using Longblack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Longblack.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static readonly string[] Roles = ["Admin", "Manager", "Staff"];

    // Fixed id so this matches the row inserted by db/006-multi-store-transfers-schema.sql when
    // that script has already been run — either path resolves to the same single Warehouse Store.
    private static readonly Guid WarehouseStoreId = Guid.Parse("00000000-0000-0000-0007-000000000001");

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(DatabaseSeeder));

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@longblack.com";
        var adminPassword = configuration["Seed:AdminPassword"]
            ?? throw new InvalidOperationException("Seed:AdminPassword is not configured.");

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Seeded default admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to seed admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        var warehouseExists = await db.Stores.AnyAsync(s => s.Type == StoreType.Warehouse);
        if (!warehouseExists)
        {
            var now = DateTimeOffset.UtcNow;
            db.Stores.Add(new Store
            {
                Id = WarehouseStoreId,
                Name = "Warehouse",
                Type = StoreType.Warehouse,
                Status = StoreStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system",
                UpdatedBy = "system"
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded the Warehouse store.");
        }
    }
}

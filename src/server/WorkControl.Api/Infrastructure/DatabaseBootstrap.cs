using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Domain;

namespace WorkControl.Api.Infrastructure;

public static class DatabaseBootstrap
{
    private const string InitialMigration = "202610300001_InitialCreate";

    public static async Task InitializeAsync(IServiceProvider services, bool seedLocalDemo, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
        if (db.Database.IsSqlite()) await BaselineLegacySqliteAsync(db, ct);
        await db.Database.MigrateAsync(ct);
        if (seedLocalDemo) await DemoData.SeedAsync(db, ct);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in AppRoles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    private static async Task BaselineLegacySqliteAsync(WorkControlDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var exists = connection.CreateCommand();
        exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Organizations';";
        var hasLegacySchema = Convert.ToInt32(await exists.ExecuteScalarAsync(ct)) > 0;
        if (!hasLegacySchema) return;

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('202610300001_InitialCreate', '10.0.15');
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task<int> BootstrapAdminAsync(IServiceProvider services, string email, string password)
    {
        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.Users.AnyAsync())
        {
            Console.Error.WriteLine("Bootstrap отменён: в базе уже есть пользователи.");
            return 2;
        }
        var user = new AppUser { Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = true, IsActive = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) Console.Error.WriteLine(error.Description);
            return 3;
        }
        await userManager.AddToRoleAsync(user, AppRoles.Admin);
        Console.WriteLine($"Локальный администратор {email} создан. Пароль не сохранён приложением.");
        return 0;
    }
}

using Microsoft.EntityFrameworkCore;

namespace ERP.FournisseurService.Infrastructure.Persistence.Seeders;

public class DatabaseSeeder
{
    private readonly FournisseurSeeder _fournisseurSeeder;
    private readonly FournisseurDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        FournisseurSeeder fournisseurSeeder,
        FournisseurDbContext context,
        ILogger<DatabaseSeeder> logger)
    {
        _fournisseurSeeder = fournisseurSeeder;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Applies pending migrations then runs seeders in dependency order:
    /// 1. Categories first — clients reference them
    /// 2. Clients second
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Applying pending migrations...");
        await _context.Database.MigrateAsync();

        _logger.LogInformation("Starting database seed...");

        await _fournisseurSeeder.SeedAsync();

        _logger.LogInformation("Database seed complete.");
    }
}

// ── Extension method for Program.cs ──────────────────────────────────────────

public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Registers all seeders in DI and exposes a one-line call for Program.cs.
    /// </summary>
    public static IServiceCollection AddDatabaseSeeders(
        this IServiceCollection services)
    {
        services.AddScoped<FournisseurSeeder>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    /// <summary>
    /// Runs the full seed pipeline.
    /// Call this from Program.cs in Development only.
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
}
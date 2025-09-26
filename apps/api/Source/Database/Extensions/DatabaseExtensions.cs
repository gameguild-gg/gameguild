namespace GameGuild.Database.Extensions;

/// <summary>
/// Extension methods for database seeding
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Ensures the database is created and seeded with initial data
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task EnsureCreatedAndSeededAsync(this ApplicationDbContext context, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        // Ensure database is created
        bool created = await context.Database.EnsureCreatedAsync(cancellationToken);

        // Always seed data (seeder will check if data already exists)
        await context.SeedAsync(serviceProvider, cancellationToken);

        if (created)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Applies pending migrations and seeds the database with initial data
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task MigrateAndSeedAsync(this ApplicationDbContext context, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        // Apply any pending migrations
        await context.Database.MigrateAsync(cancellationToken);

        // Seed the database
        await context.SeedAsync(serviceProvider, cancellationToken);
    }
}
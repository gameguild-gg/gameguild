namespace GameGuild.Database;

/// <summary>
/// Central database seeder that orchestrates all individual seeders
/// </summary>
public class DatabaseSeeder(IEnumerable<IDataSeeder> seeders) : IDataSeeder
{
    private readonly IEnumerable<IDataSeeder> _seeders = seeders;

    /// <summary>
    /// Seeds all data using registered seeders
    /// </summary>
    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        foreach (IDataSeeder seeder in _seeders)
        {
            await seeder.SeedAsync(context, cancellationToken);
        }
    }
}
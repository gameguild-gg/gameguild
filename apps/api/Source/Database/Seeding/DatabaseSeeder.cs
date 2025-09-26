namespace GameGuild.Database.Seeding;

/// <summary>
/// Central database seeder that orchestrates all individual seeders
/// </summary>
public class DatabaseSeeder : IDataSeeder
{
    private readonly IEnumerable<IDataSeeder> _seeders;

    /// <summary>
    /// Initializes a new instance of the DatabaseSeeder
    /// </summary>
    /// <param name="seeders">Collection of individual data seeders</param>
    public DatabaseSeeder(IEnumerable<IDataSeeder> seeders)
    {
        _seeders = seeders;
    }

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
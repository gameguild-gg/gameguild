namespace GameGuild.Database.Seeding;

/// <summary>
/// Interface for database data seeders
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Seeds data into the database
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);
}
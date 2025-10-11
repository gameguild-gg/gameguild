namespace GameGuild.Core.Domain.Services;

/// <summary> Interface for database seeding operations </summary>
public interface IDatabaseSeeder {
  /// <summary> Seeds the database with initial data </summary>
  /// <returns> A task representing the async operation </returns>
  Task SeedAsync();
}

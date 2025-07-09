namespace GameGuild.Common.Application.Services;

/// <summary>
/// Interface for seeding the database with initial data
/// </summary>
public interface IDatabaseSeeder {
  /// <summary>
  /// Seeds the database with initial data including global default permissions
  /// </summary>
  Task SeedAsync();

  /// <summary>
  /// Seeds global default permissions that apply to all users
  /// </summary>
  Task SeedGlobalDefaultPermissionsAsync();

  Task SeedGlobalProjectDefaultPermissionsAsync();
}

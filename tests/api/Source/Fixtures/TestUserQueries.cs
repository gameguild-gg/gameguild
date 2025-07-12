using GameGuild.Common;
using GameGuild.Modules.Users;
using HotChocolate.Types;


namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Simple test GraphQL queries for debugging
/// </summary>
[ExtendObjectType<Query>]
public class TestUserQueries {
  /// <summary>
  /// Simple test method to return a hardcoded user
  /// </summary>
  public async Task<User?> User(Guid id) {
    // Return a simple test user for debugging
    await Task.Delay(1); // Make it async
    return new User {
      Id = id,
      Name = "Test User",
      Email = "test@example.com",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  /// <summary>
  /// Simple test method to return hardcoded users
  /// </summary>
  public async Task<IEnumerable<User>> Users() {
    await Task.Delay(1); // Make it async
    return new List<User> {
      new User {
        Id = Guid.NewGuid(),
        Name = "Test User 1",
        Email = "test1@example.com",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new User {
        Id = Guid.NewGuid(),
        Name = "Test User 2",
        Email = "test2@example.com",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      },
      new User {
        Id = Guid.NewGuid(),
        Name = "Test User 3",
        Email = "test3@example.com",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      }
    };
  }
}

using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Database;

/// <summary>
/// Extension methods for configuring owned value objects in Entity Framework
/// </summary>
public static class ValueObjectConfiguration {
    /// <summary>
    /// Configures owned value objects for improved domain modeling
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureValueObjects(this ModelBuilder modelBuilder) {
        // Note: User entity value objects (EmailAddress, PhoneNumber, Balance, AvailableBalance) 
        // are now configured in UserConfiguration.cs via IEntityTypeConfiguration<User>
        // to avoid conflicts between multiple entity configurations.

        // Future value object configurations for other entities can be added here
    }
}

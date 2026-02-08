using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     EF Core model configuration for the Users module.
/// </summary>
public sealed class UsersModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(User).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Identity.Users", StringComparison.Ordinal) == true);
    }
}

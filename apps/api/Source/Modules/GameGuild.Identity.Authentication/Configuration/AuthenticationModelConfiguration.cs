using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     EF Core model configuration for the Authentication module.
/// </summary>
public sealed class AuthenticationModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        // The legacy AuthUser entity was merged into the unified
        // GameGuild.Identity.Users.User principal (see UsersModelConfiguration); no
        // configuration in this assembly is excluded anymore (issue #1156).
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RefreshToken).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Authentication", StringComparison.Ordinal) == true
                    || type.Namespace?.StartsWith("GameGuild.Identity.Authentication", StringComparison.Ordinal) == true);
    }
}

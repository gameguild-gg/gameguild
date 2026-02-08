using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     EF Core model configuration for the Authentication module.
/// </summary>
public sealed class AuthenticationModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RefreshToken).Assembly,
            type => (type.Namespace?.StartsWith("GameGuild.Authentication", StringComparison.Ordinal) == true
                     || type.Namespace?.StartsWith("GameGuild.Identity.Authentication", StringComparison.Ordinal) == true)
                    && !type.Name.Contains("AuthUser", StringComparison.Ordinal));
    }
}

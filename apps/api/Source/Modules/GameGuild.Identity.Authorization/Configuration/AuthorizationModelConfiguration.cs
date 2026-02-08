using GameGuild.Identity.Authorization.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     EF Core model configuration for the Authorization module.
///     Delegates to the existing <see cref="AuthorizationModule.ConfigureAuthorizationModel"/> method.
/// </summary>
public sealed class AuthorizationModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        AuthorizationModule.ConfigureAuthorizationModel(modelBuilder);
    }
}

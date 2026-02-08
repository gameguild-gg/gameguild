using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     EF Core model configuration for the Tenants module.
/// </summary>
public sealed class TenantsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Tenant).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Identity.Tenants", StringComparison.Ordinal) == true);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Database;

/// <summary> Temporary configuration for Tenant entity to handle missing AdminEmail column </summary>
internal class TenantConfiguration : IEntityTypeConfiguration<GameGuild.Modules.Tenants.Tenant> {
    public void Configure(EntityTypeBuilder<GameGuild.Modules.Tenants.Tenant> builder) {
        // Allow AdminEmail and IsDefault properties to be included in model
        // builder.Ignore(t => t.AdminEmail);
        // builder.Ignore(t => t.IsDefault);
    }
}

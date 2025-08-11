using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Tenants;

internal class TenantPermissionConfiguration : IEntityTypeConfiguration<TenantPermission> {
  public void Configure(EntityTypeBuilder<TenantPermission> builder) {
    // No need to configure User and Tenant relationships here since they are already
    // configured in the WithPermissions base class with proper foreign key attributes
    
    // Configure any additional properties or constraints specific to TenantPermission if needed
  }
}

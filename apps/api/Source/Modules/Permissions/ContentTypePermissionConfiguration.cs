using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Permissions;

internal class ContentTypePermissionConfiguration : IEntityTypeConfiguration<ContentTypePermission> {
  public void Configure(EntityTypeBuilder<ContentTypePermission> builder) {
    // No need to configure User and Tenant relationships here since they are already
    // configured in the WithPermissions base class with proper foreign key attributes
    
    // Configure additional properties or constraints specific to ContentTypePermission if needed
    builder.Property(ctp => ctp.ContentType)
           .IsRequired()
           .HasMaxLength(100);
  }
}

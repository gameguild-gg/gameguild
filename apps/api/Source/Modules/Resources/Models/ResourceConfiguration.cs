namespace GameGuild.Modules.Resources;

/// <summary>
/// Entity configuration for Resource
/// </summary>
public class ResourceConfiguration : IEntityTypeConfiguration<Resource> {
  public void Configure(EntityTypeBuilder<Resource> builder) {
    // Configure the relationship between Resource and ResourceMetadata
    builder.HasOne(r => r.Metadata)
           .WithMany()
           .HasForeignKey("MetadataId")
           .IsRequired(false)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure other base properties
    builder.Property(r => r.Title).IsRequired().HasMaxLength(255);
    builder.Property(r => r.Description).HasMaxLength(2000);
    builder.Property(r => r.Visibility).IsRequired();

    // Ignore computed property - it's calculated from Tenant property
    builder.Ignore(r => r.IsGlobal);
  }
}

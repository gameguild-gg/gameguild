using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Users;

/// <summary>
///     Entity Framework configuration for UserMetadata
/// </summary>
public class UserMetadataConfiguration : IEntityTypeConfiguration<UserMetadata>
{
    public void Configure(EntityTypeBuilder<UserMetadata> builder)
    {
        // Configure relationships
        builder.HasOne(um => um.User)
            .WithOne(u => u.Metadata)
            .HasForeignKey<UserMetadata>(um => um.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(um => um.UserId).IsUnique();

        // CustomFields, Tags, and ExternalReferences properties are configured via [Column(TypeName = "jsonb")] attributes
        builder.Property(e => e.CustomFields).IsRequired();
        builder.Property(e => e.Tags).IsRequired();
        builder.Property(e => e.ExternalReferences).IsRequired();
    }
}

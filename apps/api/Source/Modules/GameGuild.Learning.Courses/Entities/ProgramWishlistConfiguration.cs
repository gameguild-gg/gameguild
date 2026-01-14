

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

 namespace GameGuild.Learning.Courses;

/// <summary> EntityBase Framework configuration for ProgramWishlist entity </summary>
public class ProgramWishlistConfiguration : IEntityTypeConfiguration<ProgramWishlist> {
  public void Configure(EntityTypeBuilder<ProgramWishlist> builder) {
    // Additional configuration if needed
  }
}

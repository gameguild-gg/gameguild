using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Entity Framework configuration for ProgramWishlist entity
/// </summary>
public class ProgramWishlistConfiguration : IEntityTypeConfiguration<ProgramWishlist> {
  public void Configure(EntityTypeBuilder<ProgramWishlist> builder) {
    // Additional configuration if needed
  }
}

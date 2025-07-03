using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Program.Models;

[Table("program_wishlists")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(AddedAt))]
public class ProgramWishlist : BaseEntity {
  public Guid UserId { get; set; }

  public Guid ProgramId { get; set; }

  /// <summary>
  /// When the program was added to wishlist
  /// </summary>
  public DateTime AddedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Optional notes about why the user saved this program
  /// </summary>
  [Column(TypeName = "text")]
  public string? Notes { get; set; }

  // Navigation properties
  [ForeignKey(nameof(UserId))]
  public virtual User.Models.User User { get; set; } = null!;

  [ForeignKey(nameof(ProgramId))]
  public virtual Program Program { get; set; } = null!;
}

/// <summary>
/// Entity Framework configuration for ProgramWishlist entity
/// </summary>
public class ProgramWishlistConfiguration : IEntityTypeConfiguration<ProgramWishlist> {
    public void Configure(EntityTypeBuilder<ProgramWishlist> builder) {
        // Additional configuration if needed
    }
}

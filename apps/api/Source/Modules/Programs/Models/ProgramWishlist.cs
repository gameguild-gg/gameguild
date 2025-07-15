using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs;

[Table("program_wishlists")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(AddedAt))]
public class ProgramWishlist : Entity {
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
  [ForeignKey(nameof(UserId))] public virtual User User { get; set; } = null!;

  [ForeignKey(nameof(ProgramId))] public virtual Program Program { get; set; } = null!;
}

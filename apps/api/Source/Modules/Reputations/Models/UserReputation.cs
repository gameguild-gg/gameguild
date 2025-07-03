using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Resources.Models;
using GameGuild.Modules.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Reputations.Models;

/// <summary>
/// Tracks a user's reputation score and tier
/// </summary>
[Table("UserReputations")]
[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(Score))]
[Index(nameof(CurrentLevelId))]
public class UserReputation : ResourceBase, IReputation {
  /// <summary>
  /// The user this reputation belongs to
  /// </summary>
  [Required]
  [ForeignKey(nameof(UserId))]
  public required User User { get; set; }

  [Required] public Guid UserId { get; set; }

  /// <summary>
  /// Current reputation score
  /// </summary>
  public int Score { get; set; }

  /// <summary>
  /// Current reputation tier (linked to configurable tier)
  /// </summary>
  [ForeignKey(nameof(CurrentLevelId))]
  public ReputationTier? CurrentLevel { get; set; }

  public Guid? CurrentLevelId { get; set; }

  /// <summary>
  /// When the reputation was last updated
  /// </summary>
  public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When the user's reputation tier was last recalculated
  /// </summary>
  public DateTime? LastLevelCalculation { get; set; }

  /// <summary>
  /// Number of positive reputation changes
  /// </summary>
  public int PositiveChanges { get; set; }

  /// <summary>
  /// Number of negative reputation changes
  /// </summary>
  public int NegativeChanges { get; set; }

  /// <summary>
  /// History of reputation changes for this user
  /// </summary>
  public ICollection<UserReputationHistory> History { get; set; } = new List<UserReputationHistory>();
}

public class UserReputationConfiguration : IEntityTypeConfiguration<UserReputation> {
  public void Configure(EntityTypeBuilder<UserReputation> builder) {
    // Configure a relationship with User (can't be done with annotations)
    builder.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure a relationship with CurrentLevel (can't be done with annotations)
    builder.HasOne(ur => ur.CurrentLevel)
           .WithMany()
           .HasForeignKey(ur => ur.CurrentLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Filtered unique constraint (can't be done with annotations)
    builder.HasIndex(ur => ur.UserId).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
  }
}

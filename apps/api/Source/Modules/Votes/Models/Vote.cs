using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.Voting.Models;

/// <summary>
/// Represents a vote (upvote/downvote) on a voteable entity.
/// </summary>
public class Vote : BaseEntity {
  private User.Models.User _user = null!;

  private VoteType _type;

  private int _weight = 1;

  private Guid _entityId;

  private string _entityType = string.Empty;

  /// <summary>
  /// Navigation property to the user who cast this vote
  /// Entity Framework will automatically create the UserId foreign key
  /// </summary>
  [Required]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Type of the vote (Upvote or Downvote)
  /// </summary>
  [Required]
  public VoteType Type {
    get => _type;
    set => _type = value;
  }

  /// <summary>
  /// Weight of the vote (allows for weighted voting systems)
  /// Default is 1 (standard weight)
  /// </summary>
  [Required]
  public int Weight {
    get => _weight;
    set => _weight = value;
  }

  /// <summary>
  /// Calculated value of the vote based on type and weight
  /// Returns positive weight for upvotes and negative weight for downvotes
  /// </summary>
  public int Value {
    get => Type == VoteType.Upvote ? Weight : -Weight;
  }

  /// <summary>
  /// Foreign key for the entity being voted on
  /// </summary>
  public Guid EntityId {
    get => _entityId;
    set => _entityId = value;
  }

  /// <summary>
  /// The type of entity being voted on (for polymorphic relationships)
  /// </summary>
  [MaxLength(255)]
  public string EntityType {
    get => _entityType;
    set => _entityType = value;
  }
}

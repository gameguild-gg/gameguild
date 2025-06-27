using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Rating.Models;

/// <summary>
/// Represents a rating (e.g., 1-5 stars) on a rateable entity.
/// </summary>
public class Rating : BaseEntity {
  private int _value;

  private User.Models.User _user = null!;

  private Guid _entityId;

  private string _entityType = string.Empty;

  private string? _comment;

  /// <summary>
  /// The rating value (e.g., 1-5 stars)
  /// </summary>
  [Range(1, 5)]
  public int Value {
    get => _value;
    set => _value = value;
  }

  /// <summary>
  /// Navigation property to the user who provided this rating
  /// Entity Framework will automatically create the UserId foreign key
  /// </summary>
  [Required]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Foreign key for the entity being rated
  /// </summary>
  public Guid EntityId {
    get => _entityId;
    set => _entityId = value;
  }

  /// <summary>
  /// The type of entity being rated (for polymorphic relationships)
  /// </summary>
  [MaxLength(255)]
  public string EntityType {
    get => _entityType;
    set => _entityType = value;
  }

  /// <summary>
  /// Optional comment/review text associated with the rating
  /// </summary>
  [MaxLength(1000)]
  public string? Comment {
    get => _comment;
    set => _comment = value;
  }
}

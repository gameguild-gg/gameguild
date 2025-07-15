using System.ComponentModel.DataAnnotations;


namespace GameGuild.Common.Models;

/// <summary>
/// Base entity with common properties
/// </summary>
public abstract class BaseEntity {
  /// <summary>
  /// Entity identifier
  /// </summary>
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();

  /// <summary>
  /// Creation timestamp
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Last update timestamp
  /// </summary>
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Entity version for optimistic concurrency
  /// </summary>
  [Timestamp]
  public byte[] RowVersion { get; set; } = Array.Empty<byte>();

  /// <summary>
  /// Soft delete flag
  /// </summary>
  public bool IsDeleted { get; set; } = false;

  /// <summary>
  /// Soft delete timestamp
  /// </summary>
  public DateTime? DeletedAt { get; set; }
}

namespace GameGuild.Common.Authorization;

/// <summary>
/// Represents user context for 3-layer DAC authorization
/// </summary>
public class UserContext {
  /// <summary>
  /// Gets or sets the user identifier
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// Gets or sets the tenant identifier
  /// </summary>
  public Guid TenantId { get; set; }
}

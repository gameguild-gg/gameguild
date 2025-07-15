namespace GameGuild.Common;

/// <summary>
/// Provides access to the current user's context information
/// </summary>
public interface IUserContext {
  /// <summary>
  /// Gets the current user's unique identifier, null if not authenticated
  /// </summary>
  Guid? UserId { get; }

  /// <summary>
  /// Gets the current user's email address
  /// </summary>
  string? Email { get; }

  /// <summary>
  /// Gets the current user's name
  /// </summary>
  string? Name { get; }

  /// <summary>
  /// Gets all claims associated with the current user
  /// </summary>
  IDictionary<string, object> Claims { get; }

  /// <summary>
  /// Gets whether the user is authenticated
  /// </summary>
  bool IsAuthenticated { get; }
}

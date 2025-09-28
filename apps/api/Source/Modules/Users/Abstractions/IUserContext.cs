namespace GameGuild.Core.Domain.Identity;

/// <summary> Interface for accessing current user context Domain interface for user identity concerns </summary>
public interface IUserContext {
  /// <summary> Gets the current user ID </summary>
  Guid? UserId { get; }

  /// <summary> Gets the current user email </summary>
  string? Email { get; }

  /// <summary> Gets the current user name </summary>
  string? Name { get; }

  /// <summary> Gets all user claims </summary>
  IDictionary<string, object> Claims { get; }

  /// <summary> Checks if user is authenticated </summary>
  bool IsAuthenticated { get; }

  /// <summary> Gets user roles </summary>
  IEnumerable<string> Roles { get; }

  /// <summary> Checks if user has specific role </summary>
  bool IsInRole(string role);
}
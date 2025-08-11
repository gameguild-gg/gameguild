using System.Security.Claims;


namespace GameGuild.Common;

/// <summary>
/// Interface for requests that require authorization
/// </summary>
public interface IAuthorizedRequest {
  /// <summary>
  /// Required roles for this request
  /// </summary>
  string[]? RequiredRoles { get; }

  /// <summary>
  /// Required permissions for this request
  /// </summary>
  string[]? RequiredPermissions { get; }

  /// <summary>
  /// Custom authorization logic
  /// </summary>
  Task<bool> IsAuthorizedAsync(ClaimsPrincipal? user, CancellationToken cancellationToken) {
    return Task.FromResult(true); // Default implementation
  }
}

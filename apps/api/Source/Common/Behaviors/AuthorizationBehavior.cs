using System.Security.Claims;
using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Authorization behavior for securing commands and queries
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse>(IHttpContextAccessor httpContextAccessor, ILogger<AuthorizationBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
    var requestName = typeof(TRequest).Name;
    var user = httpContextAccessor.HttpContext?.User;

    // Check if request requires authorization
    if (request is not IAuthorizedRequest authorizedRequest) return await next();

    logger.LogDebug("Checking authorization for {RequestName}", requestName);

    // Check if user is authenticated
    if (user?.Identity?.IsAuthenticated != true) {
      logger.LogWarning("Unauthorized access attempt for {RequestName}", requestName);

      return CreateUnauthorizedResponse<TResponse>();
    }

    // Check required roles
    if (authorizedRequest.RequiredRoles?.Any() == true) {
      var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
      var hasRequiredRole = authorizedRequest.RequiredRoles.Any(role => userRoles.Contains(role));

      if (!hasRequiredRole) {
        logger.LogWarning(
          "Insufficient permissions for {RequestName}. User roles: {UserRoles}, Required: {RequiredRoles}",
          requestName,
          string.Join(", ", userRoles),
          string.Join(", ", authorizedRequest.RequiredRoles)
        );

        return CreateForbiddenResponse<TResponse>();
      }
    }

    // Check required permissions
    if (authorizedRequest.RequiredPermissions?.Any() == true) {
      var userPermissions = user.FindAll("permission").Select(c => c.Value).ToList();
      var hasRequiredPermission = authorizedRequest.RequiredPermissions.Any(permission =>
                                                                              userPermissions.Contains(permission)
      );

      if (!hasRequiredPermission) {
        logger.LogWarning(
          "Insufficient permissions for {RequestName}. Required: {RequiredPermissions}",
          requestName,
          string.Join(", ", authorizedRequest.RequiredPermissions)
        );

        return CreateForbiddenResponse<TResponse>();
      }
    }

    // Custom authorization logic
    if (!await authorizedRequest.IsAuthorizedAsync(user, cancellationToken)) {
      logger.LogWarning("Custom authorization failed for {RequestName}", requestName);

      return CreateForbiddenResponse<TResponse>();
    }

    logger.LogDebug("Authorization passed for {RequestName}", requestName);

    return await next();
  }

  private static TResponse CreateUnauthorizedResponse<T>() {
    var error = ErrorMessage.Failure("Authorization.Unauthorized", "Authentication is required");

    return CreateErrorResponse<T>(error);
  }

  private static TResponse CreateForbiddenResponse<T>() {
    var error = ErrorMessage.Failure("Authorization.Forbidden", "Insufficient permissions");

    return CreateErrorResponse<T>(error);
  }

  private static TResponse CreateErrorResponse<T>(ErrorMessage error) {
    // Handle Result pattern
    if (typeof(T) == typeof(Result)) return (TResponse)(object)Result.Failure(error);

    if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Result<>)) {
      var resultType = typeof(T).GetGenericArguments()[0];
      var failureMethod = typeof(Result).GetMethod("Failure", [typeof(ErrorMessage)])!
                                        .MakeGenericMethod(resultType);

      return (TResponse)failureMethod.Invoke(null, [error])!;
    }

    // Fallback - throw exception for non-Result responses
    throw new UnauthorizedAccessException(error.Description);
  }
}

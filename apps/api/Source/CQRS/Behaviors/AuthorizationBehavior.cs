using System.Security.Claims;

namespace GameGuild.CQRS;

/// <summary>
/// Interface for requests that require authorization
/// </summary>
public interface IAuthorizedRequest
{
    /// <summary>
    /// Required roles for this request
    /// </summary>
    string[]? RequiredRoles { get; }

    /// <summary>
    /// Required permissions for this request
    /// </summary>
    string[]? RequiredPermissions { get; }

    /// <summary>
    /// Custom authorization logic for this request
    /// </summary>
    /// <param name="user">The current user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal? user, CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior for authorizing requests using Result<T> pattern
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthorizationBehavior class
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor</param>
    /// <param name="logger">Logger</param>
    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor, ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request pipeline with authorization
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next handler delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response or authorization failure result</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (request is not IAuthorizedRequest authorizedRequest)
        {
            // Request doesn't require authorization, proceed
            return await next().ConfigureAwait(false);
        }

        var user = _httpContextAccessor.HttpContext?.User;
        var isAuthorized = false;

        try
        {
            // Check role-based authorization
            if (authorizedRequest.RequiredRoles?.Length > 0)
            {
                isAuthorized = authorizedRequest.RequiredRoles.Any(role => user?.IsInRole(role) == true);
                if (!isAuthorized)
                {
                    _logger.LogWarning("User {UserId} failed role authorization for {RequestType}. Required roles: {RequiredRoles}",
                        user?.Identity?.Name, typeof(TRequest).Name, string.Join(", ", authorizedRequest.RequiredRoles));
                }
            }

            // Check permission-based authorization
            if (!isAuthorized && authorizedRequest.RequiredPermissions?.Length > 0)
            {
                isAuthorized = authorizedRequest.RequiredPermissions.Any(permission =>
                    user?.HasClaim("permission", permission) == true);
                if (!isAuthorized)
                {
                    _logger.LogWarning("User {UserId} failed permission authorization for {RequestType}. Required permissions: {RequiredPermissions}",
                        user?.Identity?.Name, typeof(TRequest).Name, string.Join(", ", authorizedRequest.RequiredPermissions));
                }
            }

            // Custom authorization logic
            if (!isAuthorized)
            {
                isAuthorized = await authorizedRequest.IsAuthorizedAsync(user, cancellationToken);
                if (!isAuthorized)
                {
                    _logger.LogWarning("User {UserId} failed custom authorization for {RequestType}",
                        user?.Identity?.Name, typeof(TRequest).Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization for request {RequestType}", typeof(TRequest).Name);
            isAuthorized = false;
        }

        if (!isAuthorized)
        {
            var error = user?.Identity?.IsAuthenticated == true
                ? Error.Forbidden("Authorization.Forbidden", "You do not have permission to perform this action")
                : Error.Unauthorized("Authorization.Unauthorized", "You must be authenticated to perform this action");

            // If TResponse is a Result or Result<T>, return an authorization failure result
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                    .MakeGenericMethod(valueType);

                return (TResponse)failureMethod.Invoke(null, [error])!;
            }
            else if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }
            else
            {
                // Fallback to throwing exception for non-Result responses (backward compatibility)
                throw user?.Identity?.IsAuthenticated == true
                    ? new UnauthorizedAccessException("You do not have permission to perform this action")
                    : new UnauthorizedAccessException("You must be authenticated to perform this action");
            }
        }

        return await next().ConfigureAwait(false);
    }
}

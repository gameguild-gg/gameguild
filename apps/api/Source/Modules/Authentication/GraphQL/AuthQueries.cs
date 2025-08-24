using GameGuild.Common;
using GameGuild.Modules.Users;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// GraphQL queries for Authentication module using CQRS pattern.
/// Provides clean separation between GraphQL layer and business logic.
/// </summary>
[ExtendObjectType<Query>]
public class AuthQueries {
  /// <summary>
  /// Gets user by email using CQRS pattern.
  /// </summary>
  /// <param name="email">The user's email address</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>The user if found, null otherwise</returns>
  [GraphQLDescription("Retrieves a user by their email address")]
  public async Task<User?> GetUserByEmail(
    [GraphQLDescription("The email address to search for")]
    string email,
    [Service] IMediator mediator
  ) {
    var query = new GetUserByEmailQuery { Email = email, IncludeDeleted = false };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets the current authenticated user's profile.
  /// </summary>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <param name="contextAccessor">HTTP context accessor for user claims</param>
  /// <returns>The current user's profile</returns>
  [GraphQLDescription("Retrieves the current authenticated user's profile")]
  [Authorize] // Requires authentication
  public async Task<User?> GetCurrentUser(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor contextAccessor
  ) {
    var userId = contextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) { return null; }

    var query = new GetUserByIdQuery { UserId = userGuid, IncludeDeleted = false };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets the current authenticated user's complete profile information.
  /// </summary>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <param name="contextAccessor">HTTP context accessor for user claims</param>
  /// <returns>The current user's complete profile</returns>
  [GraphQLDescription("Retrieves the current authenticated user's complete profile")]
  [Authorize] // Requires authentication
  [Error<UnauthorizedAccessException>]
  public async Task<UserProfileDto> GetCurrentUserProfile(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor contextAccessor
  ) {
    var userId = contextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? contextAccessor.HttpContext?.User?.FindFirst("nameid")?.Value;

    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) { throw new UnauthorizedAccessException("Invalid user claims"); }

    var query = new GetUserProfileQuery { UserId = userGuid };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Validates if an email is available for registration.
  /// </summary>
  /// <param name="email">The email to validate</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>True if email is available, false otherwise</returns>
  [GraphQLDescription("Checks if an email address is available for registration")]
  public async Task<bool> IsEmailAvailable(
    [GraphQLDescription("The email address to check")]
    string email,
    [Service] IMediator mediator
  ) {
    var query = new GetUserByEmailQuery { Email = email };
    var user = await mediator.Send(query);

    return user == null; // Email is available if no user found
  }
}

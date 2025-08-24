using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// GraphQL queries for UserProfile module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class UserProfileQueries {
  /// <summary>
  /// Get all user profiles with optional filtering using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<UserProfile>> GetUserProfiles([Service] IMediator mediator, bool includeDeleted = false, int skip = 0, int take = 50, string? searchTerm = null, Guid? tenantId = null) {
    var query = new GetAllUserProfilesQuery {
      IncludeDeleted = includeDeleted,
      Skip = skip,
      Take = take,
      SearchTerm = searchTerm,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }

  /// <summary>
  /// Get a user profile by ID using CQRS pattern
  /// </summary>
  public async Task<UserProfile?> GetUserProfileById([Service] IMediator mediator, Guid id, bool includeDeleted = false) {
    var query = new GetUserProfileByIdQuery { UserProfileId = id, IncludeDeleted = includeDeleted, };

    var result = await mediator.Send(query);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }

  /// <summary>
  /// Get a user profile by user ID using CQRS pattern
  /// </summary>
  public async Task<UserProfile?> GetUserProfileByUserId([Service] IMediator mediator, Guid userId, bool includeDeleted = false) {
    var query = new GetUserProfileByUserIdQuery { UserId = userId, IncludeDeleted = includeDeleted, };

    var result = await mediator.Send(query);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }
}

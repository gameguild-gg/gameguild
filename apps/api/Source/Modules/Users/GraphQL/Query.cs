using GameGuild.Common;
using GameGuild.Common.Models;
using GameGuild.Modules.Users.Inputs;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// GraphQL queries for User module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class UserQueries {
  /// <summary>
  /// Gets all users with optional filtering using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<User>> Users(
    [Service] IMediator mediator,
    bool includeDeleted = false,
    bool? isActive = null,
    int skip = 0,
    int take = 50
  ) {
    var query = new GetAllUsersQuery { IncludeDeleted = includeDeleted, IsActive = isActive, Skip = skip, Take = take };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets a user by their unique identifier using CQRS pattern
  /// </summary>
  public async Task<User?> GetUserById(
    [Service] IMediator mediator,
    Guid id,
    bool includeDeleted = false
  ) {
    var query = new GetUserByIdQuery { UserId = id, IncludeDeleted = includeDeleted };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets a user by their unique identifier (alias for GetUserById)
  /// </summary>
  public async Task<User?> User(
    [Service] IMediator mediator,
    Guid id,
    bool includeDeleted = false
  ) {
    var query = new GetUserByIdQuery { UserId = id, IncludeDeleted = includeDeleted };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets a user by their email address using CQRS pattern
  /// </summary>
  public async Task<User?> GetUserByEmail(
    [Service] IMediator mediator,
    string email,
    bool includeDeleted = false
  ) {
    var query = new GetUserByEmailQuery { Email = email, IncludeDeleted = includeDeleted };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Search users with advanced filtering and pagination using CQRS pattern
  /// </summary>
  public async Task<PagedResult<User>> SearchUsers(
    [Service] IMediator mediator,
    SearchUsersInput input
  ) {
    var query = new SearchUsersQuery {
      SearchTerm = input.SearchTerm,
      IsActive = input.IsActive,
      MinBalance = input.MinBalance,
      MaxBalance = input.MaxBalance,
      CreatedAfter = input.CreatedAfter,
      CreatedBefore = input.CreatedBefore,
      UpdatedAfter = input.UpdatedAfter,
      UpdatedBefore = input.UpdatedBefore,
      IncludeDeleted = input.IncludeDeleted,
      Skip = input.Skip,
      Take = input.Take,
      SortBy = input.SortBy,
      SortDirection = input.SortDirection,
    };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets user statistics using CQRS pattern
  /// </summary>
  public async Task<UserStatistics> GetUserStatistics(
    [Service] IMediator mediator,
    UserStatisticsInput input
  ) {
    var query = new GetUserStatisticsQuery { FromDate = input.FromDate, ToDate = input.ToDate, IncludeDeleted = input.IncludeDeleted };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets users with low balance using CQRS pattern
  /// </summary>
  public async Task<PagedResult<User>> GetUsersWithLowBalance(
    [Service] IMediator mediator,
    decimal threshold = 10m,
    bool includeDeleted = false,
    int skip = 0,
    int take = 50
  ) {
    var query = new GetUsersWithLowBalanceQuery { ThresholdBalance = threshold, IncludeDeleted = includeDeleted, Skip = skip, Take = take };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets active users only using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<User>> GetActiveUsers([Service] IMediator mediator) {
    var query = new GetAllUsersQuery { IsActive = true };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets soft-deleted users using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<User>> GetDeletedUsers([Service] IMediator mediator) {
    var query = new GetAllUsersQuery { IncludeDeleted = true };
    var users = await mediator.Send(query);

    return users.Where(u => u.DeletedAt != null);
  }

  /// <summary>
  /// Legacy method - Gets all users using traditional service pattern (deprecated)
  /// </summary>
  [Obsolete("Use GetUsers with IMediator instead")]
  public async Task<IEnumerable<User>> GetUsersLegacy([Service] IUserService userService) { return await userService.GetAllUsersAsync(); }
}

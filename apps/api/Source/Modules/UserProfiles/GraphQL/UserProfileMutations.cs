using GameGuild.Common;
using GameGuild.Modules.UserProfiles.Inputs;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// GraphQL mutations for UserProfile module using CQRS pattern
/// </summary>
[ExtendObjectType<Mutation>]
public class UserProfileMutations {
  /// <summary>
  /// Create a new user profile using CQRS pattern
  /// </summary>
  public async Task<UserProfile> CreateUserProfile([Service] IMediator mediator, CreateUserProfileInput input) {
    var command = new CreateUserProfileCommand {
      GivenName = input.GivenName ?? string.Empty,
      FamilyName = input.FamilyName ?? string.Empty,
      DisplayName = input.DisplayName ?? string.Empty,
      Title = input.Title,
      Description = input.Description,
      TenantId = input.TenantId,
    };

    var result = await mediator.Send(command);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }

  /// <summary>
  /// Update an existing user profile using CQRS pattern
  /// </summary>
  public async Task<UserProfile> UpdateUserProfile([Service] IMediator mediator, UpdateUserProfileInput input) {
    var command = new UpdateUserProfileCommand {
      UserProfileId = input.Id,
      GivenName = input.GivenName,
      FamilyName = input.FamilyName,
      DisplayName = input.DisplayName,
      Title = input.Title,
      Description = input.Description,
    };

    var result = await mediator.Send(command);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }

  /// <summary>
  /// Delete a user profile using CQRS pattern
  /// </summary>
  public async Task<bool> DeleteUserProfile([Service] IMediator mediator, Guid id, bool softDelete = true) {
    var command = new DeleteUserProfileCommand { UserProfileId = id, SoftDelete = softDelete, };

    var result = await mediator.Send(command);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }

  /// <summary>
  /// Restore a soft-deleted user profile using CQRS pattern
  /// </summary>
  public async Task<bool> RestoreUserProfile([Service] IMediator mediator, Guid id) {
    var command = new RestoreUserProfileCommand { UserProfileId = id, };

    var result = await mediator.Send(command);

    if (!result.IsSuccess) throw new GraphQLException(result.Error.Description);

    return result.Value;
  }
}

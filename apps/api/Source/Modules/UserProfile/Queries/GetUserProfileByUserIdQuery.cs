using MediatR;

namespace GameGuild.Modules.UserProfile.Queries;

/// <summary>
/// Query to get user profile by user ID
/// </summary>
public class GetUserProfileByUserIdQuery : IRequest<Models.UserProfile?> {
  private Guid _userId;

  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }
}

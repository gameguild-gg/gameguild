namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to get multiple users by their email addresses
/// </summary>
public sealed class GetUsersByEmailsQuery : GameGuild.CQRS.IRequest<IEnumerable<UserDto>> {
  /// <summary>
  ///     Collection of email addresses to retrieve
  /// </summary>
  public required IEnumerable<string> Emails { get; init; }
}

/// <summary>
///     Handler for GetUsersByEmailsQuery
/// </summary>
public sealed class GetUsersByEmailsQueryHandler(IUserRepository userRepository) : GameGuild.CQRS.IRequestHandler<GetUsersByEmailsQuery, IEnumerable<UserDto>> {
  public async Task<IEnumerable<UserDto>> Handle(GetUsersByEmailsQuery request, CancellationToken cancellationToken) {
    var users = await userRepository.GetByEmailsAsync(request.Emails, cancellationToken);

    return users.Select(user => new UserDto {
      Id = user.Id,
      Email = user.Email,
      Username = user.Username,
      GivenName = user.GivenName,
      FamilyName = user.FamilyName,
      CreatedAt = user.CreatedAt,
      UpdatedAt = user.UpdatedAt
    });
  }
}

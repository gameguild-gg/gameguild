using MediatR;


namespace GameGuild.Modules.Credentials.Queries;

/// <summary>
/// Query to get credentials by user ID using CQRS pattern
/// </summary>
public class GetCredentialsByUserIdQuery : IRequest<IEnumerable<Credential>> {
  /// <summary>
  /// The user ID to search for
  /// </summary>
  public Guid UserId { get; set; }

  public GetCredentialsByUserIdQuery(Guid userId) { UserId = userId; }
}

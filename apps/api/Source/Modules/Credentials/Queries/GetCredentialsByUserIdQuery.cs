using GameGuild.CQRS;


namespace GameGuild.Modules.Credentials.Queries;

/// <summary> Query to get credentials by user ID using CQRS pattern </summary>
public class GetCredentialsByUserIdQuery : IRequest<IEnumerable<Credential>> {
  public GetCredentialsByUserIdQuery(Guid userId) { UserId = userId; }

  /// <summary> The user ID to search for </summary>
  public Guid UserId { get; set; }
}

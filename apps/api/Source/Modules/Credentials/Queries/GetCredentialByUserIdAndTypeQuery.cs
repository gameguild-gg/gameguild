using GameGuild.CQRS;


namespace GameGuild.Modules.Credentials.Queries;

/// <summary> Query to get a credential by user ID and type using CQRS pattern </summary>
public class GetCredentialByUserIdAndTypeQuery : IRequest<Credential?> {
  public GetCredentialByUserIdAndTypeQuery(Guid userId, string type) {
    UserId = userId;
    Type = type;
  }

  /// <summary> User ID to search for </summary>
  public Guid UserId { get; set; }

  /// <summary> Type of credential to search for </summary>
  public string Type { get; set; } = string.Empty;
}

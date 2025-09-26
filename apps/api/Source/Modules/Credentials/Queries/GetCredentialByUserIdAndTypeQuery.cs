using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Query to get a credential by user ID and type using CQRS pattern </summary>
public class GetCredentialByUserIdAndTypeQuery(Guid userId, string type) : IRequest<Credential?>
{
    /// <summary> User ID to search for </summary>
    public Guid UserId { get; set; } = userId;

    /// <summary> Type of credential to search for </summary>
    public string Type { get; set; } = type;
}

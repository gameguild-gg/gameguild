using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Query to get credentials by user ID using CQRS pattern </summary>
public class GetCredentialsByUserIdQuery(Guid userId) : IRequest<IEnumerable<Credential>>
{
    /// <summary> The user ID to search for </summary>
    public Guid UserId { get; set; } = userId;
}

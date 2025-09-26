using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Query to get credential by ID using CQRS pattern
/// </summary>
public class GetCredentialByIdQuery(Guid id) : IRequest<Credential?>
{
    /// <summary>
    /// The credential ID to search for
    /// </summary>
    public Guid Id { get; set; } = id;
}

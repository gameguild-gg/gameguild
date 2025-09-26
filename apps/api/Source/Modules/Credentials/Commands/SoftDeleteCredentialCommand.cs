using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Command to soft delete a credential using CQRS pattern
/// </summary>
public class SoftDeleteCredentialCommand(Guid id) : IRequest<bool>
{
    /// <summary>
    /// Credential ID to soft delete
    /// </summary>
    public Guid Id { get; set; } = id;
}

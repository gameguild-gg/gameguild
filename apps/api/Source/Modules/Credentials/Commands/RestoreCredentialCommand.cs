using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Command to restore a soft-deleted credential using CQRS pattern
/// </summary>
public class RestoreCredentialCommand : IRequest<bool>
{
    /// <summary>
    /// Credential ID to restore
    /// </summary>
    public Guid Id { get; set; }

    public RestoreCredentialCommand(Guid id)
    {
        Id = id;
    }
}

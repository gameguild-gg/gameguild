using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Command to deactivate a credential using CQRS pattern
/// </summary>
public class DeactivateCredentialCommand : IRequest<bool>
{
    /// <summary>
    /// Credential ID to deactivate
    /// </summary>
    public Guid Id { get; set; }

    public DeactivateCredentialCommand(Guid id)
    {
        Id = id;
    }
}

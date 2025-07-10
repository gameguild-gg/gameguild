using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Command to activate a credential using CQRS pattern
/// </summary>
public class ActivateCredentialCommand : IRequest<bool>
{
    /// <summary>
    /// Credential ID to activate
    /// </summary>
    public Guid Id { get; set; }

    public ActivateCredentialCommand(Guid id)
    {
        Id = id;
    }
}

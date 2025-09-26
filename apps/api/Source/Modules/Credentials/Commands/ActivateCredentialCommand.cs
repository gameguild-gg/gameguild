using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Command to activate a credential using CQRS pattern
/// </summary>
public class ActivateCredentialCommand(Guid id) : IRequest<bool>
{
    /// <summary>
    /// Credential ID to activate
    /// </summary>
    public Guid Id { get; set; } = id;
}

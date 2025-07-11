using MediatR;


namespace GameGuild.Modules.Credentials.Commands;

/// <summary>
/// Command to mark a credential as used using CQRS pattern
/// </summary>
public class MarkCredentialAsUsedCommand : IRequest<bool>
{
    /// <summary>
    /// Credential ID to mark as used
    /// </summary>
    public Guid Id { get; set; }

    public MarkCredentialAsUsedCommand(Guid id)
    {
        Id = id;
    }
}

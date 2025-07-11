using MediatR;


namespace GameGuild.Modules.Credentials.Commands;

/// <summary>
/// Command to hard delete a credential using CQRS pattern
/// </summary>
public class HardDeleteCredentialCommand : IRequest<bool>
{
    /// <summary>
    /// Credential ID to permanently delete
    /// </summary>
    public Guid Id { get; set; }

    public HardDeleteCredentialCommand(Guid id)
    {
        Id = id;
    }
}

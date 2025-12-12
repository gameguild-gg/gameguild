using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record BulkRevokeAccessCommand : ICommand<BulkAccessRevocationResult>
{
    public List<AccessRevocationRequest> Revocations { get; init; } = new List<AccessRevocationRequest>();

    public Guid RevokedBy { get; init; }
}

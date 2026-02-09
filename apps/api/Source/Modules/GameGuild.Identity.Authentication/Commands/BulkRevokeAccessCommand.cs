using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record BulkRevokeAccessCommand : ICommand<BulkAccessRevocationResult>
{
    public List<AccessRevocationRequest> Revocations { get; init; } = new List<AccessRevocationRequest>();

    public Guid RevokedBy { get; init; }
}

using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetComplianceStatusQuery : IQuery<ComplianceStatusDto>
{
    public Guid TenantId { get; init; }
}

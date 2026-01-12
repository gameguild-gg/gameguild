using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetComplianceStatusQuery : IQuery<ComplianceStatusDto>
{
    public Guid TenantId { get; init; }
}

using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetComplianceStatusQuery : IQuery<ComplianceStatusDto>
{
    public Guid TenantId { get; init; }
}

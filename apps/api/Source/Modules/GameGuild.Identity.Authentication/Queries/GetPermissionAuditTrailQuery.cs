using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetPermissionAuditTrailQuery : IQuery<PermissionAuditTrailDto>
{
    public Guid? UserId { get; init; }

    public Guid? TenantId { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
